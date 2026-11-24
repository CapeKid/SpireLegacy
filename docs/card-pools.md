# Shared family and class libraries

Version 1.0.20 gives each heir **82 ordinary reward cards on regular and 85 on public-beta**, matching the native StS2 characters on regular v0.107.1 and public-beta v0.111.0. Each class also has three basic starters. Regular has 20 common, 36 uncommon and 26 rare rewards; beta has 20 common, 38 uncommon and 27 rare. Ancient cards are separate and tracked in #26. The earlier 75-card target was based on StS1.

There are **188 distinct active cards on regular and 197 on beta**. Each class has the same 34 shared IDs: 8 common, 16 uncommon and 10 rare (15 Skills, 12 Attacks and 7 Powers). Every class also starts with the shared basic Family Motto. Regular class specialties add 3 basics, 12 commons, 20 uncommons and 16 rares; beta adds two uncommons and one rare per class. [The catalogue](card-catalogue.md) lists shared, unique and beta-only cards. [The expansion guide](sts2-pool-expansion.md) covers new choices, upgrades and balance comparisons.

## Starting deck

Each heir starts with 4 Strikes, 4 Guards, the class signature and Family Motto (0 Energy Skill: draw 1 card, or 2 upgraded). Family Motto reacts to the heir: the Knight gains 3 Block, the Mage gains an extra Arcane Charge, and the Ranger counts it as an Attack for Hunter Rhythm.

## Shared core

Shared cards keep identical rules, upgrades, art and IDs in all classes. The selected class's passive changes how they fit a deck. Six cards marked in the catalogue (Family Motto, Heirloom Blade, Clean Armor, Banner Volley, Heraldry and Birthright) also carry one line per class. In combat only the heir's line applies; the Compendium shows all three. Knight Block lines are not scaled by Dexterity, because the Manor already scales the card's printed values.

| Shared route | Support and payoffs | Class connections |
| --- | --- | --- |
| Exhaust | Reforge, Enchanted Ink, Last Stand, Merciless, Ancestral Salvage | Knight adds Forge Sparks and Cast Off; Mage adds Ash Reading and Transmutation; Ranger can exhaust utility cards to cycle into repeatable shots. |
| Retention | Stored Ember, Crystal Seed, Memory Prism, Time Pocket, Eternal Bookmark, Hearth Memory | Knight adds Patient Riposte; Mage adds Tower Library, Moonstone Lance and Geode Ward; Ranger adds Perch Discipline and Long Vigil. |
| Sequencing | Study, Long Reach, Battle Meditation, Woven Incantation, Tracking Rhythm, Heraldry | Skills support Knight/Mage passives; Attacks support Ranger rhythm. Heraldry starts each turn with the heir's passive already in motion. |
| Attacks | Heirloom Blade, Banner Volley, Lineage Strike, Birthright, Crown Breaker, Family Oath | Lineage Strike costs 1 less for each Power played this combat; Crown Breaker refunds Energy and cards on a kill. |
| Debuffs | Challenge, Tripwire, Inherited Grudge, Old Grudges | Inherited Grudge strikes again at Weak or Vulnerable targets; Old Grudges pays off Hunter Rhythm, Challenge and Tripwire. |
| Flexible bursts | Meditate, Guardian Angel, Mirror Manuscript, Comet Calendar, Generations | Generations adds a free, exhausting class card each turn (3 Energy, 2 upgraded). |

Cold Read, Arms Vault, Spellweave, Forest Remedy, Master Plan, Burnished Legacy, Secret Thesis, Measured Breath, Alchemist's Patience and Perfect Timing left the shared core. They stay loadable by ID for old saves and future pools.

Shared cards are identified in the catalogue and count toward each class's pool. The native pool chooses beta additions from the installed game's built-in card distribution, excluding other mods' injected cards. Both packages retain all model IDs so saves can load previously acquired cards even when branch eligibility changes.

## Class identity

| Class | Unique routes | Examples |
| --- | --- | --- |
| Knight | Block into damage, blood costs, Strength multi-hit, Wound fuel and exhaust | Shield Slam, Battle Rhythm, Living Fortress; Blood Price and Forged in Pain; Sword Dance and Grand Melee; Splintered Pike, Scar Tissue and Furnace Breath; Forge Sparks and Phoenix Forge. |
| Mage | Charge spending, Skill chains, retained spells, spell exhaust and debuff magic | Kindle, Overcharge and Arcane Reservoir; Arcane Meter; Tower Library and Sun in a Bottle; Rewrite and Transmutation; Toxic Theory and Venom Rune. |
| Ranger | Attack chains, manual discard, poison, retained shots and Scry | Finishing Flurry and Arrow Cascade; Sort Quiver, Loose Fletching, Pocket Wind, Light Pack and Tailwind; Double Dose and Poisoned Arsenal; Long Vigil and Perch Discipline; Watch the Wind, Owl Watch and Forest Oracle. |

Mage-only charge effects and the class-specific Hunter Rhythm upgrade remain unique. Shared engines work without either passive. Multiple routes overlap: retaining an Attack can support Strength or attack chains; exhaust can improve cycling while fueling Block/damage; debuffs enable both direct hits and poison.

## Save compatibility and verification

All 360 previously released IDs and illustrations remain registered so existing decks and saves can load. The 173 cards outside the regular active set (164 on beta) are excluded from normal class reward/shop pools. Historical authoring class is retained in cards.json; card_pools.json and branch_cards.json define current membership. Effects that deliberately access other colors, such as Prismatic Shard, retain native behavior. Version 0.5.0 rebalances legacy cards as well as the active set.

Preflight checks exact totals/rarities, identical shared membership, disjoint unique memberships, class-compatible shared mechanics, distinct rules within each pool, shop type coverage and verified art. Native checks cover selected-class pool membership, shared card copies/upgrades, six-choice rewards, compendium and merchants. Native rewards now have 26 rares per class on regular and 27 on beta. Full-run balance and physical Deck verification of this pool revision remain pending.

The [0.5.0 manor and balance guide](manor-and-balance.md) explains weaker starters, class passives, upgraded draw/discard rules and the progression economy.
