"""Enemy-caused death and native victory revival, with no HP/resource assignment."""
import concurrent.futures,hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1]
profiles=[Path(p) for p in sys.argv[2:4]];tag=sys.argv[4];port=int(sys.argv[5])
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'coop-survival-{tag}.json';assert not output.exists()
records=[];cases=[]
def store():
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(dict(records=records,cases=cases),indent=2),encoding='utf-8')
def invoke(index,command,**args):
    r=call(profiles[index],dict(command=command,**args),timeout=60,expected_hash=expected)
    r['peer']=index+1
    return r
def collect(r,logged=False):
    if not logged:records.append(r);store()
    assert r['response']['ok'],r
    assert {m['id'] for m in r['response']['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return r['response']['result']
def send(index,command,**args):
    record=call(profiles[index],dict(command=command,**args),timeout=60,expected_hash=expected)
    record['peer']=index+1;records.append(record);store()
    return collect(record,logged=True)
def both(command):
    # Native room loading has a peer barrier. Never await one peer's mutation
    # before dispatching the other, and never replay a timed-out mutation.
    with concurrent.futures.ThreadPoolExecutor(2) as pool:
        futures=[pool.submit(invoke,i,command) for i in range(2)]
        return [collect(f.result()) for f in futures]
def wait(predicate,seconds=45):
    until=time.monotonic()+seconds
    while time.monotonic()<until:
        result=predicate()
        if result:return result
        time.sleep(.3)
    raise TimeoutError('Natural survival predicate; inspect receipts, never replay.')
def sync():
    a,b=[send(p,'coop-state') for p in range(2)]
    return a if a['players'] and a['players']==b['players'] and a['enemies']==b['enemies'] and not a['actionRunning'] and not b['actionRunning'] else False
for number,(cls,trait) in enumerate([('mage','megahealth'),('ranger','superhealer')]):
    seed='REVIVAL'+str(number+1)
    case=dict(cls=cls,trait=trait,seed=seed,deathObserved=False,revived=False,turns=[]);cases.append(case);store()
    send(0,'coop-host',**{'class':'knight','traits':['megahealth'],'port':port+number,'seed':seed,'manor':{'vitality':5,'smith':2,'armory':2,'library':1}})
    send(1,'coop-join',**{'class':cls,'traits':[trait],'port':port+number,'manor':{}})
    wait(lambda: (a:=send(0,'coop-state'))['session'] and a['session']==send(1,'coop-state')['session'])
    send(0,'coop-ready');send(1,'coop-ready')
    wait(lambda: send(0,'coop-state')['players'] and send(1,'coop-state')['players'])
    time.sleep(12)
    send(0,'coop-vote');send(1,'coop-vote')
    wait(lambda: (a if (a:=sync()) and all(p['phase']=='Play' for p in a['players']) else False))
    both('coop-survival-enter')
    state=wait(lambda: (a if (a:=sync()) and all(p['phase']=='Play' for p in a['players']) else False))
    case['initial']=state;store()
    actual=next(r['response']['metadata']['seed'] for r in reversed(records) if r['response']['metadata']['seed'])
    assert actual==seed,(seed,actual)
    deadline=time.monotonic()+900
    for step in range(1000):
        assert time.monotonic()<deadline,'Natural battle exceeded 15-minute cap'
        state=wait(sync)
        victim=state['players'][1];guardian=state['players'][0]
        if victim['hp']==0:case['deathObserved']=True;case.setdefault('deathTurn',guardian['turn'])
        if not state['combatInProgress']:
            assert case['deathObserved'],'Combat ended before an enemy-caused death'
            assert victim['hp']==1,'Required native revival was suppressed or multiplied'
            assert guardian['hp']>0,'No surviving teammate'
            case['revived']=True;case['final']=state;store();break
        assert guardian['hp']>0,'Defending teammate died before completing the scenario'
        assert guardian['turn']<=80,'Native turn cap exceeded'
        result=send(0,'coop-survival-step',attack=case['deathObserved'])
        if result['action']=='play':
            assert result['energyAfter']==result['energyBefore']-result['cost'],result
            continue
        if result['action']=='end-turn':
            old=guardian['turn'];case['turns'].append({'turn':old,'guardianHp':guardian['hp'],'victimHp':victim['hp'],'enemies':state['enemies']});store()
            if victim['hp']>0:send(1,'coop-turn')
            wait(lambda: (a if (a:=sync()) and (not a['combatInProgress'] or a['players'][0]['turn']>old and a['players'][0]['phase']=='Play') else False))
        else:time.sleep(.3)
    else:raise AssertionError('Natural battle action cap exceeded')
    for peer in range(2):
        assert not any(send(peer,'coop-contexts').values())
        assert len(send(peer,'coop-roundtrip'))==2
    before=[send(peer,'lifecycle-state')['profile'] for peer in range(2)]
    send(0,'coop-abandon')
    for peer in range(2):
        final=wait(lambda: (s if (s:=send(peer,'lifecycle-state'))['profile']['active'] is None else False))
        assert final['profile']==final['persisted']
        assert final['profile']['generation']==before[peer]['generation']+1
        assert final['profile']['settledId']==before[peer]['active']['id']
        assert not any(send(peer,'coop-contexts').values())
        send(peer,'select')
    print(tag,cls,trait,'enemy-caused death and native 1-HP revival passed',flush=True)
