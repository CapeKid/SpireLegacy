# Bounded defensive reward policy

This is the first of the three remaining balancing passes agreed after PR #96: improve defensive reward decisions and compare several seeds, address specific supported card problems, then perform final regressions and prepare a Workshop update. This pass changes only the opt-in diagnostic pilot. Production cards, manor upgrades, traits, crowns and published packages are unchanged.

## Decision rule

The new `defensive` reward mode retains the delayed-aware `deck` scorer and adds a bounded immediate-defense preference. It computes the average of each deck card's literal immediate Block divided by `max(1, cost)`. Below the heuristic target of 3, the pressure is `clamp(3 - average, 0, 0.5)`. An offered card receives `min(3, Block / max(1, cost) * pressure)` extra score. An empty deck receives no extra score.

This rewards affordable protection rather than merely counting defensive cards. It does not mention a particular card, floor or seed. Delayed Block earns its existing discounted score but contributes no immediate coverage or new bonus. The prior policy remains available as the paired control. Combat decisions and map routing are unchanged.

The target and weights are explicit heuristics, not measured probabilities. Coverage omits manor Dexterity, conditional Block, relics, draw order and turn-by-turn Energy allocation. Zero-cost cards use a divisor of one. The policy can still make poor choices; these trials cannot establish human win rates or balance for other classes.

## Trial scope

Five fresh-profile natural trials use owned regular v0.107.1, full-manor Knight, A0, no traits, tactical combat and first-legal map routing. Seed 2 checks the previously troublesome case. Seeds 3 and 4 pair `deck` and `defensive` modes. No reward is forced, and no cards, health or resources are injected into these campaigns.

The seed-2 run is a whole-policy rerun, not the matched one-choice intervention in [the previous comparison](balance-defensive-ablation.md). Earlier choices can change subsequent draws, offers and RNG use.

## Results

All five trials reach natural deaths with successful commands and no caps. [Trial CSV](balance-defensive-trials.csv) records five rows; [card observations](balance-defensive-trial-cards.csv) records 168 rows. Earlier datasets are preserved.

| Seed | Policy | Death endpoint | Crowns | Combat turns | Block Potion uses |
| --- | --- | --- | --- | --- | --- |
| 2 | Defensive | Act 2, floor 25 | 74 | 76 | 0 |
| 3 | Defensive | Act 2, floor 27 | 94 | 73 | 1 |
| 3 | Prior deck | Act 2, floor 27 | 94 | 73 | 1 |
| 4 | Defensive | Act 1, floor 15 | 51 | 44 | 0 |
| 4 | Prior deck | Act 1, floor 15 | 51 | 43 | 0 |

Every trial takes all offered reward screens without skipping; interventions are zero. Crowns reflect survival and full-manor Treasury bonuses, not a crown-price experiment.

The new policy automatically chooses Fortify at floor 12 on seed 2 and reaches the same floor-25 endpoint as the earlier one-off Fortify intervention. The earlier automatic policy died at floor 17. This checks the rule on the motivating case but is not held-out evidence or an immutable-build causal comparison.

On seed 3, both policies choose the same 13 rewards and reach the same endpoint with identical combat-turn and potion-use counts. On seed 4, the first differing reward is floor 3: defensive chooses Grit Teeth while control chooses Long Reach. Defensive has more health at floor 13 (35 versus 15), but both subsequently die at floor 15. Its extra combat turn does not establish a useful survival improvement.

The two new paired seeds show no endpoint gain or endpoint regression. Keep `defensive` as an explicit diagnostic option rather than claiming it is an optimal or generally superior default. No numerical card changes are justified by these endpoints alone.

## Remaining two passes

The targeted payoff pass is now documented in [three finisher corrections](balance-targeted-finishers.md). Once reviewed and merged, only the final regression and Workshop-preparation pass remains in this sequence.

The sequence is now complete in [release 1.0.28](release-1.0.28.md): both branches pass the final regressions and packages are prepared. The paragraphs below preserve the decision at the time of this policy study, not additional unfinished passes.

Next, review concrete card payoff and cost issues against native character benchmarks and representative legal decks. Change only specific supported problems, with focused regression checks; do not start another open-ended pilot feature pass or tune this rule to these seeds. A final pass then checks the resulting changes on both branches and prepares the Workshop update. This pass does not publish a release.

## Validation and limits

Eight new pure checks cover preference under deficient coverage, unchanged control behavior, adequate coverage, weak-reward skipping, the bonus cap, empty decks and delayed protection. The complete Core suite passes 3,448 assertions. Release compilation has zero warnings/errors; content preflight checks 32 sheets, 2,062 rows and 26,117 cells.

Native reward fixtures exercise legal procurement, same-pair replay returning to normal skip, absent-card conservation and reward-screen closure. They test native integration, while the pure checks test the new Block scoring rule. Campaigns exercise automatic decisions through ordinary rewards.

Those two fixtures pass on regular v0.107.1 and beta v0.111.0 (four cases, six reward flows including replay). Private evidence tags are `reward-intervention-defensive-reward-regular.json` and `reward-intervention-defensive-reward-beta.json`. Both use final source `784705b` and DLL `ea8b5b438da6017180f6aeaaa59b0fcf694094d7c1903682095d21ccf72c94f6`.

The first three campaigns use source `8d978c4` and DLL `990e7fac0d38c773c01ea18325d4adb87acf4dbf44c70ded4b1239930bff1dd9`. The final pair uses the fixture build above. The intervening change only switches the existing reward fixtures to the new mode; campaign scoring is identical. Each new-seed pair shares one immutable DLL. The CSV records the individual hashes rather than treating all campaigns as a single build.

All native work uses independent private owned-game labs, disposable profiles and verified PID cleanup. It does not modify real saves, installed games, Steam branches or Workshop/Melty listings. There is no physical Steam Deck test in this pass.
