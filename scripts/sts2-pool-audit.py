"""#54 exact-build registration/reward/shop/filter/save integration checks."""
import hashlib,json,sys,time,shutil
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2];records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/('pool-audit-'+tag+'.json');assert not output.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r)
    output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('Playable combat')
beta='beta' in tag
send('trait-speed',value=8)
registry=send('pool-audit-registry');assert registry['historical']==360 and registry['expanded']==beta,registry
membership=json.loads((root/'sheets/card_pools.json').read_text())+(json.loads((root/'sheets/branch_cards.json').read_text()) if beta else [])
ancients=json.loads((root/'sheets/ancient_cards.json').read_text())
for cls,pool in zip(['knight','mage','ranger'],registry['detail']):
    keys={r['cardId'] for r in membership+ancients if r['classId']==cls}
    assert {r['key'] for r in pool['cards']}==keys,pool
    assert len(keys)==len(pool['cards']) and sum(c['rarity']=='Basic' for c in pool['cards'])==3
    assert tuple(sum(c['rarity']==r for c in pool['cards']) for r in ['Common','Uncommon','Rare'])==((20,38,27) if beta else (20,36,26))
if 'import' not in tag:send('pool-audit-library',menu=True)
for cls in ['knight','mage','ranger','ironclad']:
    send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-POOLS-54','manorTier':0});ready()
    if 'import' in tag and cls!='ironclad':
        source=root/'private/sts2-evidence'/f'pool54-{("regular" if beta else "beta")}-{cls}-cards.json'
        shutil.copy2(source,profile/'pool-cards.json')
        restored=send('pool-audit-history',**{'import':True});assert len(restored['checks'])==720
    if 'import' not in tag:
        send('pool-audit-library')
        sparse=send('pool-audit-sparse');assert len(sparse)==20
        if cls!='ironclad':
            saved=send('pool-audit-history');assert len(saved['checks'])==720
            shutil.copy2(profile/'pool-cards.json',root/'private/sts2-evidence'/f'pool54-{"beta" if beta else "regular"}-{cls}-cards.json')
    result=send('pool-audit-owner');assert len(result)==1
    if cls!='ironclad':assert set(result[0]['allowed'])=={r['cardId'] for r in membership if r['classId']==cls and not r['cardId'].endswith(('_strike','_guard','_talent'))},result
    send('save');send('resume');ready();send('pool-audit-owner')
    send('pool-audit-registry')
    print(tag,cls,'reward/shop/transform/filter/history checks pass',flush=True)
send('abandon')
