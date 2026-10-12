"""Real 2/3/4-peer ENet ownership, lobby ordering and reconnect audit."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1]
tag=sys.argv[1];port=int(sys.argv[2]);profiles=[Path(p).resolve() for p in sys.argv[3:7]]
assert len(profiles)==4
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'coop-audit-{tag}.json';evidence=[]
assert not output.exists()
def send(index,command,**args):
    record=call(profiles[index],dict(command=command,**args),timeout=90,expected_hash=expected)
    record['peer']=index+1;evidence.append(record);output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    r=record['response'];assert r['ok'],r
    assert {m['id'] for m in r['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return r['result']
def wait(predicate,seconds=60):
    deadline=time.monotonic()+seconds
    while time.monotonic()<deadline:
        result=predicate()
        if result:return result
        time.sleep(.2)
    raise TimeoutError('Co-op audit predicate')
def synced(count,phase=None):
    def poll():
        states=[send(i,'coop-state') for i in range(count)]
        players=states[0]['players']
        if not players or len(players)!=count:return False
        if any(s['actionRunning'] for s in states):return False
        if phase and any(p['phase']!=phase for s in states for p in s['players']):return False
        return states if all(s['players']==players for s in states) else False
    return wait(poll)
def lobby(index,cls,host=False,**extra):
    return send(index,'coop-host' if host else 'coop-join',**{'class':cls,'traits':[],'port':port,'peer':index+1,**extra})
def owner_checks(count):
    for i in range(count):
        assert len(send(i,'coop-roundtrip'))==count
        r=send(i,'coop-owner-checks');assert r==dict(ownerRefusals=3,playerUnchanged=True,contextsRestored=True),r
        assert not any(send(i,'coop-contexts')[key] for key in ['creating','multiplayer','eligibility','healing'])
def upgrade_owner(index,count):
    changed=[send(peer,'coop-upgrade',owner=index+1) for peer in range(count)]
    assert all(c==changed[0] for c in changed),changed
def map_start(count):
    time.sleep(12)
    for i in range(count):send(i,'coop-vote')
    return synced(count,'Play')
def settle(count):
    before=[send(i,'lifecycle-state')['profile'] for i in range(count)]
    send(0,'coop-abandon')
    for i in range(count):
        final=wait(lambda: (r if (r:=send(i,'lifecycle-state'))['profile']['active'] is None else False))
        current=final['profile'];assert current==final['persisted']
        if before[i]['active']:
            run_id=before[i]['active']['id'];assert current['generation']==before[i]['generation']+1
            assert current['settledId']==run_id and current['chronicle']['settledRuns'].count(run_id)==1
        else:assert current['generation']==before[i]['generation'] and current['crowns']==before[i]['crowns']
def menu(count):
    for i in range(count):send(i,'select')
    time.sleep(1)

# Two identical-class Heirs, host ready before join, rapid toggles, leave/rejoin.
lobby(0,'knight',True,lesson='knight');send(0,'coop-ready')
lobby(1,'knight',traits=['superhealer'])
wait(lambda: next((p for p in send(1,'coop-state')['lobby'] if p['id']==1 and p['snapshot']=='knight'),False))
probe=send(1,'coop-lobby-checks');assert probe['immutableReady'] and probe['requiredFrozenFields']
wire=send(1,'coop-wire-checks');assert wire==dict(boundedLengthFailures=4,utf8Roundtrip=True)
for ready in [False,True,False,True]:send(0,'coop-ready',ready=ready)
send(1,'coop-replay');send(1,'coop-leave')
wait(lambda: len(send(0,'coop-state')['lobby'])==1)
assert 2 not in send(0,'coop-state')['pending']
lobby(1,'knight',traits=['superhealer']);send(1,'coop-replay');send(1,'coop-ready')
synced(2);owner_checks(2);map_start(2)
send(1,'manor');assert not send(1,'coop-state')['paused']
assert send(0,'coop-play')['played'];send(1,'close');synced(2)
for i in range(2):
    upgrade_owner(i,2)
    assert send(i,'coop-play',upgrade=True)['upgraded'];synced(2)
settle(2);menu(2);print(tag,'two identical-class owners, ordering, rejoin, manor and settlement pass',flush=True)

# Mixed native/Heir lobby and host loss before start.
lobby(0,'knight',True);lobby(1,'ironclad');send(0,'coop-leave')
wait(lambda: send(1,'coop-contexts')['lobby'] is False)
assert send(1,'coop-contexts')['pending']==0 and not send(1,'coop-contexts')['handler']
time.sleep(2)
lobby(0,'knight',True);lobby(1,'ironclad');send(0,'coop-ready');send(1,'coop-ready');synced(2);map_start(2)
assert len(send(0,'coop-roundtrip'))==2
assert send(0,'coop-state')['players'][1]['snapshot'] is None
assert send(1,'coop-play')['played'];synced(2);settle(2);menu(2)
print(tag,'mixed native owner and host-loss cleanup pass',flush=True)

# Four peers, all classes and native Random resolving to another Knight.
lobby(0,'knight',True,lesson='knight',manor={'vitality':1,'smith':1});send(0,'coop-ready')
lobby(1,'mage',traits=['megahealth'])
lobby(2,'ranger',traits=['superhealer'],lesson='ranger')
# Three-peer synchronization before the fourth joins.
wait(lambda: len(send(0,'coop-state')['lobby'])==3)
send(1,'coop-ready');send(0,'coop-ready',ready=False)
lobby(3,'random',heirClass='knight',lesson='mage')
seed=send(0,'coop-random-seed');assert seed['resolvedHeirs']==1
send(3,'coop-hold',hold=True);send(3,'coop-ready');send(2,'coop-ready');send(0,'coop-ready')
time.sleep(2);blocked=send(0,'coop-state')
assert blocked['players'] is None and all(p['ready'] for p in blocked['lobby']),blocked
assert next(p for p in blocked['lobby'] if p['id']==4)['snapshot'] is None
send(3,'coop-hold',hold=False);states=synced(4)
assert [p['heir']['classId'] for p in states[0]['players']]==['knight','mage','ranger','knight']
assert all(not s['pending'] and not s['session'] for s in states),states
owner_checks(4);map_start(4)
for i in range(4):
    actors=send(i,'visual');assert len(actors)==4
    for actor in actors:
        player=next(p for p in send(i,'coop-state')['players'] if p['NetId']==actor['owner'])
        assert actor['classId']==player['heir']['classId'] and actor['bannerIndex']==player['snapshot']['banner']
for upgraded in [False,True]:
    for i in range(4):
        if upgraded:upgrade_owner(i,4)
        played=send(i,'coop-play',upgrade=upgraded)
        assert played['played'] and played['pile']=='Discard' and not played['tableNodePresent']
        assert played['upgraded']==upgraded;synced(4)
for i in range(4):
    revived=send(i,'coop-revival-checks');assert len(revived)==4 and all(p['revivedHp']==1 for p in revived)
synced(4)
before=states[0]['players']
for i in [3,2,1,0]:send(i,'coop-save-quit')
time.sleep(2)
send(0,'coop-resume-host',port=port+1)
for i in range(1,4):send(i,'coop-resume-join',port=port+1,peer=i+1)
for i in range(4):send(i,'coop-resume-ready')
restored=synced(4,'Play')
for old,new in zip(before,restored[0]['players']):
    assert old['snapshot']['id']==new['snapshot']['id']
    for field in ['heir','familyId','banner','motto','bonuses','chronicleRelics','ownerNetId']:
        assert old['snapshot'][field]==new['snapshot'][field],field
owner_checks(4);settle(4);menu(4)
for i in range(4):
    scopes=send(i,'coop-contexts');assert not any(scopes.values()),scopes
send(0,'start',**{'class':'ranger','quick':True,'traits':[]})
wait(lambda: send(0,'state')['phase']=='Play')
played=send(0,'expansion-play',card='ranger_strike',upgrade=True);assert played['pile']=='Discard'
send(0,'abandon');print(tag,'four owners, delayed random barrier, paid cards, revival, resume and solo transition pass',flush=True)
