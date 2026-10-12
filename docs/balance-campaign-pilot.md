# Natural campaign balance pilot

This stage replaces the fixed fourteen-card opening-fight fixture with natural runs: ten starter cards, Neow, legal map children, normal encounters, actual rewards, chests, rest sites and naturally acquired relics. It changes test tooling only. Card definitions, manor effects, traits, crowns and production gameplay are unchanged from PR #87.

The regular branch matrix uses `SPIRELEGACY-CAMPAIGN-1`, Ascension 0, no traits and an attack-first pilot. All nine runs reached a natural death; none was abandoned or counted as a victory. Floors are native `TotalFloor`, including Neow. Each run starts in a fresh disposable family and save profile, with no chronicle inheritance from another trial. The test profile uses generation 3 to avoid the first-family quick-start shortcut and reveals Neow's Timeline epoch so a new lab profile can take the ordinary opening event.

| Class | No manor: death floor / crowns | Partial manor: death floor / crowns | Full manor: death floor / crowns |
| --- | --- | --- | --- |
| Knight | 5 / 15 | 8 / 25 | 17, first boss / 56 |
| Mage | 4 / 13 | 8 / 25 | 15 / 50 |
| Ranger | 4 / 13 | 6 / 19 | 12, elite / 40 |

Partial manor is half each maximum level rounded down: +8 HP, +1 Strength, +1 Dexterity, +10% crowns and +1 post-combat healing; no Library draw. Full manor adds +20 HP, +2 Strength, +2 Dexterity, +20% crowns, +1 draw and +2 post-combat healing.

In these matched trials, manor progression substantially extends survival for all three classes. That supports the intended progression effect, but one seed and a simple pilot cannot establish character balance or campaign win rates. Early deaths without manor are not sufficient evidence for more buffs. The pilot lacks expert target selection, deckbuilding, potion use and shop purchases, and takes the first legal path even when it includes elites. These omissions can make its losses much harsher than experienced play.

A twelfth run repeats no-manor Knight with the defense-first policy on the same regular seed. It dies on floor 4 after 33 turns, versus floor 5 after 16 turns for attack-first. Prolonging fights while missing lethal opportunities can be worse than taking some immediate damage. This is a direct warning against tuning cards solely to either simple policy.

Beta v0.111.0 also completed a Mage/no-manor/attack-first run, dying on floor 3 for 10 crowns. A separate beta Mage/full-manor/defense-first run at Ascension 1 with Hero Complex and Diva started correctly at 112 HP, played through events, combat, treasure and a rest site, and died on floor 11 for 79 crowns. That verifies this diagnostic path and the difficult-trait startup on beta. It does not isolate trait difficulty: manor, policy and ascension also differ. Do not compare beta and regular results as paired encounters.

## Reward and card-use evidence

[balance-campaigns.csv](balance-campaigns.csv) records terminal state, crowns, policy, seed, branch and the exact DLL hash for each selected run. [balance-campaign-cards.csv](balance-campaign-cards.csv) records real reward offers, selected cards, observed hand entries, paid manual plays and cards left in hand at turn end. These are **pilot choices**, not human pick rates. The pilot always takes a card and has no skip policy; the data cannot tell whether a human would reject the whole offer. Special event grants and automatic card plays are outside these reward/manual-play counts.

The hand observer samples each frame and after card resolution, covering mid-turn draws and recovery as well as the initial hand. It detaches before ending the turn so the next turn's draw is counted once by the next decision. Retained cards appear again in the next turn's starting-hand sightings. `handEntries` is observed availability, not a native draw-hook count; `unplayedAtTurnEnd` includes retained cards and deliberate leftovers, but excludes fuel already consumed by other cards. HP fields and per-play HP changes are net values, not raw damage totals.

Some cards deserve a more informed follow-up:

- **Challenge** was played only 3 times in the full-manor Knight run and once in the full-manor Mage run. The attack-first scorer ignores the future payoff of Vulnerable, favoring cheaper immediate attacks. Improve that decision before interpreting low use as a weak card.
- **Reforge** was never played in the full-manor Mage or Ranger runs. The same policy spends Energy on attacks before its exhaust/draw/Block utility. Test it with an exhaust engine and purposeful fuel selection; this sample does not justify another numerical buff.
- **Volley** was played on all five observed hand entries in full-manor Ranger. The previous damage increase makes it usable in this run, but the sample is small and does not establish optimal power.
- Free **Cold Read** was used frequently across classes. Its selection frequency is partly built into the standalone draw score. It filters cards rather than supplying net hand growth; investigate it with cycle/Skill-trigger decks before calling that an infinite or nerfing it.

No additional stat changes are made from these pilot biases. The next balancing step is an experienced archetype policy or recorded human runs with skip, potion, shop and upgrade decisions, more matched seeds, and vanilla-character controls. Then isolate Challenge's Vulnerable payoff, Reforge's exhaust engine and expensive single-hit finishers versus multi-hit Strength/Vigor. Later-act and victory behavior remain untested because every collected run died in Act 1.

## Reproduce safely

Build Release against the owned regular API. Use an independently copied lab created by `scripts/sts2-lab.py`, never the installed game. The wrapper validates lab provenance, creates a new private profile, launches hidden/offscreen with the owned `--force-steam=off` option, and stops only its verified executable/PID. Existing lab clients and profiles are refused. All game data and raw journals remain ignored/private.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular2-lab `
  -Tag my-knight-zero -Class knight -Tier 0 -Policy attack -Python python
```

Use tiers 0/1/2, policies `attack`/`defense`, and optional `-Seed`, `-Ascension` and comma-separated `-Traits`. The pilot uses paid `TryManualPlay` and ordinary end turns. It injects no combat health, Energy, Block, enemy changes, defensive buffs or kills. It does not run the game's AutoSlay combat/event handlers, which apply such cheats. Only manor tier and starting heir are fixture inputs. Tutorials are disabled in these disposable profiles.

The bridge executes one bounded decision per request and journals each correlated response immediately. Unsupported overlays, stalled decisions and play/turn/time limits stop with explicit statuses; they are not losses or victories. Timed-out mutations are never automatically retried. Export only explicitly selected, completed journals:

```powershell
python scripts/summarize-balance-campaigns.py regular-knight-none regular-knight-partial regular-knight-full
```

Early pilot diagnostics that skipped Neow or failed beta RNG construction are excluded. The per-trial hashes preserve earlier successful builds: changes between them repair reward API compatibility, remove duplicate observer-disconnect errors, respect disabled rest buttons and add outcome fields. Earlier selected builds logged those disconnect errors after the observer had already detached; the completed measurements are retained with their exact hashes. Card data, combat scoring, target selection and hand-count boundaries are identical throughout the exported matrix. The final regular-built DLL completes natural diagnostic runs on both owned regular v0.107.1 and beta v0.111.0. The beta handicap client's startup log also contains a nonfatal Godot WorkerThreadPool `Invalid Task ID`; every campaign bridge command still completed. Its engine-level cause was not investigated in this balance pass. No installed game, real save, Steam branch or Workshop release was changed.
