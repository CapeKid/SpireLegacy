"""Synthetic threat calibration; never count injected fixture deaths as campaign outcomes."""
import hashlib
import json
from pathlib import Path
import sys
from oracle_protocol import call

root = Path(__file__).resolve().parents[1]
profile, tag = Path(sys.argv[1]).resolve(), sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output = root/'private/sts2-evidence'/f'threat-pilot-{tag}.json'
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
    for cls in ('knight', 'mage', 'ranger', 'ironclad'):
        for kind in ('threat', 'covered', 'nonlethal'):
            send('start', **{'class': cls, 'manorTier': 0, 'quick': False, 'traits': [],
                            'seed': 'THREAT146-CALIBRATION', 'ascension': 10})
            result = send('balance-threat-probe', card=f'{cls}_strike' if cls != 'ironclad' else cls, kind=kind)
            assert result['incoming'] > 0 and result['energySpent'] == 1, result
            assert result['attackerDead'] != result['harmlessDead'], result
            expected_attacker = kind == 'threat'
            passed = result['chosenAttacker'] == expected_attacker
            report['cases'].append(dict(cls=cls, kind=kind, passed=passed, result=result))
            if not passed: report['violations'].append(f'{cls}/{kind}')
            save()
            print(tag, cls, kind, 'PASS' if passed else 'FAIL', flush=True)
    report['status'] = 'passed' if not report['violations'] else 'failed'
except Exception as error:
    report.update(status='error', error=str(error))
    raise
finally:
    save()
assert not report['violations'], report['violations']
