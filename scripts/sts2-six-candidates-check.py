"""Paid effects for the six reviewed cards, with no/full manor and both upgrades."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
assert profile.is_relative_to((root/'private').resolve())
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'six-candidates-{tag}.json';assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();evidence=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record);output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']
cards=[('mage','mage_slow_incantation'),('ranger','ranger_second_quiver'),('mage','mage_mana_fountain'),('mage','mage_eternal_bookmark'),('knight','knight_battlefield_recovery'),('knight','knight_iron_avalanche')]
for tier in [0,2]:
    for cls,key in cards:
        for upgraded in [False,True]:
            for supported in ([False,True] if key=='knight_iron_avalanche' else [False]):
                send('start',**{'class':cls,'manorTier':tier,'traits':[],'quick':True,'seed':'SPIRELEGACY-SIX-CANDIDATES'})
                deadline=time.monotonic()+30
                while send('state')['phase']!='Play':
                    assert time.monotonic()<deadline,'Startup timeout';time.sleep(.1)
                r=send('balance-probe',card=key,upgrade=upgraded,supported=supported)
                assert r['roundTrip'],r
                dex=strength=2 if tier==2 else 0;library=1 if tier==2 else 0
                if key=='mage_slow_incantation':
                    assert r['energyDelta']==-1 and r['blockDelta']==(2+dex if upgraded else 0),r
                    assert r['queuedEnergy']==r['queuedDraw']==1 and r['queuedBlock']==3+dex,r
                    assert r['firstEnergy']==r['secondEnergy']==4 and r['firstHand']==r['secondHand']==6+library,r
                    assert r['firstBlock']==r['secondBlock']==3+dex and r['replaySpent']==1 and r['pile']=='Discard',r
                elif key=='ranger_second_quiver':assert r['drawn']==0 and r['energyDelta']==0 and r['pile']=='Exhaust',r # No discarded Attacks in this fixture; the dedicated reload driver checks recovery.
                elif key=='mage_mana_fountain':assert r['energyDelta']==2+int(upgraded) and r['drawn']==0 and r['pile']=='Exhaust' and r['healing']==0,r
                elif key=='mage_eternal_bookmark':assert r['energyDelta']==(-1 if upgraded else -2) and r['amount']==2 and r['retained']==0 and r['automaticRetained'] and r['handAfterTurn']>=8,r
                elif key=='knight_battlefield_recovery':assert r['energyDelta']==-1 and r['drawn']==2+int(upgraded) and r['healing']==4+int(upgraded) and r['pile']=='Exhaust',r
                else:
                    damage=(8 if upgraded else 6)+strength+(20 if supported else 0)
                    assert r['energyDelta']==-2 and r['setupSpent']==int(supported) and r['pile']=='Exhaust' and r['damage'] and all(d==damage for d in r['damage']),r
                print(tag,tier,key,upgraded,supported,r,flush=True)
send('abandon')
