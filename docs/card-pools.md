# Expanded class libraries

Version 0.3.0 contains **120 cards per class, 360 total**. Each Knight, Mage or Ranger draws exclusively from that class's own **117 reward/shop cards**: 35 common, 56 uncommon and 26 rare, alongside three basics. This is 45 additional cards per class over 0.2.0, a 60% larger individual library. The other classes' cards do not contribute to an heir's ordinary draft choices.

All 225 existing cards remain available with their IDs, starter genetics and save behavior preserved. The 135 additions have individual illustrations, upgrades and authored build roles. [The catalogue](card-catalogue.md) lists all 360 cards. `sheets/card_builds.json` identifies every addition's intended build and role; these are design labels rather than a guarantee of a winning deck.

## Knight choices

| Strategy | Added enablers/support | Added payoffs | Existing connections |
| --- | --- | --- | --- |
| Armor | Buckler Jab, Layered Mail, Cathedral Guard, Guard Rotation | Rampart Rush, Castle Heart, Iron Avalanche | Shield Slam, Battle Rhythm, Living Fortress |
| Exhaust | Shield Drill, Hot Rivets, Burning Orders, Cast Off, Ancestral Salvage | Forge Sparks, Ashen Advance, Salvage Guard, Phoenix Forge | Burnished Legacy, Forge Memory, Arms Vault |
| Blood costs | Blood Oath, Grit Teeth, Blood Temper, Painful Lesson, Warrior Feast | Crimson Banner, King's Ransom, Bloodfire Mantle | Forged in Pain, Blood Price, Battlefield Recovery |
| Multi-hit Strength | Measured Cut, Weapons Master, Focused Defense, Warlord's Command | Redoubled Blow, Siege Volley, Serrated Barrage, Grand Melee | Twin Cut, Sword Dance, Ancestral Fury |
| Status fuel | Splintered Pike, Heavy Harness, Jagged Charge | Scar Tissue, Furnace Breath, Iron Furnace | Exhausting Wounds also feeds exhaust powers |
| Delayed defense/tempo | Rationed Assault, Stand Ready, Flanking March, Armor Cache | Ready Arsenal, Siege Tomorrow | Reinforce, Brave Advance |

Patient Riposte adds a retained Attack that grows over successive turns. Status-fuel cards offer larger immediate effects at the cost of Wounds; those become fuel for Evolve, Fire Breathing or exhaust engines.

## Mage choices

| Strategy | Added enablers/support | Added payoffs | Existing connections |
| --- | --- | --- | --- |
| Skill sequencing | Warding Verse, Page Turner, Triple Formula, Bookmark Ward, Rune Familiar | Woven Incantation, Arcane Meter, Archmage Thesis, Mirror Manuscript | Arcane Reservoir, Rune Storm, Spell Threads |
| Delayed turns | Slow Incantation, Ice Cocoon, Tomorrow's Flame, Winter Reserve | Hourglass Array, Delayed Nova, Comet Calendar, Stasis Dome | Spellweave, Time Pocket, Mana Shield |
| Exhaust spells | Ash Reading, Paper Shield, Fading Star, Spell Pyre, Phoenix Notes, Returned Prophecy | Ashen Chorus, Ashbolt, Book of Embers | Secret Thesis, Rekindle, Rune Scrub |
| Retained spells | Patient Study, Tower Library, Wax Seal, Eternal Bookmark | Stored Ember, Crystal Seed, Moonstone Lance, Geode Ward, Sun in a Bottle | Rune Detonation, Memory Prism |
| Poison/debuffs | Corrosive Ink, Chilling Words, Hex Rain, Viper Script | Alchemist's Patience, Plague Constellation | Toxic Theory, Venom Rune, Toxic Cloud |
| Charge-powered hits | Scripted Spark, Rune Scatter, Conduit Lance, Grand Incantation | Charge multipliers apply across repeated hits | Kindle, Overcharge, Arcane Reservoir |

Third-Skill engines grant Block now or Energy next turn. Retained spells reward waiting, and Establishment reduces their combat cost. These encourage different timing from immediately spending charges on an Attack, while also allowing mixed decks.

## Ranger choices

| Strategy | Added enablers/support | Added payoffs | Existing connections |
| --- | --- | --- | --- |
| Discard | Sort Quiver, Quick Exchange, Trail Reset, Briar Retreat, Quiver Repack, Scatter Supplies | Loose Fletching, Pocket Wind, Shed Cloak, Light Pack, Tailwind, Spring Nock, Spare Provisions, Empty Quiver, Ghost Luggage, Falcon Courier | Trail Sense, Reposition, Ranger Kit |
| Poison | Hissing Arrow, Toxic Snare, Silent Mixture, Mire Volley, Spreading Blight | Serpent Trail, Venom Reserve, Venom Eclipse, King Cobra | Poisoned Arsenal, Double Dose, marked-target Attacks |
| Retained shots/defense | Perch Discipline, Conserve Arrows | Held Shot, Hidden Buckler, Eagle Focus, Camouflage Nest, Long Vigil, Ancient Canopy | Measured Breath, Camouflage, Deep Cover |
| Attack chains | Pine Needle, Running Shot, Thread the Needle | Pursuit Volley, Tracking Rhythm, Arrow Cascade, Hunt Without End | Hunter Rhythm, Finishing Flurry, Endless Quiver |
| Scry planning | Trail Sign, Marked Path, Owl Watch, Chosen Trail | Forest Oracle grants Block each time Scry triggers | Watch the Wind, Scouting |

Discard payoffs trigger when a card is actually discarded during your turn. Ordinary end-turn discarding grants nothing. Tailwind pays once per turn; Light Pack pays per card. Tracking Rhythm's Vigor applies after the third Attack resolves, rewarding a fourth Attack. Forest Oracle connects deck filtering to defense.

## Integration and limits

Rewards, shops, transformations, upgrades and relic modifiers use native Slay the Spire behavior. Question Card chooses from 26 distinct class rares. The old small-pool reward replacement remains removed. Every addition belongs to one specific class.

Preflight checks exact class/rarity counts, shop coverage, complete references, distinct within-class rules, added build roles, and unique verified illustrations. Native scenarios test discard, exhaust, retention/cost reduction, Skill and Attack sequencing, Scry and Wound interactions. Bigger pools make individual cards harder to find; common/uncommon support and existing-card connections give strategies multiple routes. Full-run balance and win-rate comparisons still require playtesting.
