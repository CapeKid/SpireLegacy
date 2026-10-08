# Spire Legacy — local development build

A solo custom character for **Slay the Spire 1, regular branch**. Your first heir is random. After each climb, name your family, choose one of three heirs and spend legacy crowns on permanent manor upgrades.

## What players get

- Three classes: Knight, Mage and Ranger, with distinct 10-card starting decks and separate reward pools.
- All 61 eligible entries in the installed RL2 trait library have turn-based adaptations, with two compatible traits per heir. Hover icons or names for exact effects. [Trait coverage and adaptation notes](docs/traits.md).
- Thirty cards spanning attacks, defenses, powers, draw and energy; every card can be upgraded and has its own original illustration showing its effect.
- Six manor upgrades for health, Strength, Dexterity, card draw, legacy earnings and recovery.
- A customizable family name and four banner colors.
- Normal Slay the Spire enemies, events, maps and three-act runs; the first run goes directly to a first-floor fight.
- Persistent family progression, run identities, atomic saves and backup recovery.
- Genuine Rogue Legacy 2 class and trait sprites loaded from the player's installed copy at launch. These appear on heir choices, the combat HUD and the character. Cards use original effect-based illustrations. Combat values are original adaptations for turn-based play, not an exact port of Rogue Legacy 2's action combat.

## Required games and platform

Windows x64 has been tested in-game. The native Linux x86_64 package supports Steam Deck installation, with physical hardware verification pending. Both require owned Steam copies of Slay the Spire 1 (regular branch, build 10180494 / v2.3.4 tested on Windows) and Rogue Legacy 2 (build 13303339 content target). Steam library discovery locates Rogue Legacy 2 automatically. It does not modify that game's installation or saves. No multiplayer.

## Build

Design lives in `sheets/*.json`. Run `scripts/build.ps1`: it checks every cell for missing values, checks sheet references and real source assets, generates Java definitions and compiles the mod. Runtime asset conversion uses `scripts/load_rl.py`; a packaged Python runtime removes the need for players to install Python.

This repository contains the development source and issue fixes. Local tools, game jars, runtime caches, extracted game content, test saves and release archives are excluded. The Windows build scripts currently expect the development setup described below; a fresh clone does not include those dependencies.

- Java 8 JDK under `tools/jdk/`; Python 3.12 environment under `tools/python/` with Pillow and UnityPy (PyInstaller and archspec for the portable reader).
- Redistributable ModTheSpire 3.30.3 and BaseMod 5.56.0 jars under `tools/`, plus your own installed StS1 `desktop-1.0.jar` at the path in `scripts/build.ps1`.
- Local asset inspection via `scripts/inspect_rl.py` creates `private/rl_asset_index.json`; the owned host card atlas is inspected locally as `private/cardui.atlas` for preflight.
- Packaging also expects the locally frozen reader under `private/helper-release/ReadRogueLegacy` and dependency license sources in sibling toolkit checkouts. No game assets belong in a release.

All 30 original generated illustrations are in `art/originals/`. The built-in imagegen prompts, exported paths and verified image hashes are in `sheets/card_art.json`. `scripts/prepare_card_art.py` exports the card-sized resources. [Review all card artwork](media/card-art-review.png).

Private inspection data, extracted content, lab game files and test saves belong only under ignored `private/`. Never distribute them. The release contains original code/UI art, redistributable modding dependencies and a converter, not game content.

## Local development test

The isolated game is in `private/lab`. `scripts/launch-test.ps1` enables an opt-in command bridge and separate family saves under `private/runtime`. Original saves/preferences are backed up with universal-modder. These test commands are disabled in ordinary launchers.

## Status

Version 0.1.3 includes the cursor-layer fix, new selection art, character-specific family controls, the complete eligible trait catalog, and a native Linux/Steam Deck package. [Steam Deck installation and controls](docs/steam-deck.md) describe the bundled reader and Steam Input setup. The Linux reader extracted all 66 selected sprites from the actual owned game files in a Linux container, and seven launcher-discovery/error checks pass. Physical Deck gameplay and suspend/resume remain unverified.

Compilation, 12,083 progression/platform-path checks, all 30 card-upgrade checks, all 63 trait-power loading checks and nine runtime trait-rule checks pass, including the native healing patch. The expanded trait catalog was tested in real combat: Pacifist prevented attacks and applied Poison, and Limitless provided four Energy on both the first and second turn. Earlier packaged-launcher tests verified local content preparation, card play, death settlement, heir choices, a manor purchase and its health bonus after restart. Gameplay screenshots are in `media/`.

Melty validates all install mappings and its one-click check says yes. Its automatic detector does not recognize this package layout; the supplied `melty.json` corrects that. It flags executable code and the bundled Python standard-library archive for review, and requires playing through Melty before publishing. That Melty install has not yet been tested. A full three-act playthrough and balance testing have not been completed. Sound could not be verified in this test environment (no working OpenAL playback device).

Prepared for a Melty draft release; publication requires a successful Melty launch and explicit creator approval. The listing title is Spire Legacy. Creator: CapeKid. Original mod code and content use the MIT license; remixes with attribution are allowed. The source game's art is a genuine small pixel emote and genuine class/trait icons, not full Rogue Legacy 2 character animations.

## Credits

Slay the Spire: Mega Crit. Rogue Legacy 2: Cellar Door Games. Game assets remain in each player's copy.
ModTheSpire: Anthony Moore and contributors (MIT). BaseMod: t-larson, kiooeht, test447 and contributors (MIT). UnityPy: K0lb3 and contributors (MIT). Python: Python Software Foundation. Pillow and texture-decoding dependencies: their respective authors and licenses. PyInstaller: contributors, GPL with bootloader exception. universal-modder: rehan-remade and contributors (MIT), used as a development toolkit.
Mod created by CapeKid with Codex. Original mod code and content use the MIT license; remixes with attribution are allowed. Third-party dependencies retain their own licenses.
