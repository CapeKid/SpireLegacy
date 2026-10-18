# 1.0.36 — unpublished Mage test build

Issue #185: Mage's Arcane Flow turns Skills into card access instead of duplicating Knight's damage bonus. Each Skill stores 1 Arcane Charge, capped at 2. The next Attack consumes the charges and draws that many cards, once per player turn. An uncharged Attack does not use the draw allowance. Further Attacks still consume charges; unspent charges carry between turns. Arcane Reservoir still adds its printed damage per charge to every hit.

The draw uses the native draw command, including hand limits and No Draw. Replays and multiple hits cannot repeat the draw. The allowance belongs to the individual player's class power and resets on that player's turn.

This is a local test build. Public Workshop item 3816525393 remains on 1.0.35. Do not upload this build or push its branch publicly without the owner's instruction. Balance needs full-run playtesting.
