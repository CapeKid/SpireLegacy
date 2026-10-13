# Knight and Mage multi-hit bonuses (#130)

Knight's Counterguard and Mage's Arcane Charges now add damage to **every hit of the next Attack card**, including repeated hits on the same enemy. Charges are spent once after that card resolves. A separately replayed Attack receives no further bonus unless primed again. Reservoir follows the same rule; its card/power descriptions, class summaries, catalogue and authoring policy are updated together. Ranger's Hunter Rhythm continues counting Attack cards rather than hits.

The previous first-hit-per-enemy behavior was an explicit balance restriction. In both tested branches, native Strength 2 and Vigor 2 made Twin Strike deal **7, 7**, while a primed Knight or two-charge Mage dealt **7, 5**. After this change both classes deal **7, 7**; upgraded Twin Strike changes from **9, 7** to **9, 9**. Native Vigor lasts for an entire AttackCommand and is then consumed; native Strength applies per powered hit and persists. Class charges remain card-scoped, so attacks that execute multiple damage commands can also receive the bonus throughout that card.

Implementation removes the class target-history set and its callback. The modifier requires an owned Attack card and powered damage. Saved Charges/AttacksThisTurn properties, charge caps and card amounts are unchanged. Separate inherited first-hit Weak/Block-shred traits are unaffected.

[Validated native evidence](class-multihit-evidence.json) records exact source/DLL hashes and the baseline from main `70ff82d`. Independent offline Windows labs used regular **v0.107.1**, beta **v0.111.0**, BaseLib **3.4.7**, Spire Legacy **1.0.29**, and disposable profiles:

- Native Strength/Vigor controls and pre-change class damage, base/upgraded, on both branches.
- **76 paid solo class cases**: native/mod multi-hit cards, single/area/random-target attacks, zero-charge attacks, repeat plays, lethal chains, Mage Reservoir and Ranger controls. Saved properties and independent clone counters also pass.
- **8 two-peer Knight/Mage cases**: the primed Attack gets the bonus on both hits; its next Attack gets none. Other owners' charges/hands/energy remain unchanged. Both peers wait for native action checksum checkpoints, with checksum checking enabled.
- Zero-warning build; **164,053 core assertions**, **20 harness checks**, preflight and disposable authoring regeneration checks pass.

Physical Steam Deck/Linux testing, complete campaign balance and distribution uploads are outside these checks. This intentionally buffs multi-hit class synergy; broader win-rate tuning needs playtesting. Native renderer diagnostics remain in private logs; the co-op harness deliberately emits its synthetic owner diagnostic.

To reproduce, build against an owned regular SDK and run `scripts/run-balance-campaign.ps1 -PowerAuditChecks` with fresh `class130` tags in copied private labs. Run `scripts/run-coop-audit.ps1 -Pair` with `class130` tags for owner checks. `scripts/sts2-class-multihit-evidence.py` validates completed receipts and exports this issue's proof. Baseline tags require the pre-change implementation.
