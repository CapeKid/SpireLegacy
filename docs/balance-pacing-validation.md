# Multi-seed balance and manor pacing (#146)

This is an automated measurement checkpoint, not an experienced-player balance sign-off. Card stats, manor prices, and crown payout rules were not changed. The issue remains open for experienced archetype play, complete winning campaigns, and the full purchase curve.

## What was measured

The opt-in `planned` pilot makes legal native card plays and purchases. It scores rewards against the current deck, chooses upgrades by estimated score improvement, removes low-value cards, favors recovery routes when hurt, and uses a limited set of potions. Shops enforce native prices and acquisition hooks. Lucky Fysh's gold gain during card acquisition is recorded separately from the displayed purchase price.

Standalone campaigns use fresh private families, deterministic seeds `BALANCE146-A` and `BALANCE146-B`, A0/A5, and no/partial/full manor investment. The matrix includes all three heir classes, native Ironclad controls, and Hero Complex cases. The CSV lists the precise coverage; this is not a fully crossed matrix.

Two trajectories carry actual crowns, manor purchases, and Chronicle progress across twelve natural returns. They use trait-free A0 heirs and seeds `PACING146-1` through `PACING146-12`. Purchases use earned funds and the real prerequisite/cost/persistence path. The pilot tries at most one purchase after each return, in this order: vitality, armory, smith, garden, library, vitality, smith, armory, garden, vitality, vitality, vitality, then four vault levels. No crowns are injected into these trajectories.

| Trajectory | Earned over 12 returns | First upgrade | Second upgrade | Final bank |
| --- | ---: | --- | --- | ---: |
| Knight, regular | 205 | Vitality, 80 crowns, return 5 | Armory, 120 crowns, return 12 | 5 |
| Ranger, beta | 154 | Vitality, 80 crowns, return 7 | Not yet affordable | 74 |

Both first purchases fall within the documented five-to-seven-return target. Knight's second purchase took seven more returns. These are two controlled trajectories, not a human pacing guarantee or evidence that the entire 2620-crown manor is appropriately paced.

The Knight trajectory required native checkpoint recovery at returns 9 and 10 after fixture limitations: an early game-over observation before settlement completed, then an unsupported Scroll Boxes bundle selector. The canonical export retains these interventions and each loaded DLL stage. It is not an uninterrupted run sequence. Ranger required no checkpoint recovery.

## What the results do and do not show

In the earlier pilot revision, the paired A5 Ranger seed A reached floor 5 without manor and floor 23 with full manor on regular, versus 11 and 17 on beta. This demonstrates survival improvement in those particular trials. Branch RNG, path choices, and card/relic rewards differ; these figures are not branch win rates or causal estimates for individual upgrades.

The pilot lost native Ironclad controls too: floors 17/14 on regular and 14/9 on beta. Automated losses cannot establish that heir cards need buffs. Do not pool different policy revisions into a single balance estimate. Every report includes its exact source commit, DLL hash, game version, BaseLib version, and mod version. Earlier artifacts through `fced4cc` use the initial planned policy; artifacts from `bfae60d` onward use threat-aware fuel and recovery selectors. Later changes repair fixture observation, bundle selection, and shop logging. The Knight trajectory spans these stages; its first upgrade was earned entirely under the earlier policy.

On October 14, 2026, the user reported usually playing Ascension 10 and winning Ascension 9 with full manor investment on the latest mod. This supports viability under experienced human play. Class, branch, seed, exact build, and decision/card records were not supplied, so it is a qualitative report rather than a matched trial or a measured win rate.

Completed commands do not imply clean engine logs. Several beta receipts contain `Invalid Task ID` errors, including a native Ironclad control; private logs also contain compatibility-renderer particle warnings and missing native SFX paths. The manifest records log-pattern counts without attributing them to a proven mod defect. Failed fixture commands are retained separately and excluded from completed outcomes.

Important controlled conditions and limits:

- Starting fixtures skip FTUE and use generation 3 with Neow available. These are fresh investment states, not literal first-ever tutorial families. Class selection is fixed rather than a random heir choice.
- Native act selection uses the first act model in each slot. The pilot accelerates combat animations temporarily and restores the engine time scale; it grants no combat resources in these natural campaigns.
- Combat/reward scores are heuristics, not expert multi-turn planning. Events usually choose the first non-immediately-lethal option. Unknown shop relics score zero, and potion support is limited.
- Campaigns have a 20-minute/1200-decision cap, 50 plays per combat decision, and a 45-second continuous-wait budget. Caps and unsupported decisions are retained as incomplete evidence, never wins or natural losses.
- Chronicle, changing seeds, inherited effects, and purchases all evolve in the trajectories. Later survival cannot be attributed solely to a manor upgrade.
- Private checkpoint continuation never retries a timed-out mutation. Only explicitly supported failures with successful correlated receipts can resume; prior failed reports remain intact.
- Physical Deck and Steam multiplayer testing were not performed here. #110 was closed at the user's request with its remaining device coverage waived.

## Reproduction and evidence

Use an independently copied, owned game lab under ignored `private/`, with its provenance file and required installed mod dependencies. The wrapper refuses an occupied lab, creates an isolated profile, pins the built DLL hash, and cleans up only its verified process. It does not modify personal saves or the real Steam installation.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab `
  -Tag YOUR-UNUSED-TAG -Class ranger -Tier 2 -Policy planned `
  -Seed BALANCE146-A -Ascension 5 -Rewards defensive -Python python

./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab `
  -Tag YOUR-UNUSED-PACING-TAG -Class ranger -PacingChecks -Python python

python scripts/export-balance-pacing.py private/sts2-evidence docs/evidence/balance146
```

The committed derived evidence contains:

- `manifest.json`: receipt hashes, exact runtime/source stages, status, and exclusions. Superseded trajectories are excluded to prevent double counting.
- `campaigns.csv`: completed standalone outcomes and controlled inputs. `wins=0` is a loss, not a win merely because the driver says `finished`.
- `trajectories.csv`: all 24 durable settlements, purchases, bank conservation, and checkpoint recovery flags.
- `cards.csv`: actual offered/picked/played/hand-entry counts, scoped by trial. This is exposure data, not card win rates. Bundles and shop removals require the decision detail for interpretation.
- `decisions.jsonl`: route, reward, shop, potion, upgrade, and combat decisions with source stages. No owned game source, binaries, personal profiles, or save files are distributed.

The exporter rejects active workers, mismatched request IDs, duplicate commands within a canonical report, malformed artifact identities, duplicate settlements, and crown conservation errors. Raw receipts remain private for local replay/inspection.

Validation: release build succeeded with zero warnings/errors; the core checks passed 164,059 assertions; the harness checks passed 20 checks; six exporter tests cover valid evidence, active workers, duplicate commands, mismatched replies, duplicate settlements, and crown creation. These checks validate the measurement machinery and core regressions, not experienced-player balance.

## Remaining acceptance work

1. Record experienced archetype decisions across matched seeds and higher Ascensions, including successful complete campaigns for each class.
2. Expand hard trait/relic combinations and compare equivalent policy revisions on both branches.
3. Continue actual earned-fund trajectories through the full manor, including wins and different purchase orders. Twelve early returns cannot validate long-term pacing.
4. Use those results to decide whether card stats or manor prices need adjustment. The current evidence supports measuring further, not blanket tuning.
