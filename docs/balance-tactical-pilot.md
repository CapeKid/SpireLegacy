# Tactical balance pilot

This continues the natural-run stage after PRs #87 and #88 were merged. It improves the diagnostic player rather than changing card stats. The earlier pilot's low use of Challenge and Reforge mixed card value with poor ordering and fuel selection.

The new `tactical` policy uses the owned game's damage and Block previews, modified Energy costs, visible incoming attacks and all hittable targets. It values an affordable follow-up after Vulnerable, totals area attacks across enemies, estimates Weak/Poison utility and recognizes exhaust-engine triggers with status/curse fuel. Mandatory hand discard/exhaust choices prefer low immediate-value cards; recovery prefers higher-value cards. Each manual play records its decision reason and selected enemy model. Attack-first and defense-first remain available for controls, and reward selection still uses the earlier standalone-utility policy.

This is a heuristic, **not an expert solver**. Damage previews are estimates, especially for class bonuses, multi-hit effects, random targeting, damage caps, reflected damage and boss phase changes. Lethal priority applies only to simple single-hit Legacy attacks and does not optimize overkill Energy spending. The Vulnerable calculation considers one affordable follow-up and a limited next-turn payoff, not an entire sequence. It has no purposeful archetype deckbuilding, reward skipping, potion use, shop purchases or upgrade planning. Mandatory event upgrade/transform choices may be poor because the generic fuel selector does not identify the prompt's purpose. Do not treat it as an optimal player or use its deaths to prescribe blanket buffs.

## Native decision checks

Four paid-play checks pass on each owned branch, regular v0.107.1 and beta v0.111.0, using the same regular-built DLL:

| Decision | Actual native result |
| --- | --- |
| Challenge before Strike | Applies 2 Vulnerable, then Strike deals 7 damage; spends 3 Energy total |
| Reforge with Wound and Feel No Pain | Chooses Reforge, exhausts the Wound and gains 6 Block |
| Killable second target | Chooses Strike against the second enemy at 1 HP and kills it instead of attacking the first enemy |
| Area damage | Chooses Sweeping Edge over Strike and deals 3 damage to each of two enemies |

The checks explicitly enter owned Nibbits Normal with two enemies. Durable targets and the injected exhaust engine belong only to these effect fixtures, not the natural campaign runs. Initial diagnostics used a one-enemy quick-start encounter and mistakenly used single-target Volley as an area card; those failed diagnostics are excluded from the final verification. Corrected fixtures verify both target count and actual resolved effects.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular2-lab `
  -Tag tactical-check-new -Class knight -Checks -Python python
```

The eight final checks are retained privately as `tactical-two-regular` and `tactical-two-beta`. Their DLL hash is `cf6299070b1e2c4a6e055cddbf7c4ef1ac43221249d0d249bda487b49114c1a5`.

## Matched natural runs

The comparison uses regular v0.107.1, no manor, no traits, Ascension 0, natural starter decks, the same first legal path and the same reward policy. Seeds are `SPIRELEGACY-CAMPAIGN-1` and `SPIRELEGACY-CAMPAIGN-2`. It compares attack-first and tactical for each of the three classes. Seed-1 attack-first controls are the completed trials from PR #88; all new runs use the final DLL above. There are no card-data changes between stages.

The accompanying [trial CSV](balance-tactical-campaigns.csv) and [card CSV](balance-tactical-campaign-cards.csv) retain exact hashes and observations. Nine new runs plus three existing controls make twelve paired observations, not twelve independent new campaigns. Card counts describe the pilot's actual offers, picks and manual plays rather than human pick rates. All selected runs end at a natural death in Act 1; later-act and victory balance remain unproven.

| Class | Seed 1: attack-first → tactical death floor | Seed 2: attack-first → tactical death floor |
| --- | --- | --- |
| Knight | 5 → 6 | 6 → 12 |
| Mage | 4 → 5 | 6 → 8 |
| Ranger | 4 → 5 | 6 → 6 |

On seed 1, Knight reaches floor 6 rather than 5, Mage 5 rather than 4, and Ranger 5 rather than 4. Reforge is played five times in tactical Knight and Ranger versus zero times in their attack-first controls. Challenge is played once in tactical Knight versus zero in attack-first. This supports investigating ordering/engine usage before changing their numerical stats; it does not prove that either card is ideally balanced.

On seed 2, Knight reaches floor 12 rather than 6 and Mage reaches 8 rather than 6, while Ranger reaches floor 6 with either policy. Survival improvement is therefore uneven and sensitive to the encountered fights and available cards. The broader next step is vanilla-character controls with equally complete heuristics, purposeful archetype/reward choices, potion/shop/upgrade decisions and additional seeds. Card-specific isolation should compare Challenge's full Vulnerable payoff and Reforge's native exhaust-engine combinations rather than only damage per Energy.

Reproduce campaigns with `-Policy tactical` and an explicit seed. Export to a separate prefix to preserve the previous stage's dataset:

```powershell
python scripts/summarize-balance-campaigns.py --prefix balance-tactical-campaign `
  regular-knight-none tactical-knight-seed1 attack-knight-seed2 tactical-knight-seed2
```

Core validation remains 3,401 assertions; sheet preflight passes 32 sheets. The regular-owned Release build has no warnings or errors. All clients use independent private labs and fresh profiles, and the wrapper stops only verified PIDs. No installed game, real save, Steam branch, production card data or Workshop release is changed.
