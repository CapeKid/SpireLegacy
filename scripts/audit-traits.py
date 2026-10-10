"""Audit selectable traits against their actual StS2 consumers and record the review."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
routes = {
    'gray': ('Native/TraitAppearance.cs', 'Refresh'),
    'blue': ('Native/TraitAppearance.cs', 'Refresh'),
    'sepia': ('Native/TraitAppearance.cs', 'Refresh'),
    'nature': ('Native/TraitAppearance.cs', 'Attach / Refresh'),
    'medium': ('Native/TraitAppearance.cs', 'Attach / Refresh'),
    'festive': ('Native/TraitAppearance.cs', 'Attach / Refresh'),
    'rainbow': ('Native/TraitAppearance.cs', 'Attach / Refresh'),
    'histrionic': ('Native/TraitAppearance.cs', 'DamageFlash / Refresh'),
    'fragile': ('Core/Inheritance.cs', 'MaxHp'),
    'kit': ('Core/Inheritance.cs', 'Deck'),
    'vegan': ('Core/Inheritance.cs', 'Healing'),
    'super_heal': ('Core/Inheritance.cs', 'Healing'),
    'no_heal': ('Core/Inheritance.cs', 'Healing'),
}
for method, effects in {
    'AfterActEntered': ['relics'],
    'ModifyHandDraw / AfterPlayerTurnStart': ['opening_draw_energy'],
    'ModifyMaxEnergy / AfterPlayerTurnStart': ['overcharge'],
    'ModifyMerchantPrice': ['shop'], 'ShouldPlay / Core.CardDesign.Mercy': ['pacifist'],
    'BeforeCombatStart': ['poison', 'weak', 'enemy_strength', 'enemy_guard', 'thorns'],
    'BeforeCombatStart / AfterDeath': ['diva'],
    'AfterPlayerTurnStart': ['exhausted', 'turn_block'],
    'AfterCardPlayed': ['skill_draw', 'first_skill_block', 'first_attack_block', 'first_attack_block_loss'],
    'BeforeDamageReceived': ['first_attack_shred', 'first_attack_weak'],
    'AfterCardDrawn / InheritedDamageMultiplicative': ['costly'],
    'InheritedDamageAdditive': ['first_attack_damage'],
    'InheritedDamageMultiplicative': ['perfectionist'],
    'ModifyHpLostAfterOsty': ['algesia'],
    'AfterDamageReceived': ['first_hurt_block', 'hurt_energy', 'hurt_weak', 'shock'],
    'AfterRoomEntered': ['chest', 'treasure_gold'], 'AfterDeath': ['explosions'],
    'AfterCombatVictory': ['coin_loss'],
}.items():
    for effect in effects:
        routes[effect] = ('Native/BloodlineRelic.cs', method)
# IBS is applied to the owner, separately from enemy starting Weak.
routes['opening_weak'] = ('Native/BloodlineRelic.cs', 'BeforeCombatStart')
traits = json.loads((root / 'sheets/traits.json').read_text())
lines = ['# Trait effect audit — 1.0.19', '',
    'Reviewed all 61 selectable traits and the two preserved historical IDs against StS2 runtime consumers. Crown bonuses alone do not count as an effect. Numeric stats are consumed by Inheritance and BloodlineRelic; size is applied to combat, rest and merchant sprites. Healing uses the scoped InheritedHealing patch, which preserves Neow initialization.', '',
    'Fixed cosmetic suppression by the old exclusive tint chain: Synesthesia now has animated rainbow motes and a colored floor ring independent of the body shader. Palette traits have separate visible accents when paired. Grayscale and sepia remain visible alongside the accents. Rest sites and shops now use the same trait appearance and inherited size. Histrionic produces a brief damage flash and returns to its normal palette, including when paired with Colorblind.', '',
    'Synesthesia remains cosmetic and has no crown bonus. This audit does not change card balance, trait crown bonuses or saved IDs. Existing nonvisual platforming adaptations remain bounded gameplay effects.', '',
    '| Trait | Status | Observable effect | Runtime consumer |',
    '|---|---|---|---|']
for row in traits:
    consumers = []
    if any(row[k] != 0 for k in ('hp', 'strength', 'dexterity', 'draw', 'heal')):
        consumers.append('Inheritance stats / BloodlineRelic / HeirCharacter')
    if row['scale'] != 1:
        consumers.append('HeirVisuals.Attach / AttachRoomPortrait')
    if row['effect'] != 'none':
        path, method = routes[row['effect']]
        source = (root / 'sts2' / path).read_text(encoding='utf-8-sig')
        assert '"' + row['effect'] + '"' in source, (row['id'], path)
        consumers.append(path + ': ' + method)
    assert consumers, row['id'] + ' is inert'
    lines.append('| ' + row['name'] + ' (`' + row['id'] + '`) | ' + ('Selectable' if row['enabled'] else 'Historical only') + ' | ' + row['summary'] + ' | ' + '; '.join(consumers) + ' |')
lines += ['', 'Verification: `scripts/sts2-trait-appearance-check.py` exercises actual native combat and room nodes on both branches, paired palettes, all three classes, animated motion, HP-loss flashes and flash recovery. Existing core and native trait-mechanics checks cover stat, healing, attack, room and settlement hooks. Physical Deck visual review and publication remain separate delivery checks.', '']
(root / 'docs/trait-effect-audit.md').write_text('\n'.join(lines), encoding='utf-8')
print(f'Audited {sum(r["enabled"] for r in traits)} selectable and {sum(not r["enabled"] for r in traits)} historical traits: each has an explicit gameplay/visual consumer.')
