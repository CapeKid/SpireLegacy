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

state=send('start', **{'class':'mage','traits':['onehitdeath','magic'],'quick':False,'manorTier':0,'ascension':2,'seed':'FRAGILE-START'})
assert state['hp']==state['maxHp']==1,state
choice=send('neow-leafy-choose')
assert choice==dict(offered=False,startingMaxHp=1,maxHp=1,hp=1,transformed=True,finished=True),choice
send('ancient-health-save-lock')
restored=send('resume')
assert restored['hp']==restored['maxHp']==1,restored
advanced=send('advance')
assert advanced['hp']==advanced['maxHp']==1,advanced
send('abandon')

state=send('start', **{'class':'knight','traits':[],'quick':False,'manorTier':0,'ascension':2,'seed':'FRAGILE-START'})
assert send('neow-leafy-eligible')
choice=send('neow-leafy-choose')
assert choice['maxHp']==state['maxHp']-12 and choice['hp']>0 and choice['transformed'] and choice['finished'],choice
send('abandon')
print('Leafy Poultice resolves safely for Fragile, recovers on resume, and charges the ordinary cost.',flush=True)

def start_cost_case(traits):
    send('start', **{'class':'mage','traits':traits,'quick':False,'manorTier':0,'ascension':2,'seed':'FRAGILE-START'})

start_cost_case(['onehitdeath'])
shears=send('neow-shears-choose')
assert not shears['eligible'] and not shears['offered'] and shears['hp']==1 and shears['removed']==2 and shears['finished'] and not shears['ended'],shears
send('ancient-health-save-lock')
assert send('resume')['hp']==1
send('abandon')

start_cost_case([])
shears=send('neow-shears-choose')
assert shears['eligible'] and shears['hp']==shears['hpBefore']-16 and shears['removed']==2 and not shears['ended'],shears
send('abandon')

start_cost_case(['onehitdeath'])
beacon=send('event-max-hp-choice',vakuu=False)
assert beacon==dict(hp=1,maxHp=1,finished=True,reward='FRESNEL_LENS'),beacon
send('abandon')

start_cost_case(['onehitdeath'])
vakuu=send('event-max-hp-choice',vakuu=True)
assert vakuu['hp']==vakuu['maxHp']==1 and vakuu['finished'] and vakuu['reward'] in ('DISTINGUISHED_CAPE','SERE_TALON'),vakuu
send('ancient-health-save-lock')
assert send('resume')['hp']==1
send('abandon')

start_cost_case([])
defeat=send('neow-shears-choose',forceLowHp=True)
assert defeat['hp']==0 and defeat['ended'] and not defeat['resumable'],defeat
print('Shears and the max-HP events resolve safely, and an Ancient defeat leaves no resumable save.',flush=True)
