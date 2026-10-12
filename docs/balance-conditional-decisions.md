# Conditional-card decisions and HP-aware controls

This stage continues after merged PR #91. It changes the diagnostic pilot and records two native-control reruns; production card stats, manor progression and release packages are unchanged.

## Conditional decisions

The native damage preview already includes Legacy conditional bonuses through the branch-compatible damage hooks. The pilot must not add them again. Known single-hit execution, guarded, marked and retained-damage attacks now qualify for the existing estimated-lethal priority, using that preview. Multi-hit attacks, Block-derived and chain damage remain outside lethal priority; reflected damage and boss phases still require a richer solver.

Deck-aware reward scoring now gives partial credit to these conditions:

- Execution receives one quarter of its extra damage as a prospective payoff, rather than assuming every target is below half HP.
- Guarded bonuses depend on the proportion of current deck cards with literal Block.
- Marked bonuses depend on Poison or Vulnerable sources; Weak does not qualify. Guarded/marked coverage is capped at 75%, and their per-hit bonus respects the hit count.
- Retained damage/Block growth receives half of one retention's increment. It does not assume unlimited future growth or optimal holding decisions.

These are transparent heuristic weights, not measured activation probabilities. Support from conditional Block, card generators, potions, relics, special Powers and other complex effects remains incomplete. Retention combat damage uses the actual updated dynamic variable; reward scoring uses a separate discounted expectation. The shared adapter carries special type, upgraded amount and hit count without changing gameplay rules.

Twelve pure decisions test supported/unsupported guarded and marked rewards, Poison/Vulnerable versus Weak, per-hit marked bonuses, partial execution, discounted damage/Block retention, empty support and unknown conditions. Native fixtures cover base/upgraded Executioner Axe at the exact half-HP boundary with an alternative target one HP above it; Counter Lunge and Ambush before/after their conditions activate; and Patient Riposte after real turn-end retention. Each checks target choice, lethal reason, paid damage/death and reward metadata. Exact native validation totals and immutable DLL are recorded in MODLOG.md.

These bounded effect fixtures use a no-manor, no-trait Knight, including an injected Ambush, to isolate the card condition. They do not evaluate a natural Ranger deck or its long-turn rhythm choices. The native control fixtures retain actual Ironclad.

## Native control reruns

The two natural runs use the **merged HP-aware policy before this stage's conditional changes**. They retain regular v0.107.1, A0, no manor/traits, fully unlocked actual Ironclad, normal starters/rewards/rests/relics, tactical combat, deck-aware rewards and the first legal map child. Each uses a new guarded disposable profile. No durable fixture enemies or injected cards are used in campaigns.

| Seed | Previous native control | HP-aware rerun |
| --- | --- | --- |
| SPIRELEGACY-CAMPAIGN-1 | Death, Act 1 floor 17 | Death, Act 1 floor 17 |
| SPIRELEGACY-CAMPAIGN-2 | Death, Act 1 floor 17 | Death, Act 1 floor 17 |

Both reruns reach the Vantom boss and die naturally, with 46 recorded combat turns each. Seed 1 makes nine card-reward choices and finishes with 19 cards; seed 2 makes eight choices and finishes with 18. Neither skips rewards. Final-turn journals show paid cards resolve while the player remains alive, followed by enemy-turn death; these are not card-cleanup failures.

The unchanged terminal floors do not establish that the HP-cost correction is useless, nor that the cards need blanket buffs. In seed 1 the pilot still spends 3 HP on survivable Bloodletting before a lethal enemy turn: reserving one HP for the immediate cost does not optimize survival against incoming attacks. Potion planning, stronger native utility valuation and longer-turn decisions remain important limitations.

Public evidence: [balance-hp-aware-controls.csv](balance-hp-aware-controls.csv) and [balance-hp-aware-control-cards.csv](balance-hp-aware-control-cards.csv), two trial rows and 47 card rows. Campaign DLL SHA-256: `5d01a68a6bb5e3eaa933e7575487046897a054a6b128089c08d0d722c0faddab`; source is merged main `8a80e71`. Historical earlier CSVs remain unchanged. There are no natural campaign results for the new conditional policy yet, and two seeds do not estimate character win rates.

Next: validate remaining Block/chain/exhaust finishers, plan safe potion use and purposeful archetype rewards, then rerun the supported Heir/native comparisons. Broaden character coverage only after the pilot understands the relevant mechanics. Tests use independently copied private regular/beta labs with fresh profiles and verified PID cleanup; installed game files, real saves and Steam branches are untouched.
