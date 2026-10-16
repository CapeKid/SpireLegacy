"""Synthetic Strength/Vigor ordering controls; not natural-run balance outcomes."""
import hashlib,json,sys
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profile,tag=Path(sys.argv[1]).resolve(),sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'setup-pilot-{tag}.json'
assert not output.exists(),'Never overwrite native evidence.'
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
report=dict(synthetic=True,status='running',cases=[],evidence=[])
def save():
    output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(report,indent=2),encoding='utf-8')
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    report['evidence'].append(record);save()
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']
try:
    for cls in ('knight','mage','ranger','ironclad'):
        for kind,sequence in [('safe',False),('safe',True),('energy',True),('lethal',True),('threat',True),('covered',True)]:
            send('start',**{'class':cls,'manorTier':0,'quick':False,'traits':[],
                            'seed':'SETUP146-CALIBRATION','ascension':0})
            result=send('balance-setup-probe',kind=kind,sequence=sequence)
            selected=sequence and kind in ('safe','covered')
            assert result['selectedSetup']==selected,result
            assert result['energySpent']<=result['energyBefore'],result
            if selected:
                assert result['Reason']=='setup-before-attack',result
                assert result['damage']>result['beforePreview'],result
                assert result['strength']>0 or result['vigor']>0,result
                assert result['vigorRemaining']==0,result
            elif kind=='lethal':assert result['targetDead'],result
            report['cases'].append(result);save()
            print(tag,cls,kind,sequence,'PASS',flush=True)
    report['status']='passed'
except Exception as error:
    report.update(status='error',error=str(error));raise
finally:save()
