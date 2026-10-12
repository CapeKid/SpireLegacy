# Inherited traits and starter genetics audit (#51)

Baseline: merged main `093c75644d978e38c18ee1db2f1cfcd315968c0f`. All game tests use independent owned Windows labs, disposable native/family profiles and official offline mode. No personal saves, installed game branches, publishing or merges are changed.

## Findings and fixes

| Finding | Reproduction | Fix |
| --- | --- | --- |
| [#112](https://github.com/CapeKid/SpireLegacy/issues/112) | A Pacifist starter detached/reassigned to an ordinary heir remains a Mercy Skill. Six original base/upgraded Strike cases fail on regular. Beta's native recipient-aware duplicate also retains donor genes: its clone/transfer methods write the backing owner directly. | Rebind to the recipient's frozen traits; replace only inherited Damage/Block contributions so upgrades and card-local buffs/growth remain intact. Hook the native setter and beta's CreateCloneForPlayer/GiveToAnotherPlayer. Native startup includes these two optional patch classes only where supported; the strict no-target rollback guard remains enabled for every installed class. |
| [#113](https://github.com/CapeKid/SpireLegacy/issues/113) | Actual draw/discard/re-draw of one Overcompensation Strike raises its native cost from 2 to 3 in the same turn. | Apply one surcharge per card per owning combat turn, reset tracking at combat start, and allow the next turn to charge that same card again. Retain the existing damage benefit. |
| [#114](https://github.com/CapeKid/SpireLegacy/issues/114) | Shocked consumes its pending flag on the next Attack draw, including the current turn; the text said next turn. Diva's death callback also rewards teammate kills, whereas the text implied personal kills. | Describe the next Attack draw and each enemy death precisely. No trait amounts or crown percentages change. |

Ignored baseline receipts preserve exact source/DLL/game/mod identity. Recipient baseline uses production behavior from main plus test probes at `67f4d38`; cost baseline is `51ab8ec`, with cost behavior unchanged from main. Failed fixture runs are excluded from final receipts. The adjacent evidence JSON records final identities and counts.

## Coverage

[adversarial-traits-consumers.json](adversarial-traits-consumers.json) maps every **61 enabled traits** to its stats, effect, consumer and evidence. Healthy/Strong remain two disabled historical definitions: offers/network selections exclude them, while known historical saved inheritance remains readable. Unknown IDs still fail existing family validation; literal name/summary checks reject formatting placeholders.

The pure suite passes **124,044 assertions**. New checks enumerate all **1,820 enabled compatible pairs**, every class's HP override/clamp, five healing amounts including odd rounding, and all nine Basics in both upgrade forms. They verify symmetric offer exclusions over 500 seeds, retired-save compatibility and retired network-selection rejection. Existing broad capped trait/Treasury payout tests remain enabled; this is not evidence that every synergy is balanced.

Native checks on regular v0.107.1/public-beta v0.111.0, BaseLib 3.4.7 and mod 1.0.29 include:

- Each enabled trait, rotating Knight/Mage/Ranger: actual starting HP/Strength/Dexterity, native healing prefix and delivered healing where HP permits it, owner draw/Energy/shop modifiers, visible cosmetic nodes/parameters, and 54 model transitions across nine Basics. Transitions cover base/upgrade, native duplicate, serialize/load, downgrade and removal. Traits are not temporarily changed on production players.
- All nine Basics, base/upgraded, transfer in both directions between independently deserialized Mage Pacifist/Ranger ordinary owners. Native beta clone-for-recipient and give-to-player APIs, ordinary setter binding and preservation of an additional card-local damage buff are checked. These model peers are not a claim of real network transport; separate ENet ownership tests cover that boundary.
- Paid base/upgraded starter cards for relevant single traits and all three classes' Hero Complex/Diva, Super Healer/Vegan and Pacifist/Breaker pairs. Native save/resume preserves inherited stats and text. Real deck transformations to Basic Skill, Basic Attack and ordinary Attack retain recipient traits and survive serialization.
- Actual Overcompensation draw/discard/re-draw and next-turn re-draw of the same combat card. Actual enemy powered hits, Algesia/Weak, first-hit/first-Attack caps, even-turn Energy loss, initial enemy Block/Strength/Poison, treasure entry gold/HP loss, palette/flash state, and native death rewards/explosions. Masochism uses real damage followed by the native turn-start callback twice to prove once-only pending delivery; this isolated callback is not a natural-turn campaign result.
- Actual native/colorless Flash of Steel, Ironclad Strike, an ordinary Ranger arrow and Finesse: Pacifist rejects Attacks without spending energy or moving the card, and permits the Skill. Native Ironclad control retains ordinary play behavior. Tests grant a bounded fixture energy budget to isolate eligibility; they are not balance measurements.
- Antique/Hoarder native common relic counts and repeated-act idempotence; Innovator's actual starting deck matches its frozen generation seed. Hero Complex/Diva Neow initialization works for every class. Existing Clumsy/first-Skill/first-hurt/first-Attack/shred regressions run again on each branch.
- Representative cosmetic pairs across all three classes: Colorblind/Synesthesia retains grayscale and six rainbow motes/arcs; Nostalgic/Festive retains sepia and its festive accent; Blue/Nature retains both accents. Hero Complex/Diva's actual native one-HP revival routine is also invoked for each class, with the scoped healing bypass restored afterward. This sets fixture HP to zero and restores it afterward; it does not claim a naturally killed co-op campaign.

Paid cards must leave the play pile and rendered table; native save/model/owner values must match. The fixture speeds only its own process to 8x, then uses 1x for the transient Histrionic flash. No input/graphics/registry settings are changed. All complete correlated replies, exact identities, seed `SPIRELEGACY-TRAITS-51` and older interaction-driver diagnostics remain ignored under private evidence folders.

Reproduce with fresh evidence tags/profiles:

```powershell
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_REGULAR -Class knight -TraitAuditChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_BETA -Class knight -TraitAuditChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_regular_edges -Class knight -TraitAuditChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_beta_edges -Class knight -TraitAuditChecks
./scripts/run-coop-audit.ps1 -Branch regular -Tag UNIQUE_OWNER_REGULAR -Pair -Port 25101
./scripts/run-coop-audit.ps1 -Branch beta -Tag UNIQUE_OWNER_BETA -Pair -Port 25201
```

## Limits

These are controlled native effect/model tests, not 61 full campaigns or native coverage of every compatible pair. Pure pair enumeration establishes stated HP/healing/gene/offer formulas, not balance or win rates. Cosmetic diagnostics prove live nodes/parameters; physical Deck/Linux rendering and input remain untested. Seamless multiplayer reconnects/full campaigns remain #110; exhaustive card/action conservation and class-resource trigger ordering remain #52/#53. Balance of combinations that negate another trait's drawback belongs to #60. Crown farming #105 is unchanged. No packages are refreshed or uploaded by this PR.

Final logs retain Compatibility-renderer particle warnings, native missing-backup deletion diagnostics during fixture abandonment, and one missing screen-shake target during a regular treasure-room damage probe (the asserted HP loss succeeds). The co-op driver deliberately emits a synthetic nested owner exception to verify diagnostic isolation. Beta still emits Godot `Invalid Task ID`; its cause is not established or fixed here and remains within #110's native timing investigation. Passing commands do not imply clean engine logs. Guarded launchers exit successfully and process inspection confirms the owned lab clients are stopped.

Fixture corrections are documented rather than counted as bugs: owner guards require detachment on regular, beta duplication requires a recipient, cloned players require a run context, cards need a combat scope before entering hand, and CombatState is derived from pile membership. The first regular startup correctly rejected an unsupported optional patch class under the existing no-target guard; branch-specific class selection fixes that introduced regression without weakening rollback. The accelerated flash initially expired before observation; it now uses native 1x timing. Failed records stay private and are excluded from final passing counts.
