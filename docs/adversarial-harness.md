# Adversarial audit baseline (#47)

The harness is a prerequisite for audits #48–#63. It changes test isolation and evidence collection, without changing card or progression balance.

## Disposable owned-game setup

Build the release DLL against the owned game version being tested. Set `STS2_DIR` to that owned installation or an existing read-only snapshot. Create a **new** lab:

```powershell
python scripts/sts2-lab.py --configuration Release --lab-directory private/audit-regular-lab --user-directory SpireLegacy2PortLabAuditRegular --workshop-directory "C:/Program Files (x86)/Steam/steamapps/workshop/content/2868840"
$env:HEIR_TEST_MODE='1'
$env:HEIR_TEST_ROOT=(Resolve-Path private).Path
$env:HEIR_DATA_DIR=Join-Path $env:HEIR_TEST_ROOT 'audit-regular-profile'
$env:HEIR_TEST_SOURCE_SHA=(git rev-parse HEAD)
$env:HEIR_EXPECTED_DLL_SHA256=(Get-FileHash sts2/bin/Release/net9.0/SpireLegacy.dll -Algorithm SHA256).Hash.ToLowerInvariant()
```

Launch only the copied executable, with a fresh family profile per client. The builder refuses existing labs, copies owned files independently, copies the mod's original audio and scene resources, and disables subscribed Workshop mods in dedicated lab settings. Verify response metadata contains only BaseLib and SpireLegacy. Never repair an old hardlinked lab in place. No installed game, personal profile or branch change is required.

Test initialization rejects nonstandard test flags, missing absolute root/data paths, non-lab Godot user directories, and data paths outside the root after resolving directory links. It runs before family access. The replacement game backend is `GodotFileIo`, with a profile-specific directory under the dedicated Godot user directory; it does not use the Steam Cloud backend. Ordinary launches do not register the oracle. The native gate fixture exercises the ordinary initialization gate inside an already isolated process, avoiding a production Cloud-backed launch.

## Checks and evidence

```powershell
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release
python scripts/sts2-harness-check.py
python scripts/audit-coverage.py
python scripts/sts2-harness-native-check.py solo private/audit-regular-profile regular
python scripts/sts2-harness-native-check.py coop private/audit-host-profile private/audit-guest-profile regular 24881
```

Repeat the native checks using an independently copied owned public-beta snapshot and the same regular-built release DLL. The co-op check requires two clients of the same game branch with different family data paths. It uses a local ENet connection and checks distinct owner snapshots, native card serialization and playable owned cards. Detailed reconnect/settlement campaigns belong to #50.

All 22 existing scenario clients use the shared correlated transport and retain native metadata. Set `HEIR_EXPECTED_DLL_SHA256` when running them; the focused harness baseline always computes the expected release hash. Python optimized mode is rejected because it disables their assertions. Responses record the baseline source SHA, optional tracked working-patch hash, actual DLL SHA-256, game/mod/BaseLib versions, platform, loaded mods, isolated profile, request ID and actual active run seed. The single-player fixture uses `SPIRELEGACY-PORT-TEST`; co-op input and port are recorded with the request. Commit the source before a reproducible release run; a dirty baseline SHA is not a claim that the build came from clean main.

Every tracked product file is assigned a primary issue in the committed [coverage manifest](adversarial-audit-coverage.json). The fixture and native scripts store their complete evidence under `private/sts2-evidence`. Keep owned-game copies, screenshots, logs, profiles and receipts private. Publish the result summary, never the game files.

## Failure semantics

Requests are delivered atomically, carry UUIDs and use an exclusive client lock. Stale/partial responses cannot satisfy a new request. Answered IDs cannot be reused by the client. The bridge persists each claim **before** execution and rejects reuse after a process restart. A timed-out mutation is ambiguous: inspect the log, receipt and resulting state; never retry automatically. Lost responses do not trigger a second action. A stale client lock after a crash requires explicit inspection and a new disposable profile.

Partial request JSON produces a diagnostic, not success. Transient request locks defer execution; response publication retries asynchronously. Commands execute serially across awaited work. Full original and inner exceptions survive metadata errors; known credential assignments and bearer tokens are redacted. Do not put credentials in oracle input or errors.

The adversarial fixtures deliberately corrupt independently expected facts for wrong owner, duplicate payout, skipped effect, unawaited work and a missing node, and verify that assertions reject each. These validate the assertion/transport contract; they do **not** constitute exhaustive mutation coverage of the corresponding gameplay subsystems. Those native gameplay audits retain their own issue boundaries.

## Explicit gaps

This chunk establishes a Windows native baseline. Physical Steam Deck/Linux controller checks, full campaigns, exhaustive card/power mutations, co-op reconnect matrices and packaging tests remain in their assigned audits. Legacy StS1 command consumption is labeled only as consumption; it is not proof of an effect. There is no automatic Workshop upload or merge in this audit.

## Completed baseline, 2026-10-11

Started from main `a16dfe77dd09241690220cb75a7af468bba6a7ef`. The tested regular-built release DLL SHA-256 is `d622871a3c37389264cbd18882b8904b61078fcd7ab63e3c5aba859c8a6dcb3b`. Native metadata verifies Windows 10.0.26300, BaseLib 3.4.7, Spire Legacy 1.0.26, and no unrelated loaded mods. Test family directories were fresh `private/sts2-harness-{regular,beta}5-{host,guest}-profile` directories; dedicated copied lab Godot directories ended in `HarnessRegular2`/`HarnessBeta2`.

| Check | Result |
| --- | --- |
| Deterministic core | 3,254 assertions pass |
| Adversarial transport/copy fixtures | 20 checks pass; planted failures rejected |
| Regular v0.107.1 solo | Knight/Mage/Ranger base and upgraded paid Strike, actual damage/energy, Discard, no table node, serialization pass |
| Public-beta v0.111.0 solo | Same DLL and checks pass |
| Regular/public-beta local co-op | Distinct Knight/Mage owner snapshots and traits, native serialization, paid card resolution/cleanup and remote-owner exception context pass |
| Native negative cases, both branches | Duplicate claim rejected; awaited original/inner exception retained; partial JSON fails and next request recovers; ordinary bridge gate adds no subscription; save backend is GodotFileIo |
| Repository/design checks | 1,524 product files assigned without gaps; 32 sheets, 2,062 rows and 26,117 cells pass preflight |

Confirmed findings are [#66](https://github.com/CapeKid/SpireLegacy/issues/66), [#67](https://github.com/CapeKid/SpireLegacy/issues/67), [#68](https://github.com/CapeKid/SpireLegacy/issues/68), [#69](https://github.com/CapeKid/SpireLegacy/issues/69), [#70](https://github.com/CapeKid/SpireLegacy/issues/70) and [#71](https://github.com/CapeKid/SpireLegacy/issues/71). The new assertions caught a reused-ID stale success, a Windows response-replacement lock race, and a co-op helper passing an enemy target to self-targeted Guard. Those defects were fixed before the final baseline. Evidence is `harness-{regular,beta}-final-{solo,coop}.json` and `harness-fixtures.json` under the ignored evidence directory.
