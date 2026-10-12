"""Native candidate probes and matched opening-encounter benchmarks; not campaign win rates."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=root/sys.argv[1];tag=sys.argv[2];after=sys.argv[3]=='after'
seeds=int(sys.argv[4]) if len(sys.argv)>4 else 3
evidence=[];benchmarks=[];started=False
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
bench_only=len(sys.argv)>5 and sys.argv[5]=='bench'
if bench_only and sys.argv[6]!='current':
    loaded=json.loads((profile.parent/'sts2-evidence'/f'balance-followup-{sys.argv[6]}.json').read_text())
    expected=loaded['evidence'][0]['response']['metadata']['dllSha256']
if len(sys.argv)>7:
    previous=json.loads((root/'private/sts2-evidence'/f'balance-followup-{sys.argv[7]}.json').read_text())
    benchmarks=previous['benchmarks'];evidence=previous['evidence']
completed={(r['cls'],r['build'],r['tier'],r['seedIndex']) for r in benchmarks}

def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=180,expected_hash=expected)
    evidence.append(record);reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'},reply['metadata']
    return reply['result']

def start(cls,tier=2,seed='SPIRELEGACY-BALANCE-1'):
    global started
    send('start',**{'class':cls,'manorTier':tier,'traits':[],'quick':True,'seed':seed})
    started=True
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('Playable balance encounter')

try:
    if not bench_only:
        start('mage');loop=send('balance-loop')
        assert loop['returnedRecovery']==(not after) and loop['returnedOriginal']==(not after) and loop['energySpent']==0,loop
        print(tag,'discounted recovery loop',loop,flush=True)
        if after:
            start('ranger');discard_loop=send('balance-loop',kind='discard')
            assert not discard_loop['returnedRecovery'] and not discard_loop['returnedOriginal'] and discard_loop['energySpent']==0,discard_loop
            print(tag,'discounted discard loop',discard_loop,flush=True)
    if after and not bench_only:
        retained=send('balance-retention');assert retained['checks']>0 and retained['saveRoundTrip'] and retained['newCombatReset'] and retained['upgradePreservesGrowth'],retained
        print(tag,'all retention cards',retained,flush=True)
    probe_cases=[('knight','knight_second_wind'),('mage','mage_rune_familiar'),('ranger','ranger_long_vigil'),('ranger','ranger_poison_tip'),('mage','mage_toxic_cloud')]+([('mage','mage_rekindle')] if after else [])
    for cls,card in [] if bench_only else probe_cases:
        for upgraded in [False,True]:
            start(cls);r=send('balance-probe',card=card,upgrade=upgraded)
            if card=='knight_second_wind':
                assert r['ordinaryBlock']==(13 if upgraded else 11) and r['fuelExhausted']==(1 if after else 2),r
                assert r['engineBlock']==r['ordinaryBlock']+(2 if after else 4),r
            elif card=='mage_rune_familiar':
                draw=2 if after and upgraded else 1
                assert r['firstDraw']==draw and r['secondDraw']==draw,r
                assert r['cost']==(1 if after or upgraded else 2),r
            elif card=='ranger_long_vigil':
                initial=(19 if after else 17) if upgraded else 15;growth=7 if upgraded else 6
                assert r['values']==[initial,initial+growth,initial+2*growth] and r['roundTrip']==after,r
                assert r['energySpent']==(2 if after else 3) and r['damage']==initial+2*growth+2,r
            elif card in ['ranger_poison_tip','mage_toxic_cloud']:
                amount=(6 if upgraded else 4) if after else (4 if upgraded else 3)
                assert r['initial'][0]==amount and r['twoTurnDamage'][0]==2*amount-1,r
            else:assert r['returned'] and r['pile']=='Exhaust',r
            print(tag,card,upgraded,r,flush=True)
    for cls,build in [('knight','exhaust'),('mage','powers'),('ranger','retention'),('ranger','poison')]:
        for tier in [0,1,2]:
            for seed in range(1,seeds+1):
                if (cls,build,tier,seed) in completed:continue
                start(cls,tier,f'SPIRELEGACY-BALANCE-{seed}')
                r=send('balance-encounter',build=build)
                assert not r['exhaustedBudget'],r
                benchmarks.append(dict(cls=cls,tier=tier,seedIndex=seed,**r))
                print(tag,cls,build,tier,seed,'win',r['victory'],'HP lost',r['damageTaken'],'turns',r['endedTurns'],flush=True)
    if started:send('abandon')
finally:
    (root/'private/sts2-evidence'/f'balance-followup-{tag}.json').write_text(json.dumps(dict(evidence=evidence,benchmarks=benchmarks),indent=2),encoding='utf-8')
