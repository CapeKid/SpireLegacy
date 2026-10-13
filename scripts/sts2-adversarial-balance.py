"""#60 paid loop minimization, native benchmarks and fixed-deck pressure tests."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
output=root/'private/sts2-evidence'/('adversarial-balance-'+tag+'.json');assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=180,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2))
    assert r['response']['ok'],r
    assert {m['id'] for m in r['response']['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return r['response']['result']
def start(cls,tier=0,traits=[]):
    send('start',**{'class':cls,'manorTier':tier,'traits':traits,'quick':True,'seed':'SPIRELEGACY-AUDIT-60'})
    deadline=time.monotonic()+45
    while send('state')['phase']!='Play':
        assert time.monotonic()<deadline;time.sleep(.15)
send('presentation-prepare');send('balance-audit-references')
if 'draw-budget' in tag:
    for cls in ['knight'] if 'baseline' in tag else ['knight','mage','ranger']:
        for upgraded in [False,True]:
            for key in ['native','study','knight_battle_meditation']:
                start(cls);r=send('balance-audit-draw',card=key,upgrade=upgraded)
                draw=(2 if key=='study' or key=='knight_battle_meditation' and 'baseline' in tag else 3)+int(upgraded)
                discarded=0 if key=='study' else 1
                assert r['drawn']==draw and r['discarded']==discarded and r['hand']==draw-discarded,r
                assert r['energySpent']==r['cost']==1 and r['pile']=='Discard',r
                print(tag,cls,r,flush=True)
    send('abandon');raise SystemExit(0)
if 'native-recovery' in tag:
    for cls in ['knight','mage','ranger']:
        for mixed in [False,True]:
            start(cls);r=send('balance-audit-native-recovery',mixed=mixed)
            assert r['plays']==12 and r['energySpent']==0 and r['blockGained']==(30 if mixed else 60),r
            assert r['aPile']=='Hand' and r['bPile']=='Discard',r
            print(tag,cls,r,flush=True)
    send('abandon');raise SystemExit(0)
if 'exchange' in tag:
    rows={r['id']:r for r in json.loads((root/'sheets/cards.json').read_text())}
    for upgraded in [False,True]:
        start('ranger');control=send('balance-audit-exchange',card='native',upgrade=upgraded)
        for key in ['ranger_trail_reset','ranger_quiver_repack','ranger_empty_quiver']:
            start('ranger');r=send('balance-audit-exchange',card=key,upgrade=upgraded);d=rows[key]
            assert r['fuel']==control['fuel'],(control,r)
            assert r['energySpent']==d['cost']-int(upgraded and d['upgradeCost']),r
            assert r['blockGained']==control['blockGained']+d['block']+(d['upgradeBlock'] if upgraded else 0),r
            assert r['queuedBlock']==d['nextBlock']+(d['upgradeMagic'] if upgraded and d['nextBlock'] else 0),r
            assert r['pile']==('Exhaust' if d['exhaust'] else 'Discard'),r
            print(tag,key,upgraded,r,flush=True)
    send('abandon');raise SystemExit(0)
if 'controls' in tag:
    for cls in ['knight','mage','ranger']:
        for kind in ['discard','exhaust']:
            start(cls);r=send('balance-loop',kind=kind)
            assert not r['returnedRecovery'] and not r['returnedOriginal'] and r['energySpent']==0,r
        for upgraded in [False,True]:
            start(cls);r=send('balance-audit-native-loop',upgrade=upgraded)
            assert r['plays']==12 and r['energySpent']==r['exhausted']==0 and r['damage']>0,r
            print(tag,cls,upgraded,'native legal loop',r,flush=True)
    send('abandon');raise SystemExit(0)
if 'matrix' in tag:
    builds={'knight':['armor','exhaust','blood','strength'],'mage':['sequencing','mage-exhaust','mage-retained','delayed'],'ranger':['discard','poison','retention','chain']}
    risks={'knight':['vampire','superhealer'],'mage':['bonusmagicstrength','megahealth'],'ranger':['cantattack','nomeat']}
    members={(r['classId'],r['cardId']) for r in json.loads((root/'sheets/card_pools.json').read_text())}
    traits={r['id']:r for r in json.loads((root/'sheets/traits.json').read_text())}
    assert all(set(pair)<=traits.keys() for pair in risks.values()),'Use canonical trait IDs before starting trials'
    if 'matrix-ranger' in tag:builds={'ranger':builds['ranger']}
    for cls,choices in builds.items():
        for build in choices:
            for tier,risk in [(0,False),(2,False),(2,True)]:
                pair=risks[cls] if risk else []
                if pair:assert pair[1] not in traits[pair[0]]['excludes'] and pair[0] not in traits[pair[1]]['excludes']
                start(cls,tier,pair);r=send('balance-audit-encounter',build=build,item=risk)
                assert all((cls,c) in members for c in r['additions']),r
                assert not r['exhaustedBudget'],r
                print(tag,cls,build,tier,risk,'win',r['victory'],'death',r['deadPlayer'],'net HP loss',r['damageTaken'],'turns',r['endedTurns'],flush=True)
    send('abandon');raise SystemExit(0)
for cls in ['ranger'] if 'baseline' in tag else ['knight','mage','ranger']:
    for upgraded in [False,True]:
        for native in [False,True]:
            start(cls);r=send('balance-audit-reset',upgrade=upgraded,native=native)
            assert r['roundTrip'] and r['ownerIsolated'],r
            if 'baseline' not in tag or native:
                assert r['plays']==r['exhausted']==2,r
                assert r['blockGained']==4 and r['samples'][-1]['hand']==0,r
            else:assert r['plays']==(3 if not upgraded else 12) and r['exhausted']==0,r
            print(tag,cls,upgraded,native,r,flush=True)
send('abandon')
