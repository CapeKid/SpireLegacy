"""Gameplay expectations for #55 native receipts, independent of runtime hook claims."""
def relic_effect(cls, r):
    assert r['roundTrip'] and r['counter'] in range(4), r
    key, up, plays = r['key'], r['up'], r['plays']
    blocks = [p['block'] for p in plays]
    draws = [p['draw'] for p in plays]
    guard = 6 if up else 4
    if key == 'heirloom_brooch': assert blocks == [9, 4] and draws == [0, 0], r
    elif key == 'training_dummy': assert blocks == [0, 0] and [p['vigor'] for p in plays] == [1, 0], r
    elif key == 'threadbound_ledger': assert draws == [1, 0], r
    elif key == 'wayfarer_ribbon': assert blocks == ([10, 7] if up else [7, 4]), r
    elif key == 'guardian_knot': assert blocks == [3, 0], r
    elif key == 'scholar_bookmark': assert blocks == [4, 4] and draws == [1, 0], r
    elif key == 'family_seal':
        assert blocks == [4, guard, 4, 4, guard, 0] and draws == [0, 0, 1, 0, 0, 0], r
        assert [p['counter'] for p in plays] == [1, 2, 3, 3, 3, 3], r
    elif key == 'triune_crest':
        if cls == 'knight': assert blocks == [guard + 4, guard], r
        elif cls == 'mage': assert blocks == [guard, guard] and [p['charges'] for p in plays] == [2, 2], r
        else: assert [p['vigor'] for p in plays] == [3, 0], r
    elif key == 'legacy_hourglass': assert r['turn'] == 3 and r['energy'] == 4 and r['draw'] == 6, r
    elif key == 'trail_compass': assert r['turn'] == 2 and r['vigor'] == 3 and r['energy'] == 3, r
    else: raise AssertionError(key)
    if key in {'heirloom_brooch', 'guardian_knot', 'scholar_bookmark', 'triune_crest', 'legacy_hourglass', 'trail_compass'}:
        assert r['CombatClaimed'] and r['counter'] == 0, r
    elif key != 'family_seal': assert r['TriggerTurn'] == r['turn'] and r['counter'] == 0, r

def potion_effect(cls, r):
    assert r['completed'] and r['hpChange'] == r['energyChange'] == 0, r
    block, draw = r['blockChange'], r['handChange']
    if r['key'] == 'borrowed_breath': assert (block, draw) == (6, 1), r
    elif r['key'] == 'briar_vial':
        assert (block, draw) == (0, 0), r
        powers = {p['id']: p['amount'] for p in r['enemyPowers']}
        assert powers['POWER.POISON_POWER'] == 4 and powers['POWER.WEAK_POWER'] == 1, r
    else:
        assert (block, draw) == {'knight': (8, 0), 'mage': (0, 1), 'ranger': (0, 2)}[cls], r
        powers = {p['id']: p['amount'] for p in r['powers']}
        if cls == 'mage': assert r['charges'] == 2, r
        else: assert powers['POWER.VIGOR_POWER'] == (2 if cls == 'knight' else 3), r
