"""All Legacy immediate HP payments and named native controls; synthetic only."""
import hashlib,json,sys
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profile,tag=Path(sys.argv[1]).resolve(),sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'hp-cost-pilot-{tag}.json'
assert not output.exists(),'Never overwrite native evidence.'
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
rows=json.loads((root/'sheets/cards.json').read_text(encoding='utf-8'))
cards=[(r['classId'],r['id']) for r in rows if r['hpLoss']>0]
cards += [('ironclad','native_'+key) for key in ('offering','bloodletting','hemokinesis','haunt')]
report=dict(synthetic=True,status='running',expectedCases=len(cards)*2,cases=[],evidence=[])
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
    for cls,key in cards:
        for upgraded in (False,True):
            send('start',**{'class':cls,'manorTier':0,'quick':False,'traits':[],
                            'seed':'HPCOST146-CALIBRATION','ascension':0})
            result=send('balance-hp-cost-probe',card=key,upgraded=upgraded)
            report['cases'].append(result);save()
            assert result['estimatedLoss']==result['expectedLoss'],result
            if result['expectedLoss']>0:
                assert not result['exactSelected'] and not result['underSelected'] and result['fallbackSelected'],result
            else:assert result['exactSelected'] and result['underSelected'],result
            assert result['safeSelected'] and result['alive'] and result['hpAfter']==1,result
            assert result['actualHpLost']==result['expectedLoss'] and result['blockBefore']>=50,result
            print(tag,key,upgraded,'PASS',flush=True)
    report['status']='passed'
except Exception as error:
    report.update(status='error',error=str(error));raise
finally:save()
