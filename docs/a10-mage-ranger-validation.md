# Ascension 10 Mage and Ranger validation (#146)

This follow-up isolates investment and class coverage on the regular branch. It does not estimate experienced-player win rates or change card stats, prices, or crown rules. See [the paid manor study](long-term-manor-pacing.md) for the completed purchase curves and earlier A10 Knight results.

## Protocol

Twelve fresh disposable campaigns cross Mage/Ranger, seeds `BALANCE146-C`/`BALANCE146-D`, and manor tiers 0/1/2 at Ascension 10. Tier 0 grants no levels, tier 1 sets each upgrade to integer half its maximum, and tier 2 sets every upgrade to maximum. These starting investment states are fixture inputs, not purchases earned in these standalone runs. All heirs are trait-free, generation 3, with Neow enabled and FTUE disabled.

The existing `planned` combat/route/potion/shop/upgrade policy and `defensive` deck-aware reward policy remain fixed throughout. There are no forced reward interventions, debug combat entries, injected combat resources, or mutation retries. Each campaign uses a separate profile. Both independently copied labs use the same regular game build, dependencies and mod DLL. Native act selection chooses the first available act model in each slot.

Each run has a 20-minute and 1200-decision limit, a 50-card limit per combat decision, a 45-second continuous-wait limit and a 180-second command timeout. The guarded wrapper verifies lab provenance, refuses an occupied lab, and cleans up its exact executable/PID. Failed or capped trials are retained and excluded from natural outcomes. Public evidence contains derived decisions and hashes, never personal profiles, owned game files or decompiled source.

The pilot remains a heuristic: it lacks expert multi-turn strategy, supports limited potions and shop relics, and usually selects the first non-immediately-lethal event option. Identical seeds help reproduce a specific configuration; class, reward and route differences can still change subsequent outcomes. Comparisons report reached floors and actual choices, not a causal effect of individual manor upgrades.

## Results

All twelve runs ended in natural deaths (`hp=0`, `wins=0`), without caps, unsupported decisions, forced rewards or command failures. The export includes 1,306 correlated commands and 502 non-wait decisions. All commands used game `v0.107.1`, BaseLib `v3.4.7`, mod `1.0.30`, source `7bc3db8ec5f4063d887cd6370776b3dec2f743eb`, and DLL SHA-256 `5a5e7eeec4ed16b06aaf1e51330bc1784cce81c5b6a70a8b4a3ea8ff8db14d22`. That runtime source is unchanged in merged main `d185b66`; this PR changes export tooling/documentation only. Both batch wrappers exited successfully and their lab clients were stopped.

Each cell lists terminal floor / earned crowns for seeds C and D:

| Class | Manor | Seed C | Seed D |
| --- | --- | ---: | ---: |
| Mage | None | 6 / 11 | 4 / 6 |
| Mage | Partial | 6 / 12 | 6 / 12 |
| Mage | Full | 7 / 15 | 7 / 16 |
| Ranger | None | 6 / 11 | 4 / 6 |
| Ranger | Partial | 8 / 17 | 5 / 9 |
| Ranger | Full | 7 / 15 | 5 / 10 |

The twelve runs made **zero potion uses, shop purchases, rest-site choices or rest-site upgrades**. The configured pilot supports those decisions, but these early deaths did not exercise them. Event-driven upgrades are separately visible in combat card records; they are not rest-site upgrades. This study does not complete deliberate shop/upgrade/potion or developed-archetype campaign coverage. The empty `trajectories.csv` means no paid multi-return trajectory was collected in this study, not that standalone deaths paid no crowns.

Mage reached the same or later floor with greater investment on these seeds; Ranger's full-manor seed C ended earlier than its partial-manor trial. Investment changes combat, rewards and later choices, so the small sample cannot establish an individual upgrade's causal effect or a class ranking. The all-loss pilot result must not override the user's reported human A10 win or be used alone to justify card buffs.

Every private error log contains two compatibility-renderer particle warnings. The recorded `ERROR`, `InvalidTaskId` and `double free` patterns are absent; this is a bounded log-pattern observation, not a claim that every possible engine warning was checked.

Derived evidence is in [the campaign table](evidence/balance146-a10-mage-ranger/campaigns.csv), [exact artifact/receipt manifest](evidence/balance146-a10-mage-ranger/manifest.json), [card exposure counts](evidence/balance146-a10-mage-ranger/cards.csv) and [decision records](evidence/balance146-a10-mage-ranger/decisions.jsonl).

## Reproduction

Build the current mod against the owned regular references and use an independently copied lab under ignored `private/`, with its provenance and dependencies. Run every combination with a unique unused tag:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-coop-regular3-lab `
  -Tag balance146-a10-mage-regular-C-0 -Class mage -Tier 0 `
  -Policy planned -Rewards defensive -Seed BALANCE146-C -Ascension 10 `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

Collect only the twelve new `balance-campaign-balance146-a10-{mage,ranger}-regular-{C,D}-{0,1,2}.json` receipts into a new private study directory after both workers stop. Export that directory to `docs/evidence/balance146-a10-mage-ranger` with `scripts/export-balance-pacing.py`. Older published studies remain unchanged. The exporter now permits standalone studies and writes a header-only trajectories table rather than requiring unrelated paid returns.

## Remaining scope

Issue #146 remains open for recorded current-build winning campaigns, experienced archetype decisions and representative hard-trait/relic combinations. This matrix covers higher-Ascension class/investment configurations, not twelve hand-built archetypes or beta parity. The user's A10 full-manor Knight win on Steam Deck/public-beta remains a qualitative report with unknown seed, exact build and decision record.

The next useful step is to calibrate class-specific decision policies against recorded combat choices and matched native controls, then collect campaigns that actually reach shops/rest sites and develop an archetype. Repeating the same early-loss policy on more seeds will not resolve the experienced-play gap. Follow with hard-trait/relic cases under the calibrated policy and complete winning campaigns, preserving build, branch and policy distinctions. The completed 80/120-return paid manor curves need no additional identical loss-heavy extension for this purpose.

The exporter passes all 16 integrity tests, including standalone and failed-only studies. Audit ownership maps this study to #146; prior public evidence snapshots are unchanged.
