The Knight, Mage and Ranger now use different combat loops, in addition to their separate decks and reward pools. Class passives appear as combat powers, and starter cards show their class rule in hover tooltips.

| Class | Combat loop | Starter identity |
| --- | --- | --- |
| Knight | A Skill primes the next Attack for +4 damage; the counter is consumed on attacking. | Sword Strike gives 2 base Block along with damage. Guard prepares a counterattack. |
| Mage | Each Skill adds one Arcane Charge, capped at three. The next Attack gains +2 damage per charge and consumes them. Arcane Reservoir increases damage per charge. | Spark has 5 base damage; Ward has 4 base Block. Skill/spell sequences build a burst. |
| Ranger | Every third Attack in a turn draws a card and applies 1 Vulnerable to its target. Endless Quiver increases chain draw. | Sidestep gives 4 base Block, draws one card and discards one, helping cycle toward the next attack chain. |

Starter genetics apply to BASIC cards, including class signatures, and remain visible in printed damage, Block and card text. Genetic modifiers stack, while class/reward cards retain their own authored values. Ordinary Strength, Dexterity and trait powers still apply on top.

| Trait | Starter-card adaptation |
| --- | --- |
| Gigantism | Attacks gain 3 base damage; Skills lose 1 base Block. |
| Dwarfism | Attacks lose 1 base damage; Skills gain 2 base Block. |
| Combative | Attacks gain 2 base damage; Skills lose 1 base Block. |
| Bookish | Attacks lose 1 base damage; Skills draw one extra card. |
| Vampirism | Starter Attacks heal 1 HP when played; healing modifiers still apply. |
| Only Heart | Starter Skills draw one extra card and lose 1 base Block. |
| Hollow Bones | Starter Skills draw one extra card. |
| Pacifist | Starter Attacks become Mercy Skills: 4 Block and 2 Poison instead of attack damage. Upgrading gives 7 Block and 3 Poison. Other starter effects remain; reward Attacks are still prohibited. |

These adaptations are in `sheets/starter_genes.json`; the class loops are in `sheets/class_mechanics.json`. Card copies are constructed from the active heir's immutable traits, so upgrades, reward choices and save reconstruction use the same rules. Innovator's mixed starting deck follows the active heir's class passive.

The card set contains 37 unique illustrations and cards. Every class has common, uncommon and rare Attacks and Skills, with at least two distinct Attacks and Skills overall for the merchant's duplicate prevention. Every class also has uncommon and rare Powers; the native game's common-Power request falls back to uncommon as intended. Preflight validates this coverage on every build.

The seven new cards are Shield Wall, Royal Rally, Meditate, Arcane Reservoir, Smoke Screen, Escape Plan and Endless Quiver. New artwork uses the existing chunky cartoon visual direction. Full three-act balance and physical Steam Deck testing remain separate validation work.
