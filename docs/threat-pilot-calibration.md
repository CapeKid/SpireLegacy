# Threat-aware pilot calibration (#146)

This step repairs a demonstrated decision error in the opt-in balance test pilot. Gameplay card effects, prices, crown payouts and ordinary player input are unchanged. It follows the [A10 Mage/Ranger matrix](a10-mage-ranger-validation.md), which recorded early deaths without establishing expert play quality.

## Reproduced decision error

The old pilot awards 100 score to a verified single-hit lethal. When two targets have equal card scores, it breaks ties by target HP. In a two-Nibbit native fixture, this selected the harmless 1-HP buffing enemy over the 2-HP attacking enemy, despite both being killable. This occurred with Knight, Mage and Ranger basic strikes and native Ironclad Strike. These are synthetic fixture observations, not a claim that every prior campaign suffered this exact mistake.

The correction adds the value of prevented **uncovered** incoming damage to verified lethal choices, using the same 1.6 weight as immediate Block. Damage already covered by player Block gets no additional survival credit. Nonlethal attacks get no kill credit. This remains a local heuristic; it is not an expert solver and does not predict multi-hit, reflect, phase-change or death-trigger interactions.

Campaign play records now include `score`, `incomingBefore`, `blockBefore` and `targetThreat` before each play, alongside the existing reason, target and actual energy/HP deltas. This makes individual choices assessable without inferring unseen enemy intent from terminal floor counts.

## Native calibration

Each class has three cases: uncovered incoming damage with both enemies killable; the same encounter with enough injected Block to cover incoming damage; and an attacking enemy whose HP is too high for a lethal. Each case starts a fresh trait-free A10 run with no manor and then enters a debug Nibbits encounter. Enemy HP, hand and Block are fixture setup. Exactly one strike is played through the normal native card path, with its energy use and actual target death recorded. No synthetic result is counted as a natural campaign or crown-pacing outcome.

The baseline has four failed threat choices and eight passed controls. The corrected regular and public-beta matrices each pass all twelve cases. The baseline's driver exits unsuccessfully because its policy expectations fail; its native commands themselves succeeded. It is preserved in [calibration evidence](evidence/balance146-threat-calibration/calibration.json) with the original receipt hash and failed status.

Each exported case is checked against its correlated start/probe receipt, unique command IDs, exact class/kind, native result and verdict. Public evidence omits private profile paths and save identifiers. Exact source/DLL/game/dependency versions are listed separately for each matrix; baseline and corrected artifacts are not pooled.

The baseline runtime source is `3f0e287f89c63754e38d65efb9ad78f04ca49a46`, DLL SHA-256 `70284588af277114aef114d89a7e71d48fe9859f10ac936bd413e4ad809f92bd`. Corrected fixtures and the natural follow-up use source `01ad843bb0ae9c6b765cf75fb33945adc0a09c10`, DLL SHA-256 `871ea7a01a4fbf4b4bb16d10daab5edb3348d3a88b67299ea6d54fcb148425a0`. Game versions are regular `v0.107.1` and public-beta `v0.111.0`, with BaseLib `v3.4.7` and mod `1.0.30`.

## Natural A10 follow-up

Four fresh trait-free A10 campaigns use the corrected artifact on regular, generation 3, Neow enabled, FTUE disabled, `planned` combat/route policy and `defensive` rewards. Native Ironclad has no manor; the two heir repeats have full manor. These use normal generated rewards and legal decisions, without debug entries, injected combat resources or reward interventions. Limits remain 20 minutes/1200 decisions per run, 50 plays per combat decision, 45 seconds continuous waiting and 180 seconds per command.

| Character | Seed | Manor | Terminal floor | Heir crowns earned | Recorded plays |
| --- | --- | --- | ---: | ---: | ---: |
| Ironclad | C | None | 9 | 0 (native control) | 94 |
| Ironclad | D | None | 6 | 0 (native control) | 55 |
| Mage | C | Full | 7 | 15 | 55 |
| Ranger | C | Full | 7 | 15 | 40 |

All four finished in natural deaths, without caps, unsupported actions or failed commands. The [campaign table](evidence/balance146-threat-calibration/campaigns.csv), [manifest](evidence/balance146-threat-calibration/manifest.json), [card observations](evidence/balance146-threat-calibration/cards.csv) and [214 decisions](evidence/balance146-threat-calibration/decisions.jsonl) retain provenance and choices. All 244 plays contain the four new telemetry fields. There were 28 plays labelled estimated lethal against an attacking target with uncovered incoming damage; this is a preview/decision label, not a measurement of total damage prevented.

Mage and Ranger each reached floor 7 under both the earlier and corrected policies for seed C/full manor. The rule is verified by controlled fixtures; unchanged terminal floors do not show whether it altered every campaign decision. Native controls also lost. This supports treating pilot quality as an unresolved limitation rather than inferring a card balance defect from the heir deaths. Classes have different native resources and rewards; these are common-environment controls, not a causal class-power ranking. The baseline synthetic matrix uses the old pilot, but no old-pilot A10 Ironclad counterpart was collected in this step.

No campaign used potions, shops or rest-site choices/upgrades. Thus this follow-up still does not complete those natural decision coverage requirements or developed-archetype/winning-campaign coverage. The header-only trajectories table indicates this study contains no paid multi-return trajectory; the earlier completed manor curves remain unchanged.

## Reproduction

Compile the committed source against owned game references. Use independently copied regular/public-beta labs under ignored `private/`, with their provenance and dependencies. The same corrected DLL is tested in both branches:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-coop-regular3-lab `
  -Tag YOUR-UNUSED-THREAT-TAG -Class knight -ThreatChecks `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

`-ThreatChecks` runs all three heirs and native Ironclad regardless of the wrapper's required `-Class` argument. The wrapper enforces a single selected fixture driver, fresh disposable profiles, exact lab provenance, DLL provenance and executable/PID cleanup. The driver refuses to overwrite evidence and asserts native effects as well as policy choices.

## Limits and next work

Validation: design preflight and compilation pass with zero warnings/errors; 20 harness checks, 17 pacing/launcher checks and 16 exporter integrity checks pass. The [existing native tactical regression suite](evidence/balance146-threat-calibration/regressions.json) passes all 33 cases on each branch, covering potion permission/rescue, Vulnerable follow-up, area/multi-hit utility, conditional/retained/finisher damage, reward skipping and fatal/nonfatal HP costs. This is separate from the 24 corrected threat cases. All four lab executables were verified stopped after their wrappers exited. Prior published evidence snapshots are unchanged.

This calibrates one decision rule. It does not establish experienced-player win rates, a good overall archetype policy or winning campaign viability. #146 remains open for developed archetypes, deliberate shop/rest/potion use, representative difficult trait/relic combinations and recorded current-build winning campaigns. The user's reported A10 full-manor Knight win on Steam Deck/public-beta remains qualitative evidence with unknown build/seed/decision record. No Steam or Melty release is performed by this issue.
