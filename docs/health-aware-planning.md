# HP costs in reward and upgrade planning (#146)

The [play-safety correction](blood-hp-cost-pilot.md) taught the combat pilot to reject fatal immediate payments. Reward descriptions and acquisition scores still omitted those payments, so rewards, shops and upgrades could value blood cards as free resources.

The planning descriptor now carries the verified immediate `HpLoss`. Deck/defensive reward policies receive the player's current HP; the shared planner used by shops, bundles, removals and upgrades also receives that context. The historical standalone reward baseline keeps its existing scoring path. Callers with unknown HP still pay the base tax rather than treating unknown health as zero.

The acquisition heuristic subtracts one score point per HP payment, plus pressure proportional to payment / remaining HP, capped at two extra points per paid HP. A payment that would currently be fatal scores below the reward skip threshold. The gate is temporary: the same descriptor can become worthwhile after healing. Zero-payment cards retain their previous scores. These are explicit heuristic weights, not an empirically measured HP-to-damage exchange rate or an expert blood-build policy. The policy assumes no future heal when considering a currently fatal reward.

Upgrade evaluation scores the original and its upgraded clone with the same HP context. An unchanged payment therefore cancels from the marginal gain when both forms are affordable. If both forms are currently fatal, their zero marginal difference lets a useful non-payment upgrade win. Mandatory upgrade screens can still require a choice when every available marginal gain is zero. The evaluator preserves the original card while comparing its clone.

Ordinary card effects, prices, manor upgrades, save formats and player choices are unchanged. The descriptor is a test-planning record, not a new gameplay restriction. Shared score consumers also receive the base payment tax, including the Pacifist pilot's fuel-value heuristic.

## Results

Both final pinned-branch runs passed: 38 base/upgraded model forms and 148 HP contexts per branch, plus two actual reward screens and low/healthy upgrade-selector choices. This totals 76 model forms and 296 HP contexts across the two branches; these are technical checks, not independent balance samples.

| Decision | 3 HP | 60 HP |
| --- | --- | --- |
| Native reward screen | Measured Cut | Bloodied Blade |
| Shared upgrade selector | Guard (gain 3 vs 0) | Blood Price (gain 4 vs 3) |

The reward offers, deck and health inputs are controlled. Bloodied Blade scores below the skip threshold at 3 HP and approximately 5.842 at 60 HP; Measured Cut scores 5. The low-HP upgrade selection is applied through the native command, leaving Blood Price unchanged. Reward selection preserves the injected HP and the combat turn in both final runs.

Final runtime source is `7b36b34cc80f2399d8192808816b9ebc39cbd743`, DLL SHA-256 `75f55f4b90748675340c79db54e9916c04c2b5a00393b08f8cda3bc368c373ef`. Both use mod `1.0.30` / BaseLib `v3.4.7` with owned copied regular `v0.107.1` and beta `v0.111.0`. Coverage is limited to these pinned versions. The build/design preflight passed with zero warnings/errors, all 20 harness checks passed, and the core suite passed 166,710 assertions, including 2,648 new HP-planning assertions. Both test clients were verified stopped.

[Calibration evidence](evidence/balance146-health-planning/calibration.json) records exact receipt/artifact hashes, descriptors, context scores, upgrade choices and native reward callbacks. The first fixture version made an unnecessary follow-up call after the reward overlay auto-closed, which entered combat and reduced HP after the correctly scored selection. Those receipts remain unmodified and excluded from final counts; their hashes and reward observations are preserved, while duplicate model rows stay private. The corrected fixture checks overlay presence and asserts HP/turn stability.

Each final error log contains three backup-deletion error lines and renderer warnings. Neither has a matched `InvalidTaskId`, fatal-error or already-freed-pool message. These are pattern counts, not a blanket clean-log claim or a fix for the earlier native cleanup diagnostics.

No shop transaction, rest-site navigation, natural campaign, win or paid manor return was collected. Shops/bundles/removals receive the same health-aware score through the shared planner, but wiring is not evidence those paths were exercised here. Issue #146 remains open for broader archetype decisions, recorded winning campaigns and experienced-player comparison; this change supplies a calibrated health-cost input for that work.

The subsequent [A10 Knight policy replay](a10-policy-replay.md) applies the merged corrections in two natural campaigns. It verifies avoidance of the historical fatal payment on regular and exposes unchanged destructive event choices on beta; neither run wins or improves its floor/crown outcome.

## Native calibration

Use a committed build and a new evidence tag in an independent, provenance-verified owned lab:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab `
  -Tag YOUR-UNUSED-TAG -Class knight -HealthPlanningChecks `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

Each branch uses fresh trait-free, no-manor starts for the three heirs and native Ironclad. The model probes cover both forms of all 15 Legacy payment cards and the native Offering, Bloodletting, Hemokinesis and delayed Haunt controls. They verify the descriptor, current-HP propagation, finite scores, fatal-budget behavior, marginal upgrade values and source-card preservation. HP and encounters are injected fixture inputs.

The Knight probe exercises the actual shared upgrade selector at low and healthy HP and applies its low-HP selection through the native upgrade command. Separate fresh starts exercise real native reward screens with Bloodied Blade and Measured Cut, using the normal campaign reward-selection path and no forced reward. The UI cases do not play either card or measure their HP payment again. They are acquisition/upgrade checks, not rest-site navigation or a shop-purchase test.

No fixture here is a natural campaign, victory, crown return or independent win-rate sample. Repeated payment, healing, relic/trait interactions, broader archetype planning and experienced-player comparison remain outside this bounded heuristic.
