"""Native Touch of Orobas acquisition and inherited carrier persistence."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
out=root/'private/sts2-evidence'/('orobas-'+tag+'.json');assert not out.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);out.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.15)
    raise TimeoutError('Native play phase')
send('trait-speed',value=8)
for cls in ['knight','mage','ranger']:
    for traits in ([[]] if 'baseline' in tag else [[],['cantattack'],['antique']]):
        send('start',**{'class':cls,'traits':traits,'quick':True,'seed':'SPIRELEGACY-OROBAS-86','manorTier':0});ready()
        result=send('orobas-refinement',baseline='baseline' in tag)
        if 'baseline' in tag:assert result['circlet'],result
        else:
            send('save');send('resume');ready();send('orobas-refinement',inspect=True)
            for upgraded in [False,True]:
                played=send('expansion-play',card=cls+'_strike',upgrade=upgraded);assert played['pile']=='Discard' and not played['tableNodePresent'],played
        send('abandon')
print(tag+': '+('native Circlet fallback reproduced for all classes' if 'baseline' in tag else 'nine native refinements/save-resumes and paid base/upgraded cards pass'),flush=True)
