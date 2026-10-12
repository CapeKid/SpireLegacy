# Delayed-aware reruns and a defensive reward comparison

This stage follows merged PR #95. It tests the delayed-aware policy and compares one legal reward choice under matched recorded conditions. Card stats, traits, manor/crown values and release packages are unchanged.

## Natural endpoints

Three fresh-profile trials use owned regular v0.107.1, full-manor Knight, A0, no traits, normal cards/rewards/rests and the same tactical combat/first-legal-path policy. All reach natural deaths with successful commands and no caps.

| Run | Reward policy | Outcome | Crowns | Combat turns |
| --- | --- | --- | --- | --- |
| Seed 1 | Delayed-aware, automatic | Act 2, floor 27 | 88 | 96 |
| Seed 2 | Delayed-aware, automatic | Act 1 boss, floor 17 | 45 | 41 |
| Seed 2 | Same policy, choose Fortify once at floor 12 | Act 2, floor 25 | 74 | 76 |

The previous engine-aware policy reached floor 17 on both seeds. Accounting for delayed effects restores the earlier floor-27 endpoint on seed 1; it does not repair seed 2's choice. Seed 1 is a policy rerun, not a one-choice causal comparison, because other delayed reward scores can also change.

Neither automatic run nor the intervention consumes a Block Potion. This does not establish whether any usable Block Potion was acquired; the snapshot still lacks complete potion inventory and eligibility. Crown differences accompany longer survival and Treasury bonuses, not a progression-price experiment.

## The matched seed-2 choice

The two seed-2 trials have identical **recorded** prefixes: 67 non-wait decisions before the floor-12 reward, including actions, snapshot HP/gold/decks/relics/manor, paid plays and recorded targets. The offer and scores also match: Counter Lunge 9, Fortify 7, Study 6. Timestamp, request-ID and wait-count differences are excluded. This is not a claim that every hidden game/RNG field is captured.

The automatic policy chooses Counter Lunge. The intervention chooses the already offered Fortify through the same native reward UI, exactly once. No extra card, HP, Energy, Block, potion or enemy change is injected into campaigns. All subsequent decisions use the unchanged automatic policy; legal deck changes can naturally alter later draws, choices and RNG use.

The matched-prefix digest is `469b1cd156e226c93a85a8b33b0d36555e1146de6dd30e2ccc256bce24a70925`. [balance-defensive-prefix.json](balance-defensive-prefix.json) records the pair, offer, picks and hashes. Reproduce the comparison with `scripts/compare-balance-prefix.py delayed-knight-full-seed2 fortify-knight-full-seed2 12`; it rejects differing inputs, branches, hashes or recorded prefixes.

On this one seed/deck, the defensive choice clears the first boss and survives eight more floors. This supports investigating the pilot's defensive reward balance rather than prescribing a Counter Lunge nerf or Fortify buff. It does not establish that Fortify is always better, estimate win rates, or validate every class. A one-off intervention is experimental evidence, not the default reward policy.

## Guarded intervention tooling

The wrapper accepts `-ForcedReward fortify -ForcedRewardFloor 12` only with deck rewards and a positive floor. The native pilot can choose only a matching card already in the offer. An absent ID follows normal choice/skip behavior; it cannot manufacture a card. The driver requires the requested intervention to occur exactly once before a completed trial is accepted. The exporter records the requested card/floor and applied count, with empty/zero defaults for older journals.

The final native helper also remembers applied floor/card pairs weakly by run, preventing the same intervention from being replayed on another reward. Native fixtures verify legal procurement, same-pair replay reverting to normal skip, absent-card conservation and normal closure on both branches. Only the isolated opt-in bridge calls this code.

Campaign DLL: `fe307b45652dcfdffb4a61855c3768c7421b5ca78f82f3badd1f78b081b536ae`, source `df3134a`. The later replay-guard/helper build is separately identified in MODLOG.md; the campaign driver already confirms one intervention in the selected trial. The three campaigns use the same immutable DLL and policy. [balance-defensive-ablations.csv](balance-defensive-ablations.csv) has three trial rows; [balance-defensive-ablation-cards.csv](balance-defensive-ablation-cards.csv) has 94 card rows. Earlier datasets are preserved.

## Next balancing decision

Add an explicit, general defensive/archetype objective and test it on more independent seeds and specified decks. Avoid hard-coding Fortify or adjusting weights solely to reproduce this result. Include potion inventory/eligibility, purposeful shop and upgrade choices, and resource cost/availability when selecting support cards. Repeat automatic delayed-aware comparisons across manor tiers before changing numerical card stats.

All trials/fixtures use independent private owned-game labs, disposable profiles and verified PID cleanup. No real saves, installed-game folders, Steam branches or Workshop/Melty listings are changed.
