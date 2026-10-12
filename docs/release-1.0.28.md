# Spire Legacy 1.0.28

Follow-ups before publication: [Spellweave's paid upgrade](spellweave-balance.md) and [six further card corrections](six-card-balance-review.md). The archived candidate and exact hashes below precede those fixes; regenerate and validate packages from the reviewed updated source before uploading.

Release candidate for Slay the Spire 2 regular v0.107.1 and public-beta v0.111.0, with BaseLib 3.4.7. This package includes the merged balancing and recovery work through PR #98.

## Player changes

- Royal Execution now deals 20/26 damage before its half-health execute bonus, at the same three-Energy cost.
- Celestial Spear now deals 26/34 damage, at the same three-Energy cost.
- Arrow Cascade now deals four damage per preceding Attack to each enemy. It still Exhausts and costs 2/1 Energy.
- Includes the earlier targeted draw, exhaust-recovery, Poison and retention corrections, discounted recovery-cycle safeguards, and Vigor applying throughout multi-hit attacks.
- Includes hardened family-save validation and backup recovery from the merged persistence work.

Base/upgraded values exclude traits and combat powers. The no-manor starter baseline remains intentionally weaker; class mechanics and manor/crown values are unchanged by the final finisher pass. The new defensive reward policy is an opt-in diagnostic pilot feature, not an automatic mechanic affecting player reward choices.

## Installation and scope

Standalone: `SpireLegacy-StS2-1.0.28.zip`. Workshop uploader workspace: `SpireLegacy-StS2-1.0.28-workshop.zip`. Melty bundle, including BaseLib: `SpireLegacy-StS2-1.0.28-melty.zip`.

The Workshop item remains private and keeps BaseLib as a required item. Update existing StS2 item **3816525393**, preserving the local uploader's `mod_id.txt`; that marker is intentionally excluded from the portable archive. Preparing the package does not upload it. The Melty bundle's launcher mapping targets Windows; a new Melty-installed launch is not verified by archive inspection. No Rogue Legacy 2 installation, extraction runtime or game assets are required or distributed.

See [installation and Deck controls](https://github.com/CapeKid/SpireLegacy/blob/main/docs/sts2-port.md), [targeted finisher evidence](https://github.com/CapeKid/SpireLegacy/blob/main/docs/balance-targeted-finishers.md), and [defensive pilot comparisons](https://github.com/CapeKid/SpireLegacy/blob/main/docs/balance-defensive-policy.md). Native branch regressions cover the release DLL; physical Steam Deck testing and human full-campaign balance feedback remain separate checks.

## Final regression results

The exact packaged DLL, SHA-256 `56380292b9026c3065d2ddbb2bdec2f27dbede6d46d9654ddfe48bcd8211c483`, passes the following existing native fixtures on both owned Windows game branches. Test source is `f138a138975a2c41a25a4f5660557474c6e5009e`, based on merged main `212dd2c`; later changes only add packaging inspection and release documentation. Both clients report Spire Legacy **1.0.28**, BaseLib **3.4.7**, and their actual game versions.

| Suite | Regular v0.107.1 | Beta v0.111.0 |
| --- | ---: | ---: |
| Three finisher payoffs, no/full manor, base/upgraded, setup/no setup | 24 passed | 24 passed |
| Tactical effects, emergency Block Potion, native controls and HP-cost safety | 33 passed | 33 passed |
| Delayed Energy, draw and Block, base/upgraded | 6 passed | 6 passed |
| Legal reward override, replay skip and absent-card conservation | 2 passed | 2 passed |
| Vigor across every multi-hit row, baseline/boosted pairs and native controls | 86 passed | 86 passed |
| **Total** | **151 passed** | **151 passed** |

The Vigor suite includes Rapid Fire at both upgrade levels and checks table cleanup, discard/exhaust destination, attack counts and Vigor conservation/consumption. The reward suite includes an additional replay flow within each legal-override case. These are effect-isolation fixtures, not 302 campaign runs or a human win-rate estimate.

Also passed: 3,454 Core assertions including persistence fault/concurrency checks; Release build with zero warnings/errors; 32-sheet preflight (2,062 rows/26,117 cells); and disposable pool regeneration with stable identities, membership and targeted overrides. The final archive inspector rejects unexpected/missing/duplicate files and content mismatches, checks CRCs, verifies original asset hashes and the packaged DLL/manifest, and checks Workshop visibility, preview size, BaseLib dependency and the Melty filename mapping.

Raw journals remain private under tags `release128-payoffs-{regular,beta}` and `release128-regression-{regular,beta}`. The public [validation record](https://github.com/CapeKid/SpireLegacy/blob/main/docs/release-1.0.28-validation.json) retains counts and immutable package/DLL hashes without private save paths or game files. All tests use independent game copies, fresh isolated profiles and verified client-PID cleanup; installed games, real saves and Steam branches remain untouched.

This completes all three planned passes: defensive policy comparisons, targeted card payoffs, and final regression/package preparation. Further balance changes should follow concrete playtest feedback rather than continuing the diagnostic-pilot sequence. Workshop and Melty publication have not been performed in this pass.
