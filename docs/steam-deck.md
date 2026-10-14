# Steam Deck — Slay the Spire 2

For the current mod, follow the [StS2 installation guide](sts2-port.md#workshop-installation). Install Slay the Spire 2, BaseLib and the StS2 Spire Legacy Workshop item; Rogue Legacy 2 and a content reader are no longer required. Regular and public-beta are supported; co-op peers must use matching game and mod versions.

On character selection, choose The Heir, then press **Y**, or move down from its portrait and press **A**, to open Family Manor & Heir Lab. Use the **D-pad/left stick** to move, **A** to activate, **B** to return or close, and **LB/RB** to scroll. Hold a direction to repeat. The fixed **Close / Back (B)** button remains below the scrolling content. Labels and disabled controls are excluded from Manor navigation.

The family-name and playtest crown editors offer controller-selectable on-screen keys, Backspace, Clear text, Save and Cancel. External/Steam keyboards can also edit the focused field. Purchases are staged: selecting an upgrade spends nothing. Use **Review purchases**, then **Confirm and save**. Closing with staged upgrades offers Keep selecting or Discard and close. Ready co-op players and active climbs cannot purchase upgrades or select another heir.

Solo Manor browsing pauses the game and restores its previous pause state on close. Co-op browsing keeps networking and gameplay running. The modal closes when leaving its owning character-select/run screen. Family progression uses `SpireLegacy2/family.json` in the platform local application-data directory, or `HEIR_DATA_DIR`; old StS1 family files and caches use a different directory.

The [Manor audit](adversarial-manor.md) records native Windows controller-action checks on both branches, including a 1280×800/16:10 layout and two-peer networking. Synthetic actions and inspected screenshots do not certify physical Deck buttons, touch/keyboard switching or Linux/Proton transport; those remain device playtests under #110.

## Historical StS1 instructions

The remainder documents the retired StS1 package and is not the installation procedure for the current StS2 mod. Historical launch/build commands are disabled in the current checkout; see the [historical isolation audit](historical-sts1-audit.md). The paths and dependency versions below are preserved records, not current recommendations.

Spire Legacy has a native Linux x86_64 launcher and bundled content-reader runtime. Steam Deck uses this package; no Proton setting, Python installation, sudo command or SteamOS system change is required by the mod. The creator verified Workshop loading, gameplay and controls with both Steam Linux Runtime and Proton after the 0.1.8 update. The 0.4.0 shared-pool revision is verified in the owned Windows game; it has not yet had a full Deck playthrough.

## Install on Steam Deck

For Workshop installation, use the [Workshop guide](workshop.md). The manual package steps follow.

1. Install your owned Slay the Spire 1 and Rogue Legacy 2 copies through Steam. Select the regular StS1 branch. In StS1 Properties → Compatibility, enable the specific compatibility tool override and select **Steam Linux Runtime 1.0 (scout)** so Steam installs its native Linux runtime. RL2 may use Proton normally; the mod reads its local data files without launching RL2.
2. Switch to Desktop Mode. Extract `SpireLegacy-0.5.0-linux-x86_64.zip` into StS1's game folder, found with Steam's Manage → Browse local files. The resulting folders must be `SlayTheSpire/mods` and `SlayTheSpire/HeirOfTheSpire`. Preserve the complete ReaderLinux folder.
3. Add a Non-Steam Game with target `/bin/bash`. Set Launch Options to the quoted absolute path of `HeirOfTheSpire/Play.sh`, for example `"/home/deck/.local/share/Steam/steamapps/common/SlayTheSpire/HeirOfTheSpire/Play.sh"`. Set Start In to the quoted StS1 game folder. Leave forced compatibility disabled for this shortcut.
4. Start the shortcut once in Desktop Mode, then use it from Gaming Mode. First launch prepares icons and character content from the installed RL2 copy. Game files remain in their owned installations.

Steam libraries on an SD card are detected from Steam's `libraryfolders.vdf`. Custom locations can be passed as `--host "/path/to/SlayTheSpire" --game "/path/to/Rogue Legacy 2"` after the script path. `HEIR_STEAM_DIR` can identify another Steam installation.

## Controls and saves

Use Steam Input's Keyboard and Mouse layout: right trackpad as mouse, R2 as left click. Traits can be inspected with the trackpad. Optional keyboard bindings for the manor are M to open Family Manor, I to open Heir Settings, arrow keys/Tab to move the gold focus marker, Enter to activate, and Escape to return. Assign these to buttons or back paddles as preferred. Family naming uses the on-screen keyboard through Steam+X.

Family progression, extracted content cache and launch log are stored under `~/.local/share/HeirOfTheSpire/` by default. `XDG_DATA_HOME` and `HEIR_DATA_DIR` are supported. To move an existing Windows family, copy its `family.json` and backup from `%LOCALAPPDATA%/HeirOfTheSpire/` while both games are closed; prepare the content cache again on the Deck.

## Troubleshooting and validation

The launcher verifies both games, the native ELF Java runtime, and required mod files before starting. Run `/bin/bash "/absolute/path/HeirOfTheSpire/Play.sh" --check` in Konsole to inspect detected paths. Launch errors are recorded in `~/.local/share/HeirOfTheSpire/launch.log`. A Windows runtime error means Steam needs to restore StS1's native Linux installation.

Linux reader dependencies are pinned for CPython 3.12, x86_64 GNU/Linux. The reader is tested in a Linux container against the actual owned RL2 and StS1 content, and library-discovery tests include SD-card-style paths with spaces. The creator completed physical Deck verification and asked to close GitHub issue #5. A fresh full-run test remains useful for the 0.4.0 pool revision.

## Build the Linux package

On the Windows development setup, prepare dependencies with `uv python install cpython-3.12.13-linux-x86_64-gnu --install-dir tools/linux-python --no-bin` and `uv pip install --python-version 3.12 --python-platform x86_64-unknown-linux-gnu --target tools/linux-site UnityPy==1.25.4 Pillow==12.0.0`. Build the mod normally, then run `tools/python/Scripts/python.exe scripts/package_linux.py`. Python and package licenses accompany the runtime. Game data is excluded.

Valve documents [Desktop Mode and trackpad mouse input](https://help.steampowered.com/en/faqs/view/671A-4453-E8D2-323C/) and [Steam Input/Steam Deck compatibility requirements](https://partner.steamgames.com/doc/steamhardware/compat).

The manor now accepts native controller actions and offers an in-game on-screen keyboard for family names/crowns. Select Read full traits for a wrapped, scrollable detail view. See [manor controls](manor-and-balance.md). Physical Deck validation of these 0.5.0 controls remains pending.
