"""Synthetic native reward/upgrade health-context calibration."""
import hashlib,json,sys
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile,tag=Path(sys.argv[1]).resolve(),sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'health-planning-{tag}.json';assert not output.exists()
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
    for cls,kind in [(c,'models') for c in ('knight','mage','ranger','ironclad')]+[('knight','reward-low'),('knight','reward-high')]:
        send('start',**{'class':cls,'manorTier':0,'quick':False,'traits':[],'seed':'HEALTHPLAN146','ascension':0})
        result=send('balance-health-planning-probe',kind=kind);report['cases'].append(result);save()
        if kind=='models':
            assert len(result['rows'])==dict(knight=20,mage=6,ranger=4,ironclad=8)[cls],result
            if cls=='knight':
                u=result['upgrade'];assert u['lowPicked']=='knight_guard' and u['highPicked']=='knight_blood_price',u
                assert u['guardUpgraded'] and u['paidUnchanged'],u
        else:
            picked='knight_measured_cut' if kind=='reward-low' else 'knight_bloodied_blade'
            assert result['successfullySelected'] and result['acquired']==[picked],result
            assert result['choice']['detail']['picked']==picked,result
        print(tag,cls,kind,'PASS',flush=True)
    report['status']='passed'
except Exception as error:
    report.update(status='error',error=str(error));raise
finally:save()
