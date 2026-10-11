# Family Chronicle

Open **Family Manor & Heir Lab**, then **Family Chronicle**. Use D-pad/left stick to choose an unlocked entry, A to read, B to return, and LB/RB to scroll. Locked entries show their requirement and reward, without pretending to be selectable. Unread stories are marked NEW. Read status persists separately from unlock status: reading is optional and never spends crowns.

Nine original stories follow the family: its founding, first completed climb, first confirmed manor repair, a lesson from each class, and an epilogue for each class victory. Finish a Knight, Mage or Ranger climb that reached floor 6 or higher to record that class's lesson. Death and abandonment count; unfinished/resumed climbs do not award it early. All three lessons unlock relics for every heir in that family, starting with the next climb:

| Lesson | Added Uncommon relic | Effect |
|---|---|---|
| Knight | Guardian Knot | Once per combat, after an Attack while you have Block, gain 3 Block. |
| Mage | Scholar Bookmark | Once per combat, after a Power, draw 1 card. |
| Ranger | Trail Compass | At the start of turn 2, gain 3 Vigor once per combat. |

These are additional native reward/shop possibilities, not free starting relics. They preserve the existing seven family relics, all ordinary card choices, six Ancient cards and three potions. Each has an original packaged icon and selection outline. Guardian Knot is much smaller than Anchor's immediate 10 Block, Bookmark grants only one draw per combat, and Compass adds a delayed next-Attack bonus rather than permanent Strength. Pacifist excludes the two attack-specific relics, as it does the existing attack-specific keepsakes.

A class victory unlocks the corresponding story and a selectable family motto: **Unbroken Oath**, **Living Archive**, or **Open Trail**. Read that entry and choose **Use family motto** to display it beneath the combat banner. Clear it from the Chronicle list. Mottos grant no combat stats or crown bonus.

Progress is additive in the existing `family.json`, alongside purchased upgrades and run snapshots. Repeated settlement cannot duplicate class records/unlocks. Existing families retain all crowns, names, offers and manor levels; generation and purchased upgrades recover the first-return/restoration entries. Old saves do not identify past heirs' classes or victories, so those feats begin recording now. No victories are invented. An old unfinished run keeps its old relic pool. Every new run snapshots its eligible Chronicle relics; save/resume uses that snapshot even if the family file later gains a milestone. Progress and read flags use the existing atomic save/backup mechanism.

BaseLib **3.4.7** explicitly skips custom-character unlock, boss and elite epoch checks in `Patches/Compatibility/UnknownCharacterPatches.cs`, with a TODO for custom epochs. Its localization model registry also notes that EpochModel is not an AbstractModel. This version offers no supported custom Timeline content-registration route. The Chronicle therefore uses its own family-local screen and counters; no native character's Timeline epoch is overwritten or unlocked.

Core regression checks cover threshold boundaries, death/victory distinctions, duplicated outcomes, truthful legacy migration, confirmed purchases, read/motto persistence and frozen run eligibility. `scripts/sts2-chronicle-check.py regular` (or `beta`) exercises locked/unlocked native bags, reward/shop generation, acquisition, bounded effects and saved claims, native save/resume, and actual manor controller actions in isolated owned-game profiles. Physical Steam Deck readability and controls remain acceptance checks on the device; the PC lab verifies the same action routes.
