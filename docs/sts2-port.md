# Spire Legacy for Slay the Spire 2

Every climb ends. Your bloodline grows.

This is the native StS2 successor to Spire Legacy 0.5.0, created by CapeKid. It supports regular v0.107.1 and public-beta v0.111.0 in one DLL, with BaseLib 3.4.7. Native card play and cleanup are tested on both branches. The original StS1 Java mod is retained as historical source; the StS2 release does not use Java, ModTheSpire, BaseMod or an installed StS1 copy.

## Included

- Knight, Mage and Ranger heirs with distinct starting decks and class mechanics.
- 82 reward cards per class on regular (20 common / 36 uncommon / 26 rare), matching native characters. Public-beta has 85 (20 / 38 / 27), also matching native characters. Each class has three additional basics and 34 shared family cards. See [pool expansion](sts2-pool-expansion.md). All 360 historical card definitions, upgrades and original illustrations remain registered.
- All 61 eligible inherited RL2 traits, compatible pairs, inherited starter changes, three heirs after a climb and the first family's quick start.
- Seven original [Heir-exclusive relics](heir-relics.md), with native reward/shop eligibility and bounded per-turn/per-combat effects. Family Bloodline remains starter-only.
- Six manor upgrades, slower crown earnings, staged purchases with an explicit confirmation, family naming, four visible banner emblems and a temporary playtest crown editor.
- Full wrapped, scrolling trait descriptions, mouse tooltips and keyboard/controller navigation with an on-screen text keyboard.
- Family run identities, inherited card snapshots, atomic saves and backup recovery. A completed run settles crowns once.
- Original class sprites, portraits, card illustrations and 66 original trait/class vector icons included in the download. No game assets or content readers are distributed. Rogue Legacy 2 is not required.

The basic-card baseline remains: plain strikes deal 5 damage, guards supply 4 Block, and class HP starts at 60/52/56 before inheritance and manor bonuses. Upgraded drawing cards retain the safeguards from 0.5.0. Version 1.0.5 removes unrelated trait bonuses and overlapping starter modifiers, limits treasure income to treasure rooms, and makes Flame Barrier a defensive Skill with temporary retaliation. See [trait review](traits.md). Balance against StS2's enemies still needs full-run playtesting.

StS2 adaptations preserve native encounters and rewards. Dusty Tome grants an upgraded rare card from the current heir's pool because the migrated set has no Ancient-rarity cards. Native relics that require a specific base-game starter card or relic keep their eligibility checks. Ancients use generic dialogue where available, with existing Ironclad dialogue as a fallback when the game supplies no custom-character dialogue. The Architect ending uses original Knight, Mage and Ranger conversations, each with first-victory and returning-victory text. The active heir's class selects the dialogue; the game's existing Heir win count selects first or returning lines. Native speakers, response buttons and ending animations are preserved. These six conversations are currently in English.

## Workshop installation

