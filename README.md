# Spire Legacy â€” local development build

A solo custom character for **Slay the Spire 1, regular branch**. Your first heir is random. After each climb, name your family, choose one of three heirs and spend legacy crowns on permanent manor upgrades.

## What players get

- Three classes: Knight, Mage and Ranger, with distinct 10-card starting decks and separate reward pools.
- All 61 eligible entries in the installed RL2 trait library have turn-based adaptations, with two compatible traits per heir. Hover icons or names for exact effects. [Trait coverage and adaptation notes](docs/traits.md).
- 75 cards per class: 30 shared family cards and 45 unique cards, including 3 basics. Each pool has 20 common, 36 uncommon and 16 rare cards. The character has 165 distinct active cards, with original illustrations and upgrades. [Card pool guide](docs/card-pools.md) · [Current card catalogue](docs/card-catalogue.md).
- Six manor upgrades for health, Strength, Dexterity, card draw, legacy earnings and recovery.
- A customizable family name and four banner colors.
- Normal Slay the Spire enemies, events, maps and three-act runs; the first run goes directly to a first-floor fight.
- Persistent family progression, run identities, atomic saves and backup recovery.
- Genuine Rogue Legacy 2 class and trait sprites loaded from the player's installed copy at launch. These appear on heir choices, the combat HUD and the character. Cards use original effect-based illustrations. Combat values are original adaptations for turn-based play, not an exact port of Rogue Legacy 2's action combat.

[Steam Workshop packaging and installation](docs/workshop.md) supports normal mod startup with automatic local content preparation. The Workshop upload workspace defaults to private visibility. [Spire Legacy on Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3816067184) is currently hidden for creator testing, with ModTheSpire and BaseMod set as Required Items. Windows subscription loading and fresh-cache preparation passed in an isolated native game run.

## Required games and platform

Windows x64 has been tested in-game. The native Linux x86_64 package supports Steam Deck installation, with creator-verified Steam Deck gameplay using both Steam Linux Runtime and Proton. Both require owned Steam copies of Slay the Spire 1 (regular branch, build 10180494 / v2.3.4 tested on Windows) and Rogue Legacy 2 (build 13303339 content target). Steam library discovery locates Rogue Legacy 2 automatically. It does not modify that game's installation or saves. No multiplayer.

## Build

Design lives in `sheets/*.json`. Run `scripts/build.ps1`: it checks every cell for missing values, checks sheet references and real source assets, generates Java definitions and compiles the mod. Runtime asset conversion uses `scripts/load_rl.py`; a packaged Python runtime removes the need for players to install Python.

This repository contains the development source and issue fixes. Local tools, game jars, runtime caches, extracted game content, test saves and release archives are excluded. The Windows build scripts currently expect the development setup described below; a fresh clone does not include those dependencies.

- Java 8 JDK under `tools/jdk/`; Python 3.12 environment under `tools/python/` with Pillow and UnityPy (PyInstaller and archspec for the portable reader).
- Redistributable ModTheSpire 3.30.3 and BaseMod 5.56.0 jars under `tools/`, plus your own installed StS1 `desktop-1.0.jar` at the path in `scripts/build.ps1`.
- Local asset inspection via `scripts/inspect_rl.py` creates `private/rl_asset_index.json`; the owned host card atlas is inspected locally as `private/cardui.atlas` for preflight.
- Packaging also expects the locally frozen reader under `private/helper-release/ReadRogueLegacy` and dependency license sources in sibling toolkit checkouts. No game assets belong in a release.

All 360 original generated illustrations are in `art/originals/`. The built-in imagegen prompts, exported paths and verified image hashes are in `sheets/card_art.json`. `scripts/prepare_card_art.py` and `scripts/export_expansion_art.py` export the card-sized resources; `scripts/review_card_art.py` creates the labeled review sheets. Review the artwork: [Knight](media/card-art-knight.png), [Mage](media/card-art-mage.png), [Ranger](media/card-art-ranger.png). The 0.3.0 additions also have labeled build-role reviews: [Knight additions](media/added-card-art-knight.png), [Mage additions](media/added-card-art-mage.png), [Ranger additions](media/added-card-art-ranger.png).

Private inspection data, extracted content, lab game files and test saves belong only under ignored `private/`. Never distribute them. The release contains original code/UI art, redistributable modding dependencies and a converter, not game content.

## Local development test

The isolated game is in `private/lab`. `scripts/launch-test.ps1` enables an opt-in command bridge and separate family saves under `private/runtime`. Original saves/preferences are backed up with universal-modder. These test commands are disabled in ordinary launchers.

## Status

Version 0.5.0 retains 75 cards per class: 30 shared family cards and 45 unique cards, including three basics. The shared core connects exhaust, retention, draw, delayed resources and debuffs to distinct Knight, Mage and Ranger payoffs. There are 165 active cards across the character; all 360 historical IDs remain registered for existing saves. Native rewards use the full rarity pools; the 0.1.9 sparse-pool reward replacement has been removed. Existing card IDs and family saves remain compatible. [Class mechanics and starter genetics](docs/classes-and-genes.md) explain inherited starters; [Steam Deck installation and controls](docs/steam-deck.md) cover setup.

[Verification results](docs/card-pool-verification.md) cover native pools, upgrades, rewards, merchants and new engine scenarios.

The creator verified loading and gameplay on Steam Deck using both Steam Linux Runtime and Proton with the 0.1.8 content-preparation changes. The 0.5.0 manor/balance revision is tested in the owned Windows game with isolated saves; its new balance still needs full-run playtesting.


Melty validates all install mappings and its one-click check says yes. Its automatic detector does not recognize this package layout; the supplied `melty.json` corrects that. It flags executable code and the bundled Python standard-library archive for review, and requires playing through Melty before publishing. That Melty install has not yet been tested. A full three-act playthrough and balance testing have not been completed. Sound could not be verified in this test environment (no working OpenAL playback device).

Prepared for a Melty draft release; publication requires a successful Melty launch and explicit creator approval. The listing title is Spire Legacy. Creator: CapeKid. Original mod code and content use the MIT license; remixes with attribution are allowed. The source game's art is a genuine small pixel emote and genuine class/trait icons, not full Rogue Legacy 2 character animations.

## Credits

Slay the Spire: Mega Crit. Rogue Legacy 2: Cellar Door Games. Game assets remain in each player's copy.
ModTheSpire: Anthony Moore and contributors (MIT). BaseMod: t-larson, kiooeht, test447 and contributors (MIT). UnityPy: K0lb3 and contributors (MIT). Python: Python Software Foundation. Pillow and texture-decoding dependencies: their respective authors and licenses. PyInstaller: contributors, GPL with bootloader exception. universal-modder: rehan-remade and contributors (MIT), used as a development toolkit.
Mod created by CapeKid with Codex. Original mod code and content use the MIT license; remixes with attribution are allowed. Third-party dependencies retain their own licenses.

Version 0.5.0 adds full trait detail panels, in-game family naming, visible combat banners, slower crown earnings, staged and confirmed manor purchases, and a temporary crown editor. All 360 registered cards are rebalanced, with weaker plain starters and class baselines so manor investment matters. See the [manor and balance guide](docs/manor-and-balance.md). Full-run balance remains subject to playtesting.
