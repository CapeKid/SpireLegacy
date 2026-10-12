"""Paid finisher effects with/without setup, no/full manor, on the owned native branches."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
assert profile.is_relative_to((root/'private').resolve())
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'finisher-payoffs-{tag}.json';assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
evidence=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record);output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']
for tier in [0,2]:
    for cls,key in [('knight','knight_royal_execution'),('mage','mage_celestial_spear'),('ranger','ranger_arrow_cascade')]:
        for upgrade in [False,True]:
            for supported in [False,True]:
                send('start',**{'class':cls,'manorTier':tier,'traits':[],'quick':True,'seed':'SPIRELEGACY-FINISHER-PAYOFF'})
                deadline=time.monotonic()+30
                while send('state')['phase']!='Play':
                    assert time.monotonic()<deadline,'Combat startup timeout'
                    time.sleep(.1)
                result=send('balance-probe',card=key,upgrade=upgrade,supported=supported)
                strength=2 if tier==2 else 0
                if cls=='knight':
                    damage=(26 if upgrade else 20)*(2 if supported else 1)+strength;cost=3
                elif cls=='mage':
                    damage=(34 if upgrade else 26)+strength+(2 if supported else 0);cost=3
                    assert result['charges']==(2 if supported else 0),result
                else:
                    damage=(8 if supported else 0)+strength;cost=1 if upgrade else 2
                    assert result['precedingAttacks']==(2 if supported else 0),result
                assert result['damage'] and result['damage'][0]==damage,result
                if cls=='ranger':assert all(d==damage for d in result['damage']),result
                assert result['energySpent']==cost and result['cost']==cost,result
                assert result['pile']==('Exhaust' if cls=='ranger' else 'Discard') and result['roundTrip'],result
                print(tag,tier,key,upgrade,supported,result,flush=True)
send('abandon')
