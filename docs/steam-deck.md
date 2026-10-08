Spire Legacy has a native Linux x86_64 launcher and bundled content-reader runtime. Steam Deck uses this package; no Proton setting, Python installation, sudo command or SteamOS system change is required by the mod. Hardware verification is still pending.

## Install on Steam Deck

1. Install your owned Slay the Spire 1 and Rogue Legacy 2 copies through Steam. Select the regular StS1 branch. Disable forced compatibility for StS1 so Steam installs its native Linux runtime. RL2 may use Proton normally; the mod reads its local data files without launching RL2.
2. Switch to Desktop Mode. Extract `SpireLegacy-0.1.5-linux-x86_64.zip` into StS1's game folder, found with Steam's Manage → Browse local files. The resulting folders must be `SlayTheSpire/mods` and `SlayTheSpire/HeirOfTheSpire`. Preserve the complete ReaderLinux folder.
3. Add a Non-Steam Game with target `/bin/bash`. Set Launch Options to the quoted absolute path of `HeirOfTheSpire/Play.sh`, for example `"/home/deck/.local/share/Steam/steamapps/common/SlayTheSpire/HeirOfTheSpire/Play.sh"`. Set Start In to the quoted StS1 game folder. Leave forced compatibility disabled for this shortcut.
4. Start the shortcut once in Desktop Mode, then use it from Gaming Mode. First launch prepares icons and character content from the installed RL2 copy. Game files remain in their owned installations.

Steam libraries on an SD card are detected from Steam's `libraryfolders.vdf`. Custom locations can be passed as `--host "/path/to/SlayTheSpire" --game "/path/to/Rogue Legacy 2"` after the script path. `HEIR_STEAM_DIR` can identify another Steam installation.

## Controls and saves

Use Steam Input's Keyboard and Mouse layout: right trackpad as mouse, R2 as left click. Traits can be inspected with the trackpad. Optional keyboard bindings for the manor are M to open Family Manor, I to open Heir Settings, arrow keys/Tab to move the gold focus marker, Enter to activate, and Escape to return. Assign these to buttons or back paddles as preferred. Family naming uses the on-screen keyboard through Steam+X.

Family progression, extracted content cache and launch log are stored under `~/.local/share/HeirOfTheSpire/` by default. `XDG_DATA_HOME` and `HEIR_DATA_DIR` are supported. To move an existing Windows family, copy its `family.json` and backup from `%LOCALAPPDATA%/HeirOfTheSpire/` while both games are closed; prepare the content cache again on the Deck.

## Troubleshooting and validation

The launcher verifies both games, the native ELF Java runtime, and required mod files before starting. Run `/bin/bash "/absolute/path/HeirOfTheSpire/Play.sh" --check` in Konsole to inspect detected paths. Launch errors are recorded in `~/.local/share/HeirOfTheSpire/launch.log`. A Windows runtime error means Steam needs to restore StS1's native Linux installation.

Linux reader dependencies are pinned for CPython 3.12, x86_64 GNU/Linux. The reader is tested in a Linux container against the actual owned RL2 and StS1 content, and library-discovery tests include SD-card-style paths with spaces. Those checks do not replace testing the native game, Steam Input, on-screen keyboard and suspend/resume on a physical Deck. GitHub issue #5 remains open for that verification.

## Build the Linux package

On the Windows development setup, prepare dependencies with `uv python install cpython-3.12.13-linux-x86_64-gnu --install-dir tools/linux-python --no-bin` and `uv pip install --python-version 3.12 --python-platform x86_64-unknown-linux-gnu --target tools/linux-site UnityPy==1.25.4 Pillow==12.0.0`. Build the mod normally, then run `tools/python/Scripts/python.exe scripts/package_linux.py`. Python and package licenses accompany the runtime. Game data is excluded.

Valve documents [Desktop Mode and trackpad mouse input](https://help.steampowered.com/en/faqs/view/671A-4453-E8D2-323C/) and [Steam Input/Steam Deck compatibility requirements](https://partner.steamgames.com/doc/steamhardware/compat).
