"""Bounded native delayed-effect checks; fixture setup is not natural-run evidence."""
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
output=root/'private/sts2-evidence'/f'delayed-reward-{tag}.json'
assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
evidence=[]

def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record)
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=record['response']
    assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']

for effect in ['energy','draw','block','spellweave']:
    for upgraded in [False,True]:
        kind=f'delayed-{effect}'+('-upgraded' if upgraded else '')
        send('start',**{'class':'mage' if effect=='spellweave' else 'knight','manorTier':0,'quick':True,'traits':[],'seed':'SPIRELEGACY-DELAYED-CHECK'})
        deadline=time.monotonic()+30
        while send('state')['phase']!='Play':
            if time.monotonic()>deadline:raise TimeoutError('Delayed check play phase')
            time.sleep(.2)
        r=send('balance-tactical-probe',kind=kind)
        assert r['immediateEnergy']==2,r
        if effect=='spellweave':
            assert r['immediateBlock']==(2 if upgraded else 0),r
            assert r['firstEnergy']==r['secondEnergy']==4 and r['firstHand']==r['secondHand']==7,r
            assert r['replaySpent']==1 and r['queuedEnergy']==r['queuedDraw']==0 and r['roundTrip'] and r['pile']=='Discard',r
            print(tag,r,flush=True)
            continue
        if effect=='energy':assert r['chosen'] and r['NextEnergy']==1 and r['deliveredEnergy']==4,r
        elif effect=='draw':assert r['NextDraw']==(3 if upgraded else 2) and r['deliveredHand']==5+r['NextDraw'],r
        else:assert r['NextBlock']==(6 if upgraded else 5) and r['deliveredBlock']==r['NextBlock'],r
        print(tag,r,flush=True)
