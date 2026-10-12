# Native controls and deck-aware rewards

This stage continues from merged PR #89. It adds actual Ironclad runs and a shared reward heuristic, without changing production cards, manor bonuses or crown payouts.

The previous tactical policy understood several effects only on Legacy cards. Native draw, Energy, debuffs, Strength Powers and multi-hit cards could receive inadequate scores. A shared adapter now reads owned native dynamic-variable names alongside Legacy sheet effects. It recognizes native Bash, Shrug It Off, Inflame, Offering and Twin Strike; Twin Strike's two hits are explicit because its owned model uses a literal hit count rather than a Repeat variable. Generic Repeat variables are also read.

The optional `deck` reward policy considers literal damage/Block, draw, Energy, debuffs, Strength, Powers, defensive card coverage, existing Poison/exhaust support and duplicate count. It skips offers whose best score is below five. Skipping resolves the native nullable choice, leaves the reward unclaimed, remembers the parent reward for that run and lets the normal rewards screen perform final skipping/history cleanup. Forced or unmapped event rewards fall back to taking a card. The old standalone policy remains available. The exporter records reward policy and skip count and does not count a null pick as an acquired card.

## Verified decision paths

Nine paid decision/effect fixtures pass on each owned branch, regular v0.107.1 and beta v0.111.0:

- The four Heir ordering/fuel/target/area checks from the prior stage still pass.
- Actual Ironclad uses Bash first, applies 2 Vulnerable and follows with a 9-damage native Strike.
- Shrug It Off is preferred over Defend when Block is needed; it gains 8 Block and draws one card.
- Twin Strike is valued as two hits and resolves for 10 total damage.
- A weak duplicate reward is skipped without adding cards, claiming it or reopening the reward flow.
- Offering at 5 HP produces native death. The paid-play wait returns on death instead of timing out waiting for a discarded card or table visual that will never settle.

The fixtures use two durable enemies and synthetic offers only to isolate decisions. Natural campaign runs receive none of those injected cards, health changes or effects. Seven pure reward-policy checks cover defensive deficits, duplicates/skip, exhaust support, established Poison, deterministic ties and empty offers. Total core validation is 3,408 assertions; preflight passes 32 sheets. The regular-owned Release build has zero warnings/errors.

The final fixtures are `control-death-regular` and `control-death-beta`, with DLL SHA-256 `ecb72d14a594156e5418b11138f342822fb3652f0ce08ba85ebd530a004e7349`. Earlier nested-selector/constructor diagnostics are excluded. The regular selector is reused in nested fixture decisions; native room-based reward creation avoids the beta-incompatible constructor.

## Comparison setup

The natural comparison uses regular v0.107.1, Ascension 0, no traits, the tactical combat policy and deck-aware rewards. For each of `SPIRELEGACY-CAMPAIGN-1/2`, it includes Knight at manor tier 0, Knight at tier 2 and actual Ironclad. Every run uses a new guarded family/save profile. Ironclad receives its normal starter deck, 80 HP and Burning Blood; manor and inherited traits are refused for native controls. Knight starts at 60 HP without manor or 80 HP with full manor, plus its normal inherited/class mechanics.

Ironclad's three card-unlock epochs (2, 5 and 7) are revealed only in the disposable native control profile, so its complete reward pool is available. An initial fresh-profile Ironclad diagnostic reached Act 2 but was excluded after self-inflicted death triggered the old card-cleanup timeout. It also lacked these unlocks. The final controls are rerun with full card unlocks and death-aware waits.

The same seeds and first-legal-child policy are reproducible inputs, **not a guarantee of identical cross-character encounters or rewards**. Native maps, event outcomes, card pools and relics can differ by character/content; rewards diverge further as survival changes. This is a qualitative progression/control comparison, not a statistical win-rate estimate or an encounter-by-encounter card equivalence test.

Public evidence is [balance-native-controls.csv](balance-native-controls.csv) and [balance-native-control-cards.csv](balance-native-control-cards.csv). Trial rows include character input, manor tier, policy, seed, outcome/status, crowns, reward choices/skips and immutable DLL hash. Raw private journals additionally identify the actual native character, source commit, loaded mods, disposable profile and correlated requests. Successful earlier Heir runs retain their own hashes; their scoring/card data match the final controls. The later helper change only recognizes death during unresolved cleanup and exposes Ironclad's unlocks.

| Cohort | Seed 1: death act / floor | Seed 2: death act / floor |
| --- | --- | --- |
| Knight, no manor | Act 1 / 6 | Act 1 / 14 |
| Knight, full manor | Act 2 / 27 | Act 2 / 25 |
| Ironclad, full native card unlocks | Act 1 / 17 (boss) | Act 1 / 17 (boss) |

All six runs reach a natural death. Full-manor Knight clears the first boss on both seeds; no-manor Knight clears neither. In these trials the manor substantially compensates for the weaker starting state, and full-manor Knight survives longer than this Ironclad pilot. That does not establish overall superiority, optimal builds or a target win rate. Three rewards were skipped in the full-manor seed-1 run; none of the other selected runs skipped a reward. The exported card table has 149 rows.

No-manor Knight earns 18/30 crowns and full-manor Knight earns 88/74. The full-manor runs include the Treasury multiplier. These are single-run snapshots; they do not measure long-term crown spending or justify changing prices/payouts. Deck-aware rewards improve no-manor seed 2 from the prior standalone policy's floor 12 to 14, while seed 1 remains at 6; the reward decision alone is not consistently transformative.

## Limits and next balancing work

The shared adapter is deliberately incomplete. Calculated damage, generators whose Cards variable does not mean draw, special Powers, HP-loss costs, reflected damage, multi-phase enemies and complicated native multi-hit effects may be misvalued. The pilot can still choose a lethal HP-cost card; correctly recording that death does not make the decision good. Literal reward scoring misses some Legacy conditional finishers and retention growth too. These omissions affect both pools differently and prevent strong numerical conclusions about relative character strength.

There is no potion/shop/upgrade planner, no explicit archetype target and no optimal turn solver. The first-legal path can take dangerous elites; mandatory upgrade/transform prompts still use a generic card selector. Capped or unsupported runs must remain checkpoints rather than losses. Use these observations to select bounded card/engine investigations and human playtests, not to raise every card's stats to force this pilot to win.

Next: purposeful exhaust/retention/Poison reward selection, conditional finisher valuation, HP-cost/potion decisions and same-encounter vanilla controls with specified decks. Extend to Mage/Ranger controls only after their corresponding native mechanics are represented, rather than using an Ironclad comparison to judge every class.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular2-lab `
  -Tag my-native-control -Class ironclad -Tier 0 -Policy tactical -Rewards deck -Python python
```

The wrapper validates independent lab provenance, refuses existing profiles/clients, launches hidden and offscreen, and stops only its verified PID. Native epochs and tutorials are changed only in those guarded test saves. Installed game files, real saves, Steam branches and Workshop releases remain untouched.
