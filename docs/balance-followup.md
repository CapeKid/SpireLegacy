# Balance follow-up: setup costs, Poison and retained growth

This pass follows the eight fixes merged in PR #84. It addresses the next three candidates, Poison seeds, historical recovery and a confirmed recovery loop. Starter cards, manor bonuses, crown payouts, saved card IDs and reward-pool membership are unchanged.

| Card | Change | Purpose |
| --- | --- | --- |
| Second Wind | Exhaust one other hand card instead of two; keep 1 Energy and 9/11 Block | Useful defensive exhaust support without consuming most of a small hand. Exhaust engines still reward the one consumed card. |
| Rune Familiar | 1 Energy at both levels; draw 1/2 whenever a Power is played | Affordable setup and an upgrade that improves the engine. Power cards are consumed, so the draw trigger alone is finite. |
| Long Vigil | 2 Energy; 15/19 initial damage, still growing by 6/7 when retained | A playable retained finisher; full Blacksmith supplies 17/21 initial damage. |
| Poison Tip | Apply 4/6 Poison instead of 3/4 | A dedicated Poison seed has a useful edge over hybrid Poison attacks. Still below the owned regular Deadly Poison's 5/7. |
| Toxic Cloud | Apply 4/6 Poison to all enemies instead of 3/4 | Better payoff for a two-Energy setup turn without adding Weak, free Energy or global Poison scaling. |
| Rekindle (historical) | Recover one exhausted card instead of selecting zero | Restore the advertised effect for existing saves and cross-color access. |

Poison still receives no Strength bonus. Full manor helps Poison builds through extra draw, health and defense rather than changing the native Poison formula. Over two enemy turns, a lone 3/4 seed dealt 5/7 damage; the new 4/6 seed deals 7/11. Doubling remains single-use, and its setup should be assessed as a combination rather than comparing Poison directly with immediate attack damage.

## Confirmed functional problems

Two discounted recovery cards could retrieve one another for zero Energy. The native baseline reproduced Arms Vault returning Returned Prophecy and Returned Prophecy returning Arms Vault. That creates repeatable free Skill/Exhaust triggers after cost-reduction setup. Exhaust recovery now excludes every Spire Legacy card with an Exhaust-recovery effect, not merely another copy of itself. Ordinary exhausted cards and discard recovery remain eligible. The card text states the restriction.

The same cycle reproduced between discounted Memory Prism and Hooked Arrow through the discard pile. Discard recovery now excludes both discard-recovery and Exhaust-recovery cards, closing mixed/copy variants too. Ordinary Attacks and Skills remain eligible; a discard-recovery card can still be rescued from Exhaust. Both native zero-Energy loop probes now stop before returning the partner card, including the empty-eligible-pile case.

Retention growth was changed only in dynamic variables and disappeared in native serialization. Long Vigil's base damage grew 15 → 21 → 27, but the restored card lost that growth. A saved combat-growth field now reconstructs both retained damage and Block, survives upgrade/downgrade, and resets for a new combat. The native fixture checks all 12 affected historical/current card types at both upgrade levels; Long Vigil additionally goes through two real end-turn flushes and paid play.

Pool expansion previously overwrote several PR #84 changes. It now reapplies targeted balance overrides. A disposable regeneration test verifies all targeted cards, stable identities, pool membership and regular/beta sizes. References and catalogue text are updated with the gameplay data.

## Reproducible native balance checks

`scripts/sts2-balance-followup.py` drives isolated owned-game labs. Every response records actual DLL hash, game branch, seed, mods and disposable profile. Candidate probes verify paid effects, the closed recovery loop, Poison decay and retained-card serialization. Protection and durable targets are used only in those effect-isolation probes.

The encounter matrix uses native Nibbits Normal, the ten-card starter deck plus four fixed archetype cards, ascension zero, no inherited handicap traits, and the same seeds at three manor tiers. It covers Knight exhaust, Mage Powers, Ranger retention and Ranger Poison. The deterministic pilot prioritizes Powers, immediate defense against visible incoming damage, then draw, Poison and attacks. It receives no injected Energy, health, Block or enemy-stat changes in these encounters. Native test selection resolves prompts; the pilot is capped at 20 turns and 30 plays per turn. Tutorials are disabled only in the disposable balance profiles before combat; the fixture also clears incidental modals. Animation time is accelerated only while the guarded fixture runs.

