"""#58 owner presentation and native shop callbacks in two private peers."""
import hashlib,json,sys,time,threading
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];host,guest=sys.argv[2:4];tag=sys.argv[4];port=int(sys.argv[5]);records=[];receipt_lock=threading.Lock()
output=root/'private/sts2-evidence'/('presentation-coop-'+tag+'.json');assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
def send(profile,command,**args):
    r=call(Path(profile),dict(command=command,**args),timeout=90,expected_hash=expected)
    with receipt_lock:
        records.append(r);output.write_text(json.dumps(records,indent=2))
    assert r['response']['ok'],r
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
with ThreadPoolExecutor(max_workers=2) as pool:
    list(pool.map(lambda p:send(p,'enter',room='Shop'),[host,guest]))
for profile in [host,guest]:
    before=send(profile,'room-visual');assert len(before)==2
    assert {a['classId'] for a in before}=={'knight','mage'}
    r=send(profile,'presentation-shop');assert r['bought'] and r['goldSpent']>0 and r['cardsAdded']==1
    print(tag,'shop response',r,flush=True)
    if 'baseline' not in tag:assert all(s['skillEvents']==(1 if s['owner']==r['owner'] else 0) for s in r['sprites']),r
send(host,'coop-abandon');print(tag,'owner presentation callbacks passed',flush=True)
