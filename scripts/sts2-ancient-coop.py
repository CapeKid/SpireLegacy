"""#56 actual two-peer party ownership and native event model callbacks."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];host,guest=sys.argv[2:4];tag=sys.argv[4];port=int(sys.argv[5]);records=[]
output=root/'private/sts2-evidence'/('ancient-coop-'+tag+'.json');assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
def send(profile,command,**args):
    r=call(Path(profile),dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
send(host,'coop-host',**{'class':'ironclad' if 'native' in tag else 'knight','traits':[],'port':port,'manor':{}});send(host,'coop-ready')
send(guest,'coop-join',**{'class':'ranger' if 'ranger' in tag else 'mage','traits':[],'port':port,'manor':{}})
deadline=time.monotonic()+45
while True:
    a,b=[send(p,'coop-state') for p in [host,guest]]
    if a['session'] and a['session']==b['session'] and len(a['lobby'])==len(b['lobby'])==2:break
    if time.monotonic()>deadline:raise TimeoutError('Authenticated lobby')
    time.sleep(.2)
send(guest,'coop-ready');time.sleep(12);send(host,'coop-vote');send(guest,'coop-vote')
deadline=time.monotonic()+45
while True:
    a,b=[send(p,'coop-state') for p in [host,guest]]
    if a['players'] and a['players']==b['players'] and all(p['phase']=='Play' for p in a['players']):break
    if time.monotonic()>deadline:raise TimeoutError('Native peer synchronization')
    time.sleep(.2)
checks=[send(p,'ancient-audit-owners') for p in [host,guest]];assert checks[0]==checks[1] and len(checks[0])==2,checks
assert all(len(p['ancientKeys'])==24 for p in checks[0])
for profile in [host,guest]:assert send(profile,'power-coop-checkpoint',minimum=0)['enabled']
send(host,'coop-abandon');print(tag,'remote/native Ancient and ending ownership passed',flush=True)
