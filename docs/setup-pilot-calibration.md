# Strength/Vigor ordering calibration (#146)

The deterministic pilot used immediate per-card scores to choose its next play. That can spend a multi-hit Attack before an affordable Strength/Vigor setup card, losing an existing build's payoff. This is a measurement-policy gap; it does not establish that the cards need buffs.

The opt-in pilot now performs one bounded ordering check after choosing its ordinary best card. It can put a known direct Strength/Vigor setup before that Attack when both fit the current energy budget and incoming damage is fully covered by existing Block. It preserves an Attack whose native preview already appears lethal. It leaves the original choice intact when that choice is not a targeted Attack. It recalculates its ordinary choice after each actual play rather than executing a reserved action blindly.

Setup candidates come from the ordinary legal/affordable choices. The conservative adapter permits native Inflame or a non-Attack Legacy card with positive Strength/Vigor, no damage, HP loss, healing, immediate draw or discard, and no special/named-power effect. This deliberately excludes Sorcery Lesson's draw, Blood Oath's HP payment, conditional setup and arbitrary native cards. The returned `setup-before-attack` score is an ordering priority, not a forecast of damage or an expert full-turn evaluation. Ordinary gameplay, card stats and player controls are unchanged.

## Results

All 48 final synthetic cases passed: 24 on each pinned owned branch. Each branch covers the three heirs and native Ironclad with old-order and new-order paid plays, plus four controls per character. In both branches the new chooser declines to reorder with insufficient energy, an already lethal Attack or uncovered incoming damage; it permits the setup when incoming damage is already fully blocked.

| Character | Setup → Attack | Old-order damage | Setup-first damage |
| --- | --- | --- | --- |
| Knight | Weapons Master → Redoubled Blow | 4 | 28 |
| Mage | Spell Surge → Conduit Lance+ | 20 | 30 |
| Ranger | Patient Hunter → Rapid Fire | 8 | 20 |
| Native Ironclad | Inflame → Twin Strike | 10 | 14 |

These are native observed totals against the controlled target, not new card values. Knight gains its existing +2-per-hit Skill counter as well as Weapons Master's Strength/Vigor. Mage's setup is a Power, so this example gains Strength without a Skill charge. Ranger's Vigor applies across all four hits. Each paid follow-up fits the budget, matches its modified native damage preview, and consumes Vigor where present.

Final runtime source is `cef879515c246b010624b309b8955f0fe34b8034`; DLL SHA-256 is `e5b0bde65313b890c23de1e9b29b2593ce75f20218abfc16369c5cbfbe6722c2`. Both tests use mod `1.0.30` and BaseLib `v3.4.7`; copied game versions are regular `v0.107.1` and beta `v0.111.0`. This is not evidence for newer live game versions. The build/design preflight passed with zero warnings/errors and all 20 harness checks passed. Both copied clients were verified stopped.

[Calibration evidence](evidence/balance146-setup-sequencing/calibration.json) includes exact receipt/artifact hashes, case results and log-pattern counts. The initial fixture error is excluded from the 48 final cases and retained with its last observation. Regular's final error log contains 18 backup-deletion error lines; beta contains 18 plus one startup `Invalid Task ID` from Godot script registration. Neither has a fatal-error match. Renderer warnings also remain. These logs are not described as clean, and the completed assertions do not establish a cause for the native messages.

This closes one specific build-sequencing gap in #146. No natural campaign, win or paid manor return was collected in this calibration. Reward/build planning, other archetype sequencing and recorded winning campaigns remain before drawing broader balance conclusions. In particular, immediate HP-cost safety for Legacy blood cards still needs calibration: the current adapter recognizes the named native self-damage cards but does not yet describe Legacy `hpLoss` payments. The new setup rule explicitly excludes those payments.

## Reproduction

Build a committed artifact and use a new evidence tag with a provenance-verified independent owned regular/beta lab:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab `
  -Tag YOUR-UNUSED-TAG -Class knight -SetupChecks `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

The guarded driver creates fresh starts with no traits/manor upgrades for Knight, Mage, Ranger and native Ironclad. Each probe injects a two-card hand, target HP and a controlled energy budget in Nibbits. It compares the previous chooser (`sequence=false`) with the new chooser, then checks insufficient energy, an already lethal Attack, uncovered incoming damage, and incoming damage fully covered by existing Block. The synthetic incoming value tests the ordering guard; it is not a claim about the enemies' actual intentions.

The Mage case uses upgraded Conduit Lance: its ordinary version already scores below Spell Surge and therefore does not expose this ordering gap. Other fixture cards are unupgraded. The probe plays through the native paid-card path, observes Strength/Vigor, compares actual damage with the native modified preview and checks that the follow-up remains affordable. A Vigor stack must be consumed by the Attack. These injected fights cannot count as natural campaigns, wins, crown pacing or representative human play.

An initial fixture incorrectly expected Sorcery Lesson to qualify despite its immediate draw. The rule correctly excluded it; the initial private error receipt is retained separately. The fixture was corrected to Spell Surge without broadening the conservative rule. Native repeated-start logs can contain backup-deletion errors and renderer warnings; log-pattern counts must accompany results rather than a blanket clean-log claim.
