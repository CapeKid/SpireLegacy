# Heir potions (1.0.22)

The Heir has a dedicated three-potion character pool. Native generation appends shared potions once, preserving their availability without listing them twice. Exclusive potions use normal drops, shops and in-combat potion generators, along with native belt slots, prices, targeting, consumption and saved identities. They do not replace shared or other characters' potions.

| Potion | Rarity / target | Effect | Native comparison |
| --- | --- | --- | --- |
| Borrowed Breath | Common / self | Gain 6 Block and draw 1 card. | Block Potion grants 12 Block; Swift Potion draws 3. A smaller flexible combination. |
| Briar Vial | Uncommon / enemy | Apply 4 Poison and 1 Weak. | Poison Potion applies 6 Poison; Weak Potion applies 3 Weak. Both components are reduced. |
| Triune Draught | Uncommon / self | Knight: 8 Block and 2 Vigor. Mage: 2 Arcane Charges, capped at 2, and draw 1. Ranger: 3 Vigor and draw 2. | One-use class resource. Less pure Block/draw than the common native equivalents; no persistent Strength or Dexterity. |

All are combat-only and require a living valid target. Block follows native unpowered rules. Vigor applies to every hit of the next Attack. Draw obeys native hand limits and draw prevention. Mage charges remain capped at two, including repeated draughts. The active heir class determines the draught's effect when used, including after loading a saved belt. Native queueing spends no Energy and removes the used potion exactly once.

Pacifist can use the vial because it is a potion, and benefits from the tonic and the draught's defensive/draw component. Its inability to play Attacks still limits Vigor and Arcane Charge use. These potions grant no healing, permanent stats, crowns or manor bonuses, so they do not bypass Hero Complex or change Super Healer. Existing shared potions and family/run saves retain their IDs; no belt migration is necessary.

The top-bar belt, native popup and enemy targeting use normal game controls. Selection outlines and keyword tooltips are provided. Controller focus and saved belts are verified in isolated owned Windows labs; physical Steam Deck input and campaign balance remain playtesting requirements.

All three 128px vector icons and white selection outlines are original CapeKid artwork, MIT. `scripts/author-heir-potions.py` reproduces them; `sheets/potion_art.json` records SHA-256 and provenance. No game-derived artwork or extracted content is required. `sheets/heir_potions.json` defines stable IDs, rules and balance references.

Development checks use `scripts/sts2-potion-check.py regular` or `beta` against opt-in private test profiles. Normal play does not enable the test bridge. Tests cover distinct native options, shared availability, drops/shops/in-combat generators, all class effects, charge cap, invalid targets, actual native queue removal, serialization and full run save/resume, original inventory art and controller focus.
