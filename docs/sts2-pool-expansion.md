# StS2 card-pool expansion (1.0.20)

Issue #23 expands each class to the native character reward-card count while keeping a shared family core. The baseline was measured in the owned game on both branches, excluding basics and Ancient-rarity cards.

| Branch | Common | Uncommon | Rare | Rewards per class | Basics | Shared / unique, including basics |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Regular v0.107.1 | 20 | 36 | 26 | 82 | 3 | 34 / 51 |
| Public-beta v0.111.0 | 20 | 38 | 27 | 85 | 3 | 34 / 54 |

The regular expansion adds four shared rares and six unique rares per class. Beta adds two uncommons and one rare per class. That is 187 distinct active cards on regular, or 196 on beta. All 360 historical models and their original illustrations remain registered, including cards not currently offered. Existing saved IDs are preserved. Ancient-specific additions remain issue #26.

Eligibility follows the built-in Ironclad rarity distribution at startup, excluding other mods' injected cards. The same DLL supports both tested branches; only reward eligibility changes. Beta-only cards remain loadable on regular by their saved ID. Future native roster changes require a fresh parity audit.

## Additions and upgrades

Values below are before traits, manor bonuses, and combat powers. Retain, Exhaust and HP costs are intentional limits. Effects that share an engine use the existing bounded implementation.

### Shared family rares

| Card | Availability / rarity | Energy (upgraded) | Base | Upgraded | Build and budget |
| --- | --- | ---: | --- | --- | --- |
| Family Oath | Both / Rare | 2 (2) | 6 damage x2; 1 Strength; Exhaust | 7 damage x2; 2 Strength; Exhaust | strength. Inflame: slower setup, smaller Strength gain, one-use attack. |
| Ancestral Salvage | Both / Rare | 2 (2) | 6 Block; Return a card from your Exhaust pile to your hand.; Exhaust | 9 Block; Return a card from your Exhaust pile to your hand.; Exhaust | exhaust recovery. Recover one exhausted card for 2 Energy with modest Block, then Exhaust; cannot recover itself. |
| Eternal Bookmark | Both / Rare | 2 (1) | At the end of your turn, Retain up to 2 card(s). | At the end of your turn, Retain up to 2 card(s). | retention. Well-Laid Plans: pay 2 Energy to retain two cards; upgrade lowers setup cost. |
| Forest Remedy | Both / Rare | 2 (1) | 4 heal; Remove all debuffs from yourself.; Exhaust | 4 heal; Remove all debuffs from yourself.; Exhaust | healing and cleanse. Paid, one-use small heal and debuff removal; respects inherited healing and Hero Complex. |

### Knight

| Card | Availability / rarity | Energy (upgraded) | Base | Upgraded | Build and budget |
| --- | --- | ---: | --- | --- | --- |
| Royal Execution | Both / Rare | 3 (3) | 16 damage; Deal 2 times damage if the target is at or below half HP. | 20 damage; Deal 2 times damage if the target is at or below half HP. | finisher. Bludgeon: lower damage unless the enemy is already at half HP; no Energy refund. |
| Meteor Hammer | Both / Rare | 2 (2) | 5 damage x3; 1 Weak; Exhaust | 6 damage x3; 1 Weak; Exhaust | multi-hit and Weak. Twin Strike: two-Energy, three-hit, one-use attack with one turn of Weak; upgrade adds only one per hit. |
| Unbroken Wall | Both / Rare | 2 (2) | 18 Block; Exhaust | 23 Block; Exhaust | defense and exhaust. Impervious: lower one-use Block budget, same two-Energy cost. |
| Battlefield Recovery | Both / Rare | 2 (2) | 2 draw; 4 heal; Exhaust | 3 draw; 5 heal; Exhaust | HP and draw. Offering: paid one-use recovery with smaller draw, small healing and no Energy gain. |
| Forge Memory | Both / Rare | 2 (1) | 4 Block; Whenever you Exhaust a card, draw 1 card(s). | 4 Block; Whenever you Exhaust a card, draw 1 card(s). | exhaust draw engine. Dark Embrace: paid one-card-per-Exhaust engine with four initial Block. |
| King's Ransom | Both / Rare | 0 (0) | 2 Energy; 4 HP loss; Exhaust | 3 Energy; 4 HP loss; Exhaust | HP for Energy. Offering: trades HP for Energy once; draws no cards. |
| Serrated Spear | Public-beta / Uncommon | 1 (1) | 5 damage; 1 Vulnerable | 7 damage; 1 Vulnerable | Vulnerable setup. Bash: smaller attack and one turn of Vulnerable at one Energy. |
| Vengeful Armor | Public-beta / Uncommon | 1 (1) | 3 Block; Whenever an enemy attacks you, deal 2 damage to it. | 3 Block; Whenever an enemy attacks you, deal 3 damage to it. | retaliation. Caltrops: smaller recurring Thorns budget with modest initial Block. |
| Castle Heart | Public-beta / Rare | 2 (1) | 5 Block; At the end of your turn, gain 4 Block. | 5 Block; At the end of your turn, gain 4 Block. | end-turn defense. Afterimage: fixed four end-turn Block instead of scaling with cards played; paid setup and modest initial defense. |

