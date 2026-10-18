# 1.0.37 — unpublished Mage pool test

Builds on the private Arcane Flow test from 1.0.36. Six existing Mage cards now support preparing, timing, and using its once-per-turn draw. Existing card IDs, art, rarities, and pool membership stay compatible with saved cards.

| Card | Base → upgraded | Purpose |
| --- | --- | --- |
| Ember Dart | 0 Energy, 3 → 5 damage, now Retain | Hold an Attack until Skills fill charges, even with no Energy left. |
| Spell Notes | 1 Energy, Retain; Scry 3 → 4, draw 1 | Prepare the top of the deck for Arcane Flow; replaces draw/discard. |
| Rune Detonation | 1 Energy, Retain; 6 → 9 damage; refund 1 Energy with two charges and Arcane Flow ready | A full-charge payoff leaves Energy to play the drawn cards. Further Attacks and replays cannot repeat the refund that turn. |
| Moonstone Lance | 1 Energy, Retain; 5 → 7 damage, grows by 2 → 3 per retained turn | A cheaper Attack for retained hands; lower growth pays for the lower cost. |
| Overcharge | 0 Energy, Exhaust; lose 3 HP, gain 2 charges, next turn gain 2 → 3 Energy | Bank charges and Energy for the next draw turn; removes immediate draw. |
| Grand Thesis | 1 Energy, Exhaust; draw 2 → 3, gain 1 charge, next turn gain 1 → 2 Energy | Combines setup and next-turn resources; its Skill play also supplies the normal passive charge. |

Rune Detonation checks its owner's Mage class, charges and unused draw allowance during resolution. The class power consumes charges and claims the draw after the Attack; replayed resolutions therefore cannot refund again. No other class receives the refund when borrowing the card. The pilot accounts for the refund only when its conditions hold.

Arcane Reservoir still gives charged multi-hit Attacks a damage build. Existing exhaust, debuff, and retention options remain available. The pool changes add alternatives to spending every charge on the first available Attack; they do not increase Arcane Flow's draw limit.

This is a local test build. Public Workshop item 3816525393 remains on 1.0.35. No branch, PR, release, Workshop or Melty upload has been published for this test. Follow the [private Steam Deck test guide](mage-private-test.md). Full-climb balance and physical Deck confirmation remain playtesting tasks.
