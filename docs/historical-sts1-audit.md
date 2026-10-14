# Historical StS1 isolation audit (#63)

Baseline: `5787bd97d16416d8b3de744dd28e44177b992a3f`. The current product targets Slay the Spire 2. This audit retires executable paths and records historical findings; it does not restore StS1 support or alter native gameplay, cards, traits, progression, or controller behavior. The confirmed current-checkout contamination finding is tracked in [#160](https://github.com/CapeKid/SpireLegacy/issues/160).

## Confirmed current-checkout findings and fixes

1. **Historical reconstruction could replace active design before failing.** In a disposable tree, the baseline `create_sheets.py` replaced a sentinel `classes.json`, then raised `TypeError: card() got an unexpected keyword argument 'power'`. The old expansion, pool-curation, trait, balance, and UI initializers also have direct writes to shared sheets/resources. Sixteen retired Python writers/tools now refuse execution and imports before dependencies or mutations. There is no environment-variable bypass. Edit current sheets deliberately and use `sts2-generate.py`; consult a historical Git revision for historical reconstruction.
2. **Generic launch/build/package names still performed StS1 operations.** Root `Play.cmd`/`Play.sh`, four PowerShell wrappers, Python launch/content-reader CLIs, Java generation and three historical packagers now fail before changing permissions, copying files, modifying saves or starting processes. Existing historical implementations remain readable. Reader helper imports remain available only for isolated synthetic fixtures; ordinary CLI execution is retired.
3. **Root configuration selected the wrong game and distribution.** Root `melty.json` declared StS1 plus RL2; `workshop/config.json` used StS1 item `3816067184`. These records moved to `historical/sts1/`. Active configuration is `sts2/melty.json` and `sts2/workshop.json`, for StS2 app `2868840`, item `3816525393`, and BaseLib item `3737335127`. Historical uploader scripts are disabled; their stale staging/item-retention behavior cannot update an active release.
4. **Future package inputs needed an explicit retirement boundary.** Existing active packaging already builds closed manifests, does not glob dirty staging, validates archives against exact expected bytes, and rejects wrong Workshop/Melty configuration. It now additionally refuses historical runtime directories, JAR/Java/Python/executable launch files, reader ZIP/manifests, and save/cache files even if a future declared input tries to add them. Native mod DLLs, original assets, and the pinned BaseLib DLL/PCK remain allowed.

The obsolete root command names emit the current StS2 instructions instead of silently launching the other game. Historical Workshop/Deck guides explicitly mark their remaining instructions and IDs as retired. Mixed Windows punctuation in the Deck guide was normalized to UTF-8 during this audit.

## Coverage and execution boundaries

The companion [coverage CSV](historical-sts1-coverage.csv) inventories every historical Java file, non-StS2 script, historical configuration and applicable resource/license record, with content hashes and classifications. The [inventory generator](../scripts/historical-audit-inventory.py) reads source only. The generic audit manifest remains ignored.

| Boundary | Review and disposition |
| --- | --- |
| 360 `src/main/java/heir/cards/*.java` | Generated one-card identity constructors. Historical card behavior is in Java `HeirCard`, `CardEffectAction`, `CardPowers`, `BuildPower`, `ClassPower` and `HeirEnginePower`; none are compiled by the C# project or shipped in native packages. Original illustrations are intentionally shared and remain included. |
| Remaining 29 Java files | Inventory covers generated `Data`, registration/hooks, pools, starter genetics, trait rules/tips/powers, Manor/input/banner, profile persistence, paths/bootstrap/test bridge, and historical checks. Runtime integration is exclusively ModTheSpire/BaseMod/StS1. Preserved as unsupported source; no Java build entry point remains active. |
| Historical Java profiles | `PlatformPaths` uses `HeirOfTheSpire`, with an explicit environment override; `Profile` uses temporary/backup files and limited JSON validation. These paths are separate from current `SpireLegacy2` defaults. Current builds do not import Java saves or call Java bootstrap. No personal save migration or probe was performed. |
| Readers and Linux launcher | Synthetic discovery rejects ambiguous Unity content folders; malformed manifest JSON fails before image writes. Historical source remains available for provenance/tests. Direct CLIs are disabled; no UnityPy/Python/content reader is bundled or required by the current mod. |
| Historical build/packagers/config | Refuse before dependency lookup, compilation, copying, process launch, cache creation or upload workspace updates. Archived configuration is not discovered at the root. |
| Shared live authoring | Typed sheet validation, original art exporters, native icon/relic/potion/presentation/dialogue authors, `balance_policy.py`, `sts2-pool-expansion.py`, catalogue and trait-audit tools remain active. They are not retired merely because their filenames lack `sts2-`. Native generation and deterministic author regressions cover these paths. |
| Active release | C# explicitly compiles only `Core`, `Native`, `Generated`; assembly resources embed validated design JSON. Closed payloads read declared original art and current configuration. Active source has no invocation of old launchers/readers or reference to StS1 app/item IDs. |
| Credentials/configuration | Tracked historical config contains public game/item IDs and old package mappings, not a publishing credential. Historical readers query Steam registry/library manifests to discover content; no ownership/authentication bypass is attempted. Ignored tools and private publishing credentials are outside the shipped/tracked audit scope and were not read. |

## Explicit legacy-only deferrals

- **Reader output containment:** with a synthetic one-pixel atlas and `host_assets.json` output `../escaped.png`, the imported historical `host_assets` helper writes beside the cache. It also trusts asset-sheet source/output names and does not make a complete extraction transaction. This is reproduced inside a disposable directory, without owned game assets. The helper is not a supported current API; its CLI and packagers are disabled and native release exclusions prevent distribution. Fixing or supporting historical extraction is deferred rather than reviving it.
- **Historical initialization schema and tuning:** the initializer's `power` argument failure and old writers' obsolete pool/tuning defaults are retained as historical implementation records. Guarding execution/import prevents partial active-sheet replacement. No attempt is made to make the old design regenerate successfully.
- **Historical discovery/parser behavior:** manifest regexes and platform-specific paths are not a full VDF parser; legacy launch planning requires an exact Windows-era Unity folder in some paths. Historical profile parsing validates less than the current C# schema. These are unsupported, isolated paths, not claims of current Linux or save compatibility.
- **Historical test bridge/save safety:** Java test capture names resolve under the configured root without modern containment, and the historical bridge has no correlated request/receipt protocol. The retired Java launchers and test-command CLI cannot start/use it. Current StS2 test isolation remains the separate validated harness. Historical Java fixtures requiring StS1 dependencies are not rerun against paid-game files or personal saves.

These findings are deliberately visible. CLI retirement does not make imported historical helpers safe for arbitrary caller-supplied data or certify old releases already installed elsewhere.

## Reproduction and validation

```powershell
python scripts/check-historical-isolation.py
python scripts/historical-audit-inventory.py
python scripts/check-design-schema.py
python scripts/check-release-packages.py
```

The focused fixture uses a temporary mirror, sentinel design/save/package files, a source snapshot of the baseline initializer, and fake atlas/image data. It executes all retired Python CLIs/imports, PowerShell wrappers, Windows/Bash launchers, and release exclusions. Expected refusals leave the mirror byte-identical. It never launches a game, queries personal Steam libraries, or uses owned/extracted assets. Failures and assertions are not accepted as completed passes.

Baseline release checks passed 13 checks across three synthetic Windows/Linux/Proton layouts; design checks passed 438 mutations and eight deterministic authors. Focused retirement checks passed 59 checks. Final build/package verification is recorded in the PR. Raw fixture receipts remain ignored under `private/`.

No native C# runtime path changes, so the issue's Knight/Mage/Ranger, base/upgraded and co-op runtime matrix does not apply to this patch. The packaging fixtures demonstrate file/configuration isolation, not physical Deck gameplay, native Steam transport or a Melty-installed launch. No publishing or personal-game-folder changes are part of this audit.
