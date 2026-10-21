"""Paid native play of the eight under-tuning corrections, using a full-manor isolated lab."""
import hashlib, json, sys, time
from pathlib import Path
from oracle_protocol import call

root = Path(__file__).resolve().parents[1]
profile = root / sys.argv[1]
tag = sys.argv[2]
evidence = []
expected = hashlib.sha256((root / 'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()

def send(command, **args):
    record = call(profile, dict(command=command, **args), expected_hash=expected)
    evidence.append(record)
    reply = record['response']
    assert reply['ok'], reply
    assert {m['id'] for m in reply['metadata']['mods']} == {'BaseLib', 'SpireLegacy'}
    return reply['result']

def ready():
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        state = send('state')
        if state['phase'] == 'Play': return state
        time.sleep(.1)
    raise TimeoutError('Playable combat')

cases = [(cls, card) for cls in ['knight', 'mage', 'ranger']
         for card in ['knight_arms_vault', 'knight_battle_meditation']]
cases += [('ranger', 'ranger_second_quiver'), ('ranger', 'volley')]
cases += [('mage', card) for card in ['mage_returned_prophecy', 'mage_ash_reading', 'mage_geode_ward', 'mage_grand_thesis']]
try:
    for cls, card in cases[int(sys.argv[3]) if len(sys.argv) > 3 else 0:]:
        for upgrade in [False, True]:
            send('start', **{'class': cls, 'traits': [], 'quick': True})
            state = ready()
            if card == 'volley':
                # Weak Nibbits can have only 11 HP: measure on a durable target,
                # so overkill truncation cannot masquerade as missing hits.
                send('kill')
                send('enter', room='Boss')
                state = ready()
                assert state['enemies'][0]['hp'] > 24, state
            powers = {p['id'].lower(): p['amount'] for p in state['powers']}
            assert any('strength' in key and amount == 2 for key, amount in powers.items()), state
            assert any('dexterity' in key and amount == 2 for key, amount in powers.items()), state
            played = send('expansion-play', card=card, upgrade=upgrade)
            assert played['pile'] == ('Exhaust' if card in ['knight_arms_vault', 'ranger_second_quiver', 'mage_returned_prophecy', 'mage_ash_reading', 'mage_grand_thesis'] else 'Discard'), played
            assert not played['tableNodePresent'] and played['saveRoundTrip'], played
            assert played['hpChange'] == 0, played
            hand_before = len(played['costsBefore'])
            if card == 'knight_arms_vault':
                assert played['fuelReturned'] and played['cost'] == 1, played
                assert len(played['hand']) == hand_before + 2, played
            elif card == 'mage_returned_prophecy':
                assert played['fuelReturned'] and played['cost'] == 1, played
                assert len(played['hand']) == hand_before + 3 + int(upgrade), played
            elif card == 'knight_battle_meditation':
                assert len(played['hand']) == hand_before + 1 + int(upgrade), played
            elif card == 'ranger_second_quiver':
                assert played['cost'] == 0 and played['energyChange'] == 0, played
                assert len(played['hand']) == hand_before, played # Only a Skill is supplied in discard here.
            elif card == 'volley':
                after = send('state')
                blocked = state['enemies'][0]['block'] - after['enemies'][0]['block']
                assert played['enemyDamage'] + blocked == (18 if upgrade else 15) + 6, (state, played, after)
            elif card == 'mage_geode_ward':
                assert played['blockChange'] == (15 if upgrade else 12), played
            elif card == 'mage_ash_reading':
                assert played['cost'] == 0 and len(played['hand']) == hand_before + int(upgrade), played
            elif card == 'mage_grand_thesis':
                assert played['cost'] == 1 and played['charges'] == 2, played
                assert len(played['hand']) == hand_before + 3 + int(upgrade), played
            if card != 'ranger_second_quiver':
                assert played['energyChange'] == -played['cost'], played
            print(tag, cls, card, upgrade, 'paid effect, cleanup and round-trip passed', flush=True)
    send('abandon')
finally:
    (root / 'private/sts2-evidence' / f'undertuning-{tag}.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
