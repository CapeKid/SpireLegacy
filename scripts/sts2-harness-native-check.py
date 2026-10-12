"""Real isolated-game harness baseline. Modes: solo PROFILE TAG; coop HOST CLIENT TAG PORT."""
import hashlib,json,sys,time,uuid
from pathlib import Path
from oracle_protocol import call,assert_checks

root=Path(__file__).resolve().parents[1]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
mode=sys.argv[1];tag=sys.argv[3] if mode=='solo' else sys.argv[4];evidence=[]

def send(profile,command,ok=True,**args):
    record=call(root/profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record)
    reply=record['response'];assert reply['ok']==ok,reply
    facts=reply['metadata'];assert {m['id'] for m in facts['mods']}=={'BaseLib','SpireLegacy'},facts
    assert facts['sourceSha'] and facts['game'] and facts['platform'],facts
    return reply.get('result',reply.get('error'))

def ready(profile):
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        state=send(profile,'state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('Playable phase')

try:
    if mode=='solo':
        profile=sys.argv[2]
        facts=send(profile,'harness-facts');assert Path(facts['familyPath']).resolve().is_relative_to((root/profile).resolve()) and facts['gameStore']=='GodotFileIo',facts
        assert_checks(send(profile,'harness-production-gate'),{'productionSubscribed':False})
        request_id=uuid.uuid4().hex
        send(profile,'harness-facts',requestId=request_id)
        # Remove the prior answer only for this deliberate harmless ledger-replay fixture.
        (root/profile/'response.json').unlink()
        failure=send(profile,'harness-facts',ok=False,requestId=request_id);assert 'already claimed' in failure,failure
        failure=send(profile,'harness-failure',ok=False)
        assert 'Awaited harness fixture' in failure and 'Inner harness fixture' in failure and 'HarnessFailure' in failure,failure
        folder=root/profile;(folder/'response.json').unlink()
        pending=folder/'malformed.pending';pending.write_text('{',encoding='utf-8');pending.replace(folder/'request.json')
        deadline=time.monotonic()+10
        while not (folder/'response.json').exists():
            if time.monotonic()>deadline:raise TimeoutError('Malformed command diagnostic')
            time.sleep(.05)
        malformed=json.loads((folder/'response.json').read_text(encoding='utf-8'))
        assert not malformed['ok'] and 'Json' in malformed['error'],malformed
        evidence.append(dict(request='deliberately partial JSON',response=malformed))
        send(profile,'harness-facts')
        for cls in ['knight','mage','ranger']:
            for upgraded in [False,True]:
                send(profile,'start',**{'class':cls,'traits':[],'quick':True});ready(profile)
                played=send(profile,'expansion-play',card=cls+'_strike',upgrade=upgraded)
                assert played['enemyDamage']>0 and played['energyChange']==-played['cost'],played
                assert_checks({k:played[k] for k in ['pile','saveRoundTrip','tableNodePresent']},{'pile':'Discard','saveRoundTrip':True,'tableNodePresent':False})
        send(profile,'abandon')
        send(profile,'select')
    elif mode=='coop':
        host,guest=sys.argv[2:4];port=int(sys.argv[5])
        send(host,'coop-host',**{'class':'knight','traits':['easybreakables'],'port':port,'manor':{'vitality':1}})
        send(host,'coop-ready')
        send(guest,'coop-join',**{'class':'mage','traits':['cantattack'],'port':port,'manor':{}})
        # Ready is correctly refused until the authenticated lobby session arrives.
        # Wait for that boundary instead of racing the native message pump.
        deadline=time.monotonic()+30
        while time.monotonic()<deadline:
            a=send(host,'coop-state');b=send(guest,'coop-state')
            if a['session'] and a['session']==b['session'] and len(a['lobby'])==len(b['lobby'])==2:break
            time.sleep(.2)
        else:raise TimeoutError('Authenticated lobby session')
        send(guest,'coop-ready')
        def peers(phase=None):
            deadline=time.monotonic()+45
            while time.monotonic()<deadline:
                a=send(host,'coop-state');b=send(guest,'coop-state')
                if a['players'] and b['players'] and (phase is None or all(p['phase']==phase for s in [a,b] for p in s['players'])):
                    if a['players']==b['players']:return a,b
                time.sleep(.2)
            raise TimeoutError('Peer synchronization '+str(phase))
        a,b=peers();assert [p['heir']['classId'] for p in a['players']]==['knight','mage'],a
        ids=[p['snapshot']['id'] for p in a['players']]
        assert a['active']==ids[0] and b['active']==ids[1],(a,b)
        for profile in [host,guest]:assert len(send(profile,'coop-roundtrip'))==2
        diagnostic=send(host,'harness-owner-diagnostic',owner=2)
        assert diagnostic['originalPreserved'] and 'Heir: mage; traits: cantattack' in diagnostic['text'] and 'Inner owner diagnostic' in diagnostic['text'],diagnostic
        time.sleep(12);send(host,'coop-vote');send(guest,'coop-vote');peers('Play')
        for profile in [host,guest]:
            played=send(profile,'coop-play')
            assert played['played'] and played['pile']=='Discard' and not played['tableNodePresent'],played
            time.sleep(1)
        a,b=peers();assert a['active']==ids[0] and b['active']==ids[1]
        family_before={}
        for profile in [host,guest]:
            isolation=send(profile,'lifecycle-owners');assert isolation['checkedOwners']==1,isolation
            family_before[profile]=send(profile,'lifecycle-state')['profile']
        send(host,'coop-abandon')
        for profile in [host,guest]:
            deadline=time.monotonic()+30
            while True:
                final=send(profile,'lifecycle-state')
                if final['profile']['active'] is None:break
                if time.monotonic()>deadline:raise TimeoutError('Co-op family settlement '+profile)
                time.sleep(.2)
            initial=family_before[profile];current=final['profile'];run_id=initial['active']['id']
            assert current==final['persisted'] and current['generation']==initial['generation']+1,final
            assert current['settledId']==run_id and current['chronicle']['settledRuns'].count(run_id)==1,final
            assert current['crowns']>=initial['crowns'] and current['familyId']==initial['familyId'],final
    else:raise ValueError(mode)
    print(tag+': '+mode+' native harness baseline passed')
finally:
    out=root/'private/sts2-evidence'/('harness-'+tag+'-'+mode+'.json');out.parent.mkdir(exist_ok=True)
    out.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
