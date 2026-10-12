# Keep Spellweave paid after upgrading

Spellweave's former upgrade reduced its cost from 1 to 0 while leaving a reusable next-turn Energy/draw effect. Mage Skill charges and other Skill engines make the free setup especially valuable. This was missed in the preceding targeted card review.

Both levels now cost **1 Energy** and queue **1 Energy plus 2 cards next turn**. The upgrade adds **2 immediate Block** instead of reducing cost. It remains reusable and does not Exhaust. Neither the resource payout nor its upgrade scaling increases. Full Armory can modify the upgraded Block through the existing powered Block rules.

The card's ID, pool membership, rarity and artwork are unchanged. Authoring overrides preserve the fix during pool regeneration; the balance audit's current values agree with the card sheet. The generic upgrade and native card behavior apply the Block change without a production-code special case.

Six Core checks enforce the paid cost, unchanged delayed resources and modest upgraded protection. The native fixture plays base/upgraded Spellweave twice across consecutive turns, measuring each Energy payment, delayed delivery, immediate protection, discard destination before reshuffling, and the restored native Energy cost/rules. It uses Mage/no manor/no traits with durable enemies and protected end turns to isolate effects; these are not campaign outcomes or a physical Deck test.

All eight delayed-effect cases pass on regular v0.107.1 and beta v0.111.0 (16 total), including base/upgraded Spellweave on each branch. Each Spellweave play spends one Energy; both following turns have four Energy and seven cards, with no delayed power left after delivery. Immediate Block is zero/two. Serialization reconstructs an actual native cost of one. Both plays resolve into Discard and leave no hovering card.

The complete Core suite passes 3,460 assertions; Release compilation has zero warnings/errors; preflight checks 32 sheets, 2,062 rows and 26,117 cells; disposable regeneration preserves all targeted overrides, card IDs and pools. Tested source `dc1424f`, DLL SHA-256 `e2cc83b438980597354dd92416757ba8eaccfdae5d685a2acd43e56d83bc0b33`. Private evidence: `delayed-reward-spellweave-paid-fixed-{regular,beta}.json`.

The initial fixture checked the card's destination after ending the following turn and failed because a legitimate reshuffle moved it into Draw. The corrected fixture checks Discard immediately after the second paid play. Initial failed driver runs remain diagnostic evidence and are not counted as passes. All final clients exit through the guarded wrapper's verified-PID cleanup; installed games and real saves are untouched.

The previously prepared 1.0.28 archives and their validation record describe the earlier immutable candidate. They do not include this follow-up fix and must be regenerated and revalidated before publishing the updated code. No Workshop/Melty publication occurs in this change.
