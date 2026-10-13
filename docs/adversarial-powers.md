# Class resources and combat hook audit (#53)

Baseline: merged main `664f288`. Owned Windows regular v0.107.1 / public-beta v0.111.0, BaseLib 3.4.7, Spire Legacy 1.0.29. Tests use independent copied labs, disposable profiles and the official offline launch option. No installed branches, personal saves or published packages change.

## Confirmed finding

[Foresight empty-deck turn lock (#121)](https://github.com/CapeKid/SpireLegacy/issues/121): the power used `player.Deck.Cards.First()` only to obtain a Scry owner. In a generated-power native boundary fixture, removing the permanent deck with the native removal command and ending a real turn throws `Sequence contains no elements` in `LegacyPower.AfterPlayerTurnStart` on both branches. The native turn loop stops before Play. The combat draw pile is still nonempty, so the permanent deck is unrelated to the requested Scry.

Scry now accepts its owning Player; card-based calls delegate to that overload, and Foresight supplies its player. The same seeded real-turn fixture passes for all three heirs in both upgrade contexts on both branches, including the Scry Block delivery. This is a controlled generated-power boundary reproduction, not a claim of reaching an empty-deck campaign naturally. No other production changes are made.

## Native checks

- Fifty class cases per branch, base/upgraded: four Skills exceed Knight's one-charge / Mage's two-charge caps; single, multi-hit, area, random-target, zero-damage, replayed, duplicated and lethal Attacks test per-card counters, first-hit-per-enemy damage and charge consumption. Mage Reservoir/Arcane Echo stacking also reaches the cap without overflowing it. A separate six-case lethal multi-hit matrix checks that a dead target receives no remaining chain hits.
- Six Ranger sequences per branch play six Attacks each. Multi-hit cards advance Rhythm once per card; every third Attack draws the printed one card plus two Quiver cards. Area and random marking, display remainder and natural next-turn reset are asserted. Thunderclap's printed Vulnerable is counted separately from the two Rhythm triggers.
- Native model clone mutations prove Charges and target sets are independent. Class SavedProperty codecs round-trip values after explicitly registering the test power type through BaseLib's branch-compatible cache adapter. Native run saves do not contain live powers: three actual save/resumes per branch restart combat with zero class counters. These are separate contracts, not a claim of resuming mid-turn power state.
- Thirty-six engine cases per branch use native PowerCmd acquisition to isolate stacked third-Skill Block / next-Energy, third-Attack Vigor, first-Skill Echo, manual versus end-turn discard, once-per-turn claim resets, exhaust with native Dark Embrace/Feel No Pain, status draw recursion, unpowered Sadistic damage, Plated loss flags, removal/reapplication, and turn-start/end HP, draw and Energy effects. Status chains terminate at ten cards in hand.
- Ordinary Foresight/Scry Block, native Power-card draw, Thousand Cuts and Establishment cost reductions over two actual retained turns are checked in six additional cases per branch.
- Four retaliation cases per branch pay for base/upgraded Mage Flame Barrier and native Ironclad Flame Barrier. Blocked powered hits retaliate once per hit; unpowered/unblockable HP loss does not retaliate, and the power expires after the enemy turn. The native card is a semantic control, not a request to match its stronger printed numbers.
- Nine paid Measured Breath cases per branch cover base, upgraded/stacked, intrinsic Retain and full-hand retention. Six additional empty-hand/native Well-Laid Plans cases prove no hang, branch-specific native retention behavior, bounded choices after removing the native power, legal zero-card decline, and no permanent Retain keyword. Arbitrary executor cancellation is unsupported by the native API and is not injected as a user flow.
- Thirty-six paid Nimble copy examples per branch cover base/upgraded, native ephemeral duplicate and serialize/load-to-combat copy, each under all three owners. Immediate, delayed and exhaust-secondary Block include the enchantment and the applicable Dexterity semantics. These examples close the bounded copy gap from #52; they are not every possible enchanted relic/power combination.
- Two-peer owner matrices on both branches exercise Knight/Mage and Knight/Ranger, base/upgraded Skills, stacked effects and Ranger multi-hit Attacks. Foreign owner Energy, Block, hand and charge state must stay unchanged. Native checksums remain enabled, and both peers reach the completed-action checksum before fixture setup changes again. Exact counts and results are recorded in the evidence JSON after final completion.
- Native Ironclad base/upgraded attack, guard and healing results match with mod hooks enabled and disabled. These are ordinary native controls without custom class powers.

Paid probes assert payment, native action completion, table-node cleanup and no new CardPlayDiagnostics exception. Direct native discard/exhaust/damage/PowerCmd interventions are identified separately from paid card plays. Protected turns, inflated enemy HP, generated fuel and fixture Energy isolate hooks; they are not campaign balance evidence.

## Fixture corrections and provenance

Failed fixture attempts remain private and are excluded from passing totals. The regular branch does not automatically cache power SavedProperties; beta renames that cache type. The codec probe uses BaseLib's existing branch adapter and does not change production persistence. Lethal hits report actual HP lost separately from overkill. Thunderclap applies its own Vulnerable. Co-op state equality can precede the native checksum: a fixture removed Arcane Echo before the other peer's previous checksum, producing a diagnostic-only divergence. The corrected fixture waits for that native checkpoint before mutating setup, rather than disabling checksums.

Suites were completed across multiple diagnostic builds. Each stage records its exact DLL/source/game/BaseLib/mod identity; they must not be represented as one final-binary run. Early engine/copy suites precede the owner-based Scry fix; their tested effects are unchanged. Post-fix empty-deck and ordinary Scry suites cover the changed path. Later build changes are opt-in fixtures only. Seeded replies, original engine logs and failed attempts remain in ignored private profiles; public proof summarizes their identities and outcomes without redistributing game files.

## Reproduce and remaining gaps

```powershell
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_regular_class -Class knight -PowerAuditChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_beta_class -Class knight -PowerAuditChecks
# Tags containing engines, retention, copies, empty, supplement or chains select bounded suites.
./scripts/run-coop-audit.ps1 -Branch regular -Tag powers53_UNIQUE_regular -Pair -Port 26801
./scripts/run-coop-audit.ps1 -Branch beta -Tag powers53ranger_UNIQUE_beta -Pair -Port 26811
```

Physical Deck/Linux, real controller/transport aborts, timed disconnects and full natural co-op campaigns remain #57/#110. Exhaustive relic/potion interactions and every native alias combination remain #55; infinite resource loops, operational balance and malformed/extreme design inputs remain #60/#61. This audit uses valid gameplay counters exceeding the stated class caps; it does not inject impossible Int32 overflow as a naturally reachable run.

Passing replies do not imply clean engine logs. Renderer particle warnings, native missing-backup deletion diagnostics, the harness's deliberate synthetic owner exception, and beta NCard pool/WorkerThreadPool diagnostics are recorded separately. Their unresolved native cause remains #58/#110; they are not described as fixed or proven upstream. The confirmed Foresight exception is a separate, reproduced mod failure.
