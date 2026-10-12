# Co-op ownership and reconnect audit (#50)

Baseline is merged main `bc4ffcdae4a6f79817a042d4edca185c0b56ae52`. Tests use independent copies of the owned Windows game and fresh disposable profiles. Traffic uses local ENet peers with native version/mod checks enabled. No personal saves, installed game branches or published packages are changed.

## Findings

| Finding | Original behavior reproduced | Fix |
| --- | --- | --- |
| [#107](https://github.com/CapeKid/SpireLegacy/issues/107) | A same-ID packet changes an already-ready heir's class. Cached selections survive unready/lobby reuse; native Random is omitted from the barrier. | Protocol 2 adds host-authenticated lobby incarnation, client challenge, monotonic owner revision and explicit revocation. Ready payloads are immutable copies; roster changes rotate the incarnation. Random requires a snapshot before resolution. Non-roster senders cannot occupy the ledger. |
| [#108](https://github.com/CapeKid/SpireLegacy/issues/108) | Missing generation silently defaults inherited effects. Native ReadString allocates before the later text limit. | Require complete network schema including six bonus fields. Bound UTF-8 bytes and remaining packet bits before allocation; decode strictly. Retain older local-save compatibility separately. |
| [#109](https://github.com/CapeKid/SpireLegacy/issues/109) | Another player's snapshot constructs successfully. Sync mutates the target before native code rejects the wrong NetId. | Match snapshot owner before construction/binding. Reject wrong-target sync and changed frozen inheritance before mutation. Restore AsyncLocal scopes through exceptions. |

An ignored baseline copy compiled original production sources with minimized probes added. It reported `immutableReady=false`, `requiredFrozenFields=false`, `savedOwnerRejected=false`, and `failedSyncPreservedPlayer=false`. The missing-generation pure regression also failed before the fix. Original diagnostics and owned game files remain private.

Version **1.0.29** identifies the changed wire contract. All peers need the same mod, BaseLib and native game version. Regular/public-beta cross-version joining remains subject to native checks. This PR does not publish 1.0.29.

## Deterministic checks

The core suite passes **41,958 assertions**, including 4,000 four-owner transitions across 40 deterministic reference-model seeds. Cases cover delayed/duplicate/reordered packets, wrong protocol/session/owner, immutable ready payloads, revocation/re-ready, roster invalidation, capacity, parsed-copy isolation, 15 missing inherited fields, frozen reconnect identity and legacy solo owner-zero saves. Earlier persistence/lifecycle interruption checks remain enabled.

Native wire probes reject negative, 8,193-byte, int-max and truncated lengths before allocating payloads, and round-trip non-ASCII text. Native malformed lobby probes cannot change a ready snapshot. Captured stale packets remain ineffective after ready toggles and leave/rejoin.

## Native matrix

Real 2-, 3- and 4-player ENet groups run on regular v0.107.1 and public-beta v0.111.0, BaseLib 3.4.7 and Spire Legacy 1.0.29. The adjacent evidence receipt records the exact runtime checkpoint, DLL hash, seeds and correlated responses.

- Two identical Knight heirs: host ready before join, rapid toggles, malformed payloads/replay, leave/rejoin, trait capture, Manor browsing without pause, paid card resolution and local settlement.
- Mixed Knight/native Ironclad: pre-embark host loss clears guest handler/cache; a new lobby starts normally; native cards retain native ownership; only the Heir family settles.
- Four heirs, Knight/Mage/Ranger/Knight, with different traits, bonuses, banners and Chronicle lessons. The fourth uses native Random. Withholding its snapshot blocks an otherwise-ready party; releasing it starts the run. Every peer agrees on exposed card/player/power/resource semantics after paid base/upgraded actions. Fixture upgrades synchronize on all peers after the executor is idle.
- Native player serialization and three rejected reconnect cases preserve snapshot/cards/class pool/scopes. Actual native relic bag membership and owner-scoped eligibility match each player's frozen Chronicle unlocks.
- Each Heir's real one-HP revival routine, including Hero Complex/Super Healer, restores scoped healing state. The fixture temporarily sets HP to zero and restores original HP afterward; this is not natural campaign death.
- Four peers save/quit and rejoin native Continue with original IDs. Frozen traits, bonuses, banner, motto and relic unlocks survive. Real abandonment settles each local family once and agrees with the durable file. Handler/cache/scopes clear before another three-player combat and solo Ranger run.
- Solo Knight/Mage/Ranger paid base/upgraded cards, same-branch save/resume, Chronicle eligibility and existing Manor/controller routes regress on both branches.

Peer comparison uses complete exposed player semantics and waits for the native executor to become idle. Native checksums remain enabled; the test does not independently compute the engine's complete internal hash.

Reproduce using fresh tags, independent labs/profiles and the installed Python runtime:

```powershell
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release
./scripts/run-coop-audit.ps1 -Branch regular -Tag UNIQUE_REGULAR -Port 25141 -Scope all -Python python
./scripts/run-coop-audit.ps1 -Branch beta -Tag UNIQUE_BETA -Port 25161 -Scope all -Python python
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_SOLO -Class knight -LifecycleUiChecks
```

The four-client wrapper validates independent lab provenance, refuses existing clients/profiles, copies current DLL/manifest, launches hidden in official offline mode and stops only its own verified PID/image pairs. Complete correlated requests/replies and logs remain ignored under `private/sts2-evidence/` and disposable profile folders.

## Limits and follow-up

Physical Deck/Linux, real Steam transport, network latency/loss, disconnect during the exact embark transition, active-run host loss/replacement, naturally killed co-op revival and full-campaign reward/shop ownership remain untested. Saved-run rejoin is covered; seamless active-combat reconnection is not. These require timed transport/device/campaign fixtures. Ready-selection invalidation has deterministic coverage and a native hook, but controller attempts to change character while already ready lack a dedicated native UI test. [#110](https://github.com/CapeKid/SpireLegacy/issues/110) tracks these explicitly deferred scenarios. Every inherited combat hook and class-resource trigger still requires the focused #51/#53 audits; synchronized exposed powers here are not exhaustive trigger coverage.

Failed fixture journals are retained privately and excluded from final receipts. Locally upgrading only one peer, or upgrading before a previous action completed, caused checksum divergence: upgrades now synchronize on all peers after executor idle. A stale-DLL launch following a failed build was stopped by exact PID/image verification. Native Random button caching, unsupported Standard SetSeed, branch-specific hash signatures and numeric reflection boxing were fixture corrections. A legitimate solo-select lobby was initially misclassified as stale multiplayer state. Final tests use corrected fixtures.

Compatibility-renderer particle warnings and native deletion attempts for nonexistent disposable save backups are recorded separately; save/rejoin and settlement validate actual persisted state. Crown farming [#105](https://github.com/CapeKid/SpireLegacy/issues/105) remains a separate progression-policy issue.

Beta logs additionally contain native NCard NodePool double-free diagnostics through QueueFreeSafely and Godot WorkerThreadPool `Invalid Task ID` messages. Correlated gameplay/persistence assertions pass, but these diagnostics are not fixed or proven upstream. #110 requires native-only comparisons and natural transition timing to establish their cause before assigning a runtime fix; the full logs remain private.
