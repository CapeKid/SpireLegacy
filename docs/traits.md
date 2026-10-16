Spire Legacy has 61 selectable inherited traits adapted to turn-based card play. HP, Energy, Block, status effects and legacy crowns define their effects. In-game tooltips describe the actual behavior; information-obscuring traits use stated combat penalties where appropriate.

All enabled entries have implementations. Two older Healthy and Strong save IDs remain recognized but are no longer generated. Existing saved trait IDs are preserved. Only Heart and Hollow Bones are separate traits.

[traits.json](../sheets/traits.json) contains modifiers, effects and incompatibilities; [trait coverage](../sheets/trait_coverage.json) preserves compatibility metadata. Obsolete source-library analysis has been removed from the current tree. The mod loads its own packaged icons; there is no external content inspection at launch.

## 1.0.5 trait review

Reviewed all 61 selectable traits against their design definitions and the StS2 hooks. These descriptions are original summaries; no extracted game text is distributed.

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

## 1.0.16 Pacifist balance

Pacifist's converted Basic Attacks now apply 2 Poison (3 upgraded) and grant no Block, including Shield Bash. Combat no longer starts with free Poison. Guards and other defensive Skills retain their usual effects, and other Attacks remain unplayable. The +150% crown bonus is unchanged (the existing total trait payout cap still applies). Existing heirs and saved starter trait snapshots use these updated values.

Previously, repeating converted Arrows supplied both stacking damage and 4/7 Block while every enemy began with 5 Poison. The new conversion requires separate defense and stays below the native one-Energy Deadly Poison's 5/7 Poison. Steady Aim retains its original draw effect; class and other trait Skill triggers still apply. Full-run playtesting remains necessary to tune difficulty further.

## 1.0.19 cosmetic trait audit

Synesthesia has six animated rainbow motes and a shifting colored ring around the heir's feet, visible alongside grayscale or other palettes. Cosmetic traits now apply at rest sites and shops as well as combat; inherited sprite size applies there too. Blue, Nature, Halloween and Christmas palettes retain distinct colored accents when paired. Histrionic briefly flashes red on real HP loss, then returns to the ordinary palette, even with Colorblind. HP and crown bonuses are unchanged; Synesthesia remains cosmetic with no crown bonus. See [the full 63-ID audit](trait-effect-audit.md) for each trait's gameplay or visual consumer.
