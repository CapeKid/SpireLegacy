# Engine-policy campaign comparisons and delayed rewards

This stage follows merged PR #94. Six natural runs test the merged finisher/emergency-potion policy. Their results are preserved separately from a subsequent reward-scoring correction. Production cards, traits, manor bonuses, crown values and release packages are unchanged.

## Six natural runs

All trials use owned regular v0.107.1, A0, no traits, tactical combat, deck-aware rewards and the first legal map child. Each starts from a fresh guarded family/save profile, with normal starter cards, encounters, rewards and rests. Knight is tested at no/full manor; actual Ironclad receives its normal starters, Burning Blood and all three card-unlock epochs. Fixture enemies, fuel, protection and potions are not injected into campaigns.

| Cohort | Earlier seed 1 / seed 2 terminal floors | Engine-policy seed 1 / seed 2 terminal floors |
| --- | --- | --- |
| Knight, no manor | 6 / 14 (Act 1 deaths) | 6 / 14 (Act 1 deaths) |
| Knight, full manor | 27 / 25 (Act 2 deaths) | 17 / 17 (Act 1 boss deaths) |
| Fully unlocked Ironclad | 17 / 17 (Act 1 boss deaths) | 17 / 17 (Act 1 boss deaths) |

All six new runs finish with natural deaths, not caps or failed commands. The full-manor runs perform **worse** than the earlier pilot on these two seeds. More recognized effects do not guarantee better whole-run decisions. Manor still extends Knight's survival relative to the no-manor cohort, but this sample does not establish win rates, broad card weakness or overall character parity. Same seeds do not guarantee identical encounters/rewards across characters.

No run consumes a Block Potion. This measures actual use, not acquisition or eligibility: the snapshot does not record a complete potion inventory, and this policy supports only Block Potion. The five native potion fixtures from #94 demonstrate correct consumption/conservation; these natural runs do not measure its survival benefit.

New evidence: [balance-engine-comparisons.csv](balance-engine-comparisons.csv), six trial rows, and [balance-engine-comparison-cards.csv](balance-engine-comparison-cards.csv), 131 card rows. Raw private journals include actual choices, paid plays, decision reasons, target IDs and correlated metadata. The campaign DLL is `1b1404ddc598e3737994aaac93cb5406fc2c180181a1531e15e7da039c2bc341`, built from merged main `ffcdfc8`. Earlier datasets retain their original policies/hashes. Three independently copied regular labs share the same owned game DLL; the third lab's private mod manifest was refreshed to 1.0.27 before use.

No-manor Knight earns 18/30 crowns; full-manor earns 56/45, with its Treasury multiplier. The latter dies earlier than the previous 88/74-crown runs. These are single-run snapshots, not long-term progression or upgrade-price evidence. Full-manor seed 1 skips one offer; all other trials skip none.

## Concrete decision findings

Full-manor seed 1's floor-5 offer is unchanged: Executioner Axe, Brave Advance and Stored Ember. The older scorer chooses Brave Advance (5); the engine-aware scorer chooses Executioner Axe (5.625), after giving its conditional extra damage partial credit. Brave Advance grants one next-turn Energy, but the scorer records only its five immediate damage. This is a verified omission of a real card effect, not evidence that Executioner Axe needs a stat buff.

Full-manor seed 2's floor-12 offer is also unchanged: Counter Lunge, Fortify and Study. The older scorer prefers Fortify's 7 Block score over Counter Lunge's 6. The newer guarded-bonus expectation raises Counter Lunge to 9 and takes it. That substitution replaces a defensive card with an attack. It deserves a controlled defensive-deck comparison, but these whole runs change other choices and do not isolate Counter Lunge as the cause of death. Its reward support estimate and the pilot's actual activation/energy use still differ.

Low use or unplayed-hand counts alone cannot classify cards as undertuned. This pilot misses complex triggers, potions other than Block Potion, purposeful shops/upgrades and some engine choices. Retention, fuel consumption, energy limits and deliberate defense can all create leftovers. No broader stat adjustment follows from these six deaths.

## Reward correction after the campaigns

The shared Heir reward description now includes next-turn Energy, draw and Block, including upgraded amounts. The reward policy gives each delayed resource half the immediate resource weight: Energy 2 versus 4, draw 1.5 versus 3, and Block 0.5 per Energy versus 1. These are discounted expectations, not a promise that the next turn is reached or that the resources are used optimally. Delayed Block is not counted as current-turn protection, and the tactical combat chooser is unchanged.

This fixes the Brave Advance omission without reducing conditional-card stats or forcing the older reward choices. Native cards' ambiguous delayed-variable layouts remain outside this adapter; no generic native variable is relabeled as delayed or immediate by guesswork.

Six pure checks cover actual-style Brave Advance versus Executioner Axe, delayed/immediate discounts and choices involving next-turn draw/Block. Bounded native base/upgraded fixtures verify reward metadata, paid Energy costs, queued native powers and actual next-turn delivery for Brave Advance, Patient Hunter and Reinforce. The Patient Hunter fixture uses a Knight to isolate draw from Ranger rhythm. Protection and durable enemies belong only to these effect fixtures. Exact totals and the later DLL are recorded in MODLOG.md.

The six campaign CSVs precede this correction. They must not be described as results of the delayed-aware policy, and no claim is made that this correction restores Act 2 survival.

Next: controlled same-deck defensive reward comparisons, broader recorded potion inventory/use, and purposeful archetype/shop/upgrade decisions. Rerun the delayed-aware policy before considering numerical card tuning. All tests use private independent labs and disposable profiles with verified PID cleanup; installed game files, real saves, Steam branches and Workshop/Melty listings remain untouched.
