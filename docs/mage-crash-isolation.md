# Beta Mage crash isolation (#146)

The [hard-trait study](hard-trait-relic-validation.md) recorded a native access violation during the beta Mage/Crippling Intellect campaign at floor 23. The last completed observation had 3 HP. The native stack ends in `GodotObject.Dispose` / `MegaSpineBinding.Dispose`, reached from `NCreature.GetCurrentAnimationTimeRemaining` during death animation. That stack does not identify a faulty mod card or trait. BaseLib also patches the native death-animation path.

The failed trial used sixfold combat animation speed and ran alongside another copied client. Isolation uses one copied public-beta client, a fresh disposable profile, normal animation speed, and the same seed, class, manor tier, traits and pilot configuration. It does not resume the failed save or replay its timed-out mutation.

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
