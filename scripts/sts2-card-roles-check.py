"""Exercise nine distinct card roles with paid plays in isolated owned beta labs."""
import json, os, sys, time
from pathlib import Path
from oracle_protocol import call

profile = Path(sys.argv[1])
records = []

def send(command, **args):
    record = call(profile, dict(command=command, **args), timeout=120,
                  expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256'])
    records.append(record)
    (profile/'card-roles-receipts.json').write_text(json.dumps(records, indent=2))
    assert record['response']['ok'], record
    assert {m['id'] for m in record['response']['metadata']['mods']} == {'BaseLib', 'SpireLegacyBeta'}
    return record['response']['result']

cases = {
    'knight': {
        'knight_forge_memory': ['full'],
        'knight_phoenix_forge': ['full'],
        'knight_iron_furnace': ['full', 'combined'],
        'knight_ancestral_salvage': ['full', 'cancel', 'skills'],
    },
    'mage': {
        'mage_cleanse_sigil': ['full'],
        'nova': ['zero', 'one', 'full', 'reservoir', 'replay'],
        'mage_moonstone_lance': ['zero', 'one', 'full', 'cap', 'reset'],
        'mage_returned_prophecy': ['full', 'empty', 'cancel', 'hand-cap', 'replay'],
    },
    'ranger': {'ranger_ghost_luggage': ['full']},
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
                result = send('card-roles', card=key, upgrade=up, mode=mode)
                print(cls, up, key, mode, result, 'passed', flush=True)
        send('abandon')
