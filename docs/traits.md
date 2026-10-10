Spire Legacy adapts the trait library from the locally installed Rogue Legacy 2 build 13303339 to turn-based card play. This is a gameplay adaptation: HP, Energy, Block, status effects and legacy crowns replace platforming and mana mechanics. The in-game tooltip describes the actual effect. Health and card information remain readable; traits that obscure the original game's display use stated combat penalties instead.

The audit found 85 library entries. Rogue Legacy 2's character generator includes rarity values 1–3; other rarity values are disabled or reserved. All 61 eligible entries have an adaptation, including Antique and the two seasonal appearances. The mod also understands the two old Healthy and Strong save IDs, but no longer generates them. Existing eight-trait save IDs are preserved. Only Heart is now named correctly; Hollow Bones is a separate trait.

[trait_coverage.json](../sheets/trait_coverage.json) records every library entry, its source rarity and adaptation status. [traits.json](../sheets/traits.json) contains all exact modifiers, effects and incompatibilities. Cosmetic-only entries still affect appearance. Previously inert platforming traits now have explicit card-combat adaptations, described below. Pure palette and appearance traits retain their visual effects.

The audit tools require the player's installed copy, UnityPy and TypeTreeGeneratorAPI. Extracted definitions, localization text and decompiled source stay under the ignored private directory. They are never distributed. Factual names and enum identifiers are used to identify the adaptations; descriptions and implementation are original mod content.

Validation checks that every eligible source entry resolves to an adaptation, every effect is implemented, selected icons exist in the installed game, and 2,000 seeded offer generations exclude disabled entries and incompatible pairs. Physical gameplay testing remains necessary for balance across all three acts.

## 1.0.5 trait review

Reviewed all 61 selectable traits against the locally installed RL2 definitions and the StS2 hooks. These descriptions are original summaries; no extracted game text is distributed.

| Trait | Correction |
|---|---|
| Clumsy | Remove the extra Block on every Skill; explicitly no combat modifier. |
| IBS | Remove automatic enemy Poison. |
| FMF Fan | Remove the unrelated HP penalty and enemy Weak; its formerly free crown modifier was removed in 1.0.14. |
| Mushroom Man | Keep compact appearance; remove Thorns. |
| Inter-dimensional | Remove extra attack damage; there are no projectile-blocking walls in card combat. |
| Cartographer | Remove gold after every battle; the native map already reveals routes. |
| Spelunker | -6 maximum HP; +10 gold only upon entering a treasure room. |
| Lootbox Addict | Keep -8 maximum HP; +20 gold only upon entering a treasure room. |
| Aerodynamic | First Attack each turn deals +1 damage per hit, instead of granting 4 Block. |
| Kanganthropy | First Attack each turn deals +2 damage per hit, instead of granting 6 Block. |
| Clownanthropy | Restore a -12 maximum HP drawback; reduce first-Attack Block from 3 to 2. |
| Limitless | +1 Energy each turn now comes with 1 Vulnerable at each player turn start, replacing an easily offset HP penalty. |

Remove overlapping starter-card modifiers from Gigantism, Dwarfism, Combative, Bookish, Vampirism, Only Heart and Hollow Bones. Their stated trait effects still apply. Pacifist retains its Mercy starter conversion, which supplies playable cards when Attacks are forbidden. Stable trait/card IDs and saved trait snapshots remain supported.

Other selectable traits retain their existing, explicitly described card-game adaptations. Useful inherited traits are still allowed; the review removes unrelated benefits and redundant bonuses rather than making every trait a penalty. Crown modifiers inherited from the original catalog remain.

Flame Barrier is now a 1-Energy defensive Skill: 5 Block and 2 retaliation per enemy attack this turn; upgraded, 7 Block and 3 retaliation. It uses native FlameBarrierPower, which expires after the enemy turn. It does not deal direct attack damage or grant permanent Thorns.

## 1.0.9 trait playtesting

Clumsy now loses 2 Block after the first Attack each turn, clamped to zero. Attacking before guarding avoids the penalty; further Attacks that turn do not remove more Block. This replaces its former cosmetic-only adaptation. Hero Complex keeps +40 maximum HP and disabled healing, and now adds +100% legacy crowns. Trait crown bonuses add together at settlement, subject to a 200% trait payout cap; Treasury multiplies the capped payout, and do not increase run gold. Both changes apply to existing heirs with these trait IDs.

## 1.0.11 formerly inert traits

- **Hypergonadism**: The first Attack each turn applies 1 Weak to each enemy it hits, before damage.

- **Inter-dimensional**: Lose 6 maximum HP. The first Attack each turn removes up to 3 Block from each enemy it hits, before damage.
- **Cartographer**: On the first turn of each combat, draw 1 extra card but lose 1 Energy.
- **IBS**: Start each combat with 1 Weak.
- **FMF Fan**: The first Skill played each turn grants 2 Block.
- **Mushroom Man**: Lose 6 maximum HP. After the first enemy Attack that damages you each turn, gain 3 Block. Your heir has a compact appearance.

Inter-dimensional removes guard once per enemy hit by the first Attack, including multi-hit and area attacks. It does not add damage or remove guard again on subsequent hits. Mushroom Man triggers only when an enemy Attack causes actual HP loss, once per player turn; non-attack HP costs and fully blocked attacks do not trigger it. These changes apply to existing heirs with the same saved trait IDs. Crown bonuses remain unchanged.

## 1.0.14 progression balance

Colorblind, Nostalgic, Synesthesia and FMF Fan no longer grant crown bonuses without a gameplay drawback. Bookish retains +1 draw/-10 HP but no longer grants bonus crowns; Crippling Intellect retains +1 draw/-18 HP and now grants +50% crowns. Vampirism heals 3 after combat. Treasury remains effective above the trait bonus cap. Full Healing Garden (two levels) plus Vampirism and Super Healer heals 10 after combat. Existing trait IDs, gameplay abilities, cosmetic palettes and Hero Complex healing rules are preserved.

## 1.0.15 Pacifist description

Pacifist converts all Basic Attack cards across the three classes into Skills granting 4 Block and applying 2 Poison (7 Block and 3 Poison upgraded). Other Attack cards cannot be played. Every enemy also starts combat with 5 Poison. The trait description now states all three parts; gameplay is unchanged.
