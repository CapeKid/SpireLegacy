# Family persistence audit (#48)

Baseline: merged main `7740305ea0efafb3156a63768f9a978c5e2a7a2f`. Only disposable profiles/owned-game copies are used. No publishing, installed-game changes, real saves or automatic merges.

## Confirmed findings

An ignored fixture compiles the original baseline Core files and reproduces:

| Finding | Actual baseline result | Fix |
| --- | --- | --- |
| [#79](https://github.com/CapeKid/SpireLegacy/issues/79) recovery | Missing primary ignores a 100-crown backup and returns zero. Recovery/migration replaces the good backup with `broken`. | Restore validated backup, preserve it, quarantine bad primary by same-directory rename. |
| [#80](https://github.com/CapeKid/SpireLegacy/issues/80) validation | Level6 fails outside fallback; null snapshot heir is accepted. | Validate before migration/native use, bounded reads and duplicate/schema/ID checks. |
| [#81](https://github.com/CapeKid/SpireLegacy/issues/81) competing writers | Loaded profiles save444 then555; stale bank overwrites444. Both use one temporary filename. | Unique flushed candidates, cooperating-process lock and loaded-file revision. |
| [#82](https://github.com/CapeKid/SpireLegacy/issues/82) adoption | Foreign solo snapshot can settle into another family. | Family/known-owner guards for solo/co-op, settled/competing-run refusal and rollback of failed in-memory adoption. |

Baseline evidence: `private/persistence-baseline.log`; its fixtures are original mod code, not decompiled game code. The checks are behavioral regressions, not a claim that every invalid input has been enumerated.

## Store and recovery contract

`FamilyStore` acquires `family.json.lock` with exclusive sharing. This serializes **cooperating** writers; a stale loaded revision is rejected with an explicit reload instruction. It does not promise coordination with arbitrary external editors that ignore the lock. New objects may create a new path; overwriting an existing bank requires loading that path. Purchase previews carry provenance and successful purchases update the original profile's revision.

Each candidate uses a unique same-directory name, is serialized and flushed before commit. A valid previous primary is copied to a separate flushed backup candidate and atomically renamed over `.bak` before the primary commit. Recovery does not rotate a damaged primary into the good backup: it renames the damaged file to `.corrupt.<GUID>` before promoting the candidate. If interrupted during that recovery gap, the missing primary is restored from the still-valid backup next time. Temporary candidates from terminated processes are retained as evidence and never silently promoted.

Missing primary plus valid backup recovers. Both invalid refuse with aggregate diagnostics and preserve the original files. Locked files/permission errors do not silently select older progress. Future root/Chronicle schemas refuse without downgrading to a backup. Recovery produces a notice in the Manor/console. Atomic rename plus file flushing is tested against process termination; this does **not** establish hardware power-loss durability or directory fsync behavior on Linux.

Reads are capped at8MiB before JSON parsing; oversized invalid files use bounded metadata for revision tracking and are quarantined by rename rather than copied. Objects, counters, known class/trait/manor/Chronicle IDs, list sizes, duplicate properties/IDs, and frozen bonus bounds are checked. Historical manor levels up to5 and frozen historical bonuses remain supported, with existing refund arithmetic. Root counters saturate at int.MaxValue where generation advances. Null collections/entries fail clearly before native stat arithmetic.

Missing legacy family/Chronicle metadata migrates once and persists identity. A StS1-shaped active family record receives missing family metadata while retaining run ID/heir/bonuses. This is family JSON migration, **not** importing a StS1 campaign into StS2. Future unsupported native campaign schemas are never rewritten. Legacy backups predating family identity may require a new identity when recovered; an existing native save with incompatible/missing identity is preserved and refused rather than credited into a different bank.

Local saved Heir snapshots now require validated identity/heir/bonuses. A Heir save lacking its frozen snapshot fails with a compatible-backup/original-version diagnostic; it no longer silently picks the menu heir. Foreign family/owner run effects stay attached to their saved player but cannot adopt/credit the local crown bank. Already-settled and competing active run IDs refuse. If adoption cannot save, its prior active/selected references are restored.

## Deterministic and filesystem evidence

`sts2/tests/PersistenceChecks.cs` runs with the normal Core checks, using unique temporary roots:

- Missing/corrupt primary, repeat corruption after recovery, both invalid and future schema.
-29 malformed-field cases, duplicate properties/IDs and oversized input; known backup restored.
- Six injected write/flush/backup/commit checkpoints; old/new committed progress and a valid backup remain.
- Real locked primary/backup, read-only file and Windows read-only-directory DACL. The original access rules are restored in finally; no audit/ownership privileges are requested.
- Two actual writer processes load the same revision, then compete: one save, one explicit conflict. Initial reads are staggered because the fail-fast lock can also reject simultaneous reads.
- Four actual child-process terminations after write, after flush, before commit and after commit. Verified child image/controlled PID; lock handles are released by process exit.
- Legacy metadata, StS1-shaped active record, empty family identity, existing retired-level refund idempotence, int.MaxValue counters, failed purchases and foreign solo settlement.

Core:3,344 assertions; release build against owned regular API has zero warnings/errors. Permission fixture initially requested unnecessary SACL sections and was corrected to access rules only; locked-backup failure initially surfaced as UnauthorizedAccess on Windows, now wrapped with actionable original I/O details. These fixture corrections are retained in the journal.

## Native evidence and open gaps

Owned regular v0.107.1 and public-beta v0.111.0, BaseLib3.4.7/mod manifest1.0.27; one regular-built DLL on both. The native runner covers Knight/Mage/Ranger, base/upgraded paid Strike, real same-branch save/resume after changing the menu class/family/manor, unchanged frozen identity/traits/bonuses, Guard cleanup and seven adoption challenges per scenario. Native runs test family/owner mismatch, last/historical settlement, another unfinished run, locked-save rollback and missing saved snapshots. The selected menu heir is cleared only on successful adoption.

The initial comparison included mutable floor/quick fields and failed because a synthetic debug-room resume increments TotalFloor. Immutable fields stay unchanged. Normal-map lifecycle/crown implications are explicitly deferred to #49; this is not a confirmed production-map bug. The runner retains the complete before/after snapshots while comparing immutable fields. Beta-to-regular native RNG seed incompatibility discovered in #59 remains unmodified and deferred to lifecycle/campaign migration policy (#49). A regular-to-beta player DTO preview is not a full campaign migration pass.

Physical Steam Deck/Linux filesystem/controller tests, hardware disk-full/power-cut behavior, cloud synchronization, full campaign victory/death/reconnect and campaign migration between game versions are open gaps, not passes. Simulated disk-write failures and Windows process kills do not substitute for those platform claims.

Reproduce with guarded fresh profiles and the independent owned-game lab workflow in `docs/adversarial-harness.md`. Run `dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release`, then `python scripts/sts2-persistence-check.py PRIVATE_PROFILE regular` or `beta`; use the existing native co-op harness with fresh host/guest families and a settled initial transition before map voting. Full private response metadata carries exact DLL, source baseline, branch, mods, profile and seed. A dirty source baseline is paired with the immutable DLL hash; the patch-hash field is null.

Final binary identity: `33cbf5e18435d9b7a3ed8978df9626ab249c6f67fc2666adfafc0e4af48413d1`. Native evidence is `private/sts2-evidence/persistence-{regular,beta}-review.json`; co-op evidence uses `harness-persistence-{regular,beta}-review-coop.json`. Core output: `private/persistence-core-final.log`. Required legacy progress and frozen heir fields cannot silently default; future schema detection precedes old-format field checks.

Final owner-isolation ENet co-op passes on each branch with the same DLL: distinct Knight/Mage family IDs and banks, native player roundtrips, remote-owner diagnostics, paid card resolution/cleanup and abandonment. Fresh host/guest profiles complete FTUE flags first and use a twelve-second initial transition wait before voting. The final solo/adoption and co-op logs retain only known disposable save-file deletion diagnostics; no validation, attack or owner-state exceptions escape the intended negative fixtures. Physical device/full campaign gaps above remain open.
