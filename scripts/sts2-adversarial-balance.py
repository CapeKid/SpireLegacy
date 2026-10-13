"""#60 paid loop minimization, native benchmarks and fixed-deck pressure tests."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
output=root/'private/sts2-evidence'/('adversarial-balance-'+tag+'.json');assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=180,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2))
    assert r['response']['ok'],r
    assert {m['id'] for m in r['response']['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return r['response']['result']
def start(cls,tier=0,traits=[]):
    send('start',**{'class':cls,'manorTier':tier,'traits':traits,'quick':True,'seed':'SPIRELEGACY-AUDIT-60'})
    deadline=time.monotonic()+45
    while send('state')['phase']!='Play':
        assert time.monotonic()<deadline;time.sleep(.15)
send('presentation-prepare');send('balance-audit-references')
for cls in ['ranger'] if 'baseline' in tag else ['knight','mage','ranger']:
    for upgraded in [False,True]:
        for native in [False,True]:
            start(cls);r=send('balance-audit-reset',upgrade=upgraded,native=native)
            assert r['roundTrip'] and r['ownerIsolated'],r
            if 'baseline' not in tag or native:assert r['plays']==r['exhausted']==2,r
            else:assert r['plays']==(3 if not upgraded else 12) and r['exhausted']==0,r
            print(tag,cls,upgraded,native,r,flush=True)
send('abandon')
