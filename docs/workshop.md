# Steam Workshop build

The Workshop package contains one mod JAR and a `SpireLegacyRuntime` directory beside it with Windows x64 and Linux x86_64 content readers. Keep that directory beside the JAR. ModTheSpire and BaseMod are separate Workshop dependencies, not bundled copies.

## Player installation

1. Install your owned Steam copies of Slay the Spire 1 and Rogue Legacy 2. StS1 must use its regular branch. On Steam Deck, open StS1 Properties → Compatibility, enable the specific compatibility tool override and select **Steam Linux Runtime 1.0 (scout)** to install its native Linux version. RL2 may use Proton normally.
2. Subscribe to [ModTheSpire](https://steamcommunity.com/sharedfiles/filedetails/?id=1605060445), [BaseMod](https://steamcommunity.com/sharedfiles/filedetails/?id=1605833019), and [Spire Legacy](https://steamcommunity.com/sharedfiles/filedetails/?id=3816067184). Spire Legacy is currently hidden for creator testing; sign in as CapeKid to access it.
3. Launch Slay the Spire with **Play with Mods**, enable BaseMod and Spire Legacy, then start. First startup prepares genuine RL2 sprites locally before character registration. Later starts verify the cache against your installed files. No custom launcher or Python installation is needed.
4. Select **The Heir**. For Steam Deck, use a Keyboard and Mouse Steam Input layout with the right trackpad as mouse and R2 as left click. Steam+X opens the naming keyboard. The creator verified Deck gameplay and controls with Steam Linux Runtime and Proton after the 0.1.8 preparation update.

Use either the Workshop installation or a manual mod installation. Remove an older manually installed `mods/HeirOfTheSpire.jar` before switching to Workshop so there are no duplicate mod IDs. Keep your family save.

The family save/cache location is unchanged: `%LOCALAPPDATA%/HeirOfTheSpire` on Windows, or `~/.local/share/HeirOfTheSpire` on Linux. `HEIR_DATA_DIR` and `XDG_DATA_HOME` overrides still work. Reader errors are in `content-preparation.log` in that directory. For a nonstandard RL2 installation, set `HEIR_RL2_DIR` to its installation folder. An incomplete package fails with an actionable error before loading or changing the family save.

## Prepare and upload

Version 0.1.6 also checks `/home/deck/.local/share/Steam/steamapps/common/Rogue Legacy 2` directly when Steam manifest discovery fails. It checks the equivalent `Z:` host-filesystem path when the reader runs under Proton. Version 0.1.7 accepts a differently named Unity `*_Data` folder when exactly one contains `resources.assets`. Launching RL2 once is not required. Explicit `HEIR_RL2_DIR` overrides still take precedence. Native Linux remains the recommended Deck runtime; these discovery fixes do not certify full Proton gameplay.

For a Deck failure, use the log path shown in that launch's error. Native Linux writes `~/.local/share/HeirOfTheSpire/content-preparation.log` by default; Proton writes inside its Windows prefix. They are separate files. Version 0.1.7 logs the actual bootstrap OS, reader version, checked installation paths and whether the folder is missing, inaccessible or lacks the required Unity content. A log without the reader-version line may belong to an older attempt.

Version 0.1.8 includes the complete current preparation log in ModTheSpire's debug output and error details on failure. Copy that section directly; opening the separate log file is optional. The bundled reader still identifies itself as 0.1.7 because this update changes how the mod displays its output.

Version 0.4.0 gives every class 75 cards: 30 shared family cards and 45 unique cards, with 16 distinct rares per class. The small-pool workaround from 0.1.9 remains removed. Rewards use native Slay the Spire code, preserving Question Card, Busted Crown, Prismatic Shard, upgrade rolls and ordinary uniqueness rules. Existing cards remain loadable in old saves. See the [pool guide](card-pools.md) and [full catalogue](card-catalogue.md).

1. Run `scripts/build.ps1`, then `scripts/package.py` and `scripts/package_linux.py` using the development Python environment.
2. Run `scripts/package_workshop.py`. This creates `build/workshop` and a distributable `dist/SpireLegacy-<version>-workshop.zip`. The workspace uses the official StS1 uploader's schema. `config.json` defaults to **private**; the script retains a previously assigned `steamPublishedID`.
3. Start Steam, signed into the creator account that owns StS1. From the StS1 installation folder run its bundled Java 8 runtime with the official uploader:

   ```powershell
   & './jre/bin/java.exe' '-Djava.awt.headless=true' -jar './mod-uploader.jar' upload -w 'C:/absolute/path/to/build/workshop'
   ```

4. On the resulting Workshop item's owner page, add **ModTheSpire (1605060445)** and **BaseMod (1605833019)** under **Required Items**. `dependencies.json` records these IDs for the upload checklist; the official uploader config does not accept a dependency field. The mod itself also declares BaseMod in `ModTheSpire.json` so the loader enforces it.
5. Keep the item private while testing a fresh subscription and normal Steam mod launch on Windows and a physical Deck. Public release remains a separate creator decision.

Only the `content` directory is uploaded. It contains original mod resources and redistributable readers with license notices. The game uploader, game JARs, extracted game content, developer tools and personal saves are excluded.

## Verification for 0.1.5

The official uploader successfully created private item **3816067184**. Its owner page shows hidden visibility and both Required Items. Steam downloaded all three subscribed items. Windows testing then used the downloaded ModTheSpire, BaseMod and Spire Legacy packages with an empty local mods folder and isolated game/family saves. The mod prepared a fresh cache, registered its character and reached combat. Separate packaged-reader tests passed fresh preparation, repeat verification, missing-sprite repair, path arguments and missing-game failures without creating family state. The Workshop Linux reader extracted all 66 selected sprites from owned game files in a network-disabled Linux container. These checks do not replace a physical Steam Deck launch.
