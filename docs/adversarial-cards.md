# Card resolution, targeting and pile conservation audit (#52)

Baseline: merged main `995842a`. Tests use independently copied owned Windows regular/public-beta labs, disposable family/native profiles and the official offline launch option. No production gameplay defect was confirmed. Production card effects and balance are unchanged. The journal and generated coverage remain ignored/local.

## Checks

The catalogue driver submits paid native plays for all 360 historical registered cards in both upgrade forms, plus shared-core cards under the other class owners: **856 forms per branch, 1,712 total**. Individual results and identities are in `adversarial-card-plays.csv`; stage counts and provenance are in `adversarial-cards-evidence.json`. Each case uses a fresh native debug combat, no manor bonuses or traits, bounded fixture Energy, neutralized durable enemy targets and explicit hand/draw/discard/exhaust fuel. Those interventions isolate card resolution; debug room visits are not campaign or progression evidence.

Native assertions cover actual payment, action completion, unique card membership across piles, owner and combat-scope agreement, generated Wound counts, expected Discard/Exhaust/removed results, and disappearance of the rendered card from the table. Independent sheet-based comparisons cover immediate HP, damage and Block for cards without an installed power's additional hook behavior. Saved model IDs, upgrade state, frozen trait fields, retention/discard claims, base stats and rules must agree. The power ordering and campaign balance audits remain #53/#60.

All **28 nonempty special-effect families**, base/upgraded, exercise empty and full hands/piles, this-turn cost overrides, native saved clones and ephemeral dupes. Together with target cases, these total **314 mod edge plays per branch**. Target tests use rejected null/dead targets, death during attacks, self-inflicted death, native replay and no-target autoplay. Each branch also runs **42 native Ironclad card controls**; these use the same custom Guard fuel and are not an unmodded-game comparison. Eight terminal area replay cases per branch cover the three heirs and native Thunderclap: the first play kills every enemy, leaving no live target for the replay. Legal zero-card Scry selections are distinguished from mandatory choices that cannot be skipped. Native ActionExecutor explicitly does not support arbitrary task cancellation; the fixture does not invent an unsupported selector cancellation as a user flow.

**32 held choice cases per branch** permit an actual save while the first card has been paid but not resolved. After selecting or legally declining, a second paid card queued behind that choice must resolve once. The permanent deck is compared before save, in the saved player, after both actions and after native resume. These saves follow the native room/deck contract; they do not claim resumable in-flight combat prompts.

All 720 card forms receive Nimble eligibility checks, with preview/save checks for eligible forms. **18 paid examples per branch** cover immediate, mixed and delayed Block, discard Block and exhaust-based Block. **64 Retain/Ethereal/Innate forms per branch** are flushed through actual protected turns, comparing a durable deck card with its combat clone. Growth must serialize in the combat copy, leave the deck unchanged and reset in the next actual native combat (64 reset checks).

Two-peer ENet tests prepare the same generated shared cards on both peers before the owner submits paid play (**eight plays per branch**). Shared Fortify is replayed with native Burst; Study draws for its owner. Base/upgraded forms check payment, complete action/pile/node cleanup, unchanged foreign Energy/Block/hand and synchronized native saved-player round-trips before existing settlement checks. Native transformation identity/owner/save checks from the completed [trait audit](adversarial-traits.md) remain prior evidence; this suite does not claim new transformation coverage.

## Native contracts clarified during fixture development

- Native removed Powers and dupes have no pile, but remain tracked by combat until cleanup. That is an explicit removed state, not a stranded card.
- Native SerializableCard does not preserve ephemeral dupe flags or removed keywords. Durable card ID/upgrade/design fields are checked; dupes are not promised as permanent deck copies.
- A live combat damage preview can include exhausted-card bonuses or other modifiers. A newly loaded deck model has a base preview. Persistent stats/rules are compared separately from live rendering; these two contexts are not asserted to have identical preview text.
- Native cloning needs the appropriate card scope. Dupes are created from an actual combat hand; saved run models are cloned into combat before being played.
- Terminal death/victory cases allow the native run/combat teardown instead of requiring an ordinary continuing-combat result. A self-death can leave the card in Play when the native action terminates, including the native Hemokinesis control. These cases require action completion without a new card-play exception; they do not assert ordinary pile conservation after teardown.

Failed fixture receipts remain private and are excluded from passing totals. Other corrected probes used a nonexistent native Cleave model and artificially killed every enemy before submitting a manual play, after native teardown had already removed the card scope. The valid no-target probe uses autoplay, and terminal replay uses actual lethal damage instead. These were fixture errors, not confirmed mod bugs.

Three diagnostic DLLs are recorded separately: catalogue/extras `c4b1c809…`; queued choices/co-op `0dfd34a9…`; final mod edges/native controls/terminal replays `c6cb6385…`. Full hashes and exact source checkpoints are in the evidence JSON. Later changes only add/correct opt-in test fixtures; production card/core/runtime effect files match baseline main. This is evidence for the audited production paths across these builds, not a claim that every suite ran on one final binary. No binary is published here.

Solo seed: `SPIRELEGACY-CARDS-52`. Exact co-op seeds and platform identities are recorded per stage in the evidence JSON. All native launch wrappers completed with exit code 0; process inspection confirmed the owned lab clients stopped. Baseline validation also passed 124,044 Core assertions, 20 harness checks, preflight (32 sheets / 2,062 rows / 26,477 cells), a warning-free Release build, Python compilation and whitespace checks.

## Reproduction and limits

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_REGULAR_catalog -Class knight -CardResolutionChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_BETA_catalog -Class knight -CardResolutionChecks
# Tags containing edges-model, choices, extras, controls or zero select bounded suites.
./scripts/run-coop-audit.ps1 -Branch regular -Tag cards52_UNIQUE_OWNER_REGULAR -Pair -Port 25701
./scripts/run-coop-audit.ps1 -Branch beta -Tag cards52_UNIQUE_OWNER_BETA -Pair -Port 25711
```

These are controlled native integration tests, not every possible power/relic/trait combination or full campaigns. Physical Deck/Linux, real transport/device/input aborts and seamless prompt disconnect/resume remain outstanding (#110/#57). Nimble eligibility/save/downgrade and ordinary native saved/dupe cards are covered separately; paid enchanted-duplicate combinations are not covered and are deferred with modifier interactions to #53/#55. Exhaustive transformation/reward source coverage remains #54. Balance win rates and infinite resource loops remain #60; exhaustive power/counter ordering remains #53. No personal saves, installed branches, Workshop/Melty publication or release packages are changed.

Passing replies do not imply clean engine logs. Final runs retain Compatibility-renderer particle warnings, missing-backup deletion messages during fixture abandonment, and beta NCard pool double-free/WorkerThreadPool task diagnostics. Their cause is unestablished and not fixed or claimed upstream here; resource lifetime and native timing follow-up remain #58/#110. The co-op harness also deliberately emits a synthetic owner exception to test diagnostic isolation. These messages did not create a new CardPlayDiagnostics failure or leave a tested continuing-combat paid card unresolved.
