"""Native regression for balance audit fixes; requires an opt-in private rendered lab."""
import json, sys, time
from pathlib import Path

root=Path(__file__).resolve().parents[1]; profile=(root/sys.argv[1]).resolve(); tag=sys.argv[2]; evidence=[]
assert profile.is_relative_to((root/'private').resolve())
def send(command, **args):
    out=profile/'response.json'
    if out.exists(): out.unlink()
    assert not (profile/'request.json').exists()
    pending=profile/'request.pending'; pending.write_text(json.dumps(dict(command=command,**args)),encoding='utf-8'); pending.replace(profile/'request.json')
    deadline=time.monotonic()+40
    while not out.exists():
        if time.monotonic()>deadline: raise TimeoutError(command)
        time.sleep(.05)
    r=json.loads(out.read_text(encoding='utf-8')); assert r['ok'],r
    evidence.append(dict(command=command,args=args,result=r['result'])); return r['result']
def ready(turn=1):
    for _ in range(150):
        s=send('state')
        if s['phase']=='Play' and s['turn']>=turn:return s
        time.sleep(.1)
    raise TimeoutError('combat')
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True});return ready()
def power(s,key):return sum(p['amount'] for p in s['powers'] if key.lower() in p['id'].lower())
try:
    for cls,guard,attack,charges in [('knight','knight_guard','knight_sword_dance',2),('mage','mage_guard','mage_thunderchain',2),('ranger',None,'ranger_rapid_fire',0)]:
        start(cls);send('kill');send('enter',room='Boss');ready()
        if guard:
            send('play',card=guard)
            if cls=='mage':send('play',card=guard)
        before=send('state');after=send('play',card=attack,upgrade=True)
        enemyBefore=before['enemies'][0];enemyAfter=after['enemies'][0]
        lost=enemyBefore['hp']+enemyBefore['block']-enemyAfter['hp']-enemyAfter['block']
        expected=12+4*power(before,'strength')+charges
        assert lost==expected,(cls,lost,expected,before,after)
    for card,energy,draw in [('ranger_pocket_wind',2,0),('ranger_spring_nock',0,3),('ranger_loose_fletching',0,2)]:
        start('ranger');reward=send('discard-balance',card=card)
        assert reward['firstEnergyReward']==energy and reward['firstDrawReward']==draw,reward
        assert reward['secondEnergyReward']==reward['secondDrawReward']==0,reward
        assert reward['rewardTurn']==reward['turn'],reward
    start('mage');pocket=send('play',card='mage_time_pocket',upgrade=True)
    assert power(pocket,'equilibrium')==2,pocket
    send('turn');assert power(ready(2),'equilibrium')==1
    send('turn');assert power(ready(3),'equilibrium')==0
    start('mage');send('play',card='mage_astral_mantle',upgrade=True)
    before=send('state');after=send('play',card='mage_guard')
    assert power(after,'afterimage')==1,after
    assert after['block']-before['block']==4+power(before,'dexterity')+1,(before,after)
    for card in ['mage_kindle','mage_overcharge']:
        start('mage');base=send('play',card=card);assert base['powers'],base
        start('mage');upgraded=send('play',card=card,upgrade=True)
        if card=='mage_kindle':assert upgraded['block']==base['block']+2,(base,upgraded)
        else:assert len(upgraded['hand'])==len(base['hand'])+1,(base,upgraded)
    start('mage');cards=send('cards');assert cards['count']==720,cards['count']
    send('abandon')
    print(tag+': multi-hit class bonuses once per target; saved discard rewards bounded; two retention turns; fixed Mantle Block; meaningful charge upgrades; 720 native base/upgraded card checks.')
finally:
    (root/'private/sts2-evidence'/f'balance114-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
