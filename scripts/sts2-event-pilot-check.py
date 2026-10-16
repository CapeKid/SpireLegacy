"""Bounded synthetic native event-policy controls, not balance campaigns."""
import hashlib,json,sys
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile,tag=Path(sys.argv[1]).resolve(),sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'event-policy-{tag}.json';assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
report=dict(synthetic=True,status='running',cases=[],evidence=[])
def save():
    output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(report,indent=2),encoding='utf-8')
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    report['evidence'].append(record);save();reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']
try:
    for kind in ('healthy','injured','upgraded','repeat','late'):
        send('start',**{'class':'knight','manorTier':0,'quick':False,'traits':[],'seed':'EVENT146','ascension':0})
        result=send('balance-event-probe',kind=kind);report['cases'].append(result);save()
        assert result['genericSafeControl'] and result['allFatalRejected'],result
        assert result['finished']==(kind!='healthy'),result
        print(tag,kind,'PASS',flush=True)
    report['status']='passed'
except Exception as error:
    report.update(status='error',error=str(error));raise
finally:save()
