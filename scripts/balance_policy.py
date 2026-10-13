"""Targeted balance overrides shared by authoring regeneration and release updates."""
CARD_OVERRIDES = {
    'ranger_trail_reset': dict(exhaust=True),
    'mage_slow_incantation': dict(upgradeCost=False, upgradeBlock=2),
    'mage_mana_fountain': dict(energy=2, upgradeMagic=1),
    'mage_eternal_bookmark': dict(cost=1, upgradeCost=False, upgradeMagic=1),
    'knight_battlefield_recovery': dict(cost=1),
    'knight_iron_avalanche': dict(cost=2),
    'mage_spellweave': dict(upgradeCost=False, upgradeBlock=2),
    'knight_royal_execution': dict(damage=20, upgradeDamage=6),
    'mage_celestial_spear': dict(damage=26, upgradeDamage=8),
    'ranger_arrow_cascade': dict(specialAmount=4),
    'knight_second_wind': dict(specialAmount=1),
    'mage_rune_familiar': dict(cost=1, magic=1, upgradeMagic=1, upgradeCost=False),
    'ranger_long_vigil': dict(cost=2, upgradeDamage=4),
    'ranger_poison_tip': dict(poison=4, upgradeMagic=2),
    'mage_toxic_cloud': dict(poison=4, upgradeMagic=2),
    'mage_rekindle': dict(specialAmount=1),
    'knight_arms_vault': dict(specialAmount=1),
    'ranger_second_quiver': dict(hpLoss=0, draw=2, upgradeDraw=0),
    'volley': dict(damage=5, upgradeDamage=1),
    'mage_returned_prophecy': dict(cost=1, draw=2, upgradeMagic=1),
    'mage_ash_reading': dict(cost=0, hpLoss=0, draw=1, upgradeMagic=1, exhaust=True, upgradeCost=False),
    'mage_geode_ward': dict(block=10, upgradeBlock=3),
    'knight_battle_meditation': dict(upgradeDiscard=0),
    'mage_grand_thesis': dict(cost=1, upgradeSpecial=0),
    'mage_perfect_ward': dict(block=3, upgradeBlock=2, magic=1, upgradeMagic=1),
    'ranger_perfect_timing': dict(cost=3, magic=1, upgradeMagic=0, upgradeCost=True),
    'ranger_tripwire': dict(upgradeMagic=0),
    'knight_guardian_angel': dict(cost=2, magic=1, upgradeMagic=0, upgradeCost=True),
    'mage_astral_mantle': dict(cost=2, magic=1, upgradeMagic=0, upgradeCost=True),
    'mage_blood_to_mana': dict(upgradeMagic=1),
    'ranger_escape_route': dict(upgradeMagic=1),
    'mage_kindle': dict(block=2, upgradeBlock=2, upgradeSpecial=0),
    'mage_overcharge': dict(draw=1, upgradeMagic=1, upgradeSpecial=0, exhaust=True),
}

def balance_card(row):
    row.setdefault('upgradeDraw', -1)
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
        row['summary'] = 'Playing a Skill primes your next Attack for +2 damage on every hit. Attacking consumes the counter.'
    if row['id'] == 'mage':
        row['summary'] = 'Each Skill grants one charge, up to two. Your next Attack gains +1 damage per charge on every hit and consumes them.'
