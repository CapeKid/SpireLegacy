"""Exercise Fragile startup and Neow eligibility in an isolated native lab."""
import json, os, sys, time
from pathlib import Path
from oracle_protocol import call
profile = Path(sys.argv[1])
records=[]
def send(command, **args):
    record=call(profile, dict(command=command, **args), timeout=90, expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256'])
    records.append(record)
    (profile/'fragile-start-receipts.json').write_text(json.dumps(records, indent=2),encoding='utf-8')
    response=record['response']
    assert response['ok'], response
    return response['result']
def ready():
    for _ in range(100):
        state=send('state')
        if state['phase']=='Play':
            return state
        time.sleep(.15)
    raise TimeoutError('First fight did not become playable')
send('trait-speed',value=8)
cases=[
    ('mage',['onehitdeath','magic'],2,False,0),
    ('knight',['onehitdeath','nomeat'],2,False,0),
    ('ranger',['onehitdeath','megahealth'],10,False,2),
    ('knight',['onehitdeath'],0,False,0),
    ('ranger',['onehitdeath'],2,False,0),
    ('mage',['onehitdeath','magic'],10,True,0),
    ('mage',['magic'],2,False,0),
    ('ironclad',[],2,False,0),
]
for cls,traits,ascension,quick,tier in cases:
    state=send('start', **{'class':cls,'traits':traits,'quick':quick,'manorTier':tier,'ascension':ascension,'seed':'FRAGILE-START'})
    fragile='onehitdeath' in traits
    assert state['hp']==(1 if fragile else int(state['maxHp']*.8)),state
    assert (state['maxHp']==1)==fragile,state
    assert send('neow-leafy-eligible') is (not fragile)
    if cls=='mage' and traits==['onehitdeath','magic'] and not quick:
        dialogue=send('ancient-state')
        for _ in range(3-dialogue['index']):
            dialogue=send('ancient-audit-next',controller=False)
        assert len(dialogue['options'])==3,dialogue
        time.sleep(.8)
        send('ancient-audit-choose')
        state=send('advance')
        if state['room']!='CombatRoom':
            send('enter',room='Monster')
        state=ready()
        assert (state['hp'],state['maxHp'])==(1,1),state
    elif quick:
        state=ready()
        assert (state['hp'],state['maxHp'],len(state['hand']))==(1,1,6),state
    print(cls,traits,'A'+str(ascension),'quick='+str(quick),state['hp'],state['maxHp'],'pass',flush=True)
    send('abandon')
