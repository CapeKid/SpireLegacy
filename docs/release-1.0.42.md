# 1.0.42 — distinct card roles (private beta)

Nine approved redesigns separate cards that previously repeated each other. Saved card IDs, rarity, illustrations and pool membership are unchanged. The three additional pairs remain unchanged pending owner review.

| Card | Base effect | Upgrade |
| --- | --- | --- |
| Forge Memory | 1 Energy Power: every Exhaust grants 3 Vigor; no initial Block or draw | 4 Vigor |
| Ghost Luggage | 2 Energy Power: every discard during your turn grants 3 Block next turn; no initial Block | 4 delayed Block |
| Cleanse Sigil | 1 Energy: cleanse debuffs, gain 2 Arcane Charges, Exhaust; no Block | 0 Energy |
| Nova | 3 Energy: 12 damage to all enemies, plus 6 per Arcane Charge; Exhaust | 16 damage, plus 8 per charge |
| Moonstone Lance | 1 Energy: 5 damage, plus 2 per Skill played this turn, up to 3 Skills; no Retain | 7 damage, plus 3 per Skill |
| Returned Prophecy | 1 Energy: next turn, optionally recover one exhausted card costing 1 less that turn; Exhaust; no draw | Discount 2 |
| Ancestral Salvage | 2 Energy: 6 Block, optionally recover an exhausted Attack; gain 4 Vigor if it enters hand; Exhaust | 9 Block, 6 Vigor |
| Phoenix Forge | 2 Energy Power: third Exhaust during your turn deals 14 damage to all enemies, once per turn | 18 damage |
| Iron Furnace | 2 Energy Power: drawn Statuses are exhausted for 5 Block; Curses are unaffected | 7 Block |

Nova uses normal Arcane Flow charge consumption and once-per-turn draw; a replay has no charges unless separately replenished. Moonstone Lance counts resolved Skills, including replays, with a three-Skill cap and owner-turn reset. Existing saved retained growth no longer contributes to this card.

Prophecy chooses from the Exhaust pile at next turn start, after removing the pending recovery power; replays stack the discount rather than creating repeated recovery actions. The discount lasts only that turn and skips X-cost cards. Empty/declined/full-hand recovery consumes the pending effect. Native full-hand overflow sends the selected card to discard without a discount or Vigor reward. Prophecy and Salvage cannot recover any Spire Legacy Exhaust-recovery card; discard recovery also excludes them. Native ownership, hand limits and run-bound choice lifetimes apply.

Published to private beta Workshop item **3822395190**, mod ID **SpireLegacyBeta**. Public Workshop item **3816525393** remains on **1.0.39**. No public upload or PR merge is authorized by this change.

Owned Windows native fixtures cover both card levels and both supported game versions. Physical Steam Deck input, new co-op gameplay and full-climb balance are not established by these checks.

Validation passed: design preflight, generation, authoring regeneration, 166,720 core assertions and 96 final native cases (48 per supported branch). Steam confirms private visibility and all 539 downloaded files match the tested payload. Public listing timestamp and description are unchanged. See [delivery receipt](card-roles-1.0.42-beta-delivery.json).
