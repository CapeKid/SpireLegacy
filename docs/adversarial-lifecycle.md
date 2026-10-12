# Run lifecycle and family progression audit (#49)

Baseline: latest merged main `f9f0e09`. This audit uses the isolated ownership and save-revision groundwork from #47/#48. Only disposable profiles and independent copies of the owned Windows game were used. No personal saves, installed game branches, publishing or merges were changed.

## Confirmed findings and fixes

| Finding | Baseline reproduction | Change |
| --- | --- | --- |
| [#103](https://github.com/CapeKid/SpireLegacy/issues/103) | Inject `IOException` at `before-commit` after the native hook calls `Settle`. The file retains the old bank, but memory advances crowns, generation, offers and Chronicle and removes the active heir. A repeated callback is suppressed. | Stage settlement, embarkation, adoption, heir selection, story read state and motto changes in a cloned profile. Adopt the staged state only once its durable revision advances. Commit settlement in the native `OnEnded` prefix before native history updates/save deletion. |
| [#104](https://github.com/CapeKid/SpireLegacy/issues/104) | Interrupt at `after-commit` during a manor purchase. Disk has the new upgrade/spent bank; memory still shows the old bank/level/revision. | Adopt the complete committed candidate in `finally`, even if delivery of the save result throws. Pre-commit failures retain all old live progress. |

An ignored baseline fixture compiled the original Core files and reproduced **both** disagreements. The initial regression also failed with `Failed settlement must preserve in-memory bank, generation, active heir and Chronicle`. Pure `Begin`/`Settle` remain in-memory simulation operations; production hooks now use the commit methods.

Run settlement requires the active run ID, family, owner and solo/co-op mode to agree. Historical IDs remain rejected after other settlements and restarts. Replacing an orphaned solo climb settles its recorded floors and advances the replacement's generation/quickstart in the same transaction. A selected heir remains visibly selected after serialization: the UI compares its name/class/traits rather than managed object identity. Unknown upgrade IDs now reject the entire purchase queue without throwing from design lookup. Repeated upgrade IDs intentionally buy multiple levels, within the cap and only after a fully affordable, prerequisite-valid confirmation.

## Deterministic evidence

`sts2/tests/LifecycleChecks.cs` adds:

- 12 independent seeded reference-model walks, **1,200 transitions**, mixing solo/co-op embarkation, completion, purchases, reload, old/unknown callbacks, wrong families/owners and invalid queues. Each step independently checks bank, generation, active identity, run receipts, class counters, manor levels and durable reload agreement.
- **42 fault cases**: seven transitions at before-write, after-write, after-flush, before-backup, before-commit and after-commit. Live and disk must contain the same complete old/new transaction. Failed purchases cannot unlock Restoration; failed settlements cannot unlock a lesson/victory or generate offers.
- Four actual child-process terminations across settlement write/flush/commit boundaries. Restart and replay produce exactly one payout, return, victory, generation increment and receipt.
- All nine story thresholds, including class floor 5/6/7 and wins 0/1/2; repeated reconciliation, read/motto restart persistence, locked choices and frozen run rewards/motto.
- Purchase zero/exact/max banks, level caps, repeated/unknown IDs, prerequisite order, changed balance between preview and confirmation, selection/cancellation and orphan replacement. Existing persistence checks also cover competing writers, stale revisions, locked files and read-only directories.
- **14,560** compatible trait/Treasury payout cases, plus saturating maximum counters.

Final core suite: **26,080 assertions pass**. Baseline suite: 3,833. Build has zero warnings/errors; preflight passes 32 sheets, 2,062 rows and 26,477 cells. Additional checks keep the native-bound active object intact through menu transactions so later room progress reaches the family save. The first UI fixture caught a missing field-serialization option; corrected diagnostics expose frozen snapshot fields rather than an empty object. A co-op diagnostic incorrectly ran a migrating Load behind the live revision: it is now read-only, and the co-op fixture reconciles its deliberately altered generation/manor before saving. The private launcher initially refused cleanup because its expected executable path used mixed separators; observed exact PID/image pairs were verified and stopped, and the launcher now constructs normalized paths. Failed evidence is retained privately and is not counted as a pass.

## Native evidence and limits

The completion matrix uses `SPIRELEGACY-LIFECYCLE-49`, BaseLib 3.4.7, Spire Legacy 1.0.28, regular v0.107.1 and public-beta v0.111.0. It exercises all three classes, paid base/upgraded Strikes, native save/resume, victory/non-victory callbacks, before-/after-commit interruptions, repeated callbacks, actual creature death and actual abandonment. **18 scenarios per branch** pass. The interrupted callback verifies that the native resumable save and history count remain unchanged until the family commit is complete; retry updates native history and deletes the save exactly once.

The complete matrix was rerun after the Chronicle/selection and active-binding changes. Final runtime source checkpoint is `d5fea27824a4e6a9d1b78d803c250239621f91f0`; release DLL SHA-256 is `fb2b91fa3806ac708d3f40f7e91f6fbcaac6d0858a863325fd2aa803249aa7f5`. Final completion, UI and co-op evidence records this exact binary. Three additional UI scenarios per branch verify controller story/motto routes, paid base/upgraded Strikes, native reward eligibility, frozen effects, room progress after menu transactions, save/resume and the visible persisted heir selection. Final local ENet co-op on both branches challenges remote-owner/family settlement attempts and confirms one durable receipt/generation/payout per family's actual abandonment.

Complete correlated responses, original diagnostics, seed, branch, source/DLL identity and loaded-mod inventory remain under ignored `private/sts2-evidence/`. Public receipts summarize them without distributing owned game files. See the adjacent `adversarial-lifecycle-evidence.json` for the final scenario counts and binary identities.

Reproduce core checks with `dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release`. Use the guarded runner with a fresh tag/profile:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_TAG -Class knight -LifecycleChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag OTHER_UNIQUE_TAG -Class knight -LifecycleUiChecks
```

For co-op, launch two independent same-branch labs with distinct fresh isolated profiles as described in `adversarial-harness.md`, then run `python scripts/sts2-harness-native-check.py coop HOST_PROFILE GUEST_PROFILE UNIQUE_TAG PORT`. It challenges each client's remote owner/family settlement attempts, then verifies real co-op abandonment settles each bank once and survives reload.

Victory callback fixtures are not full campaign wins. Physical Steam Deck/Linux input/filesystem checks, cloud conflict/power-loss hardware, full campaign deaths/victories/reconnects and campaign migration between game versions are explicitly untested. The upstream beta-to-regular RNG save-format incompatibility noted in #48/#59 is deferred: this mod does not rewrite native engine saves. Same-branch native save/resume is covered. A snapshot whose family receipt already finalized it is still refused for adoption; the mod does not restore an old bank or pay again to resume an already settled climb. The family file and native run file are separate transactions, not one cross-file atomic commit. An exception after family commit retains the native file for diagnosis/retry; the live bank still reflects the committed family receipt.

## Progression measurement

The formula remains `floor*2 + floor(gold/25) + 20 on victory`, then the capped trait multiplier and frozen Treasury multiplier, rounded once. With no traits/Treasury, floor-one/zero-gold failure awards 2 crowns; a 50-floor/200-gold win awards 128. Across every enabled compatible trait pair, maxima are 4 and 307 with full Treasury. All manor levels cost **2,620 crowns** across **16 purchases**; the 128-crown ordinary win cannot purchase half the manor. No overflow or cap escape was found.

The native floor counter counts reached map points, including the current room; test save/resume/debug paths can add a visited point. The native immediate-abandon checks identify a genuine progression concern: normal startup at MapRoom/floor 0 with 99 starting gold earns **3 crowns** without a played card or entered map point; quickstart at floor 1 earns **5** without clearing combat. [#105](https://github.com/CapeKid/SpireLegacy/issues/105) records the measured exploit for #60. It is explicitly deferred because separating earned gold/completed rooms from starting resources/reached floors requires a distinct progression-policy change and campaign comparisons; adjusting an arbitrary constant here would conceal the cause or penalize legitimate early deaths. Milestone requirements explicitly say “reaching floor 6” and should retain that separate meaning. No campaign migration or crown-balance tuning is included in this patch.
