"""#60 reset/Reflex paid-play isolation over the native ENet action queue."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];host,guest=map(Path,sys.argv[2:4]);tag=sys.argv[4];port=int(sys.argv[5]);records=[]
output=root/'private/sts2-evidence'/('balance-coop-'+tag+'.json');assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
def send(profile,command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
send(host,'presentation-prepare');send(guest,'presentation-prepare')
send(host,'coop-host',**{'class':'knight','traits':[],'port':port,'manor':{}});send(host,'coop-ready')
send(guest,'coop-join',**{'class':'ranger' if 'ranger' in tag else 'mage','traits':[],'port':port,'manor':{}})
deadline=time.monotonic()+45
while True:
    a,b=[send(p,'coop-state') for p in [host,guest]]
    if a['session'] and a['session']==b['session'] and len(a['lobby'])==len(b['lobby'])==2:break
    assert time.monotonic()<deadline;time.sleep(.2)
send(guest,'coop-ready');time.sleep(12);send(host,'coop-vote');send(guest,'coop-vote')
deadline=time.monotonic()+45
while True:
    a,b=[send(p,'coop-state') for p in [host,guest]]
    if a['players'] and a['players']==b['players'] and all(p['phase']=='Play' for p in a['players']):break
    assert time.monotonic()<deadline;time.sleep(.2)
for upgraded in [False,True]:
    for profile in [host,guest]:send(profile,'balance-audit-coop-prepare',upgrade=upgraded)
    for profile in [host,guest]:
        r=send(profile,'balance-audit-coop-play',upgrade=upgraded)
        assert r['exhausted']==2 and r['energySpent']==(0 if upgraded else 2) and r['ownerIsolated'] and r['checksumEnabled'],r
        for peer in [host,guest]:assert send(peer,'power-coop-checkpoint',minimum=r['checksumNext'])['enabled']
        print(tag,r,flush=True)
send(host,'coop-abandon')
