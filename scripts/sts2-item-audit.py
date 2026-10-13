"""#55 native acquisition, bounded relic triggers and potion use fixtures."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2];records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();output=root/'private/sts2-evidence'/('item-audit-'+tag+'.json')
assert not output.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        s=send('state')
        if s['phase']=='Play':return s
        time.sleep(.1)
    raise TimeoutError('Native playable phase')
send('trait-speed',value=8);clones=send('item-clones')
if 'clones' in tag:
    assert all(c['shared']==('baseline' in tag) for c in clones),clones
    print(tag,'clone isolation probe complete',flush=True);sys.exit(0)
if 'antique' in tag:
    send('start',**{'class':'knight','quick':True,'traits':['antique'],'seed':'SPIRELEGACY-ITEMS-55','manorTier':0});ready()
    r=send('item-antique-empty')
    if 'baseline' in tag:assert r['diagnostic'] and r['granted']==0,r
    else:assert r['diagnostic'] is None and r['granted']==1 and r['added']==['RELIC.CIRCLET'],r
    send('abandon');print(tag,'depleted starter grant checked',flush=True)
elif 'relic-edges' in tag:
    for cls in ['knight','mage','ranger']:
        send('chronicle-fixture',unlocked=True);send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-ITEMS-55','manorTier':0});ready()
        send('item-relic-edges');send('abandon');print(tag,cls,'nested relic/reset edges checked',flush=True)
elif 'relics' in tag:
    for cls in ['knight','mage','ranger']:
        for up in [False,True]:
            for row in json.loads((root/'sheets/heir_relics.json').read_text()):
                send('chronicle-fixture',unlocked=True)
                send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-ITEMS-55','manorTier':0});ready()
                key=row['id'];send('relic-obtain',relic=key,shop=up)
                r=send('item-relic',relic=key,upgrade=up);assert r['roundTrip'],r
                send('save');send('resume');ready();assert len(send('relic-counters'))==1
                send('abandon')
            print(tag,cls,up,'all relic identities exercised',flush=True)
elif 'sources' in tag:
    for unlocked in [False,True]:
        for cls in ['knight','mage','ranger']:
            for traits in [[],['cantattack']]:
                send('chronicle-fixture',unlocked=unlocked);send('start',**{'class':cls,'quick':True,'traits':traits,'seed':'SPIRELEGACY-ITEMS-55','manorTier':0});ready()
                send('relic-info');send('potion-info');send('abandon');print(tag,unlocked,cls,traits,'native sources checked',flush=True)
elif 'potions' in tag:
    for cls in ['knight','mage','ranger']:
        for traits in [[],['cantattack'],['superhealer']]:
            send('start',**{'class':cls,'quick':True,'traits':traits,'seed':'SPIRELEGACY-ITEMS-55','manorTier':0});ready()
            for shop in [False,True]:
                send('potion-clear')
                for key in ['borrowed_breath','briar_vial','triune_draught']:send('potion-obtain',potion=key,shop=shop)
                slots=send('potion-slots');send('save');send('resume');ready();assert send('potion-slots')==slots
                for key in ['borrowed_breath','briar_vial','triune_draught']:
                    r=send('potion-use',potion=key);assert r['completed'] and r['hpChange']==r['energyChange']==0,r
                assert all(p is None for p in send('potion-slots'))
            send('abandon');print(tag,cls,traits,'saved belt/native effects complete',flush=True)
elif 'edges' in tag:
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-ITEMS-55','manorTier':0});ready()
        send('item-potion-edges');send('abandon');print(tag,cls,'belt and queue edges checked',flush=True)
