# Heir-exclusive relics (1.0.21)

The Heir character pool starts with two common, three uncommon, and two rare relics. Every ordinary heir class can find all seven. Pacifist excludes Training Dummy, Family Seal, and Triune Crest because those effects require playable Attacks. Triune Crest follows the active class. Family Bloodline is the starter.

| Relic | Rarity | Effect | Balance reference |
| --- | --- | --- | --- |
| Heirloom Brooch | Common | Once per combat, after you play a Power, gain 5 Block. | Modest delayed defense; Anchor grants 10 Block at combat start. |
| Training Dummy | Common | The first time each turn an Attack leaves you with Block, gain 1 Vigor for your next Attack. | One Vigor per turn after a guarded Attack; it applies to every hit of the next Attack. Vajra grants 1 Strength to every Attack without this condition. |
| Threadbound Ledger | Uncommon | The first time you Exhaust a card during your turn, draw 1 card. | One draw per turn, compared with Dark Embrace drawing for every Exhaust. |
| Wayfarer Ribbon | Uncommon | The first time you discard a card during your turn, gain 3 Block. End-of-turn discards do not count. | Capped at 3 Block per turn; Tough Bandages grants 3 for every manual discard. |
| Triune Crest | Uncommon | Once per combat, Knight gains 8 Block when a Counterguard Attack hits. Mage gains 1 Energy after triggering Arcane Flow with 2 charges. Ranger applies 2 extra Vulnerable the first time Hunter Rhythm applies it. | Each class gets a one-use payoff for its signature mechanic. |
| Legacy Hourglass | Rare | Once per combat, if you end a turn with at least 2 unspent Energy, gain 3 Energy and draw 2 cards at the start of your next turn. | Bank Energy when you have a weak hand or can afford to wait. |
| Family Seal | Rare | Once per combat, after you play an Attack, a Skill, and a Power, your next Attack or Skill is played twice. | The three card types can be played on different turns. |

The audited native character pools each contain one starter plus one common, two uncommon, three rare and one shop-only relic. The Heir also has seven non-starter choices, with more frequent, lower-impact common/uncommon bonuses; none is restricted to the shop-only slot. All seven can appear in normal merchant relic slots.

These are conditional or bounded rewards, not manor replacements. They do not grant crowns, permanent family stats, passive healing or recurring Energy every turn. Relic Block follows the native unpowered Block rules. Vigor affects every hit of the next Attack, as it does for native cards. Arcane Charge stays capped at two. Native draw prevention and hand limits apply.

## Eligibility and acquisition

The native relic grab bag merges shared and selected-character relics, filters out Starter rarity, and supplies elite/event/chest rewards and merchant stock. The additions use that mechanism without a global reward patch. They are not added to the shared pool or native character pools. Family Bloodline retains its existing ID and inheritance behavior. Other characters keep their original pools.

New relics enter the grab bag when a new climb starts. Existing saved climbs retain their saved bag; this update preserves their progress rather than repopulating or duplicating their rewards. A newly acquired relic uses native reward/shop ownership, counters, hover descriptions and controller focus. There is no custom targeting UI.

## Limits and saves

Turn-limited effects record the turn before granting their bonus. Combat-limited effects claim before drawing or applying effects. Family Seal saves its card type progress until the combat ends. Legacy Hourglass saves whether its reward is waiting for the next turn. Used and unused appearance persists alongside the counters.

Native run saves preserve all acquired relic identities. The game controls its combat checkpoint behavior; an actual restarted combat receives normal fresh-combat limits. Existing family files, crown settlement, manor upgrades, saved card IDs and starter snapshots are unchanged. Full-run balance and physical Steam Deck validation remain playtesting tasks.

## Artwork and development

All seven 128px SVG icons and their separate white selection outlines are original CapeKid artwork, MIT. They use broad silhouettes, dark outlines and the existing family palette. `scripts/author-heir-relics.py` reproduces them; `sheets/relic_art.json` records file paths, SHA-256 and provenance. No extracted assets, external images or image fonts are used. Native inventory, outline, reward and large-icon views resolve these packaged textures through registered cache paths, including native call sites that inline texture getters.

`sheets/heir_relics.json` records rules, rarity, build role and current native comparisons. Both branch APIs are built from owned assemblies. `scripts/sts2-relic-check.py regular` (or `beta`) runs the opt-in private-profile bridge checks for native reward/shop eligibility and acquisition, all three classes, limits and reset behavior, saved claims/type masks/status, run save/resume, artwork and native relic focus. It also repeats existing card/reward checks. No test bridge is active in normal launches.

## Family Chronicle additions

Guardian Knot, Scholar Bookmark, and Trail Compass join the reward and shop pool for all heirs once their class lessons are recorded. Their original icons and outlines are included in the same authoring script and art manifest. The eligible run pool uses the saved family unlock snapshot. [Family Chronicle requirements, rewards and migration](family-chronicle.md)
