# Pacifist pilot decisions (#146)

The [hard-trait study](hard-trait-relic-validation.md) showed that the test pilot acquired blocked Attacks and could exhaust its Poison starters until it could no longer win. This step changes only the opt-in measurement policy and its card descriptors. Player-facing cards, traits, inheritance, prices and crown payouts are unchanged.

## Corrected policy

- The descriptor marks actual Attacks permanently blocked for their Pacifist owner. Converted starters remain Skills and usable. The shared reward/shop/removal/upgrade scorer gives blocked cards a finite score below the skip threshold, allowing all-blocked optional rewards to be skipped. It does not confuse temporary lack of energy with a permanent restriction.
- Converted starters report their actual inherited Poison: two normally, three upgraded. Combat and reward evaluation use that value rather than treating Mercy as a zero-damage card with no offensive effect.
- Planned Pacifist discard/exhaust choices rank blocked cards and statuses as fuel, and preserve supported damage/Poison cards ahead of ordinary utility. Recovery reverses that preference. A mandatory exhaust is declined when its required fuel would include the last supported damage source in hand/draw/discard. Native minimum selection requirements are never bypassed.

The last-source guard is deliberately narrow: it handles the `exhaust_one` effect, including its two-card Transmutation variant, and direct damage/Poison descriptors. It is not an expert model of passive damage powers, multi-card plans or all possible exhaust effects. Strength/Vigor-only utility and other trait-specific opportunities still need judgment; these changes do not establish an experienced-player policy.

## Native verification

Nine synthetic cases per branch cover all three heirs: normal controls, Pacifist starters and upgraded Pacifist starters. The fixture uses a debug Nibbits encounter, a controlled hand and durable enemies; these injections are excluded from natural campaign evidence. Each Pacifist case verifies native reward legality, actual Reforge fuel exhaustion, preservation of the final Poison starter under one- and two-card exhaust requirements, and the next selected play. It then plays the starter normally and compares actual Poison against the descriptor. Normal controls deal damage and receive no invented Poison.

All eighteen cases pass on regular and public-beta. The core suite passes 164,062 assertions, including new blocked-reward/skip/finite-score checks. The existing 20 harness and 17 pacing/launcher checks pass. Design preflight/build pass with zero warnings/errors. The core suite uses a disposable temporary folder under ignored `private/`; no personal saves are touched. The existing gameplay `sts2-pacifist-check.py` is preserved; the new policy driver has a separate name.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-coop-regular3-lab `
  -Tag YOUR-UNUSED-POLICY-TAG -Class ranger -PacifistChecks `
  -Python C:/Users/seth3/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe
```

`-PacifistChecks` runs all three class controls/variants regardless of the wrapper's required `-Class` argument. Raw correlated receipts stay private; derived evidence records source/DLL/game versions and receipt hashes without profile paths or names.

The [calibration records](evidence/balance146-pacifist-policy/calibration.json) retain every stage. The first eighteen cases verified the original one-card guard. An added two-card fixture initially used Aim as its supposedly non-offensive filler; Aim is itself an inherited Poison starter. Both branches correctly reported that test setup's expectation as false. Those failed setup attempts are retained with `status=error`, not counted as passing policy tests. Replacing Aim with Guard yields eighteen passing final cases, including the two-card guard. Final fixture source is `b1c863742b22e296d065fd50d9eef493765eb60f`, DLL SHA-256 `b16d35b44cc8288978f95708462e11373d417214ae310b624da56e096202c14e`.

## Bounded natural follow-up

Two new standalone Ranger campaigns use the earlier study's seed `BALANCE146-E`, Ascension 5, full manor, Antique + Pacifist, generation 3, Neow enabled and FTUE disabled. They use `planned`/`defensive` policies and no reward interventions or injected combat resources. Fresh profiles are used; neither old run is resumed or replayed. Starting relics remain Meal Ticket on regular and Gorget on beta. Caps stay 20 minutes/1200 decisions per run, 40 turns per combat, 50 plays per decision, 45 seconds continuous waiting and 180 seconds per command.

| Branch | Earlier policy | Corrected policy | Earned crowns |
| --- | --- | --- | ---: |
| Regular | Combat cap, floor 7/6 HP | Natural death, floor 14 | 72 |
| Public-beta | Natural death, floor 8 | Natural death, floor 5 | 21 |

The [174 completed-run decisions](evidence/balance146-pacifist-policy/decisions.jsonl) and [campaign table](evidence/balance146-pacifist-policy/campaigns.csv) show no blocked Attack reward picks. Regular picked Tripwire, Reforge, Retreat, Light Pack and Owl Watch; beta picked Sort Quiver, Burnished Legacy and Tripwire. Regular made one shop purchase and two rest choices; beta used one potion. Both naturally lost, without caps or command failures, and both still played inherited Poison starters in their final turns. No general survival improvement or card/trait balance conclusion follows from one seed, especially when reward and subsequent RNG/route decisions differ.

These campaigns use source `241a73f5ed3ef61c1524da1c85ee787c4a5b2445`, DLL SHA-256 `1c5f005847c97ba3b10b9f69e5117934c6aede8e94347e285cdeb6738f4dbce6`. They test the reward/Poison/fuel fixes and original one-card guard, preceding the two-card guard extension; the later guard is verified by the final synthetic cases, not claimed as naturally campaign-tested. All artifacts use mod `1.0.30`, BaseLib `v3.4.7`, and pinned copied game versions regular `v0.107.1`/public-beta `v0.111.0`. This is coverage of those versions, not a claim about every later game update.

[Initial conditions](evidence/balance146-pacifist-policy/initial-conditions.json), [receipt/artifact manifest](evidence/balance146-pacifist-policy/manifest.json) and [card observations](evidence/balance146-pacifist-policy/cards.csv) preserve provenance. The header-only trajectories file means no paid multi-return trajectory was collected. Prior public evidence is unchanged. Lab processes were verified stopped. Native log-pattern counts remain in the manifest; successful commands do not imply warning-free engine logs.

## Remaining scope

Issue #146 remains open. These are policy corrections, not balance buffs, expert win-rate estimates or a claimed solution to the separate native beta Mage death-animation crash. That crash still needs isolated reproduction at normal animation speed. Developed-archetype campaigns and recorded current-build winning runs remain requirements; the user's reported A10 full-manor Knight Steam Deck/public-beta win remains qualitative evidence with unknown seed/build/deck.
