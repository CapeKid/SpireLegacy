# Six card candidates: resource limits and setup costs

Historical 1.0.28 measurements below are preserved. Second Quiver is redesigned as selective Attack recovery in [the 1.0.40 beta](release-1.0.40.md); its older draw/Energy behavior is no longer current.

This review follows the Spellweave correction and covers the six specific candidates raised by the user. The decisions use the current effects, intended archetypes and privately inspected owned regular-game benchmarks. They are bounded tuning judgments, not campaign win-rate findings or an exhaustive balance verdict for every card.

Values separated by a slash mean base/upgraded, before traits, manor and combat powers.

| Card | Before | After | Reason |
| --- | --- | --- | --- |
| Slow Incantation | 1/0 Energy; next turn gain 1 Energy, draw 1 and gain 3 Block; reusable | 1 Energy at both levels; same delayed payout; upgrade adds 2 immediate Block | Remove repeatable free resource setup while preserving a useful upgrade, like Spellweave |
| Second Quiver | 0 Energy; draw 2/3, gain 1/2 Energy, Exhaust | Draw 2 at both levels; same Energy/cost/Exhaust | Match native Adrenaline's 2 draw and 1/2 Energy, instead of upgrading both resources |
| Mana Fountain | 0 Energy; gain 3/4 Energy, Exhaust | Gain 2/3 Energy; same cost and Exhaust | Reduce the drawback-free burst while retaining the rare's finite, single-use role |
| Eternal Bookmark | Rare Power, 2/1 Energy; choose two cards to retain each turn | 1 Energy; choose 2/3 cards | Make base setup affordable and give the rare a distinct payoff upgrade |
| Battlefield Recovery | Rare, 2 Energy; draw 2/3, heal 4/5, Exhaust | 1 Energy; same draw, healing and Exhaust | Leave Energy to use the drawn cards or defend, without increasing healing |
| Iron Avalanche | Rare, 3 Energy; 6/8 area damage, +18 if already blocking, Exhaust | 2 Energy; same damage, condition and Exhaust | Allow a paid one-Energy guard followed by the finisher within an ordinary three-Energy turn |

## Benchmarks and archetype tradeoffs

The owned regular Adrenaline costs zero, draws two, grants 1/2 Energy and Exhausts. Second Quiver now matches those resources; the previous upgrade added an extra draw with no extra cost. It remains better than shared Meditate's base draw and equal to its upgraded resource burst. No HP penalty is reintroduced.

Owned regular Turbo and Bloodletting each grant 2/3 Energy at zero cost but have status/HP drawbacks and remain reusable. Mana Fountain now supplies the same Energy without those drawbacks, balanced by rare availability and Exhaust. It still offers burst setup and can be legally recovered; Exhaust is a limit, not proof that a card cannot enable strong combos. Recovery-cycle restrictions are unchanged.

Regular Well-Laid Plans costs one Energy and allows 1/2 chosen cards to retain. Eternal Bookmark is a rare and now offers 2/3 at the same setup cost. The beta native counterpart's whole-hand retention is different; this mod's existing chosen-card hook stays bounded and makes a fresh choice each turn. Intrinsic Retain cards do not consume the choice allowance. It does not keep the entire hand indefinitely.

Battlefield Recovery's two-Energy cost competed poorly with cheaper defensive healing and often left only one Energy to use its draw. The fix changes cost rather than increasing the healing amount. Iron Avalanche still has weak unsupported damage and Exhausts: its stronger result requires protection already in place. The one-Energy guard also primes the Knight's existing counter. Multi-hit scaling, manor/crowns, traits, pool membership, rarity, IDs and artwork are unchanged.

## Separate draw upgrades

The shared `upgradeMagic` field previously increased both Second Quiver's Energy and draw. A separate `upgradeDraw` field now allows its draw increment to be zero while leaving the Energy upgrade at one. The sentinel `-1` preserves the shared upgrade behavior for every other card; the runtime also accepts older rows without the field. Only Second Quiver uses an explicit override.

All 360 source rows receive the same column, with only six gameplay rows differing from merged main after normalizing that sentinel. Both historical rebalancing and pool-expansion authoring preserve the field and targeted overrides. The current audit rows are synchronized. The previous under-tuning driver now expects Second Quiver to draw two when upgraded.

## Verification scope

The complete Core suite passes 3,833 assertions, including the six cards' costs/resources and preserved draw defaults for the other 359 cards. Release compilation has zero warnings/errors; preflight checks 32 sheets, 2,062 rows and 26,477 cells; disposable pool regeneration preserves the targeted cards, IDs, membership and branch sizes.

Native cases use no/full manor, base/upgraded cards and normal paid plays. Slow Incantation is played twice across real turns; Eternal Bookmark selects the maximum permitted cards during actual flushing; Iron Avalanche is tested with no protection and after a paid Fortify. Other cases measure draw, Energy, healing, damage, destination, table cleanup and serialized native cost/rules. Durable enemies, cleared hands and protected end turns are effect-isolation setup, not injected resources for a natural campaign. All clients use independent private copies and disposable profiles.

An initial fixture attempted to nest a maximum-choice selector inside the default test selector. The game rejects that; the corrected guarded bridge supplies the fixture's selector at the outer scope. Initial failed runs are diagnostic evidence and are not counted as passes.

All 28 cases per branch pass on owned regular v0.107.1 and beta v0.111.0 (56 total). [Recorded results](six-card-review-cases.csv) preserve individual measurements and immutable source/DLL hashes. Slow Incantation pays one Energy on both uses, then delivers four Energy, 6/7 cards and 3/5 Block at no/full manor; its upgraded immediate Block is 2/4. Second Quiver draws two while gaining 1/2 Energy at both tiers. Bookmark actually retains 2/3 selected cards plus the intrinsically retained card. Recovery draws 2/3 and heals 4/5 at a one-Energy cost. Supported Avalanche spends one Energy on Fortify and two on the attack, dealing 26/28 without manor and 28/30 with full manor, versus unsupported 6/8 and 8/10. The quick-start encounters in these cases contain one opponent; this validates the conditional payoff and affordability, not a new multi-enemy damage matrix. Area targeting is unchanged.

Tested source `911d53c7d2f409d03a700fe71da3434e5b40fe6d`, DLL SHA-256 `74d3ea23386e7c4404b4de83c11ab9a39d66d050b4d45049e50878bfa4730032`. Private final journals: `six-candidates-six-review-fixed-{regular,beta}.json`. All replies and final wrapper exits succeed, with verified client cleanup. No installed game, real save or Steam branch is changed.

Previously prepared release archives predate these changes and the Spellweave follow-up. They must be regenerated and revalidated before publication. No Workshop/Melty upload, release-version bump or physical Steam Deck test is performed in this card review.
