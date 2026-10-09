# Spire Legacy for Slay the Spire 2

Every climb ends. Your bloodline grows.

This is the native StS2 successor to Spire Legacy 0.5.0, created by CapeKid. It targets the regular StS2 branch, v0.107.1, with BaseLib 3.4.7. The original StS1 Java mod is retained as historical source; the StS2 release does not use Java, ModTheSpire, BaseMod or an installed StS1 copy.

## Included

- Knight, Mage and Ranger heirs with distinct starting decks and class mechanics.
- 75 cards per class: 30 shared family cards plus 45 class cards, including three basics. All 360 historical card definitions, upgrades and original illustrations remain registered.
- All 61 eligible inherited RL2 traits, compatible pairs, inherited starter changes, three heirs after a climb and the first family's quick start.
- Six manor upgrades, slower crown earnings, staged purchases with an explicit confirmation, family naming, four visible banner emblems and a temporary playtest crown editor.
- Full wrapped, scrolling trait descriptions, mouse tooltips and keyboard/controller navigation with an on-screen text keyboard.
- Family run identities, inherited card snapshots, atomic saves and backup recovery. A completed run settles crowns once.
- Genuine class, trait and heir sprites read from your owned RL2 installation. No game assets are in the download.

The previous balance is preserved: plain strikes deal 5 damage, guards supply 4 Block, and class HP starts at 60/52/56 before inheritance and manor bonuses. Upgraded drawing cards retain the safeguards from 0.5.0. Balance against StS2's enemies still needs full-run playtesting.

StS2 adaptations preserve native encounters and rewards. Dusty Tome grants an upgraded rare card from the current heir's pool because the migrated set has no Ancient-rarity cards. Native relics that require a specific base-game starter card or relic keep their eligibility checks. Ancients use generic dialogue where available, with existing Ironclad dialogue as a fallback when the game supplies no custom-character dialogue, including the ending.

## Workshop installation

1. Install owned Steam copies of Slay the Spire 2 and Rogue Legacy 2.
2. Subscribe to [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) and [Spire Legacy for StS2](https://steamcommunity.com/sharedfiles/filedetails/?id=3816525393). The new item is currently private for owner testing. The original StS1 Workshop item is a different mod.
3. Start StS2 on the regular branch. Accept its mod notice and enable BaseLib and Spire Legacy in the mod list. Restart if requested.
4. Select **The Heir**. **Family Manor & Heir Lab** appears beneath the character. Choose a family/heir and start a solo climb.
5. After a climb, return to the manor to choose another heir. Selecting upgrades only stages them; **Review purchases → Confirm and save** spends crowns.

Spire Legacy supports solo play. Its character cannot embark in a multiplayer lobby.

## Manual installation

Extract `SpireLegacy-StS2-1.0.1.zip` into your StS2 install folder. Its layout is `mods/SpireLegacy/SpireLegacy.json`, `SpireLegacy.dll`, `content-reader.zip` and `heir/`. Subscribe to BaseLib separately, or use the Melty bundle, which includes BaseLib. Launch the normal game executable; no separate mod launcher is required.

## Steam Deck

Use the Workshop steps in Gaming Mode. Both the DLL and local reader have native Linux x86_64 support. Prefer StS2's native Linux version; Proton uses the included Windows reader. No system Python installation is required.

RL2 discovery checks Steam's libraries, including `/home/deck/.local/share/Steam/steamapps/common/Rogue Legacy 2` and SD-card libraries. If automatic discovery fails, set this launch option:

```sh
HEIR_RL2_DIR="/home/deck/.local/share/Steam/steamapps/common/Rogue Legacy 2" %command%
```

The full content-preparation diagnostic appears in the game's log and in Family Manor when preparation fails. Use **Retry content preparation** after correcting the installation. Starting RL2 once is not required. Native Linux support is implemented; this new StS2 port has not yet been tested on a physical Deck.

If a card spends energy but never resolves, open **Family / Traits** after the failure. Version 1.0.1 shows the mod/game versions and the full failed card-action exception, including loaded mod versions, in Family Manor. **Copy card-play diagnostic** copies it for reporting. This diagnostic update does not yet establish the cause or fix of the reported Steam Deck failure.

## Melty

`SpireLegacy-StS2-1.0.1-melty.zip` includes the mod, both readers and BaseLib 3.4.7. `sts2/melty.json` declares StS2 as the primary game and RL2 as the secondary game, installs the two mods and launches `SlayTheSpire2.exe`. This launcher mapping targets Windows. Melty must validate the uploaded mapping and observe a successful launch before publication. A prepared archive alone is not a verified Melty install.

The Workshop package uses Mega Crit's official StS2 uploader workspace format: `content/`, `workshop.json` and a PNG preview below 1 MB. Its visibility defaults to **private**. It creates a new StS2 item; do not supply the StS1 item's ID. BaseLib is a Required Item.

## Saves and prior progress

StS2 saves your current climb in its own modded profile. Family progression is separate: `%LOCALAPPDATA%/SpireLegacy2/family.json` on Windows, or the .NET local application-data directory under Linux. `HEIR_DATA_DIR` can override this folder. `family.json.bak` provides recovery.

To migrate a family from StS1, finish or abandon the old climb, back up both family files and copy the old family JSON into the new folder **before starting StS2**. The family format is compatible. Existing StS1 game-run saves cannot be loaded by StS2. Keep the old copy for recovery.

## Build and validation

Requires .NET SDK 9 or later, your owned StS2 assemblies and the licensed content-reader runtimes. Set `Sts2DataDir` to the installed game's data directory when building outside the default Windows Steam location.

```powershell
python scripts/sts2-generate.py
dotnet build sts2/SpireLegacy.csproj -c Release -p:Sts2DataDir="C:/path/to/StS2/data_sts2_windows_x86_64"
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj
python scripts/sts2-reader-package.py
python scripts/sts2-package.py
```

The reader packager consumes licensed `Reader/`, `ReaderLinux/` and `licenses/` runtime folders. These are local build dependencies, not game content; see the script's `--runtime-source` option. A fresh clone does not include their binaries. Build the Windows reader from `scripts/load_rl.py` with Python 3.12, UnityPy, Pillow and PyInstaller. The Linux reader uses the pinned portable Python 3.12 runtime and corresponding Linux wheels documented in the historical Steam Deck build notes. No StS1 installation is needed for the StS2 build.

`scripts/sts2-lab.py` creates an isolated hardlinked owned-game oracle with separate saves and Steam Cloud writes disabled. `scripts/sts2-oracle.py` exercises the opt-in test bridge. Normal launches do not expose that bridge. Private game files, logs, extracted RL2 content and saves must remain ignored and must never be uploaded.

Validation includes 2,406 core assertions; all 360 base cards and all 360 upgrades executed in native combat; 720 native stat/text/art checks; 549 inherited starter upgrade/save/downgrade round trips; six-choice rewards and shops for all three pools; class counters; native boss victory rewards; normal autosave/resume without duplicating Antique; staged manor purchases, family naming, banners and controller focus. Ancient compatibility adds 90 class-specific reward/serialization checks, actual upgraded reward acquisition and 36 dialogue checks. Shop and rest-site sprites were visually inspected. Full campaign balance, physical Deck play and Melty-installed launch validation remain separate release checks.

## Credits and license

Original code and illustrations: CapeKid, MIT. Slay the Spire 2: Mega Crit. Rogue Legacy 2: Cellar Door Games. BaseLib: Alchyr and contributors, MIT. Content reader: UnityPy, Python, Pillow, PyInstaller and decoding dependencies, with their notices included. Their respective owners retain all rights to game content.
