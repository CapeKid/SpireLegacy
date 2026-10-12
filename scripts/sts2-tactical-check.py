"""Paid native tactical-policy fixtures; durable targets are fixture setup, not campaign evidence."""
import hashlib
import json
from pathlib import Path
import sys
import time
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]).resolve()
tag=sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'tactical-pilot-{tag}.json'
assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
evidence=[]
def send(command,**args):
    result=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(result)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=result['response']
    assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']
for kind in ['vulnerable','exhaust','lethal','area','execute','execute-upgraded','guarded','guarded-upgraded','marked','marked-upgraded','retained','retained-upgraded']:
    send('start',**{'class':'knight','manorTier':0,'quick':True,'traits':[],'seed':'SPIRELEGACY-TACTICAL-CHECK'})
    deadline=time.monotonic()+30
    while send('state')['phase']!='Play':
        if time.monotonic()>deadline:raise TimeoutError('Tactical check play phase')
        time.sleep(.2)
    r=send('balance-tactical-probe',kind=kind)
    if kind=='vulnerable':assert r['vulnerable']==2 and r['followUpDamage']==7 and r['energySpent']==3,r
    elif kind=='exhaust':assert r['fuelExhausted'] and r['blockGained']==6,r
    elif kind=='lethal':assert r['targetDead'] and r['Reason']=='estimated-lethal',r
    elif kind=='area':assert len(r['damage'])>1 and all(d==3 for d in r['damage']),r
    else:
        assert r['targetDead'] and r['damage']==r['expected'] and r['Reason']=='estimated-lethal',r
        assert r['rewardSpecial'] in {'execute','guard_bonus','marked_bonus','retain_damage'},r
    print(tag,r,flush=True)
for kind in ['bash','shrug','repeat','skip','lethal-cost','avoid-offering','avoid-bloodletting','avoid-hemokinesis','safe-offering','only-fatal']:
    send('start',**{'class':'ironclad','manorTier':0,'quick':False,'traits':[],'seed':'SPIRELEGACY-CONTROL-CHECK'})
    r=send('balance-control-probe',kind=kind)
    if kind=='bash':assert r['damage']==9 and r['vulnerable']==2,r
    elif kind=='shrug':assert r['block']==8 and r['draw']==1 and r['handCount']==r['previousCount'],r
    elif kind=='repeat':assert r['damage']==10 and r['hits']==2,r
    elif kind=='skip':assert r['unchangedDeck'] and not r['successfullySelected'] and r['closed'],r
    elif kind=='lethal-cost':assert r['dead'] and r['gameOver'],r
    elif kind=='only-fatal':assert r['declined'] and r['hp']==6,r
    elif kind=='safe-offering':assert r['alive'] and r['paidSelected'] and r['hpLost']==6,r
    else:assert r['alive'] and not r['paidSelected'] and r['hpLost']==0,r
    print(tag,r,flush=True)
