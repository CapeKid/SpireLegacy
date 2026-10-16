# Difficult trait and starting-relic campaigns (#146)

This checkpoint extends natural campaign coverage to legal trait/relic combinations on both branches. It follows [pilot threat calibration](threat-pilot-calibration.md). No cards, trait effects, manor prices or crown payout rules are changed.

## Predeclared protocol

Twelve fresh private campaigns cross regular/public-beta, Knight/Mage/Ranger and two trait configurations. The control has Antique alone; the challenge adds Hero Complex (`megahealth`) for Knight, Crippling Intellect (`bonusmagicstrength`) for Mage, or Pacifist (`cantattack`) for Ranger. All three pairs are canonical, enabled and mutually compatible, with at most two traits. Antique grants one real random common relic through the ordinary trait path; no item is chosen or injected by the fixture.

Every trial uses seed `BALANCE146-E`, Ascension 5, full manor, generation 3, Neow enabled and FTUE disabled. Full manor is an explicit starting-investment fixture, not paid purchases in these standalone campaigns. Trait effects, random relics, naturally offered cards, events and normal combat remain active. The existing `planned` policy with `defensive` rewards is fixed; no forced card rewards, debug combat entries, enemy HP edits or extra combat resources are used.

The control/challenge pair shares class, branch, initial investment and seed. Exact starting relics are verified from the live start response, not inferred from Antique's description. Differences between game branches can change native RNG, rewards and encounters. Trait effects also change combat and subsequent decisions, so terminal floors are exploratory descriptions, not isolated causal effects or human win rates. One seed does not establish representative trait difficulty.

Existing caps remain 20 minutes/1200 decisions per campaign, 40 turns per combat, 50 plays per combat decision, 45 seconds continuous waiting and a 180-second command timeout. Each trial has a new disposable family profile; no inherited Chronicle rewards are carried between cases. Failed or capped attempts remain in the evidence manifest and are excluded from completed outcomes. The wrapper verifies lab provenance, pins the loaded artifact and cleans up only its exact executable/PID.

## Starting-condition integrity

The exporter now records starting HP/max HP and relic IDs in `initial-conditions.json`. It checks each successful standalone start against the recorded class, traits, manor tier, Ascension and seed, then verifies the loaded heir's class and traits against those inputs. Multiple starts or a changed configuration are rejected. Native Ironclad controls are explicitly labelled without claiming their family's unused heir record represents the native player. Names, localized relic titles, private profile paths and save identifiers are omitted.

Twenty-two exporter integrity tests pass, including loaded-trait mismatch, changed seed, multiple starts, omission of names/paths and native task-ID/fatal-error counting. Separate private re-exports of the twelve earlier Mage/Ranger campaigns and all prior #146 receipts also pass with the new starting-condition checks. The latter retains 50 completed campaigns, 200 settled returns and 21,258 decisions, with the existing stopped-report annotations. Earlier public snapshots remain unchanged.

## Results and exclusions

Ten attempts ended in natural deaths (`hp=0`, `wins=0`), one hit the combat-turn cap and one crashed. The export includes 1,636 decisions from completed campaigns. Capped/crashed attempts remain in the manifest and separate diagnostics, contributing no natural outcome or earnings. No winning campaign was obtained.

| Class | Configuration | Regular | Public-beta |
| --- | --- | --- | --- |
| Knight | Antique | Death, floor 33; 115 crowns | Death, floor 33; 86 crowns |
| Knight | Antique + Hero Complex | Death, floor 24; 120 crowns | Death, floor 23; 110 crowns |
| Mage | Antique | Death, floor 24; 61 crowns | Death, floor 22; 51 crowns |
| Mage | Antique + Crippling Intellect | Death, floor 17; 61 crowns | Crash; last observation floor 23/3 HP |
| Ranger | Antique | Death, floor 21; 54 crowns | Death, floor 22; 54 crowns |
| Ranger | Antique + Pacifist | Turn cap, floor 7/6 HP | Death, floor 8; 38 crowns |

All six control/challenge pairs have matching starting relic IDs within their branch: Family Bloodline plus Meal Ticket on regular, or Family Bloodline plus Gorget on beta. Knight maximum HP is 80/120, Mage 72/54, Ranger 76/76 for control/challenge respectively. The live starts verify the canonical trait pairs and the +40/-18/0 maximum-HP differences. Ascension 5 starts wounded; those exact current-HP values are in [initial conditions](evidence/balance146-hard-traits/initial-conditions.json).