### Mage

| Card | Availability / rarity | Energy (upgraded) | Base | Upgraded | Build and budget |
| --- | --- | ---: | --- | --- | --- |
| Celestial Spear | Both / Rare | 3 (3) | 24 damage | 30 damage | heavy attack. Bludgeon: smaller repeatable damage budget, benefiting from bounded Arcane Charge. |
| Grand Thesis | Both / Rare | 2 (2) | 3 draw; Gain 1 Arcane Charge(s), up to the normal charge cap.; Exhaust | 4 draw; Gain 2 Arcane Charge(s), up to the normal charge cap.; Exhaust | draw and Arcane Charge. Skim: more expensive and one-use; also grants one bounded Arcane Charge. |
| Hourglass | Both / Rare | 1 (1) | Reduce the cost of 2 random card(s) in your hand by 1 this turn.; Exhaust | Reduce the cost of 3 random card(s) in your hand by 1 this turn.; Exhaust | temporary discounts. Bullet Time: discounts only two random eligible cards by one, once, rather than freeing the hand. |
| Infinite Script | Both / Rare | 2 (1) | 1 draw next turn; At the start of your turn, lose 1 HP and draw 1 card(s). | 1 draw next turn; At the start of your turn, lose 1 HP and draw 1 card(s). | HP for sustained draw. Paid setup, one HP per turn for one extra card, plus one next-turn card; neither free nor HP-neutral. |
| Archmage Thesis | Both / Rare | 2 (2) | 6 Block; Every third Skill you play each turn grants 4 Block. | 9 Block; Every third Skill you play each turn grants 4 Block. | three-Skill defense engine. Afterimage: triggers only every third Skill; paid setup includes modest initial Block. |
| Returned Prophecy | Both / Rare | 2 (2) | 1 draw; Return a card from your Exhaust pile to your hand.; Exhaust | 2 draw; Return a card from your Exhaust pile to your hand.; Exhaust | exhaust recovery and draw. Skim: smaller draw for 2 Energy plus one exhausted card recovered, once; cannot recover itself. |
| Frozen Spear | Public-beta / Uncommon | 2 (2) | 8 damage; 1 Weak; 1 Vulnerable | 11 damage; 1 Weak; 1 Vulnerable | debuff setup. Uppercut: smaller damage and one turn each of Weak and Vulnerable. |
| Cleanse Sigil | Public-beta / Uncommon | 1 (1) | 3 Block; Remove all debuffs from yourself.; Exhaust | 6 Block; Remove all debuffs from yourself.; Exhaust | cleanse defense. Paid one-use cleanse with modest Block; no healing or Energy refund. |
| Book of Embers | Public-beta / Rare | 2 (2) | Whenever you Exhaust a card, deal 3 damage to ALL enemies. | Whenever you Exhaust a card, deal 4 damage to ALL enemies. | exhaust damage engine. Phoenix Forge: smaller per-Exhaust damage budget, supporting Mage exhaust recovery. |

### Ranger

