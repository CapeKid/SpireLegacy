# Heir-exclusive relics (1.0.21)

Seven original relics join the Heir character pool: two common, three uncommon and two rare. Every ordinary heir class can find all seven. Pacifist excludes Training Dummy and Family Seal, plus Ranger Triune Crest, because those effects require playable Attacks. Knight/Mage Triune Crest remains eligible. Six support shared family build routes; Triune Crest follows the active class. Family Bloodline remains the sole starter and is excluded from ordinary rewards and shops.

| Relic | Rarity | Effect | Balance reference |
| --- | --- | --- | --- |
| Heirloom Brooch | Common | Once per combat, after you play a Power, gain 5 Block. | Modest delayed defense; Anchor grants 10 Block at combat start. |
| Training Dummy | Common | The first time each turn an Attack leaves you with Block, gain 1 Vigor for your next Attack. | One Vigor per turn after a guarded Attack; it applies to every hit of the next Attack. Vajra grants 1 Strength to every Attack without this condition. |
| Threadbound Ledger | Uncommon | The first time you Exhaust a card during your turn, draw 1 card. | One draw per turn, compared with Dark Embrace drawing for every Exhaust. |
| Wayfarer Ribbon | Uncommon | The first time you discard a card during your turn, gain 3 Block. End-of-turn discards do not count. | Capped at 3 Block per turn; Tough Bandages grants 3 for every manual discard. |
| Triune Crest | Uncommon | Once per combat: Knight gains 4 Block after a Skill; Mage gains 1 Arcane Charge after a Skill (maximum 2); Ranger gains 3 Vigor after an Attack. | A small one-use class resource; no permanent stat or crown multiplier. |
| Legacy Hourglass | Rare | At the start of turn 3, gain 1 Energy and draw 1 card. Once per combat. | A delayed one-use burst; Pocketwatch can draw 3 each qualifying turn. |
| Family Seal | Rare | Once each turn, after you have played an Attack, a Skill and a Power, gain 4 Block and draw 1 card. | Three distinct types required; Ornamental Fan grants 4 Block per three Attacks and Letter Opener deals 5 to all per three Skills. |

The audited native character pools each contain one starter plus one common, two uncommon, three rare and one shop-only relic. The Heir also has seven non-starter choices, with more frequent, lower-impact common/uncommon bonuses; none is restricted to the shop-only slot. All seven can appear in normal merchant relic slots.

These are conditional or bounded rewards, not manor replacements. They do not grant crowns, permanent family stats, passive healing or recurring Energy every turn. Relic Block follows the native unpowered Block rules. Vigor affects every hit of the next Attack, as it does for native cards. Arcane Charge stays capped at two. Native draw prevention and hand limits apply.

## Eligibility and acquisition

The native relic grab bag merges shared and selected-character relics, filters out Starter rarity, and supplies elite/event/chest rewards and merchant stock. The additions use that mechanism without a global reward patch. They are not added to the shared pool or native character pools. Family Bloodline retains its existing ID and inheritance behavior. Other characters keep their original pools.

New relics enter the grab bag when a new climb starts. Existing saved climbs retain their saved bag; this update preserves their progress rather than repopulating or duplicating their rewards. A newly acquired relic uses native reward/shop ownership, counters, hover descriptions and controller focus. There is no custom targeting UI.

## Limits and saves

Turn-limited effects record the turn before granting their bonus; combat-limited effects claim before drawing or applying effects. This prevents nested card effects or serialization from paying twice. Family Seal also saves its partial type sequence, so replacing/reloading the relic does not erase a partly completed sequence. Used/unused appearance persists alongside the counters. New combats reset combat limits; new turns reset type progress and turn availability. Combat-ending/dead-player cleanup and automatic end-of-turn discards do not generate rewards.

Native run saves preserve all acquired relic identities. The game controls its combat checkpoint behavior; an actual restarted combat receives normal fresh-combat limits. Existing family files, crown settlement, manor upgrades, saved card IDs and starter snapshots are unchanged. Full-run balance and physical Steam Deck validation remain playtesting tasks.

## Artwork and development

All seven 128px SVG icons and their separate white selection outlines are original CapeKid artwork, MIT. They use broad silhouettes, dark outlines and the existing family palette. `scripts/author-heir-relics.py` reproduces them; `sheets/relic_art.json` records file paths, SHA-256 and provenance. No extracted assets, external images or image fonts are used. Native inventory, outline, reward and large-icon views resolve these packaged textures through registered cache paths, including native call sites that inline texture getters.

`sheets/heir_relics.json` records rules, rarity, build role and current native comparisons. Both branch APIs are built from owned assemblies. `scripts/sts2-relic-check.py regular` (or `beta`) runs the opt-in private-profile bridge checks for native reward/shop eligibility and acquisition, all three classes, limits and reset behavior, saved claims/type masks/status, run save/resume, artwork and native relic focus. It also repeats existing card/reward checks. No test bridge is active in normal launches.

## Family Chronicle additions

The existing seven relics remain available to a new family. Three additional Uncommon relics unlock after completed class lessons: Guardian Knot (one guarded-Attack 3 Block per combat), Scholar Bookmark (one draw after a Power per combat), and Trail Compass (3 Vigor on turn 2 once per combat). They become shared family reward/shop possibilities on future climbs. Their three original icons and outlines are included in the same authoring script and art manifest. The complete registered pool has ten drops plus the Bloodline starter; the eligible run pool uses the saved family unlock snapshot. See [Family Chronicle requirements, rewards and migration](family-chronicle.md).
