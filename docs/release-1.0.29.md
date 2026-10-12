# Spire Legacy 1.0.29

Built from merged main ad8561f, including PRs #106, #111 and #115. Supports regular v0.107.1 and public-beta v0.111.0 with BaseLib 3.4.7.

- Family saves, purchases and run settlement preserve durable state through failures and replay.
- Co-op ownership and inherited snapshots are validated and isolated per family. All peers must use 1.0.29; the network protocol has changed.
- Transferred or recipient-duplicated starters use their new owner's frozen traits while preserving card-local buffs.
- Overcompensation charges once per Attack per turn, including repeated draws.
- Shocked and Diva descriptions match next-draw timing and every enemy death.
- Includes all balancing and presentation improvements shipped in 1.0.28.

Full audit evidence is in adversarial-lifecycle.md, adversarial-coop.md and adversarial-traits.md. Release checks use independent owned Windows labs and disposable saves. Physical Deck/Linux retesting and full multiplayer campaign/reconnect testing remain outstanding; native beta timing diagnostics are tracked in #110. Crown farming #105 remains open.

Steam Workshop delivery is verified separately in workshop-1.0.29-delivery.json after upload. Melty is packaged only, not published by this update.
