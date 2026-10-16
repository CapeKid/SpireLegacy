"""Synthetic Pacifist calibration; never count injected fixture deaths as campaign outcomes."""
import hashlib
import json
from pathlib import Path
import sys
from oracle_protocol import call

root = Path(__file__).resolve().parents[1]
profile, tag = Path(sys.argv[1]).resolve(), sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output = root/'private/sts2-evidence'/f'pacifist-pilot-{tag}.json'
assert not output.exists(), 'Never overwrite native evidence.'
expected = hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
report = dict(synthetic=True, status='running', evidence=[], cases=[], violations=[])

def save():
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding='utf-8')

def send(command, **args):
    record = call(profile, dict(command=command, **args), timeout=90, expected_hash=expected)
    report['evidence'].append(record)
    save()
    reply = record['response']
    assert reply['ok'], reply
    assert {m['id'] for m in reply['metadata']['mods']} == {'BaseLib', 'SpireLegacy'}
    return reply['result']

try:
    for cls in ('knight', 'mage', 'ranger'):
        for mode in ('normal','mercy','upgraded'):
            send('start', **{'class':cls,'manorTier':0,'quick':False,'traits':[] if mode=='normal' else ['cantattack'],'seed':'PACIFIST146-CALIBRATION','ascension':5})
            result=send('balance-pacifist-probe',card=f'{cls}_strike',upgraded=mode=='upgraded')
            assert result['energySpent']==1,result
            assert result['pacifist']==(mode!='normal'),result
            if mode!='normal':
                assert result['poisonApplied']==(3 if mode=='upgraded' else 2) and result['damage']==0,result
                assert result['fuelExhausted'] and result['guarded'] and result['choseStarter'],result
            else:assert result['poisonApplied']==0 and result['damage']>0 and result['blockedUsable'],result
            report['cases'].append(dict(cls=cls,mode=mode,result=result))
            save()
            print(tag,cls,mode,'PASS',flush=True)
    report['status'] = 'passed' if not report['violations'] else 'failed'
except Exception as error:
    report.update(status='error', error=str(error))
    raise
finally:
    save()
assert not report['violations'], report['violations']
