"""Targeted balance overrides shared by authoring regeneration and release updates."""
CARD_OVERRIDES = {
    'mage_perfect_ward': dict(block=3, upgradeBlock=2, magic=1, upgradeMagic=1),
    'ranger_perfect_timing': dict(cost=3, magic=1, upgradeMagic=0, upgradeCost=True),
    'ranger_tripwire': dict(upgradeMagic=0),
    'knight_guardian_angel': dict(cost=2, magic=1, upgradeMagic=0, upgradeCost=True),
    'mage_astral_mantle': dict(cost=2, magic=1, upgradeMagic=0, upgradeCost=True),
    'mage_blood_to_mana': dict(upgradeMagic=1),
    'mage_mana_fountain': dict(upgradeMagic=1),
    'ranger_escape_route': dict(upgradeMagic=1),
    'mage_kindle': dict(block=2, upgradeBlock=2, upgradeSpecial=0),
    'mage_overcharge': dict(draw=1, upgradeMagic=1, upgradeSpecial=0, exhaust=True),
}

def balance_card(row):
    if row['hits'] > 1:
        row['upgradeDamage'] = min(row['upgradeDamage'], 1)
    row.update(CARD_OVERRIDES.get(row['id'], {}))

def balance_manor(row):
    if row['id'] in ('smith', 'armory', 'garden'):
        row['maxLevel'] = 2
    if row['id'] == 'vault':
        row['summary'] = '+5% legacy earnings per level, applied after the trait bonus cap.'

def class_rules(row):
    if row['id'] == 'knight':
        row['summary'] = 'Playing a Skill primes your next Attack for +2 damage on its first hit to each enemy. Attacking consumes the counter.'
    if row['id'] == 'mage':
        row['summary'] = 'Each Skill grants one charge, up to two. Your next Attack gains +1 damage per charge on its first hit to each enemy and consumes them.'
