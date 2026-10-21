"""Exercise three rare engines with paid plays in isolated owned beta labs."""
import json, os, sys, time
from pathlib import Path
from oracle_protocol import call

profile = Path(sys.argv[1])
records = []

def send(command, **args):
    record = call(profile, dict(command=command, **args), timeout=120,
                  expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256'])
    records.append(record)
    (profile/'rare-engines-receipts.json').write_text(json.dumps(records, indent=2))
    assert record['response']['ok'], record
    assert {m['id'] for m in record['response']['metadata']['mods']} == {'BaseLib', 'SpireLegacyBeta'}
    return record['response']['result']

cases = {
    'knight': {'mage_eternal_bookmark': ['retained']},
    'mage': {
        'mage_eternal_bookmark': ['empty', 'retained', 'measured', 'stacked', 'hand-cap', 'no-draw'],
        'mage_archmage_thesis': ['full', 'woven', 'burst-skills', 'double-tap', 'expire', 'next-turn', 'stacked'],
    },
    'ranger': {'ranger_hunt_without_end': ['zero', 'one', 'full', 'cap', 'replay', 'tracking', 'stacked', 'late-setup']},
}
send('trait-speed', value=8)
for cls, cards in cases.items():
    for up in [False, True]:
        send('start', **{'class': cls, 'traits': [], 'quick': True, 'manorTier': 0, 'seed': 'CARD-ROLES'})
        for _ in range(150):
            if send('state')['phase'] == 'Play':
                break
            time.sleep(.1)
        else:
            raise TimeoutError('Run did not reach Play')
        for key, modes in cards.items():
            for mode in modes:
                result = send('rare-engines', card=key, upgrade=up, mode=mode)
                print(cls, up, key, mode, result, 'passed', flush=True)
        send('abandon')
