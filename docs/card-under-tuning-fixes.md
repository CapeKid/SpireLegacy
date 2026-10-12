# Eight targeted card corrections

These changes address the eight cards identified in the under-tuning review. The no-manor baseline remains intentionally weaker overall; manor levels, class passives, reward membership, saved card identities and trait rules are unchanged. Numbers separated by a slash mean base/upgraded.

| Card | Before | After |
| --- | --- | --- |
| Arms Vault (shared) | Advertises Exhaust recovery but selects zero cards | Returns one exhausted card; still costs 1 Energy, draws 1/2 and Exhausts |
| Second Quiver (Ranger) | 0 Energy, draw 1/2, gain 1/2 Energy, lose 4 HP, Exhaust | Draw 2/3, gain 1/2 Energy, no HP cost, Exhaust; differentiated from shared Meditate |
| Volley (Ranger) | 2 Energy, 3/4 damage three times | 2 Energy, 5/6 damage three times; full Blacksmith gives 21/24 total damage before other modifiers |
| Returned Prophecy (Mage) | 2 Energy, draw 1/2, recover one exhausted card, Exhaust | 1 Energy, draw 2/3, recover one exhausted card, Exhaust |
| Ash Reading (Mage) | 1 Energy, lose 1 HP, draw 1/2, exhaust another hand card | 0 Energy, no HP cost, draw 1/2, exhaust another hand card, then Exhaust itself |
| Geode Ward (Mage) | 2 Energy, 6/8 Block; Retain and grow by 3/4 Block when retained | 2 Energy, 10/13 Block; same retention growth; full Armory gives 12/15 initial Block |
| Battle Meditation (shared) | 1 Energy, draw/discard 2/1 or 3/2 | 1 Energy, draw/discard 2/1 or 3/1 |
| Grand Thesis (Mage) | 2 Energy, draw 3/4, grant 1/2 extra charges, Exhaust | 1 Energy, draw 3/4, grant one extra charge at either level, Exhaust; ordinary Skill charge still applies, capped at two |

Ash Reading's added Exhaust prevents a reusable zero-Energy positive-draw loop. Battle Meditation remains paid. Charge caps, multi-hit upgrade caps and native Vigor behavior remain intact. Targeted authoring overrides preserve these decisions when the historical balance pass is rerun; the catalogue and balance audit records are updated alongside the source sheets.

## Verification

The deterministic suite checks nonempty recovery selections across every currently eligible regular/beta recovery card, improved net hand size on Battle Meditation's upgrade, differentiated rare recovery, and bounded free draw/resource effects. The native reproduction is `python scripts/sts2-undertuning-check.py PRIVATE_PROFILE regular` or `beta`, with fresh full-manor families in independent owned-game labs. It pays actual Energy through manual card play, verifies effects, destination piles, removal of the played-card node and native serialization. Shared cards are exercised by all three classes. Evidence remains private.

The Release build has zero warnings/errors; 3,365 core assertions and all 32 sheets pass. All 24 scenarios per branch pass using the same DLL (SHA-256 `6e556b71e5c92b066c86232dc7faaa4430112ea9d13489426be14fc6dac433aa`) on owned regular v0.107.1 and public-beta v0.111.0. Volley uses a durable boss target in the final fixture so enemy overkill cannot truncate the measured damage. Regular evidence combines the first 14 passing shared/resource cases in `undertuning-regular-initial.json` with the 10 final cases in `undertuning-regular-rest.json`; beta evidence is `undertuning-beta.json`.

## Next balance work

1. Play matched runs at no manor, partial manor and full manor using the same seed and ascension. Start without handicap traits; then test trait interactions separately. Record act reached, damage taken, deaths, crowns earned and upgrade purchases.
2. Record card offer/pick/play frequency, cards repeatedly held or discarded, Energy spent, and actual damage/Block. Compare cards within their intended deck archetypes. Win rates alone hide dead cards and overpowered alternatives.
3. Test the next candidates: Knight Second Wind outside and inside exhaust decks; Mage Rune Familiar in power decks; Ranger Long Vigil in retention decks. Also review Poison scaling, which Blacksmith and Armory do not improve.
4. Audit historical/cross-color cards separately. The new recovery invariant found historical Mage Rekindle also has a zero-card recovery selection. It is outside the current reward pools and this eight-card change, but should be corrected before declaring all registered cards sound.
5. Recheck expensive single-hit attacks versus multi-hit attacks with Strength and Vigor, plus draw/exhaust/discount loops after these buffs. Adjust one archetype at a time and rerun matched seeds. Set difficulty targets using the observed no-manor versus full-manor progression curve, rather than forcing losses.

Native effect correctness does not establish full-run balance or physical Steam Deck performance. These changes are a targeted starting point for those playtests.
