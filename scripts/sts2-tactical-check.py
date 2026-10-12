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
for kind in ['vulnerable','exhaust','lethal','area']:
    send('start',**{'class':'knight','manorTier':0,'quick':True,'traits':[],'seed':'SPIRELEGACY-TACTICAL-CHECK'})
    deadline=time.monotonic()+30
    while send('state')['phase']!='Play':
        if time.monotonic()>deadline:raise TimeoutError('Tactical check play phase')
        time.sleep(.2)
    r=send('balance-tactical-probe',kind=kind)
    if kind=='vulnerable':assert r['vulnerable']==2 and r['followUpDamage']==7 and r['energySpent']==3,r
    elif kind=='exhaust':assert r['fuelExhausted'] and r['blockGained']==6,r
    elif kind=='lethal':assert r['targetDead'] and r['Reason']=='estimated-lethal',r
    else:assert len(r['damage'])>1 and all(d==3 for d in r['damage']),r
    print(tag,r,flush=True)
