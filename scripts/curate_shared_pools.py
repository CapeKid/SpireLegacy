"""Curate normal-sized overlapping libraries without deleting saved card IDs."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SHARED = {
    'COMMON': 'study mage_cold_read knight_reforge knight_brave_advance ranger_tripwire mage_stored_ember mage_crystal_seed knight_long_reach',
    'UNCOMMON': 'challenge knight_battle_meditation mage_spellweave mage_memory_prism knight_clean_armor knight_arms_vault mage_time_pocket mage_enchanted_ink knight_last_stand knight_burnished_legacy mage_secret_thesis ranger_measured_breath mage_woven_incantation ranger_tracking_rhythm mage_alchemist_s_patience knight_merciless',
    'RARE': 'meditate knight_guardian_angel mage_mirror_manuscript mage_comet_calendar ranger_master_plan ranger_perfect_timing',
}
UNIQUE = {
    'knight': {
        'BASIC': 'knight_strike knight_guard shield',
        'COMMON': 'riposte fortify knight_pommel_knock knight_sweeping_edge knight_twin_cut knight_tower_guard knight_iron_resolve knight_blood_price knight_shield_drill knight_splintered_pike knight_heavy_harness knight_grit_teeth',
        'UNCOMMON': 'knight_shield_slam knight_counter_lunge knight_executioner_axe knight_banner_charge knight_bloodied_blade knight_sword_dance knight_second_wind knight_double_guard knight_reinforce knight_bulwark knight_battle_rhythm knight_forged_in_pain knight_veteran_training knight_cast_off knight_forge_sparks knight_ashen_advance knight_salvage_guard knight_scar_tissue knight_furnace_breath knight_patient_riposte',
        'RARE': 'bastion knight_living_fortress knight_ancestral_fury knight_phoenix_forge knight_iron_avalanche knight_bloodfire_mantle knight_grand_melee knight_warlord_s_command knight_iron_furnace knight_siege_tomorrow',
    },
    'mage': {
        'BASIC': 'mage_strike mage_guard flame',
        'COMMON': 'fireball barrier mage_ember_dart mage_arcane_missile mage_witchfire mage_frost_ring mage_mana_shield mage_spell_notes mage_kindle mage_ash_reading mage_warding_verse mage_chilling_words',
        'UNCOMMON': 'reservoir frost mage_thunderchain mage_inferno_wave mage_venom_rune mage_mana_burn mage_rune_detonation mage_toxic_cloud mage_overcharge mage_rewrite mage_blood_to_mana mage_rune_storm mage_spell_threads mage_toxic_theory mage_arcane_meter mage_tower_library mage_moonstone_lance mage_geode_ward mage_rune_familiar mage_hex_rain',
        'RARE': 'nova surge mage_starfall mage_soulfire mage_perfect_ward mage_mana_fountain mage_transmutation mage_astral_mantle mage_sun_in_a_bottle mage_grand_incantation',
    },
    'ranger': {
        'BASIC': 'ranger_strike ranger_guard aim',
        'COMMON': 'double volley retreat ranger_barbed_arrow ranger_scattershot ranger_hidden_knife ranger_snap_shot ranger_poison_tip ranger_watch_the_wind ranger_loose_fletching ranger_pocket_wind ranger_sort_quiver',
        'UNCOMMON': 'quiver mark ranger_ambush ranger_rapid_fire ranger_razor_fletching ranger_finishing_flurry ranger_hooked_arrow ranger_ricochet ranger_reposition ranger_double_dose ranger_escape_route ranger_patient_hunter ranger_cruel_precision ranger_light_pack ranger_tailwind ranger_spring_nock ranger_venom_reserve ranger_perch_discipline ranger_pursuit_volley ranger_owl_watch',
        'RARE': 'snipe ranger_storm_of_arrows ranger_assassinate ranger_black_arrow ranger_poisoned_arsenal ranger_falcon_courier ranger_venom_eclipse ranger_long_vigil ranger_arrow_cascade ranger_forest_oracle',
    },
}

def main():
    cards = {c['id']: c for c in json.loads((ROOT/'sheets/cards.json').read_text())}
    rows = []
    for cls, groups in UNIQUE.items():
        for scope, mapping in [('shared', SHARED), ('unique', groups)]:
            for rarity, names in mapping.items():
                for key in names.split():
                    assert cards[key]['rarity'] == rarity, key
                    assert scope == 'shared' or cards[key]['classId'] == cls, key
                    rows.append(dict(id=cls+'__'+key, classId=cls, cardId=key, scope=scope))
    (ROOT/'sheets/card_pools.json').write_text(json.dumps(rows, indent=2)+'\n')
    targets = [dict(id=cls, reference='Owned Ironclad: 75 total; 30 shared and 45 unique', basic=3, common=20, uncommon=36, rare=16, total=75) for cls in UNIQUE]
    (ROOT/'sheets/pool_targets.json').write_text(json.dumps(targets, indent=2)+'\n')
    print('Curated', len(rows), 'memberships;', len({r['cardId'] for r in rows}), 'active cards')

if __name__ == '__main__':
    main()
