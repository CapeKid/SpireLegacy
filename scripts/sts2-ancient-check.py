"""Dedicated Ancient rewards and actual base/upgraded plays in isolated owned labs."""
import json,sys,time,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1];profile=(root/'private'/f'sts2-ancient-{tag}-profile').resolve();evidence=[]
deadline=time.monotonic()+140;log=root/'private'/f'ancient-{tag}.out.log'
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command,**args):
    out=profile/'response.json';out.unlink(missing_ok=True);assert not (profile/'request.json').exists()
    pending=profile/'request.pending';pending.write_text(json.dumps(dict(command=command,**args)),encoding='utf-8');pending.replace(profile/'request.json');deadline=time.monotonic()+90
    while not out.exists():
        if time.monotonic()>deadline:raise TimeoutError(command)
        time.sleep(.05)
    reply=json.loads(out.read_text(encoding='utf-8'));assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result']));return reply['result']
def ready():
    for i in range(200):
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('play')
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True});ready();send('enter',room='Boss');return ready()
try:
    for cls in ['knight','mage','ranger']:
        start(cls);counts=send('pool-parity')
        pools=[r for r in counts['heirs'] if cls.upper() in r['pool']]
        assert len(pools)==1 and pools[0]['ancient']==2 and pools[0]['ordinary'] in [82,85],pools
        rewards=send('ancients');assert rewards['ancientChecks']==90,rewards
        shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'ancient-{tag}-{cls}-tome.png')
    for row in json.loads((root/'sheets/ancient_cards.json').read_text()):
        cls=row['classId'];key=row['cardId']
        for up in [False,True]:
            start(cls);result=send('ancient-play',card=key,upgrade=up);played=result['played']
            assert played['saveRoundTrip'] and not played['tableNodePresent'] and played['energyChange']==-played['cost'],played
            if key=='knight_kingbreaker':assert played['enemyDamage']==(18 if up else 14) and played['pile']=='Exhaust',played
            if key=='knight_royal_arsenal':assert played['blockChange']==8 and result['handAttackUpgraded'] and played['pile']=='Exhaust',played
            if key=='mage_elixir_of_life':assert played['hpChange']==6 and played['blockChange']==(12 if up else 8) and played['pile']=='Exhaust',played
            if key=='mage_arcane_conductor':assert result['firstDraw']==result['resetDraw']==1 and result['secondDraw']==0,result
            if key=='ranger_ancient_canopy':assert played['blockChange']==(18 if up else 14) and any(p['amount']==7 and 'BLOCK_NEXT_TURN' in p['id'] for p in played['powers']) and played['pile']=='Exhaust',played
            if key=='ranger_perfect_escape':assert any(p['amount']==1 and 'BUFFER' in p['id'] for p in played['powers']) and played['pile']=='Exhaust',played
            print(tag+': '+key+('+' if up else '')+' paid play/effects/save passed',flush=True)
    send('abandon')
finally:
    (root/'private/sts2-evidence'/f'ancient-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')

