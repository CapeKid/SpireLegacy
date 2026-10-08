# Spire Legacy — local development build

A solo custom character for **Slay the Spire 1, regular branch**. Your first heir is random. After each climb, name your family, choose one of three heirs and spend legacy crowns on permanent manor upgrades.

## What players get

- Three classes: Knight, Mage and Ranger, with distinct 10-card starting decks and separate reward pools.
- Eight inherited traits, two per heir. Hover each icon or name in combat or the manor for exact stat changes, drawbacks, legacy earning bonuses and cosmetic size effects.
- Thirty cards spanning attacks, defenses, powers, draw and energy; every card can be upgraded and has its own original illustration showing its effect.
- Six manor upgrades for health, Strength, Dexterity, card draw, legacy earnings and recovery.
- A customizable family name and four banner colors.
- Normal Slay the Spire enemies, events, maps and three-act runs; the first run goes directly to a first-floor fight.
- Persistent family progression, run identities, atomic saves and backup recovery.
- Genuine Rogue Legacy 2 class and trait sprites loaded from the player's installed copy at launch. These appear on heir choices, the combat HUD and the character. Cards use original effect-based illustrations. Combat values are original adaptations for turn-based play, not an exact port of Rogue Legacy 2's action combat.

## Required games and platform

Windows x64, owned Steam copies of Slay the Spire 1 (regular branch, installed build 10180494 / v2.3.4 tested target) and Rogue Legacy 2 (build 13303339 tested target). Steam library discovery locates Rogue Legacy 2 automatically. It does not modify that game's installation or saves. No multiplayer.

## Build

Design lives in `sheets/*.json`. Run `scripts/build.ps1`: it checks every cell for missing values, checks sheet references and real source assets, generates Java definitions and compiles the mod. Runtime asset conversion uses `scripts/load_rl.py`; a packaged Python runtime removes the need for players to install Python.

This repository preserves the tested 0.1.2 development source. Local tools, game jars, runtime caches, extracted game content, test saves and release archives are excluded. The Windows build scripts currently expect the development setup described below; a fresh clone does not include those dependencies.

- Java 8 JDK under `tools/jdk/`; Python 3.12 environment under `tools/python/` with Pillow and UnityPy (PyInstaller and archspec for the portable reader).
- Redistributable ModTheSpire 3.30.3 and BaseMod 5.56.0 jars under `tools/`, plus your own installed StS1 `desktop-1.0.jar` at the path in `scripts/build.ps1`.
- Local asset inspection via `scripts/inspect_rl.py` creates `private/rl_asset_index.json`; the owned host card atlas is inspected locally as `private/cardui.atlas` for preflight.
- Packaging also expects the locally frozen reader under `private/helper-release/ReadRogueLegacy` and dependency license sources in sibling toolkit checkouts. No game assets belong in a release.

All 30 original generated illustrations are in `art/originals/`. The built-in imagegen prompts, exported paths and verified image hashes are in `sheets/card_art.json`. `scripts/prepare_card_art.py` exports the card-sized resources. [Review all card artwork](media/card-art-review.png).

Private inspection data, extracted content, lab game files and test saves belong only under ignored `private/`. Never distribute them. The release contains original code/UI art, redistributable modding dependencies and a converter, not game content.

## Local development test

The isolated game is in `private/lab`. `scripts/launch-test.ps1` enables an opt-in command bridge and separate family saves under `private/runtime`. Original saves/preferences are backed up with universal-modder. These test commands are disabled in ordinary launchers.

## Status

Compilation, 185 progression checks, and all 30 card-upgrade checks pass. The packaged launcher was tested from an isolated install layout: it located Rogue Legacy 2, prepared both games' local content, registered the custom character and started a normal floor-one fight. A Mage with Magic Gift drew six cards; an attack spent one energy and dealt the expected damage. Death settlement, three heir choices, a manor purchase and its health bonus after restart were tested. Gameplay screenshots are in `media/` in the source project.

Melty validates all install mappings and its one-click check says yes. Its automatic detector does not recognize this package layout; the supplied `melty.json` corrects that. It flags executable code and the bundled Python standard-library archive for review, and requires playing through Melty before publishing. That Melty install has not yet been tested. A full three-act playthrough and balance testing have not been completed. Sound could not be verified in this test environment (no working OpenAL playback device).

Prepared for a Melty draft release; publication requires a successful Melty launch and explicit creator approval. The listing title is Spire Legacy. Creator: CapeKid. Original mod code and content use the MIT license; remixes with attribution are allowed. The source game's art is a genuine small pixel emote and genuine class/trait icons, not full Rogue Legacy 2 character animations.

## Credits

Slay the Spire: Mega Crit. Rogue Legacy 2: Cellar Door Games. Game assets remain in each player's copy.
ModTheSpire: Anthony Moore and contributors (MIT). BaseMod: t-larson, kiooeht, test447 and contributors (MIT). UnityPy: K0lb3 and contributors (MIT). Python: Python Software Foundation. Pillow and texture-decoding dependencies: their respective authors and licenses. PyInstaller: contributors, GPL with bootloader exception. universal-modder: rehan-remade and contributors (MIT), used as a development toolkit.
Mod created by CapeKid with Codex. Original mod code and content use the MIT license; remixes with attribution are allowed. Third-party dependencies retain their own licenses.
