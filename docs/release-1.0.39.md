# 1.0.39 — Mage Arcane Flow and card updates

Promotes the tested 1.0.36–1.0.39 beta changes to public Workshop item **3816525393**, following the owner's explicit publication instruction. Mage now banks up to two Arcane Charges with Skills and spends them on card draw with the next Attack, once per turn. Arcane Reservoir remains the charge-based damage upgrade.

Six Mage cards support this sequence: Ember Dart retains; Spell Notes Scries before drawing; Rune Detonation conditionally refunds Energy; Moonstone Lance is a cheaper retained Attack; Overcharge and Grand Thesis bank next-turn Energy. Mage and Ranger basics are named Spark Strike and Arrow Strike, matching Knight's Sword Strike and their existing Strike tags. The separate private beta listing remains available.

Issue #184: Mana Burn no longer duplicates Knight's Bloodied Blade. It is a 0-Energy Attack that deals 5 damage (7 upgraded), plus 5 if the owner's Arcane Flow has already triggered this turn. It costs no HP and Exhausts. Bloodied Blade remains the repeatable 9/11-damage Attack with a 3-HP payment.

Mana Burn rewards playing it after another Attack has spent charges. If Mana Burn itself spends the first charges, its first play receives no bonus; a native replay can benefit after that first play triggers Arcane Flow. No Draw blocks the passive draw but does not undo its trigger. The bonus resets with Arcane Flow on the owner's next turn and does not apply to other classes borrowing Mana Burn.

The card keeps its model ID, artwork, rarity and pool membership. Its upgrade improves only printed damage, not the conditional bonus. Regeneration preserves the new design.

The same checked DLL was first delivered to private beta item **3822395190** (`SpireLegacyBeta`) and is now promoted with the public `SpireLegacy` manifest. Validation includes native paid-play checks on regular v0.107.1 and public-beta v0.111.0, 40 final Mana Burn cases, and 166,718 core assertions. Full-climb balance and physical Steam Deck confirmation remain playtesting tasks. Melty is packaged only.
