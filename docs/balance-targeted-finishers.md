# Three targeted finisher payoffs

This completes the second of the three agreed remaining balancing passes after PR #97. The campaign comparisons support correcting particular payoff problems, not increasing the entire pool. No new campaign-pilot scoring feature is added.

Numbers separated by a slash mean base/upgraded. Stats exclude traits, manor powers and class charges.

| Card | Before | After | Preserved limits |
| --- | --- | --- | --- |
| Royal Execution | 3 Energy, 16/20 damage; double printed damage against a target at or below half HP | 20/26 damage | Single target, full Energy cost, health condition |
| Celestial Spear | 3 Energy, 24/30 damage | 26/34 damage | Single target, full Energy cost, no new scaling mechanic |
| Arrow Cascade | 2/1 Energy, 2 damage per preceding Attack to all enemies, Exhaust | 4 damage per preceding Attack | No unconditional printed damage, prior Attack setup, Exhaust, same cost upgrade |

## Benchmark and choice of scope

The owned regular native Bludgeon costs 3 Energy, deals 32/42 single-target damage and is uncommon. Celestial Spear was a rare spending an entire ordinary turn for 24/30 damage. Its former upgrade added only six damage. It now deals 26/34, or 30/38 with full Smith and two Mage charges: still below Bludgeon's unconditional 32/42. A no-manor run remains weaker, while the upgrade and limited class setup give the rare a more useful payoff.

Royal Execution previously dealt 16/20 outside its half-health condition, scarcely better than low-cost alternatives for three Energy. It now deals 20/26 outside the condition and 40/52 inside it. Full Smith adds two, giving 22/28 or 42/54 without a primed Knight counter. The execute multiplier applies to printed damage before Strength, not twice to Strength or class charges. Its above-benchmark result still requires the health condition; it cannot supply the same reliable opening damage as Bludgeon.

The owned native Finisher costs one Energy and deals 6/8 damage **as a separate hit for each preceding Attack**. Finishing Flurry's 3/4 aggregate damage per Attack already gives a weaker no-manor single-target version. Arrow Cascade previously spent 2/1 Energy and Exhausted for only two per preceding Attack, including a very poor two-Attack payoff of four damage to each enemy. Raising that aggregate amount to four gives eight with two preceding Attacks. It remains an area finisher rather than copying Finisher: one hit per enemy, Strength/Vigor once per enemy, no repeated-hit scaling with the Attack count. The attack-count calculation and costs are unchanged.

Multi-hit area rares such as Starfall and Storm of Arrows are left unchanged. Full Smith adds damage on every hit, so buffing their printed damage by analogy with a single-hit finisher would compound manor scaling. Draw, recovery, Poison, manor levels and the defensive reward rule are also unchanged. These changes are bounded tuning decisions based on costs and actual mechanics, not a claim of optimal balance or measured human win rates.

## Verification

Authoring overrides preserve all three changes during pool regeneration. Card identities, art paths, membership and branch pool sizes are unchanged. The catalogue and balance audit rows agree with the sheets. Six added Core checks keep the unconditional budgets below the owned benchmark and preserve Arrow Cascade's setup and single-use limits.

The native payoff fixture uses ordinary paid manual plays, durable opponents and a cleared hand to isolate effects. It checks base/upgraded cards at no/full manor with and without setup: Royal Execution at full/half target HP, Celestial Spear after zero/two paid zero-cost Skills, and Arrow Cascade after zero/two paid zero-cost Attacks. It checks Energy spent, discard/exhaust destination, table cleanup and serialized rule consistency. All opponents remain alive so overkill cannot truncate measurements. Artificial target HP and added cards are fixture setup only; these are not natural campaign outcomes.

All 24 cases pass on each owned branch, regular v0.107.1 and beta v0.111.0, with identical expected payoffs (48 total). [Measured results](balance-finisher-payoffs.csv) preserve damage by target, Energy spent, charges/preceding Attacks, destination, serialization result and immutable source/DLL hashes. The first target is selected for single-target cards; zero damage to other targets is expected. Full Smith still gives Arrow Cascade two damage per enemy with no preceding Attack, or ten after two Attacks, from its existing powered-attack hooks. No new unconditional printed damage is added.

The complete Core suite passes 3,454 assertions. Release compilation has zero warnings/errors, preflight checks 32 sheets/2,062 rows/26,117 cells, and disposable pool regeneration preserves every targeted override, saved identity and pool membership. The regeneration check also rejects stale audit `after` values for these three cards.

Native evidence tags are `finisher-payoffs-payoffs-regular.json` and `finisher-payoffs-payoffs-beta.json`. Their DLL is `e2e840688b7e3bb66a90bb1ccdccc400285f4f08128105f980910a9cb4ea2847`, source `a335404`. A later audit-only correction synchronizes the two single-hit rares' old `after` rows, including their current non-Exhaust flag; it changes no gameplay sheet values. The final build hash is `1b8abf0ed31dbd22047351d9d10912c8e71b644d9158036384fe625ee7609e5e`. Compilation/preflight/regeneration include that correction; the native result CSV explicitly retains the tested build rather than claiming a different binary was run. The planned final regression pass will test the merged release candidate.

All native runs use independent private game copies and disposable profiles. Wrappers stop only their verified client PIDs. Real saves, installed games, Steam branches and published listings are untouched. This is not physical Steam Deck validation.

The final remaining pass is regression testing and Workshop preparation after review. This PR does not publish a Workshop or Melty update, change the release version, or merge itself.

Completed follow-up: [1.0.28 release regression and prepared packages](release-1.0.28.md). The exact release DLL now passes both branches, including all 48 finisher cases; the earlier immutable build hashes above remain the evidence for the original tuning PR.
