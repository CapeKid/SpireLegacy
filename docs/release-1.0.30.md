# Spire Legacy 1.0.30

Includes all merged main changes through PR #147 (3b8ce8e). Supports regular v0.107.1 and public-beta v0.111.0 with BaseLib 3.4.7.

- Starting and abandoning runs no longer farms crowns; settlement counts earned rewards once.
- Multi-hit attack bonuses apply consistently, and inherited card copies own their mutable state.
- Manor controller focus and confirmation guards prevent accidental activation; character facing and room presentation update correctly.
- Trail Reset Exhausts. Hand exchanges redraw before deferred Sly cards play, closing the repeatable free Block loop.
- Battle Meditation draws 3/4 cards and discards 1, costing 1 Energy. Recovery card descriptions explain excluded mod recovery cards.
- Includes merged adversarial audits of pools, relics, potions, Ancient rewards, UI, presentation and balance.

All co-op players should update together. Release checks use isolated owned Windows labs and disposable profiles. Physical Deck/Linux retesting and complete multiplayer campaigns remain outstanding; broader human balance and progression testing is tracked in #146.

Workshop delivery is verified separately after upload. Melty is packaged only by this update.
