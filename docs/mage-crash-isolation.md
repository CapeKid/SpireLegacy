# Beta Mage crash isolation (#146)

The [hard-trait study](hard-trait-relic-validation.md) recorded a native access violation during the beta Mage/Crippling Intellect campaign at floor 23. The last completed observation had 3 HP. The native stack ends in `GodotObject.Dispose` / `MegaSpineBinding.Dispose`, reached from `NCreature.GetCurrentAnimationTimeRemaining` during death animation. That stack does not identify a faulty mod card or trait. BaseLib also patches the native death-animation path.

The failed trial used sixfold combat animation speed and ran alongside another copied client. Isolation uses one copied public-beta client, a fresh disposable profile, normal animation speed, and the same seed, class, manor tier, traits and pilot configuration. It does not resume the failed save or replay its timed-out mutation.

## Results

Neither single-client trial reproduced the crash. Both matched all 179 completed non-wait decisions from the original attempt, comparing the ordered action, full state and decision detail. Both then played the two Mage Strikes and lethal Frost against Louse Progenitor, successfully ended the encounter at 5 HP, and continued to a natural death against Entomancer on floor 30. All 224 completed decisions match between the two new trials. Request IDs, timestamps, wait polling and the new top-level speed field are excluded from this comparison.

| Trial | Combat animation speed | Clients | Outcome | Crowns |
| --- | --- | --- | --- | --- |
| Original hard-trait attempt | 6 | 2 concurrently | Native crash; last observation floor 23 / 3 HP | Excluded |
| Fresh normal-speed trial | 1 | 1 | Natural death, floor 30 | 113 |
| Fresh accelerated trial | 6 | 1 | Natural death, floor 30 | 113 |

The two successful trials used Mage, A5, full manor, Antique + Crippling Intellect, seed `BALANCE146-E`, generation 3, Neow enabled and the planned/defensive pilot, with no forced reward or combat-resource interventions. Their starts verify 43/54 HP and Bloodline + Gorget. These are technical replicates of one seed, not two independent balance samples. Both include the handicap crown bonus and must not be pooled into the no-bonus manor pacing curve. Neither is a winning campaign.

Both new trials used runtime source `6dae1a835c6040c1914adf8ce57eca18113ab826`, DLL SHA-256 `2ac5625a219265fe5be577d6f341872c2fe365f2532ea81a1d53430d00593bc1`, mod `1.0.30`, BaseLib `v3.4.7` and the pinned copied beta game `v0.111.0`. These tests do not establish compatibility with any newer live game build. The original runtime source and artifact remain separately recorded; the successful trials did not alter Mage gameplay or death-animation handling. All caps stayed unchanged and neither new trial hit one. Both copied clients were verified stopped afterward.

The [manifest](evidence/balance146-animation-isolation/manifest.json) preserves all three receipt hashes and exclusions. The [comparison](evidence/balance146-animation-isolation/comparison.json) records matching prefixes and the first successful turn beyond the old failure. [Initial conditions](evidence/balance146-animation-isolation/initial-conditions.json), [campaign outcomes](evidence/balance146-animation-isolation/campaigns.csv), [card exposure](evidence/balance146-animation-isolation/cards.csv) and [decisions](evidence/balance146-animation-isolation/decisions.jsonl) retain the derived observations. The original error remains excluded rather than being rewritten as a loss. The trajectories table is header-only because no paid multi-return trajectory was collected here.

Both new private logs have zero matches for `ERROR`, `InvalidTaskId`, `Fatal error` and `double free`; renderer and FMOD warnings remain. These pattern counts are not a blanket clean-log claim. One successful pair cannot establish whether the original access violation depended on concurrent clients, resource pressure or an intermittent native/loader/mod fault. The cause remains unresolved; no gameplay or animation workaround is justified by this result alone.

The build/design preflight passed with zero warnings/errors, all 20 harness checks passed, and 27 exporter checks passed, including altered request/native speed and removed report-speed rejection. Issue #146 remains open for developed-archetype decisions, recorded current-build winning campaigns and experienced-player comparison. Another identical loss-heavy seed is not the next balance step.

## Test controls

`run-balance-campaign.ps1 -AnimationSpeed 1` selects normal combat animation speed for the natural campaign driver. The default remains 6 for existing callers. Both speeds are opt-in test-bridge behavior; ordinary gameplay is unchanged. The oracle restores the previous engine time scale in `finally`. Other fixture drivers keep their existing argument lists.

The driver records the requested speed in the report and every campaign-step request. Non-wait native replies report the speed too. The exporter verifies these agree and retains the speed in the manifest, including excluded attempts. Legacy receipts lacking the field export it as null rather than inventing an observed setting. The existing 1,200-second campaign cap, 180-second command timeout, 40-turn combat guard and 1,200-decision cap remain unchanged. Capped or crashed attempts cannot become natural terminal outcomes.

Reproduce with a committed build and a new evidence tag in an independent owned beta lab:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta2-lab `
  -Tag YOUR-UNUSED-TAG -Class mage -Tier 2 -Policy planned `
  -Rewards defensive -Seed BALANCE146-E -Ascension 5 `
  -Traits 'antique,bonusmagicstrength' -AnimationSpeed 1 `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

Verify that no other game client is running before the test. The runner verifies the copied lab's provenance, isolates the profile, pins the committed DLL and cleans up only its exact executable/PID. Preserve raw receipts and logs privately; publish only derived observations. No release or personal-save changes are part of this study.
