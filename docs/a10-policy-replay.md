# A10 Knight policy replay (#146)

Two natural campaigns replay the known full-manor Knight seed `BALANCE146-C` with the merged pilot corrections. Both end in losses at the same floor and crown payout as their historical references. The regular replay avoids the historical self-lethal blood-card payment, but that correction alone does not produce a successful campaign. Beta repeats a destructive event sequence. These observations identify remaining policy limitations; they do not justify weakening enemies, buffing cards or changing manor prices.

## Inputs and provenance

Each new campaign starts in a fresh disposable profile: Knight, no traits, full manor (Vitality 5 / Smith 2 / Armory 2 / Vault 4 / Library 1 / Garden 2), A10, generation 3 with Neow, planned combat and defensive rewards. No forced rewards or combat interventions occur. Starting investment is injected; these are not earned-fund purchase trajectories. Clients run sequentially in owned copied labs, with exact-process cleanup verified afterward. Personal saves and the real Steam installation are untouched.

New source: `edbd9331d97343457db0ec828c09ff8289140cdd`; DLL SHA-256: `1d16c0ba51b473a952566104386eae5e833b5308f29daf67f8aff6e3d6ea11e9`.

Historical source: `52cd89c4fe949d43d48f3cdc570dfcab39d3c7f7`; DLL SHA-256: `b13c50b1cdfa6a59e7125f39fc6798b06d2a08a0ac701705d8ad538431ae22d3`.

All four receipts use mod 1.0.30 and BaseLib v3.4.7. Regular is pinned to game v0.107.1; beta to v0.111.0. These are copied test versions, not assertions about current live branches. New receipts explicitly record animation speed 6. Historical code used speed 6, but its receipts lack that field; the manifest preserves it as null. Wait counts differ and are excluded from decision comparisons.

The card sheets and gameplay-effect code are unchanged between the two source revisions. The changed Core/Native files are listed in the comparison artifact and concern pilot decisions or test fixtures. Several pilot corrections intervene, so this is a comparison of cumulative policy revisions, not an isolated experiment on one fix.

## Outcomes and actual decisions

| Branch | Historical | Current | Crowns, each | Current coverage |
| --- | --- | --- | ---: | --- |
| Regular | Loss, floor 9 | Loss, floor 9 | 21 | 24 combat turns, 6 card rewards |
| Beta | Loss, floor 13 | Loss, floor 13 | 34 | 35 combat turns, 7 card rewards, 1 rest, 1 potion |

Neither replay reaches a shop, buys an upgrade or makes a setup-before-attack play. Regular uses no potion or rest site. Beta heals from 24 to 48 HP at the floor-8 rest site. It later uses a Block Potion at 1 HP, adding 12 Block to 8 against 20 incoming damage. These are exercised decisions, rather than coverage inferred from configured support. Offered, picked, hand-entry and played card observations are exported separately; they are not card win rates.

On regular, the first 51 non-wait replies match under the documented projection. The next reply, at floor 8, changes Bloodied Blade's position in the play sequence while ending at the same 36 HP. At the terminal floor-9 Byrdonis fight, the pilot begins turn 4 at 3 HP. The historical pilot played Shield and then Bloodied Blade, paying its final 3 HP. The current pilot leaves Bloodied Blade unplayed, plays Shield and two Strikes, then dies after ending the turn. Byrdonis has 6 HP left instead of 10. This verifies avoidance of that fatal payment in a natural run, not that a winning line was found or that HP safety caused every subsequent difference.

On beta, all 94 projected non-wait replies match. The projection compares action, floor, end HP, selected choice fields and ordered play IDs/upgrades/targets/net energy/net HP loss. It excludes scores, reasons, other detail and state fields; this is not full-receipt equality.

Both beta runs repeatedly decipher at floor 11. Recorded maximum HP falls 80 → 77 → 71 → 59; a later choice leaves 35 HP, and “Lose Everything” leaves 1 current HP before “Proceed” records 1 maximum HP. The pilot then dies at floor 13. `CampaignOracle` currently chooses the first visible unlocked event option that does not report immediate death. That guard does not assess escalating permanent costs. This is a concrete next policy problem, not evidence that the Knight's cards are too weak. The event observations do not establish which alternative would win.

## Evidence and limits

[Derived evidence](evidence/balance146-a10-policy-replay/manifest.json) retains four receipt hashes and exact artifact versions: two new campaigns and two historical references. Do not count the historical references as new trials or add them again to previous studies. The [comparison](evidence/balance146-a10-policy-replay/comparison.json) includes event health observations, terminal turns and the explicit decision projection. `decisions.jsonl`, `cards.csv`, `campaigns.csv` and `initial-conditions.json` provide the underlying sanitized observations. `trajectories.csv` has no rows: no paid purchase curve was measured here.

The exporter validated correlated unique command IDs, source/DLL identities, starting class/traits, unchanged paired configuration, successful replies and zero interventions. It exported 314 non-wait replies across the four receipts. The current merged-main build/design preflight passed with zero warnings/errors. This change adds evidence and documentation only; it does not change runtime code. Both new clients were verified stopped. New error logs contain two particle-warning matches on regular and one on beta, with no matched ERROR, InvalidTaskId, fatal-error, double-free or already-freed messages. These pattern counts are not a blanket clean-log guarantee.

Issue #146 remains open. No new victory, experienced-player comparison, Mage/Ranger campaign, hard-trait combination or paid manor trajectory is supplied by these two replays. The next bounded step is to calibrate event decisions against native escalating-cost and safe-exit controls, then revisit this beta seed. Broader archetype decisions and recorded winning campaigns remain required before claiming balance validation complete. The user's A10 full-manor Knight win on Steam Deck/public-beta remains qualitative evidence with unknown seed/build/deck.

The subsequent [event-policy calibration](event-pilot-calibration.md) implements that bounded next step and preserves this replay's original receipts as references.

## Reproduction

Build the committed source, then run each branch sequentially in an independently copied owned lab with a fresh unused tag:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab `
  -Tag YOUR-UNUSED-TAG -Class knight -Tier 2 -Policy planned `
  -Rewards defensive -Seed BALANCE146-C -Ascension 10 -AnimationSpeed 6 `
  -Python PATH-TO-PYTHON
```

Use the corresponding provenance-verified beta lab for the second attempt. Preserve every receipt and stop at the configured cap or error; do not retry timed-out mutations. The existing exporter can export a private folder containing only the four selected completed receipts. The comparison projection is specified above so comparisons do not accidentally equate timing noise or extra score fields with gameplay changes.
