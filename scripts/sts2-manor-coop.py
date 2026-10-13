"""Ready-boundary and live networking probes in two disposable native peers."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];host,guest=sys.argv[2:4];tag=sys.argv[4];port=int(sys.argv[5]);records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/('manor-coop-'+tag+'.json');assert not output.exists()
def send(profile,command,**args):
    r=call(Path(profile),dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
send(host,'coop-host',**{'class':'knight','traits':[],'port':port,'manor':{}})
send(guest,'coop-join',**{'class':'mage','traits':[],'port':port,'manor':{}})
deadline=time.monotonic()+45
while True:
    a,b=[send(p,'coop-state') for p in [host,guest]]
    if a['session'] and a['session']==b['session'] and len(a['lobby'])==len(b['lobby'])==2:break
    assert time.monotonic()<deadline;time.sleep(.2)
send(host,'manor');assert not send(host,'manor-audit')['paused']
state=send(host,'manor-state');upgrade=next(b['text'] for b in state['buttons'] if b['text'].startswith('Select upgrade') and not b['disabled'])
send(host,'press',text=upgrade);send(host,'press',text='Review purchases')
before=send(host,'lifecycle-state')['profile']
send(host,'coop-ready');send(host,'press',text='Confirm and save')
after=send(host,'lifecycle-state')['profile']
print(tag,'ready confirmation bank:',before['crowns'],after['crowns'],flush=True)
if 'baseline' not in tag:assert before['crowns']==after['crowns'] and before['manor']==after['manor']
send(host,'close');send(host,'coop-ready',ready=False)
send(host,'manor');send(host,'press',text='Change banner');send(host,'close');send(host,'coop-ready');send(host,'manor');send(guest,'coop-ready')
time.sleep(12);send(host,'coop-vote');send(guest,'coop-vote')
deadline=time.monotonic()+45
while True:
    a,b=[send(p,'coop-state') for p in [host,guest]]
    if a['players'] and a['players']==b['players'] and all(p['phase']=='Play' for p in a['players']):break
    assert time.monotonic()<deadline;time.sleep(.2)
beforeGuest=send(guest,'lifecycle-state')
if 'baseline' not in tag:assert not send(host,'manor-audit')['open'],'Lobby modal survived embark'
assert send(host,'manor-run-launch')['open'];state=send(host,'manor-state');assert not state['paused']
assert all(b['disabled'] for b in state['buttons'] if b['text'].startswith('Select upgrade'))
send(guest,'coop-play');send(host,'manor-state')
assert send(guest,'lifecycle-state')['profile']['crowns']==beforeGuest['profile']['crowns']
send(host,'close');send(host,'coop-abandon');print(tag,'owner modal/network checks completed',flush=True)
