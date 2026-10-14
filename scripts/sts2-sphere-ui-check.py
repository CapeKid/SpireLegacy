"""Controlled native event fixture, excluded from natural campaign/pacing exports."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]);tag=sys.argv[2]
target=root/'private/sts2-evidence'/f'sphere-ui-{tag}.json'
assert not target.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
report=dict(status='running',syntheticEvent=True,evidence=[])
def save():
    target.parent.mkdir(exist_ok=True)
    target.write_text(json.dumps(report,indent=2),encoding='utf-8')
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=180,expected_hash=expected)
    report['evidence'].append(record);save()
    assert record['response']['ok'],record
    return record['response']['result']
try:
    send('start',**{'class':'knight'},traits=[],seed='SPHERE146-UI',ascension=0,manorTier=0,quick=False)
    send('enter',room='Event',event='CrystalSphere')
    actions=[];waiting=None
    for _ in range(50):
        result=send('balance-campaign-step',policy='planned',rewardPolicy='defensive')
        action=result['action'];actions.append(action)
        assert not action.startswith('unsupported'),result
        if action=='wait':
            waiting=waiting or time.monotonic()
            assert time.monotonic()-waiting<30,'Sphere fixture stalled'
        else:waiting=None
        if 'sphere-proceed' in actions and action=='map':
            assert 'sphere-cell' in actions
            report.update(status='finished',actions=actions,cells=actions.count('sphere-cell'),exitAction=action)
            break
        time.sleep(.2)
    else:raise AssertionError('Sphere fixture decision cap')
except Exception as error:report.update(status='error',error=str(error));raise
finally:save()