Completed campaigns made 16 shop purchases and 29 rest choices: 21 card upgrades and eight healing choices. Five Fire Potions were used, all on beta; this is narrow potion coverage, not evidence of broad potion mastery. Hero Complex runs made upgrades at rest sites and no healing choices. [Usage records](evidence/balance146-hard-traits/usage.json) also retain the incomplete attempts' observed choices, clearly labelled by status and excluded from these totals. Handicap crown bonuses remain active; these payouts must not be pooled into the no-bonus manor pacing curves.

Every campaign used runtime source `01ad843bb0ae9c6b765cf75fb33945adc0a09c10`, DLL SHA-256 `871ea7a01a4fbf4b4bb16d10daab5edb3348d3a88b67299ea6d54fcb148425a0`, mod `1.0.30` and BaseLib `v3.4.7`. Games are regular `v0.107.1` and public-beta `v0.111.0`. That runtime source is unchanged in merged main `9939b4b`. This PR changes export tooling and evidence only. Exact receipt hashes, log-pattern counts and exclusion status are in [the manifest](evidence/balance146-hard-traits/manifest.json); [card observations](evidence/balance146-hard-traits/cards.csv) and [decisions](evidence/balance146-hard-traits/decisions.jsonl) preserve completed-run choices. The header-only trajectories table means this study collected no paid multi-return trajectory.

Two material limitations emerged:

- The beta Mage challenge crashed after the last logged Frost play against Louse Progenitor. Its native error log reports an access violation (`0xC0000005`) in `GodotObject.Dispose`/`MegaSpineBinding.Dispose` through `NCreature.AnimDie`, preceded by an Invalid Task ID message. Two copied clients were running concurrently, with combat accelerated sixfold. This needs isolated reproduction, including normal animation speed, before attributing it to the mod, card or trait. The 180-second response guard stopped the attempt; the pending mutation was never replayed. The original beta batch stopped, and only its two unstarted Ranger cases were launched separately with fresh profiles.
- Both Pacifist attempts acquired the unplayable non-Basic Attack Merciless. Beta also took Double Shot and Brave Advance. The capped regular case played Reforge 43 times across the run; its final three turns contain only Reforge, with no remaining hand. The pilot's amount adapter omits converted starters' `MercyPoison`, its reward policy scores blocked Attacks, and its fuel selector uses generic card value when choosing exhaust fodder. These are concrete policy limitations; the cap and loss cannot establish Pacifist's balance. The next policy calibration should account for legal rewards, inherited Poison and preservation of damage sources when exhausting cards.

[Excluded diagnostic records](evidence/balance146-hard-traits/diagnostics.json) retain the crash context and capped-run decisions without treating either as a terminal loss. Log counting now recognizes both `Invalid Task ID` and `InvalidTaskId`, and explicitly counts `Fatal error`; these are line matches, not incident counts. Renderer/audio warnings also appear in private logs. No blanket clean-engine-log claim is made.

## Reproduction

Use the committed mod artifact and independent copies of owned game installations with provenance and dependencies under ignored `private/`. Run the control and challenge for each class on each branch, using a unique evidence tag:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-coop-regular3-lab `
  -Tag YOUR-UNUSED-KNIGHT-CHALLENGE-TAG -Class knight -Tier 2 `
  -Policy planned -Rewards defensive -Seed BALANCE146-E -Ascension 5 `
  -Traits antique,megahealth `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

Collect this study's twelve `balance-campaign-balance146-traits-*.json` receipts into a separate private folder after workers stop. Export it with `scripts/export-balance-pacing.py` to `docs/evidence/balance146-hard-traits`. Do not overwrite older evidence snapshots or count running/unsupported attempts as natural outcomes.

## Remaining scope

The [single-client crash isolation follow-up](mage-crash-isolation.md) subsequently passed the original beta Mage failure point at both normal and sixfold animation speed, with all 179 prior completed decisions matching. Both fresh trials ended naturally at floor 30. The original native crash remains recorded and its cause is unresolved; this is not an animation fix or an additional independent-seed balance sample.

Issue #146 remains open for developed-archetype decisions, recorded current-build winning campaigns and experienced-player comparison. The fixed pilot remains a heuristic with limited potion/relic support and usually chooses the first non-immediately-lethal event option. Configured shop/rest/potion support is not evidence those decisions were reached or used; actual usage must be counted from these receipts. The user's A10 full-manor Knight win on Steam Deck/public-beta remains a qualitative report with unknown seed/build/deck. Physical device coverage was waived when #110 was closed; this work does not reopen it or publish a release.