1. Install Slay the Spire 2. Rogue Legacy 2 is not required.
2. Subscribe to [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) and [Spire Legacy for StS2](https://steamcommunity.com/sharedfiles/filedetails/?id=3816525393). The new item is currently private for owner testing. The original StS1 Workshop item is a different mod.
3. Start StS2 on your selected branch. Accept its mod notice and enable BaseLib and Spire Legacy in the mod list. Restart if requested.
4. Select **The Heir**. **Family Manor & Heir Lab** appears beneath the character information, clear of Ascension. Press Y, or move down from the portrait and press A. Choose a family/heir and start a solo climb.
5. After a climb, return to the manor to choose another heir. Selecting upgrades only stages them; **Review purchases ÃƒÂ¢Ã¢â‚¬Â Ã¢â‚¬â„¢ Confirm and save** spends crowns.

Spire Legacy supports solo and co-op. Each Heir brings their own frozen class, traits and manor bonuses; crowns stay with their local family. All players need matching game/mod versions. See [co-op setup and progression](coop.md).

## Family Manor controls

Use the D-pad or left stick to move through buttons and read trait descriptions; the page scrolls to the focused item. Hold a direction to keep moving. A selects, B returns from editors/reviews or closes the Manor, and LB/RB scroll a page at a time. The prominent **Close / Back (B)** button stays visible below the scrolling content. Selecting an upgrade retains your place. Closing with staged purchases still asks whether to discard them; crowns are spent only by **Confirm and save**. Keyboard/mouse input and the wider scrollbar remain available.

## Manual installation

Extract `SpireLegacy-StS2-1.0.27.zip` into your StS2 install folder. Its layout is `mods/SpireLegacy/SpireLegacy.json`, `SpireLegacy.dll` and `heir/`. Subscribe to BaseLib separately, or use the Melty bundle, which includes BaseLib. Launch the normal game executable; no separate mod launcher is required.

## Steam Deck

Use the Workshop steps in Gaming Mode. The DLL and included original artwork support the native Linux game and Proton. No Python installation is required.

Version 1.0.18 loads the included original artwork directly through Godot. No RL2 path, Python runtime, content reader or extracted-content cache is required. Old `HEIR_RL2_DIR` launch options can be removed. Existing reader/cache folders from older releases are unused and do not affect saved family progress. Native Linux and Proton share the same packaged icons; physical Deck retesting of this release remains needed.

If a card spends energy but never resolves, open **Family / Traits** after the failure. Version 1.0.3 shows the mod/game versions and the full failed card-action exception, including loaded mod versions, in Family Manor. **Copy card-play diagnostic** copies it for reporting. Version 1.0.3 adapts attack, card-sourced damage, exhaust and damage-modifier APIs across both branches. Retain this diagnostic if a card still fails.

## Melty

`SpireLegacy-StS2-1.0.27-melty.zip` includes the standalone mod and BaseLib 3.4.7. `sts2/melty.json` declares only StS2, installs the two mods and launches `SlayTheSpire2.exe`. This launcher mapping targets Windows. Melty must validate the uploaded mapping and observe a successful launch before publication. A prepared archive alone is not a verified Melty install.

The Workshop package uses Mega Crit's official StS2 uploader workspace format: `content/`, `workshop.json` and a PNG preview below 1 MB. Its visibility defaults to **private**. It creates a new StS2 item; do not supply the StS1 item's ID. BaseLib is a Required Item.

## Saves and prior progress

StS2 saves your current climb in its own modded profile. Family progression is separate: `%LOCALAPPDATA%/SpireLegacy2/family.json` on Windows, or the .NET local application-data directory under Linux. `HEIR_DATA_DIR` can override this folder. `family.json.bak` provides recovery.

To migrate a family from StS1, finish or abandon the old climb, back up both family files and copy the old family JSON into the new folder **before starting StS2**. The family format is compatible. Existing StS1 game-run saves cannot be loaded by StS2. Keep the old copy for recovery.

## Build and validation

Requires .NET SDK 9 or later and your owned StS2 assemblies. Set `Sts2DataDir` to the installed game's data directory when building outside the default Windows Steam location.

```powershell
python scripts/sts2-pool-expansion.py
python scripts/sts2-generate.py
dotnet build sts2/SpireLegacy.csproj -c Release -p:Sts2DataDir="C:/path/to/StS2/data_sts2_windows_x86_64"
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj
python scripts/sts2-package.py
```

Original icons are reproducible with `python scripts/author-heir-icons.py`; hashes and provenance are recorded in `sheets/icon_art.json` and [original-icons.md](original-icons.md). `scripts/preflight.py` validates the StS2 design and original artwork without an RL2 asset index or StS1 atlas. Historical source-asset validation is available only through `--historical-sts1-assets`.

`scripts/sts2-lab.py` creates a fresh independently copied owned-game oracle with separate saves and Steam Cloud writes disabled. Launch with `HEIR_TEST_MODE=1`, a dedicated lab user directory, and absolute `HEIR_TEST_ROOT`/`HEIR_DATA_DIR` paths; see [the adversarial harness procedure](adversarial-harness.md). `scripts/sts2-oracle.py` exercises the correlated opt-in test bridge. Normal launches do not expose that bridge. Private game files, logs, extracted RL2 content and saves must remain ignored and must never be uploaded.

Validation includes 2,434 core assertions; all 360 base cards and all 360 upgrades executed in native combat; 720 native stat/text/art checks; 549 inherited starter upgrade/save/downgrade round trips; six-choice rewards and shops for all three pools; class counters; native boss victory rewards; normal autosave/resume without duplicating Antique; staged manor purchases, family naming, banners and controller focus. Ancient compatibility adds 90 class-specific reward/serialization checks, actual upgraded reward acquisition and 36 dialogue checks. Shop and rest-site sprites were visually inspected. Full campaign balance, physical Deck play and Melty-installed launch validation remain separate release checks.

## Credits and license

Original code and illustrations: CapeKid, MIT. Slay the Spire 2: Mega Crit. Rogue Legacy 2: Cellar Door Games. BaseLib: Alchyr and contributors, MIT. Original trait/class SVG art: CapeKid, MIT. No extracted game content or reader runtimes are included; historical StS1 dependencies retain their separate notices.

## 1.0.6 startup and controller fixes

Neow's scripted starting HP restoration is exempt from inherited healing modifiers. Hero Complex still prevents actual healing during the climb; Vegan and Super Healer still modify actual healing. Starting HP retains the native Ascension restriction.

On character select, the Family Manor & Heir Lab button sits above the Heir portrait. Press Y to open it, or move down from the Heir portrait and press A. B closes the Manor and returns focus to the portrait. Static text cannot receive focus; only enabled buttons and editable fields are selectable. LB/RB scroll the page. The picker uses an original Heir portrait; generation details are recorded in `docs/heir-portrait.md` in the source repository.

The owned-game regression script `scripts/sts2-startup-ui-check.py --profile-directory private/<lab-profile> --branch beta` checks actual Neow initialization, all three Hero Complex/Diva classes, native paid card cleanup, abandon, Vegan/Super Healer healing, Ascension starting HP, portrait replacement and controller navigation. Use the isolated opt-in lab described above. Regular and public-beta native checks passed; physical Steam Deck validation of 1.0.6 remains a user check.

## 1.0.7 portrait refinement

The character picker uses a tightly cropped original vertical Heir portrait with an opaque ochre background and broad painted shading. The artwork fills the existing portrait tile dimensions alongside native heroes. This replaces the 1.0.6 transparent square cutout. All startup, controller and gameplay fixes remain included.

## 1.0.8 Ascension layout and Steam Input

The Manor launcher sits beneath the character information and reserves the full visible Ascension panel bounds, including its icon and arrows. The Y shortcut observes held-state press edges for both the mapped accept action and the native north-face controller action, accommodating Steam Input events generated after the frame callback. Switching from touch/mouse to controller retains the selected Heir. D-pad/A entry and B exit remain available. Native checks exercise Ascension 5, non-overlapping control bounds and the north-face controller action on both branches; physical Deck confirmation remains a user check.

## 1.0.9 trait playtesting

Clumsy removes 2 Block after the first Attack each turn. Playing Attacks before defensive cards avoids the penalty. Hero Complex grants +100% legacy crowns alongside +40 maximum HP and disabled healing; the existing 200% total payout cap remains. Both trait changes apply to existing heirs. The 1.0.8 Ascension and controller fixes remain included.

## 1.0.10 class sprites and animation

Knight, Mage and Ranger now have distinct original full-body sprite atlases. Each uses a four-frame idle loop, a four-frame class attack, and skill, hurt, fallen and victory poses. Native combat triggers and bloodline card-play hooks drive visual reactions without waiting on or replacing gameplay actions. Attacks swing a sword, cast from a staff or fire a bow respectively. Skill/hurt states return to idle; death and victory persist. Class-specific idle sprites also appear in rest sites and shops. Inherited size and color effects remain; family colors apply to the separate banner so armor and robes retain their intended palette.

Original artwork and full prompt provenance are recorded in [character-sprites.md](character-sprites.md). The mod includes all three PNG atlases. No extracted game artwork is included. Run `scripts/sts2-character-check.py <private-profile> <branch>` with the isolated rendered oracle to verify all three classes, paid card cleanup, damage reactions and victory/death transitions.

## 1.0.11 trait abilities

Inter-dimensional, Cartographer, IBS, FMF Fan, Mushroom Man and Hypergonadism now have explicit gameplay effects. See [traits.md](traits.md) for exact numbers and drawbacks. Existing heir IDs remain compatible; the trait tooltips use the updated definitions.

## 1.0.12 visual fixes

Mage now visibly collapses through four original death frames and holds the final defeated pose. All classes are enlarged at rest sites (2x previous size) and shops (1.75x), with ground anchors preserved. Native death timing remains unchanged.


## 1.0.18 standalone artwork and installation

The mod loads all trait and class icons from its own packaged SVG files. Existing original cards, portraits and animated sprites are preserved. Stable class/trait/card IDs and family/run snapshots are unchanged. The startup extraction process, RL2 discovery, preparation failure/retry UI and bundled readers are removed. Workshop/Melty metadata requires only StS2 and BaseLib. Old cached content is ignored; no cleanup of family saves is necessary.

## 1.0.19 trait appearance

Synesthesia has animated rainbow motes and a colored ring that stays visible alongside other palettes. Combat, rest and shop sprites use the same cosmetic effects and inherited scale. Paired palettes retain independent colored accents; Histrionic briefly flashes red on HP loss and recovers to its normal palette. No gameplay or crown bonuses change. See [trait-effect-audit.md](trait-effect-audit.md) for the complete runtime-consumer audit.

## 1.0.20 reward-card parity

Each class now matches the native ordinary reward distribution on both supported branches: regular 82 (20 common / 36 uncommon / 26 rare), public-beta 85 (20 / 38 / 27), plus three basics. Four new shared family rares and six unique rares per class expand exhaust recovery, retention, HP trades, Skill/Attack sequences, poison and discard builds. Beta adds two uncommons and one rare per class. All 360 saved model IDs and original illustrations remain registered, including beta-only cards when a save is loaded on regular. The same DLL selects eligibility from the built-in native pool. Ancient cards remain separate work. See [expansion and balance guide](sts2-pool-expansion.md).

Both owned Windows branches pass exact native/Heir pool parity, unlocked/compendium membership, six-choice rewards and merchant inventory checks for all classes. All new eligible cards resolve through paid native play in base/upgraded form (60 regular and 78 beta checks), preserve saved identities and leave no hovering card. Evidence validation checks exact damage, draws, installed power amounts, costs, HP, Block, poison, recovery and discounts. Native save/resume preserves family identity and relics. Core checks pass 2,778 assertions. Physical Steam Deck controls and full-run balance remain playtesting checks; prepared archives do not mean a Workshop or Melty upload has occurred.

## 1.0.21 Heir-exclusive relic drops

Seven original relics expand Power setup, guarded attacks, exhaust, manual discard, class resources, delayed resources and mixed-type sequences. Two common, three uncommon and two rare drops are available to all three classes. They use native reward and shop acquisition with original icons, outlines, tooltips and controller focus. Per-turn claims, per-combat claims, partial sequences and spent appearance serialize through native saves; no crown or manor multiplier is added. Existing saved grab bags remain intact; start a new climb for the expanded drop roster. See [rules and balance references](heir-relics.md).

## 1.0.22 Heir potion pool

Borrowed Breath grants 6 Block and one draw; Briar Vial applies 4 Poison and 1 Weak to an enemy; Triune Draught supplies a one-use class resource. A dedicated three-potion character pool keeps the shared pool available once. Native rewards, shops, combat generation, targeting, belt consumption and saved IDs apply. All artwork is original and packaged, with no new content dependency. See [potion rules and comparisons](heir-potions.md).

## Measured Breath on regular and public-beta

Measured Breath uses its own bounded retention power on both branches: at turn end, choose up to one card, or two when upgraded. Stacks increase the limit. Each choice lasts for that turn only; an unchosen card discards normally on the following turn. Cards already retaining themselves do not consume a choice. Other effects that retain the entire hand still apply normally. Beta's native Well-Laid Plans remains unchanged for native characters.
## 1.0.23 run-ending sprites

Abandoning outside combat now shows the ended Knight, Mage or Ranger's original death sprite. Startup and rest-site game-over clones preserve inherited size and trait appearance; shop endings use the existing heir sprite. The ended run's saved heir determines the class even after the family selects a successor. Regular and public-beta use the same fix.


## 1.0.26 inline energy layout

Inline energy symbols in descriptions and Neow options match native 24px text artwork. Large card cost symbols and animated energy counters retain their original size.


## 1.0.27 startup compatibility

Initialization runs once, validates supported branch APIs and removes partial patches on failure before family generation. Includes the merged guarded test-harness changes. The same release DLL passed owned regular v0.107.1 and public-beta v0.111.0, all three heirs, solo/co-op, paid card cleanup, same-branch resume and native character controls. See `docs/adversarial-startup.md` in the source repository for detailed coverage. Native beta saves can contain RNG values unsupported by regular builds; complete campaign migration between branches remains unverified.