The output records plays, initial-hand sightings, cards remaining unplayed at turn end, net Energy, final health, death/victory and turns. `damageTaken` is **net HP lost**, including any post-combat Garden healing, not a sum of raw damage events. Twenty-turn survivors are **capped**, not recorded as losses. Initial-hand sightings exclude mid-turn draws and are not a reward pick-rate measure.

These are controlled opening-encounter stress tests, not whole-campaign win rates. The simple defense-first pilot can miss lethal opportunities and does not optimize exhaust or retention choices. Comparing before/after within a branch helps reveal setup costs and dead draws; it cannot justify broad numerical buffs by itself. Regular and beta encounter composition can differ, so do not compare their win rates directly.

## Remaining balancing work

- Play full campaigns at the three manor tiers with matched seeds; record act reached, boss outcomes, crowns and purchased upgrades. Include attack-first play and experienced archetype play to complement the stress pilot.
- Capture actual reward offers and choices, plus mid-turn draw/play sightings. Do not use the harness's initial-hand counts as complete pick/play statistics.
- Expand to single-target bosses, multi-hit enemies and fights that punish Skills or Powers; then test difficult trait combinations and higher ascensions.
- Reassess expensive single-hit finishers versus Strength/Vigor multi-hit builds, and Poison multipliers after the seed buffs. Preserve archetype differences rather than raising every card to the same damage per Energy.

## Observed results

The controlled matrix completed 36 original-stat regular trials, 12 updated regular trials matched to seed 1, and 12 updated beta trials. The table pairs **only seed 1 on regular**, using net HP lost. These are opening encounters with fixed decks and a simple pilot, not campaign win rates.

| Build | Manor | Original outcome / HP lost | Updated outcome / HP lost |
| --- | --- | --- | --- |
| Exhaust | None | death / 60 | win / 49 |
| Exhaust | Partial | death / 68 | win / 23 |
| Exhaust | Full | turn-cap / 77 | win / 14 |
| Powers | None | death / 52 | death / 52 |
| Powers | Partial | win / 45 | win / 30 |
| Powers | Full | win / 28 | win / 22 |
| Retention | None | win / 39 | win / 33 |
| Retention | Partial | win / 27 | win / 27 |
| Retention | Full | win / 14 | win / 14 |
| Poison | None | death / 56 | win / 46 |
| Poison | Partial | win / 37 | win / 35 |
| Poison | Full | win / 20 | win / 20 |

Knight exhaust and Ranger Poison now clear the selected regular encounter at every tier. Mage Powers still fail at no manor under this pilot and improve at partial/full manor. Ranger retention already worked before this change; its lower setup cost improves flexibility without evidence for a broad pool buff. Repeat with more updated seeds, better target selection and full campaigns before treating this as a balance target.

The full 60-trial table is [balance-encounters.csv](balance-encounters.csv). Original-stat trials retain the old six card definitions; later baseline completions use the repaired diagnostic helpers. Recovery/serialization fixes do not participate in these fixed-deck trials. Failed tutorial/turn-transition runs are omitted. The final end-turn fixture checks surviving opponents as well as native phase flags, so a Poison victory cannot be mistaken for a stalled next turn.

Final effect validation: 3,401 core assertions, 32-sheet preflight, disposable authoring regeneration, 12 base/upgraded candidate probes, both zero-Energy recovery-loop refusals and 24 retention save/upgrade/new-combat cases per branch. The same final regular-built DLL (SHA-256 `e3b07ce430a427e769125cbcdc950e3f47882ffe8ba26c0eeb04eb8cb0dcdde6`) runs on owned regular v0.107.1 and beta v0.111.0. Evidence is `regular-complete-probes`, `beta-complete-probes`, `baseline-finished`, `regular-complete-matrix` and `beta-complete-matrix` under the ignored evidence directory. The final effect/loop probes use that immutable DLL; earlier completed encounter rows retain their individually recorded DLL metadata and use identical card stats/pilot policy for their stage. No installed game, real save, branch or Steam session was changed.
