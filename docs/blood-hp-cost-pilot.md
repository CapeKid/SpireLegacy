# Immediate HP-payment safety (#146)

The pilot's immediate HP-cost adapter recognized native Offering, Bloodletting and Hemokinesis but returned zero for every Legacy card. Legacy cards execute their explicit `hpLoss` payment through an unblockable, unpowered damage command. Consequently the existing one-HP-reserve rule and low-health penalty never saw those payments.

A native before-fix probe reproduced this with Blood Price: estimated cost 0, actual payment 3 HP, selection permitted at both 3 HP and 2 HP despite a legal non-payment alternative. A survivable play from 4 HP ended at 1 HP while 50 Block remained. The probe did not execute either fatal choice. That expected-failure receipt is preserved separately from successful fixed tests.

`PilotCardInfo.ImmediateHpLoss` now reads `LegacyCard.DesignCard.Amount("hpLoss")`. The tactical chooser's existing affordability check and penalty then apply to Legacy payments as well. The adapter retains its explicit native-card checks; a variable named `HpLoss` alone does not establish an immediate payment. Native Haunt uses that variable for a delayed power and remains a zero-immediate-cost control. Card effects, stats, prices, player choices and ordinary gameplay are unchanged.

## Results

All 76 final native cases passed, 38 per pinned branch:

| Coverage per branch | Cards | Base/upgraded cases |
| --- | --- | --- |
| Legacy immediate payments | 15 | 30 |
| Native Offering, Bloodletting, Hemokinesis | 3 | 6 |
| Native Haunt delayed-effect control | 1 | 2 |

The Legacy set covers ten Knight, three Mage and two Ranger cards, including eight Skills, five Attacks and two Powers. Every immediate-payment case rejects both fatal budgets, selects the non-payment fallback and pays the expected amount through a survivable native play. All survivable plays end at 1 HP, including Haunt's zero immediate payment. The tests execute no intentional lethal play. The initial expected failure is excluded from the 76 final cases.

Final runtime source is `b3111843144785bb90b2668a04f5fea1e29ef1a2`; DLL SHA-256 is `e5d92d996315f1516889ab8c681e9fc6639dcc970beabfe01eda214837c4e456`. Both tests use mod `1.0.30` and BaseLib `v3.4.7`, with copied game versions regular `v0.107.1` and beta `v0.111.0`. Coverage is limited to those pinned copies. The build/design preflight passed with zero warnings/errors and all 20 harness checks passed. Both test clients were verified stopped.

[Calibration evidence](evidence/balance146-hp-costs/calibration.json) preserves receipt/artifact hashes, actual payments, boundary decisions, fallback choices and log-pattern counts. Regular has 30 backup-deletion error lines; beta has 30, one `Invalid Task ID` message and three native NCard-pool messages reporting that a card has already been freed. Renderer warnings also remain. Neither final log has a fatal-error match. The logical HP assertions passed, but those native messages have not been explained or fixed and the logs are not described as clean.

One [historical A10 terminal reply](evidence/balance146-hp-costs/historical-terminal.json) shows the consequence of the old omission: full-manor Knight, regular branch, `BALANCE146-C`, floor 9. Its final Bloodied Blade play pays 3 HP, the heir ends at 0 HP and Byrdonis remains alive. The original source is `52cd89c4fe949d43d48f3cdc570dfcab39d3c7f7`; that receipt and outcome remain unchanged. This policy failure limits interpretation of that loss and does not imply the fixed pilot would win. No new natural campaign or purchase curve was collected here.

Issue #146 remains open for reward/build planning, other archetype decisions and recorded current-build winning campaigns. Reward and upgrade heuristics still need their own HP-cost treatment; immediate play safety alone is not experienced blood-build play. Traits and damage-modifying relics also need separate controls before generalizing this isolated check.

## Validation method

The native suite discovers all positive-`hpLoss` Legacy rows and checks both base and upgraded forms. It adds both forms of the three named native immediate-payment cards and Haunt. Each case gets a fresh trait-free, no-manor run and a controlled Nibbits encounter, with a single candidate card, high enemy HP, sufficient energy and 50 Block.

Before any paid play, it queries the real chooser at HP exactly equal to the payment and at one HP below it. It also adds a legal non-payment Defend/Guard to test fallback selection. It then removes the fallback, sets HP to payment + 1 and plays the candidate through the native paid-card path. A successful positive-cost case must decline both fatal budgets, choose the fallback, allow the survivable choice and pay exactly the estimated amount, leaving 1 HP. Haunt instead must remain selectable at 1 HP and pay zero immediately. No end-turn survival claim is made for that delayed power.

These are synthetic resource-boundary tests, not natural campaigns, victories or crown-pacing samples. No fatal card play is executed. Traits, damage-modifying relics, future self-damage and full-turn survival planning are outside this isolated check.

## Reproduction

Build a committed artifact and select a provenance-verified independent owned regular/beta copy with a fresh evidence tag:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab `
  -Tag YOUR-UNUSED-TAG -Class knight -HpCostChecks `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

Use one client at a time. The driver records every correlated response and checks the loaded DLL and mod set; it never retries timed-out mutations. The guarded runner isolates profiles and stops only its exact executable/PID. Keep raw logs and receipts privately; publish only derived observations and hashes.