| Card | Availability / rarity | Energy (upgraded) | Base | Upgraded | Build and budget |
| --- | --- | ---: | --- | --- | --- |
| Heartseeker | Both / Rare | 2 (2) | 5 damage x3; Retain | 6 damage x3; Retain | retained multi-hit. Twin Strike: slower retained three-hit attack; per-hit upgrade stays at one. |
| Serpent Oil | Both / Rare | 2 (2) | 2 Poison; Double Poison on the target.; Exhaust | 3 Poison; Double Poison on the target.; Exhaust | poison seed and multiplier. Venom Reserve is a retained multiplier; Serpent Oil adds a small Poison seed before doubling, once. Its upgrade improves only the seed. |
| Second Quiver | Both / Rare | 0 (0) | Return up to 2 discarded Attacks; Exhaust | Return up to 3 discarded Attacks; Exhaust | Selective Attack reload for Ranger sequences; no draw, Energy gain or HP cost. Meditate remains the shared resource burst. |
| Empty Quiver | Both / Rare | 1 (1) | 4 Block; Discard your hand, then draw that many cards. | 7 Block; Discard your hand, then draw that many cards. | discard and redraw. Calculated Gamble: costs one Energy, offers modest Block; redraw excludes the played card. |
| Ghost Luggage | Both / Rare | 2 (2) | 5 Block; Whenever you discard a card during your turn, gain 2 Block. | 5 Block; Whenever you discard a card during your turn, gain 3 Block. | discard defense engine. Afterimage/Light Pack: paid two-Block discard engine with small initial defense. |
| Hunt Without End | Both / Rare | 2 (2) | 3 Vigor; Every third Attack you play each turn grants 3 Vigor for your NEXT Attack. | 4 Vigor; Every third Attack you play each turn grants 4 Vigor for your NEXT Attack. | three-Attack Vigor engine. Tracking Rhythm: slower setup with initial Vigor; subsequent Vigor only every third Attack. |
| Scouting | Public-beta / Uncommon | 1 (1) | 1 draw; Scry 3. | 1 draw; Scry 4. | scry and draw. Prepared/Skim: one paid card with Scry selection, no immediate Energy gain or large draw burst. |
| Pocket Arsenal | Public-beta / Uncommon | 1 (1) | 1 draw; Reduce the cost of 1 random card(s) in your hand by 1 this turn.; Exhaust | 1 draw; Reduce the cost of 2 random card(s) in your hand by 1 this turn.; Exhaust | draw and temporary discount. Prepared/Bullet Time: paid, one-use draw and small random hand discount. |
| Unseen Hunter | Public-beta / Rare | 2 (1) | 4 Block; Whenever you play a card, gain 1 Block. | 4 Block; Whenever you play a card, gain 1 Block. | per-card defense. Afterimage: paid setup with modest initial Block; one Block per played card. |

## Balance references

Current owned StS2 definitions were checked directly: Inflame costs 1 for 2/3 Strength; Impervious costs 2 for 30/40 Block and Exhaust; Bludgeon costs 3 for 32/42 damage; Skim costs 1 for 3/4 draw; Dark Embrace costs 2/1 for one draw per Exhaust; Afterimage costs 1 for one Block per card; Well-Laid Plans costs 1 for one/two retained cards. Offering loses 6 HP for 2 Energy and 3/5 draw; Adrenaline grants 1/2 Energy and two cards once. Calculated Gamble costs zero and redraws the hand, with Exhaust removed on upgrade. These references keep the expansion below native standalone efficiency in most cases, leaving room for the manor and inherited synergies. Full-run balance remains a playtesting task.

The recovery cards cannot recover themselves during their own resolution. Free resource bursts Exhaust and require HP. Hand discounts affect a bounded number of random eligible cards for this turn. Multi-hit upgrades add at most one damage per hit. Existing charge, discard and class-counter safeguards remain intact.

## Regeneration and validation

Run `scripts/sts2-pool-expansion.py` after historical authoring/rebalance scripts, then `scripts/sts2-generate.py` and `scripts/card_catalog.py`. The expansion script is idempotent and preserves art paths and model IDs. `sheets/pool_expansion.json` records the build role and balance reference for every addition; `sheets/branch_cards.json` records beta eligibility.

The opt-in owned-game test bridge supports `pool-parity`, `pool-availability` and `expansion-play`. `scripts/sts2-pool-parity-check.py` checks all three class pools, native rewards and shops, compendium availability, paid base/upgraded plays, cleanup and card serialization on each branch. The tests use private isolated profiles, no real family or Steam Cloud saves. Physical Steam Deck testing and campaign balance are additional release checks.
