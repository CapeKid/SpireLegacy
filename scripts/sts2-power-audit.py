"""Seeded native paid class-power and combat-hook audit; disposable profiles only."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
out=root/'private/sts2-evidence'/('power-audit-'+tag+'.json');assert not out.exists()
stream=out.with_suffix('.jsonl');assert not stream.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r)
    with stream.open('a',encoding='utf-8') as f:f.write(json.dumps(r)+'\n')
    out.write_text(json.dumps(records,indent=2))
    assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.15)
    raise TimeoutError('Native playable power audit')
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'seed':'SPIRELEGACY-POWERS-53','manorTier':0})
    if cls=='ironclad':send('enter',room='Monster')
    ready();send('vigor-room');ready()
send('trait-speed',value=8)
send('compat-control',patches=True,upgrade=False)
for cls in ([] if any(x in tag for x in ['engines','retention','copies']) else ['knight','mage','ranger']):
    modes=['single','multi','area','random','zero','replay','dupe','lethal']+(['reservoir'] if cls=='mage' else [])
    if 'smoke' in tag:modes=['reservoir' if cls=='mage' else 'multi']
    for up in ([False] if 'smoke' in tag else [False,True]):
        for mode in modes:
            start(cls);send('power-class',mode=mode,upgrade=up)
            print(tag,cls,mode,up,'pass',flush=True)
    send('save');send('resume');ready()
    assert all(p['charges']==p['attacks']==0 for p in send('power-snapshot'))
for up in ([] if any(x in tag for x in ['engines','retention','copies']) else [False] if 'smoke' in tag else [False,True]):
    for mode in (['multi'] if 'smoke' in tag else ['multi','area','random']):
        start('ranger');send('power-rhythm',mode=mode,upgrade=up)
    for native in [False,True]:
        start('ironclad' if native else 'mage');send('power-barrier',native=native,upgrade=up)
if 'engines' in tag:
    for cls in ['knight','mage','ranger']:
        for up in [False,True]:
            for mode in ['third','discard','exhaust','draw-chain','sadistic','turn']:
                start(cls);send('power-engines',mode=mode,upgrade=up)
                print(tag,cls,mode,up,'pass',flush=True)
if 'retention' in tag:
    for cls in ['knight','mage','ranger']:
        for up,stack,intrinsic,full in [(False,False,False,False),(True,True,True,False),(False,False,False,True)]:
            start(cls);send('retention-check',upgrade=up,stack=stack,intrinsic=intrinsic,fullHand=full)
if 'copies' in tag:
    for cls in ['knight','mage','ranger']:
        for up in [False,True]:
            for mode in ['dupe','save-copy']:
                start(cls);assert len(send('power-copies',mode=mode,upgrade=up))==3
                print(tag,cls,mode,up,'pass',flush=True)
send('abandon');print(tag+': selected power audit suite passes',flush=True)
