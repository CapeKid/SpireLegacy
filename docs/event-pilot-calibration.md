# Event-cost policy calibration (#146)

The A10 Knight [policy replay](a10-policy-replay.md) exposed a test-pilot failure: at Tablet of Truth, choosing the first non-immediately-lethal option repeatedly paid escalating maximum-HP costs until only 1 maximum HP remained. This change bounds that decision and preserves safe exits. It changes only the opt-in isolated pilot, not ordinary gameplay, card values, event rewards or player choices.

## Decision policy

The planned pilot identifies Tablet of Truth through the native event model and stable option keys, rather than English display text or button position. It buys the initial random upgrade only when all of these conditions hold:

- The cost is positive and at most 3 maximum HP, and less than 10% of current maximum HP.
- Current HP is at least 75% of maximum and fewer than 10 HP are missing.
- At least one deck card can be upgraded.

Otherwise it chooses Smash on the initial page, or Give Up on a continuation page. After one payment the cost rises to 6, so it stops. These are conservative heuristic limits, not an empirically optimal valuation of upgrades versus permanent HP. The policy does not pursue the final whole-deck upgrade at 1 maximum HP. An unfamiliar Tablet option schema returns an unsupported decision instead of guessing a costly option.

For other events and non-planned policies, the existing first-safe-option behavior remains. Locked options and options whose native `WillKillPlayer` predicate reports death are excluded. If none remain, the pilot returns `unsupported-event`; it no longer falls back to a known fatal option. This does not evaluate every event's delayed risks or economic tradeoffs.

Event receipts now include the event ID, chosen stable key, reason, pre-choice current/maximum HP and offered keys with their native immediate-death flags. The result state remains a post-click snapshot, which can precede completion of an animation or asynchronous effect. Calibration probes wait for the expected native completion before asserting the final health values.

## Native controls

Five synthetic controls passed on each pinned branch, for ten event cases total. Each uses a fresh trait-free, no-manor Knight start and the actual native Tablet event. Injected HP, deck upgrades and forced earlier payments are fixture inputs; none of these cases counts as a natural campaign or crown return.

| Input | Native choice | Verified effect |
| --- | --- | --- |
| 80/80 HP, upgradeable deck | Initial Decipher | 77/77 HP; next cost 6 |
| 44/80 HP | Smash | 64/80 HP; event finished |
| 80/80 HP, fully upgraded deck | Smash | 80/80 HP; event finished |
| One forced earlier payment, next cost 6 | Give Up | Current/maximum HP preserved; event finished |
| Three forced earlier payments, next cost 24 | Give Up | Current/maximum HP preserved; event finished |

Every case also checks that the generic selector rejects a locked option and a native always-fatal predicate in favor of a safe control, and returns no choice when only locked/fatal options remain. The native UI execution uses the same `CampaignOracle.Step` path as natural campaigns and verifies its selected stable key against the policy's decision.

Runtime source is `9272275b9768fb6d43f683601d1e51b7dd793240`; DLL SHA-256 is `b833f67572f3bf482448913e7e07156759a979e5cbd580aaac70c709ad291b5d`. Both copied versions use mod 1.0.30 / BaseLib v3.4.7: regular v0.107.1 and beta v0.111.0. Compatibility claims are limited to those pinned versions. [Calibration evidence](evidence/balance146-event-policy/calibration.json) preserves exact receipt hashes, correlated commands and complete derived cases.

Build/design preflight passed with zero warnings/errors. Python driver and PowerShell launcher syntax checks passed; combining two fixture switches is rejected before filesystem access. Both calibration clients were verified stopped before the natural replay. Error logs retain audio-device initialization failures with dummy-audio fallback, four backup-deletion matches per branch and renderer particle warnings. Beta additionally has two `InvalidTaskId` matches. Each launch log records five warnings about replacing an unfinished Neow event, because these synthetic cases enter Tablet directly. No matched fatal-error or already-freed message appears. Successful assertions do not imply clean engine logs, and this change does not claim to fix those diagnostics.

## Natural beta replay

One new natural campaign repeats `BALANCE146-C`: A10 Knight, full manor, no traits, planned combat and defensive rewards, generation 3 with Neow, animation speed 6. It starts in a fresh disposable profile without combat or reward interventions. The historical reference is the immediately preceding merged-pilot replay, not another newly collected sample. Both use the same pinned beta version. The new run uses the source/DLL above; its prior artifact identities and both receipt hashes are preserved in the [campaign manifest](evidence/balance146-event-campaign/manifest.json).

| Policy | Natural outcome | Maximum HP at end | Earned crowns |
| --- | --- | ---: | ---: |
| Prior event selection | Loss, Act 1 floor 13 | 1 | 34 |
| Bounded event selection | Loss, Act 2 floor 33 | 80 | 87 |

The first 73 non-wait replies match under the [comparison projection](evidence/balance146-event-campaign/comparison.json): action, floor, end HP, selected choice fields and ordered play IDs/upgrades/targets/net energy/net HP loss. Added event audit fields, scores, reasons and other state are excluded. This is not full-receipt equality. The first difference is the intended floor-11 Tablet choice: Smash heals from 44 to 64 HP, rather than Decipher leaving 44 HP while reducing maximum HP from 80 to 77. The new run preserves 80 maximum HP, defeats the Act 1 boss and eventually dies to The Insatiable on floor 33. Subsequent deck upgrades, choices and RNG diverge after the event; the 53 additional crowns are not an isolated estimate of healing's value.

The new campaign records 95 combat-turn decisions, 18 card-reward selections, one potion use, three healing rests and three smith rests. It upgrades Reforge, Riposte and native Exterminate at rest sites. At floor 30 it spends 100 gold to remove one basic Strike (463 → 363 gold). Other shop offers receive zero scores in that receipt, so this is evidence of an actual paid removal, not expert evaluation of the whole shop inventory. No further shop-policy calibration is bundled into this event fix.

The exporter validates paired configuration, correlated commands, artifact identities, completed outcomes and zero interventions. It exports 330 non-wait replies across the historical reference and new run, plus actual card exposure/selection/play counts. Only one attempt is new; do not recount the historical reference in aggregate totals. No manor upgrade is purchased, and the trajectory CSV has no rows. Both campaigns begin with injected full investment, so their earnings do not measure a fresh family's first-upgrade timing.

The new campaign finishes naturally before the existing 20-minute/1,200-decision limits, with its exact client verified stopped. Its error log contains three ERROR matches, one `InvalidTaskId` match and renderer warnings; no matched fatal-error or already-freed message appears. These diagnostics remain visible and are not fixed by the event policy.

This is the first of the two bounded remaining steps agreed for #146. It demonstrates safer event decisions and better survival on this particular replay, not a win or a population balance result. One final validation-and-assessment step remains: consolidate class/build and investment coverage, collect a bounded final campaign set, then report which acceptance requirements are met and which remain unsupported. If recorded wins or experienced-player comparison remain absent, the assessment must state that limitation and recommend an explicit disposition rather than extending an indefinite series of pilot-calibration PRs. No card or manor-price change follows from this single comparison.

## Reproduction

Build a committed source revision, then use a fresh private profile and unused tag in an independently copied owned lab:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab `
  -Tag YOUR-UNUSED-TAG -Class knight -EventPolicyChecks -Python PATH-TO-PYTHON
```

Run the corresponding provenance-verified beta lab sequentially. The wrapper pins the built artifact, isolates the profile and stops only its verified client. Never retry a timed-out mutation or count these injected controls as campaign wins.
