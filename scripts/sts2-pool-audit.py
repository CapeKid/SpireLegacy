"""#54 exact-build registration/reward/shop/filter/save integration checks."""
import hashlib,json,sys,time,shutil
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2];records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/('pool-audit-'+tag+'.json');assert not output.exists()
def send(command,ok=True,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r)
    output.write_text(json.dumps(records,indent=2));assert r['response']['ok']==ok,r
    return r['response'].get('result',r['response'].get('error'))
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
for cls in ['knight','mage','ranger']:
    pool=next(p for p in registry['detail'] if cls.upper()+'_CARD_POOL' in p['pool'])
    keys={r['cardId'] for r in membership+ancients if r['classId']==cls}
    assert {r['key'] for r in pool['cards']}==keys,pool
    assert len(keys)==len(pool['cards']) and sum(c['rarity']=='Basic' for c in pool['cards'])==3
    assert tuple(sum(c['rarity']==r for c in pool['cards']) for r in ['Common','Uncommon','Rare'])==((20,38,27) if beta else (20,36,26))
if 'library-control' in tag:
    before=send('pool-audit-library',menu=True)
    send('start',**{'class':'ironclad','quick':True,'traits':[],'seed':'SPIRELEGACY-POOLS-54','manorTier':0});send('enter',room='Monster');ready()
    after=send('pool-audit-library')
    assert not before['staleMaterials'] and not after['filteredLegacy'],(before,after)
    if 'fixed' in tag:assert after['rendered']>0 and not after['staleMaterials'],(before,after)
    else:assert after['staleMaterials'],(before,after)
    send('abandon');print(tag,'native unseen-material control checked',flush=True);sys.exit(0)
for cls in ['knight','mage','ranger','ironclad']:
    send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-POOLS-54','manorTier':0})
    if cls=='ironclad':send('enter',room='Monster')
    ready()
    if 'crossrun' in tag:
        send('abandon')
        source=root/'private/sts2-evidence'/f'pool54-{"regular" if beta else "beta"}-{cls}-run.json'
        shutil.copy2(source,profile/'pool-run.json');loaded=send('pool-audit-import-run',ok=beta)
        if beta:
            if cls=='ironclad':send('enter',room='Monster')
            ready();assert loaded['cls']==cls and all(any(c['key']==r['cardId'] and c['upgraded'] for c in loaded['deck']) for r in json.loads((root/'sheets/branch_cards.json').read_text()) if r['classId']==cls),loaded
        else:
            assert 'System.UInt32' in loaded and '.rng.seed' in loaded,loaded
            send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-POOLS-54','manorTier':0})
            if cls=='ironclad':send('enter',room='Monster')
            ready()
    if 'import' in tag and cls!='ironclad':
        source=root/'private/sts2-evidence'/f'pool54-{("regular" if beta else "beta")}-{cls}-cards.json'
        shutil.copy2(source,profile/'pool-cards.json')
        restored=send('pool-audit-history',**{'import':True});assert len(restored['checks'])==720
    if 'import' not in tag:
        library=send('pool-audit-library');assert library['filtered']>0,library
        sparse=send('pool-audit-sparse');assert len(sparse)==60
        if cls!='ironclad':
            saved=send('pool-audit-history');assert len(saved['checks'])==720
            shutil.copy2(profile/'pool-cards.json',root/'private/sts2-evidence'/f'pool54-{"beta" if beta else "regular"}-{cls}-cards.json')
    result=send('pool-audit-owner');assert len(result)==1
    modifiers=send('pool-audit-modifiers')
    if cls!='ironclad':assert modifiers[0]['ownerPoolOnly'],modifiers
    if cls!='ironclad':
        design={r['id']:r for r in json.loads((root/'sheets/cards.json').read_text())}
        assert set(result[0]['allowed'])=={r['cardId'] for r in membership if r['classId']==cls and design[r['cardId']]['rarity'] in ['COMMON','UNCOMMON','RARE']},result
    send('save');send('resume')
    if cls=='ironclad':send('enter',room='Monster')
    ready();send('pool-audit-owner')
    if 'export' in tag:
        send('pool-audit-export-run')
        shutil.copy2(profile/'pool-run.json',root/'private/sts2-evidence'/f'pool54-{"beta" if beta else "regular"}-{cls}-run.json')
    send('pool-audit-registry')
    if 'import' not in tag:
        send('abandon');send('select');menu=send('pool-audit-library',menu=True,heir=True)
        assert len(menu['filteredLegacy'])==360 and menu['rendered']>0 and not menu['staleMaterials'],menu
    print(tag,cls,'reward/shop/transform/filter/history checks pass',flush=True)
if 'import' in tag:send('abandon')
