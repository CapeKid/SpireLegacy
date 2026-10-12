"""Bounded natural-run pilot. Launch a fresh isolated owned-game lab/profile first.

Usage: sts2-balance-campaign.py PROFILE TAG CLASS TIER POLICY [SEED] [ASCENSION] [TRAIT,...]
Each campaign needs its own fresh profile: inherited chronicle rewards would confound comparisons.
Every completed oracle response is journalled immediately, including failures and unsupported decisions.
"""
import hashlib
import json
from pathlib import Path
import sys
import time
from oracle_protocol import call

root = Path(__file__).resolve().parents[1]
profile = Path(sys.argv[1]).resolve()
tag, cls, tier, policy = sys.argv[2:6]
tier = int(tier)
assert cls in {'knight', 'mage', 'ranger'} and tier in range(3)
assert policy in {'defense', 'attack'}
seed = sys.argv[6] if len(sys.argv) > 6 else 'SPIRELEGACY-CAMPAIGN-1'
ascension = int(sys.argv[7]) if len(sys.argv) > 7 else 0
traits = sys.argv[8].split(',') if len(sys.argv) > 8 and sys.argv[8] else []
assert tag and all(c.isalnum() or c in '-_' for c in tag)
expected = hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output = root/'private/sts2-evidence'/f'balance-campaign-{tag}.json'
assert not output.exists(), 'Choose a new tag; never overwrite campaign evidence.'
evidence = []
report = dict(cls=cls, tier=tier, policy=policy, seed=seed, ascension=ascension, traits=traits,
              status='running', evidence=evidence)

def save():
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2), encoding='utf-8')

def send(command, **args):
    record = call(profile, dict(command=command, **args), timeout=180, expected_hash=expected)
    evidence.append(record)
    save()
    reply = record['response']
    assert reply['ok'], reply
    assert {m['id'] for m in reply['metadata']['mods']} == {'BaseLib', 'SpireLegacy'}
    return reply['result']

try:
    assert not (profile/'family.json').exists(), 'Campaign requires a fresh family profile.'
    send('start', **{'class': cls, 'manorTier': tier, 'quick': False,
                    'seed': seed, 'ascension': ascension, 'traits': traits})
    stalled = 0
    deadline = time.monotonic() + 1200
    for step in range(1200):
        if time.monotonic() > deadline:
            report['status'] = 'time-cap'
            break
        result = send('balance-campaign-step', policy=policy)
        action = result['action']
        report['lastState'] = result['state']
        print(tag, step, action, result['state']['act'], result['state']['floor'], result['state']['hp'], flush=True)
        if action == 'finished':
            report['status'] = 'finished'
            break
        if action.startswith('unsupported') or action in {'play-cap', 'turn-cap'}:
            report['status'] = action
            break
        stalled = stalled + 1 if action == 'wait' else 0
        if stalled >= 30:
            report['status'] = 'stalled'
            break
        time.sleep(.25)
    else:
        report['status'] = 'decision-cap'
except Exception as error:
    report.update(status='error', error=str(error))
    raise
finally:
    save()

print('Saved', output, report['status'], flush=True)
