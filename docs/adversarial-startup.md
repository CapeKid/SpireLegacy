# Startup and branch compatibility audit (#59)

Baseline: merged main `96ab2367cab5c4f1a72cdb45a5b07a88a169b2c7`. This audit changes startup and native API boundaries, without a release bump, upload, installed-game mutation or personal-save access.

## Confirmed findings and fixes

- [#73](https://github.com/CapeKid/SpireLegacy/issues/73): repeated initialization reloads the family and subscribes frame handlers repeatedly. `StartupGate` serializes concurrent calls, rejects recursion, leaves successful calls inert and preserves the first failure. Failed initialization requires a process restart.
- [#74](https://github.com/CapeKid/SpireLegacy/issues/74): family generation/save precedes `PatchAll`, which can leave partial hooks after failure. Mandatory APIs and all patch classes now install before family access. A failure identifies the class and original error and removes only this Harmony owner. Later initializer failure unsubscribes this mod's handlers and removes its hooks.
- [#75](https://github.com/CapeKid/SpireLegacy/issues/75): name-only/optional reflection can select unrelated overloads or fail without a useful contract diagnostic. Damage, Exhaust, LoseBlock and built-in pool generation require supported parameter and return signatures; damage hook targeting is constrained; constructor discovery handles zero parameters and reports available layouts. Invocation preserves the original inner exception.

The rollback regression caught an additional implementation trap: Harmony's `UnpatchAll(owner)` can remove another owner's registration when both use the same patch method. Removal uses `Unpatch(method, All, owner)` instead. The fixture installs existing production texture patches under another owner and checks that production inventory remains byte-for-byte identical after failure.

## Exact binary and actual environments

Release DLL, built against the owned regular API, SHA256:
`bc60b15e2c0c0929a35d9d103d036b1ecc361c85813cef6871672077b7f209bb`.

| Owned game snapshot | Game build | BaseLib | Mod |
| --- | --- | --- | --- |
| Regular | v0.107.1 / 59260271 | 3.4.7 | 1.0.26 |
| Public-beta | v0.111.0 / 41cef1ea | 3.4.7 | 1.0.26 |

These are the actual locally available builds, not a claim that the regular snapshot is Steam's newest remote depot. The installed PC game is beta. Both labs are independent copies, use dedicated Godot directories, and replace Steam Cloud save storage with guarded local storage before test commands become available. Production test mode remains disabled.

## Checks and evidence

The same DLL hash was verified in every successful oracle response. Loaded baseline mods are exactly BaseLib and SpireLegacy; additional-mod runs explicitly allow the copied Quick Restart v2.0.0 manifest.

- All 59 Harmony patch classes resolve and install on both builds. Repeated initialization keeps the same profile object, family-file hash, frame-subscription count and complete native target/patch inventory.
- Plant a target failure at `BindNewPlayerBloodline` during actual process startup. Sentinel family bytes remain unchanged and initialization never completes. Separately fail the second of two texture patch classes and assert no fixture-owner hooks remain and production hooks are unchanged.
- Missing, disabled and declared-too-old BaseLib are rejected by the native loader on both builds before family access. The version fixture changes the copied manifest to 1.0.0; it does **not** prove arbitrary old DLL ABI compatibility.
- Knight, Mage and Ranger each execute paid base/upgraded Strike, real same-branch save/resume and Guard. Check effect, energy charge, discard and absence of lingering card nodes. Hero Complex/Diva fixtures exercise startup with difficult traits.
- Throw through actual patched player creation with a missing synchronized owner and actual deserialization with a malformed family snapshot. In each class, the Harmony finalizers restore the previous inheritance scope; the malformed data is only a disposable player DTO.
- Compare Ironclad with this mod's Harmony patches disabled versus enabled, base/upgraded Strike/Defend, HP and healing, sprites and abandonment. Results are identical and the family file remains unchanged. BaseLib and registered custom models remain loaded in this control; it is not a completely vanilla process.
- Real ENet host Knight/guest Mage owner isolation, player roundtrips, remote diagnostics and paid card resolution pass on both builds. The first attempt voted during the initial scene transition and timed out with native location-buffer diagnostics. Fresh-profile retries waited twelve seconds before voting and passed; the timeout is retained as evidence rather than counted as a pass. Restarting an unfinished test family correctly refused a new lobby.
- Base/upgraded Measured Breath performs paid play and two real end-turn flushes on each branch. The native selector is called twice, enforces zero-to-one/two choices and retains zero old cards when the second choice declines them. This uses the game selector test API, not physical controller input.
- The complete startup, scope, card/resume and native-control suite also passes with Quick Restart v2.0.0 loaded on both branches. Its gameplay restart button was not activated in this audit.

Private evidence (ignored; no owned game binaries or decompiled code committed):

- `private/sts2-evidence/compatibility-{regular,beta}.json`
- `private/sts2-evidence/startup-negative-{regular,beta}.json`
- `private/sts2-evidence/harness-startup-{regular,beta}-final-coop.json`
- `private/sts2-evidence/compatibility-{regular,beta}-extra.json`
- `private/sts2-evidence/startup-cross-branch.json`
- `private/sts2-evidence/startup-choice.json`

Source metadata identifies baseline main plus this reviewed working patch; its patch hash field is null. The release DLL hash is the immutable execution identity. The final build reconfirms that hash with zero warnings/errors. Pure checks pass 3,265 assertions; adversarial transport fixtures pass 20 checks; preflight passes 32 sheets / 2,062 rows / 26,117 cells.

## Save migration result and explicit gaps

A regular serialized Ranger player with an upgraded branch-only Scouting card imports into beta with the heir snapshot and family file preserved. A beta player does **not** import into the regular snapshot: the native JSON reader rejects `$.rng.seed` as out of range for UInt32. This is a native schema difference; no seed truncation or game save rewrite was attempted. Player DTO import is not a complete campaign-save migration pass. Full run migration, settlement/recovery and backup policy remain explicitly deferred to audit #48 (persistence) and #49 (lifecycle).

All patch targets were installed and their resulting inventories recorded. Reflection searches and scoped finalizers were reviewed; integration scenarios above exercise the common routes. Exhaustive fault injection into every prefix/postfix, UI/audio/merchant/rest-site private field and future native overload remains unproven. Subsystem behavior belongs to the remaining audit chunks, especially #50 (co-op), #55 (relics/potions), #56 (ancients), #57 (UI) and #58 (presentation). Unsupported future signatures deliberately fail at startup instead of being guessed.

Physical Steam Deck/controller/Linux, full campaign victory/death, arbitrary old BaseLib binaries, other third-party mods, and full cross-branch campaign saves are open gaps. This Windows audit makes no device-pass claim. Existing native renderer/resource diagnostics and the first co-op transition failure remain in private logs.

## Reproduction

Use `scripts/sts2-lab.py` to create fresh independent owned-game copies with dedicated `SpireLegacy2PortLab...` user directories and actual Workshop discovery. Launch with the guarded test environment and exact release DLL; keep each family profile inside the physical private workspace. Do not reuse installed-game files through hardlinks.

`python scripts/sts2-compatibility-check.py PRIVATE_PROFILE regular` (or `beta`) runs baseline checks; append `QuickRestart` only when explicitly testing that copied additional mod. `scripts/sts2-startup-negative-check.ps1 -Branch regular` (or beta) requires specifically named fresh negative labs, temporarily changes only copied manifests/dedicated settings and restores them even if client cleanup fails. Verified PID image path and creation time gate process termination.

For co-op, use `scripts/sts2-harness-native-check.py coop HOST GUEST TAG PORT` with fresh families. Allow the initial scene transition to settle before voting; this audit's retry substituted a twelve-second initial wait. Do not replay a timed-out mutating command automatically.
