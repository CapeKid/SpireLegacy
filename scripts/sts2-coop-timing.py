"""Native lobby input and active host-loss checks; no physical-device claims."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profiles=[Path(p) for p in sys.argv[2:4]]
tag=sys.argv[4];port=int(sys.argv[5]);records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'coop-timing-{tag}.json'
assert not output.exists()

def send(peer,command,**args):
    record=call(profiles[peer],dict(command=command,**args),timeout=60,expected_hash=expected)
    record['peer']=peer+1;records.append(record)
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(records,indent=2),encoding='utf-8')
    assert record['response']['ok'],record
    return record['response']['result']

def wait(predicate,seconds=45):
    deadline=time.monotonic()+seconds
    while time.monotonic()<deadline:
        result=predicate()
        if result:return result
        time.sleep(.3)
    raise TimeoutError('Native timed co-op predicate; do not replay mutations.')

def local(state,peer):
    return next(p for p in state['lobby'] if p['id']==peer+1)

send(0,'coop-host',**{'class':'knight','traits':['megahealth'],'port':port})
send(1,'coop-join',**{'class':'mage','traits':['superhealer'],'port':port})
wait(lambda: (a:=send(0,'coop-state'))['session'] and a['session']==send(1,'coop-state')['session'])
time.sleep(1)  # deferred native Enable()/hotkey activation
ready=send(0,'coop-ui-select',control='ready')
assert ready['enabled'] and ready['releases']==1 and local(ready['state'],0)['ready'],ready
before=local(send(0,'coop-state'),0)
attempt=send(0,'coop-ui-select',control='ironclad')
assert not attempt['enabled'] and not attempt['focused'] and attempt['releases']==0,attempt
assert local(attempt['state'],0)==before,attempt
unready=send(0,'coop-ui-select',control='unready')
assert unready['enabled'] and unready['releases']==1 and not local(unready['state'],0)['ready'],unready
wait(lambda: 1 not in send(1,'coop-state')['pending'])
time.sleep(.5)
changed=send(0,'coop-ui-select',control='ironclad')
assert changed['enabled'] and changed['releases']==1 and 'IRONCLAD' in local(changed['state'],0)['character'],changed
wait(lambda: 'IRONCLAD' in local(send(1,'coop-state'),0)['character'])
send(0,'coop-ui-select',control='heir')
send(0,'coop-ui-select',control='ready');send(1,'coop-ui-select',control='ready')
wait(lambda: send(0,'coop-state')['players'] and send(1,'coop-state')['players'])
time.sleep(12)
send(0,'coop-vote');send(1,'coop-vote')
def combat():
    a,b=[send(p,'coop-state') for p in range(2)]
    return a if a['players']==b['players'] and all(p['phase']=='Play' for p in a['players']) and not a['actionRunning'] else False
wait(combat)
send(0,'coop-play');wait(combat);send(1,'coop-play');wait(combat)
before=[send(p,'lifecycle-state')['profile'] for p in range(2)]
lost=send(0,'coop-disconnect');assert lost['wasConnected'] and not lost['connected'],lost
wait(lambda: not send(1,'coop-state')['connected'])
time.sleep(3)
for peer in range(2):
    scopes=send(peer,'coop-contexts')
    assert not any(scopes.values()),scopes
    after=send(peer,'lifecycle-state')['profile']
    # Transport loss is resumable; it must not grant crowns or retire an heir.
    assert after['crowns']==before[peer]['crowns'] and after['generation']==before[peer]['generation'],(before[peer],after)
    assert after['active']==before[peer]['active'],(before[peer],after)
print(tag,'controller-ready gates, revocation, paid cards and active host loss passed',flush=True)
