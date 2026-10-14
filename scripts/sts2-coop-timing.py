"""Native lobby input and active host-loss checks; no physical-device claims."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profiles=[Path(p) for p in sys.argv[2:4]]
tag=sys.argv[4];port=int(sys.argv[5]);records=[]
native=len(sys.argv)>6 and sys.argv[6]=='native-characters'
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'coop-timing-{tag}.json'
assert not output.exists()

def send(index,command,**args):
    record=call(profiles[index],dict(command=command,**args),timeout=60,expected_hash=expected)
    record['peer']=index+1;records.append(record)
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(records,indent=2),encoding='utf-8')
    assert record['response']['ok'],record
    assert {m['id'] for m in record['response']['metadata']['mods']}=={'BaseLib','SpireLegacy'}
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

send(0,'coop-host',**{'class':'ironclad' if native else 'knight','traits':[] if native else ['megahealth'],'port':port})
send(1,'coop-join',**{'class':'ironclad' if native else 'mage','traits':[] if native else ['superhealer'],'port':port})
wait(lambda: (a:=send(0,'coop-state'))['session'] and a['session']==send(1,'coop-state')['session'])
time.sleep(1)  # deferred native Enable()/hotkey activation
ready=send(0,'coop-ui-select',control='ready')
assert ready['enabled'] and ready['releases']==1 and local(ready['state'],0)['ready'],ready
before=local(send(0,'coop-state'),0)
attempt=send(0,'coop-ui-select',control='heir' if native else 'ironclad')
assert not attempt['enabled'] and not attempt['focused'] and attempt['releases']==0,attempt
assert local(attempt['state'],0)==before,attempt
unready=send(0,'coop-ui-select',control='unready')
assert unready['enabled'] and unready['releases']==1 and not local(unready['state'],0)['ready'],unready
wait(lambda: 1 not in send(1,'coop-state')['pending'])
time.sleep(.5)
changed=send(0,'coop-ui-select',control='ironclad')
assert changed['enabled'] and changed['releases']==1 and 'IRONCLAD' in local(changed['state'],0)['character'],changed
wait(lambda: 'IRONCLAD' in local(send(1,'coop-state'),0)['character'])
# Native A also activates the global Embark hotkey after selecting a character.
# Revoke that new native ready state before switching back to the Heir.
if local(changed['state'],0)['ready']:
    assert send(0,'coop-ui-select',control='unready')['releases']==1
    wait(lambda: not local(send(1,'coop-state'),0)['ready'])
    time.sleep(.5)
heir=send(0,'coop-ui-select',control='heir')
assert heir['enabled'] and heir['releases']==1 and 'SPIRELEGACY' in local(heir['state'],0)['character'],heir
if not native and not local(heir['state'],0)['ready']:send(0,'coop-ui-select',control='ready')
if native:
    if local(heir['state'],0)['ready']:
        send(0,'coop-ui-select',control='unready');time.sleep(.5)
    restored=send(0,'coop-ui-select',control='ironclad')
    assert restored['releases']==1 and 'IRONCLAD' in local(restored['state'],0)['character'],restored
    if not local(restored['state'],0)['ready']:send(0,'coop-ui-select',control='ready')
send(1,'coop-ui-select',control='ready')
wait(lambda: send(0,'coop-state')['players'] and send(1,'coop-state')['players'])
time.sleep(12)
send(0,'coop-vote');send(1,'coop-vote')
def combat():
    a,b=[send(p,'coop-state') for p in range(2)]
    return a if a['players']==b['players'] and all(p['phase']=='Play' for p in a['players']) and not a['actionRunning'] else False
started=wait(combat)
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
for peer in range(2):send(peer,'select')
send(0,'coop-resume-host',port=port+1)
send(1,'coop-resume-join',port=port+1,peer=2)
for peer in range(2):send(peer,'coop-resume-ready')
resumed=wait(lambda: (s if (s:=send(0,'coop-state'))['players'] else False))
def inherited(players):
    # The native checkpoint precedes combat. Its floor/progress legitimately
    # differs from the in-combat snapshot captured before host loss.
    return [None if p['snapshot'] is None else {k:v for k,v in p['snapshot'].items() if k!='floors'} for p in players]
assert inherited(resumed['players'])==inherited(started['players']),resumed
wait(lambda: (p:=send(1,'coop-state')['players']) and inherited(p)==inherited(started['players']))
time.sleep(12)
send(0,'coop-vote');send(1,'coop-vote');wait(combat)
for peer in range(2):
    assert len(send(peer,'coop-roundtrip'))==2
    assert not any(send(peer,'coop-contexts').values())
send(0,'coop-abandon')
for peer in range(2):
    wait(lambda: send(peer,'lifecycle-state')['profile']['active'] is None)
    assert not any(send(peer,'coop-contexts').values())
print(tag,'controller-ready gates, revocation, paid cards, active host loss and saved resume passed',flush=True)
