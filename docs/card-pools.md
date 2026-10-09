# Shared family and class libraries

Version 0.4.0 gives each heir **75 cards: 30 shared family cards plus 45 unique class cards**. This matches the owned Slay the Spire 1 Ironclad library: 3 basic, 20 common, 36 uncommon and 16 rare cards. Each class has 72 reward/shop cards; the basics are unique starters, not draft rewards.

There are **165 distinct active cards** across the character, not 225: the same 30 shared IDs appear in all three pools. Shared cards contain 8 commons, 16 uncommons and 6 rares; each class contributes 3 basics, 12 commons, 20 uncommons and 10 rares. [The catalogue](card-catalogue.md) lists exactly what each heir can obtain normally and labels shared versus unique cards.

## Shared core

Shared cards keep identical rules, upgrades, art and IDs in all classes. The selected class's passive changes how they fit a deck. Study and Spellweave prime Knight counters, generate Mage charges and help Ranger find more Attacks. Long Reach consumes counters/charges or advances Hunter Rhythm. Woven Incantation rewards Skill sequences in every class; Tracking Rhythm rewards Attack sequences in every class.

| Shared route | Support and payoffs | Class connections |
| --- | --- | --- |
| Exhaust | Reforge, Enchanted Ink, Last Stand, Merciless, Burnished Legacy, Secret Thesis, Arms Vault | Knight adds Forge Sparks and Cast Off; Mage adds Ash Reading and Transmutation; Ranger can exhaust utility cards to cycle into repeatable shots. |
| Retention | Stored Ember, Crystal Seed, Memory Prism, Time Pocket, Measured Breath | Knight adds Patient Riposte; Mage adds Tower Library, Moonstone Lance and Geode Ward; Ranger adds Perch Discipline and Long Vigil. |
| Sequencing | Study, Cold Read, Long Reach, Battle Meditation, Woven Incantation, Tracking Rhythm | Skills support Knight/Mage passives; Attacks support Ranger rhythm. Either sequence can become a secondary route in any class. |
| Delayed resources | Brave Advance, Spellweave, Comet Calendar, Perfect Timing | Knight adds Reinforce and Siege Tomorrow; Mage adds Mana Shield; Ranger adds Patient Hunter. |
| Debuffs | Challenge, Tripwire, Merciless, Alchemist's Patience | Knight gains armor and Strength payoffs; Mage adds Toxic Theory and area spells; Ranger adds poison doubling and marked-target shots. |
| Flexible bursts | Meditate, Guardian Angel, Mirror Manuscript, Master Plan | Resource, protection and Skill duplication tools support several routes rather than one class-exclusive combo. |

Shared cards are identified as **Family card** in their tooltip. They count toward the 75-card total; they are not an additional colorless pool.

## Class identity

| Class | Unique routes | Examples |
| --- | --- | --- |
| Knight | Block into damage, blood costs, Strength multi-hit, Wound fuel and exhaust | Shield Slam, Battle Rhythm, Living Fortress; Blood Price and Forged in Pain; Sword Dance and Grand Melee; Splintered Pike, Scar Tissue and Furnace Breath; Forge Sparks and Phoenix Forge. |
| Mage | Charge spending, Skill chains, retained spells, spell exhaust and debuff magic | Kindle, Overcharge and Arcane Reservoir; Arcane Meter; Tower Library and Sun in a Bottle; Rewrite and Transmutation; Toxic Theory and Venom Rune. |
| Ranger | Attack chains, manual discard, poison, retained shots and Scry | Finishing Flurry and Arrow Cascade; Sort Quiver, Loose Fletching, Pocket Wind, Light Pack and Tailwind; Double Dose and Poisoned Arsenal; Long Vigil and Perch Discipline; Watch the Wind, Owl Watch and Forest Oracle. |

Mage-only charge effects and the class-specific Hunter Rhythm upgrade remain unique. Shared engines work without either passive. Multiple routes overlap: retaining an Attack can support Strength or attack chains; exhaust can improve cycling while fueling Block/damage; debuffs enable both direct hits and poison.

## Save compatibility and verification

All 360 previously released IDs and illustrations remain registered so existing decks and saves can load. The 195 cards outside the curated active set have a legacy tooltip and are excluded from current normal class reward/shop pools. Historical authoring class is retained in cards.json; card_pools.json is the authoritative draft membership table. Effects that deliberately access other colors, such as Prismatic Shard, retain native behavior. Version 0.5.0 rebalances legacy cards as well as the active set.

Preflight checks exact totals/rarities, identical shared membership, disjoint unique memberships, class-compatible shared mechanics, distinct rules within each pool, shop type coverage and verified art. Native checks cover selected-class pool membership, shared card copies/upgrades and passive behavior, rewards with Question Card/Crown/Shard, and merchants. The small-pool reward replacement remains removed; the native game has 16 rares per class to choose from. Full-run balance and physical Deck verification of this pool revision remain pending.

The [0.5.0 manor and balance guide](manor-and-balance.md) explains weaker starters, class passives, upgraded draw/discard rules and the progression economy.
