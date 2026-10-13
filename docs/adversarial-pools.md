# Card registration, rewards, shops and branch parity audit (#54)

Baseline: merged main `4352d0a`. Tests used independent owned-game Windows copies, fresh disposable family/game profiles, BaseLib 3.4.7 and Spire Legacy 1.0.29. Exact source checkpoints, DLL hashes, request counts, seeds, diagnostics and measured results are in [the validated evidence](adversarial-pools-evidence.json). Fixture-only additions produced several diagnostic DLLs; the evidence checks that every final stage contains the same three production fixes. Nothing was published or installed into a personal game folder.

Three confirmed findings were fixed:

- [#124](https://github.com/CapeKid/SpireLegacy/issues/124): pooled native card widgets retained disposed unseen/locked portrait materials after scene transitions. The main-menu → Ironclad-run → compendium control reproduced this on both branches without any Heir cards in the query. Before native `NCard.Reload`, clear only invalid cached materials; native loading recreates them. Valid materials remain intact. The same control passes after the fix, and completed stages contain no `ObjectDisposedException`.
- [#125](https://github.com/CapeKid/SpireLegacy/issues/125): Prismatic Gem expanded the canonical menu-selected Heir rather than the receiving player's frozen class. Knight/Ranger peers produced different pools and card IDs for the same owners. Regular's lazy pool replacement also discarded the original requested pool; beta already materializes that input. Preserve the requested pools and replace incidental canonical family pools with the receiving Heir's pool. For native recipients whose Gem unlocks the Heir, all three family pools are included consistently across peers. Preserve filters, bypass flags and regular's explicit custom-pool API. Resolve the regular/beta replacement-method signatures at runtime; beta removed the custom-pool API.
- [#126](https://github.com/CapeKid/SpireLegacy/issues/126): the cached compendium captured Knight's pool and continued displaying it after selecting Mage or Ranger. Refresh only the Heir filter before opening the library, using the selected menu class or local player's frozen in-run class. Native character filters remain unchanged.

## Pool and registration results

| Per Heir class | Regular v0.107.1 | Public-beta v0.111.0 |
| --- | ---: | ---: |
| Common / uncommon / rare | 20 / 36 / 26 | 20 / 38 / 27 |
| Ordinary reward cards | 82 | 85 |
| Basic identities | 3 | 3 |
| Ancient identities, excluded from ordinary rewards | 2 | 2 |
| Complete active pool / compendium class filter | 87 | 90 |
| Shared cards within basic + ordinary membership | 34 | 34 |
| Unique cards within basic + ordinary membership | 51 | 54 |

Ordinary rarity counts match all five native characters on each tested branch. Native basics are measured separately; their starting identities need not equal one another. Sheet membership is checked exactly, including shared/unique and branch-only additions. All 360 historical IDs remain canonical and resolvable, including definitions outside active pools. Enumeration contains no duplicate IDs or mutable pool entries, and no LegacyCard is injected into a native pool. Canonicals retain base damage/Block, zero upgrade/growth and empty inherited snapshots after card loading and reward generation.

The compendium displays the current active class pool, rather than all retired definitions. Four same-instance transitions (Knight → Mage → Ranger → Knight) are repeated after each of the four test runs on both branches. Every displayed membership matches the sheets. Mixed co-op libraries show the local player's frozen pool.

## Native integration coverage

Each branch completed seven evidence stages:

1. Solo Knight, Mage, Ranger and Ironclad: 65 native reward offers per owner pass, ten native merchants and every starting-deck transform; repeat after native save/resume. Reward sizes include 1, 3, 6, 12 and the entire rare pool. Every offer is distinct and owner-eligible, bosses remain rare, and the full rare selection terminates. Merchant slots remain Attack/Attack/Skill/Skill/Power; colorless slots contain no Heir cards. Created cards are mutable owner copies; canonical variables remain unchanged.
2. Sparse filters: 60 cases per character, combining empty/1/2/6-card common subsets, requested counts 0/1/3/6/7 and uniform/ordinary/boss odds. Requests that fit return distinct choices; rarity holes fall back to the available common rarity. Over-capacity inputs fail explicitly within the bounded native selection, matching Ironclad. No duplicate or foreign-class fallback is inserted. These deliberately unsupported filters are not presented as successful reward screens.
3. All 360 saved IDs × base/upgraded × three Heir owners per branch, repeated with card data exported on the other branch. Both-way card decoding preserves ID, upgrade and receiving owner pool. Full regular saves containing upgraded branch-only cards resume on beta; rewards, merchants, transforms and subsequent save/resume are then checked. Native Ironclad is a control.
4. Real native Molten/Toxic/Frozen Egg hooks: ordinary/elite/boss rewards with and without `NoHookUpgrades`, plus upgraded merchant slots, for all four characters. Also test Prismatic Gem's no-pool-modification flag, preserved filters, regular-only explicit custom pools, no random upgrade roll and manual upgrades of generated cards.
5. Knight/Mage and Knight/Ranger local two-peer sessions: both peers generate offers, shops and transforms for both owners; their complete results agree. Prismatic Gem expansion agrees, each peer's compendium matches its frozen class, and existing native paid base/upgraded actions, owner round trips and settlement checks pass.
6. The original unseen-material native Ironclad regression passes on both branches after the fix.
7. Exact ordinary rarity counts and uniqueness for all five native characters and three Heir pools.

The evidence exporter validates 392 regular and 382 beta final replies, unique correlated request IDs, loaded mods, game versions and exact DLL/source hashes. It retains minimized pre-fix material, Prismatic and cached-filter reproductions. Full private receipts/logs remain in the ignored workspace; no game binaries, assets or native saves are committed.

Pure regression suite: **164,053 assertions passed**. Preflight: 32 sheets, 2,062 rows, 26,477 cells. Harness isolation suite: 20 checks. Release build: zero warnings/errors.

## Limits and explicit deferrals

Complete beta saves cannot be downgraded to regular on these native versions. Beta's RNG seed/state is 64-bit; regular rejects it as out of range for `UInt32`, including an Ironclad-only save. The evidence preserves the full diagnostic for all three Heirs and Ironclad. No RNG truncation, migration fabrication or native-save rewrite is attempted. Card identities are compatible in both directions; that does not make the game's entire save schema backward-compatible.

Physical Steam Deck/Linux/controller verification and full natural co-op campaigns remain [#57](https://github.com/CapeKid/SpireLegacy/issues/57)/[#110](https://github.com/CapeKid/SpireLegacy/issues/110). Arbitrary third-party reward filters and wider relic/potion combinations remain [#55](https://github.com/CapeKid/SpireLegacy/issues/55)/[#61](https://github.com/CapeKid/SpireLegacy/issues/61). Regular/beta custom-pool API availability is reported separately.

Some completed labs still emit native WorkerThreadPool/particle/signal-await diagnostics. Their cause is unestablished and remains [#58](https://github.com/CapeKid/SpireLegacy/issues/58)/#110; this is not a clean-log or upstream-cause claim. The reproduced disposed-material exception is absent from all completed stages. Paid card effects are covered by the earlier #52/#53 audits; this audit does not establish campaign balance or win rates.

## Reproduction

Build the Release DLL against the owned regular SDK. Use `scripts/run-balance-campaign.ps1 -PoolAuditChecks` with the documented private independent labs, a fresh tag containing `regular` or `beta`, and the bundled Python interpreter. A tag ending `-export` runs the solo matrix and writes ignored cross-branch fixtures. After both export stages, `-import-crossrun` tests imports. `-eggs`, `-parity`, and `-library-control-fixed` run the corresponding focused stages. Every wrapper requires a new disposable profile and stops only its verified exact process ID.

Use `scripts/run-coop-audit.ps1 -Pair` with a fresh tag containing `pools54` and `fixed` for Knight/Mage; include `pools54ranger` for Knight/Ranger. Run on each branch with separate local ports. The source fixture mutates both peers identically before their paid native actions. It does not disable checksums or use personal saves.

Run `scripts/sts2-pool-audit-evidence.py` against the completed named receipts to regenerate and validate the issue-specific public proof. The generic audit coverage inventory and MODLOG journal remain ignored.
