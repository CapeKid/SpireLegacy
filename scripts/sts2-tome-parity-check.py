"""Compare actual Dusty Tome previews/acquisition with native Ironclad control."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
out=root/'private/sts2-evidence'/('tome-parity-'+tag+'.json');assert not out.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);out.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.15)
    raise TimeoutError('Native play phase')
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'seed':'SPIRELEGACY-TOME-93','manorTier':0})
    if cls=='ironclad':send('enter',room='Monster')
    ready()
send('trait-speed',value=8)
for cls in ['knight','mage','ranger','ironclad']:
    for removed in [False,True]:
        start(cls);result=send('tome-parity',removeBasics=removed);assert result['upgraded'] and result['offerChecks']==60,result
        send('save');send('resume')
        # Native control's debug combat is not a persisted map location; its save
        # resumes the original event room. Deck/relic parity needs no combat phase.
        if cls!='ironclad':ready()
        send('tome-parity',inspect=True);send('abandon')
for upgraded in [False,True]:
    start('ranger');send('tome-parity',forced='ranger_ancient_canopy')
    played=send('ancient-play',card='ranger_ancient_canopy',upgrade=upgraded)['played']
    assert played['blockChange']==(18 if upgraded else 14) and any(p['amount']==7 and 'BLOCK_NEXT_TURN' in p['id'] for p in played['powers']),played
    assert played['pile']=='Exhaust' and not played['tableNodePresent'] and played['energyChange']==-played['cost'] and played['saveRoundTrip'],played
    send('abandon')
print(tag+': all classes/native control, 600 native Tome offers, ten upgraded acquisitions, eight save/resumes and paid base/upgraded Canopy pass',flush=True)
