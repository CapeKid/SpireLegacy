# Full-sized class pools

Version 0.2.0 expands Knight, Mage and Ranger to **75 cards each**: three basics, 20 commons, 36 uncommons and 16 rares. The count and rarity split match the Ironclad in the owned regular Slay the Spire 1 installation. Each class has 72 cards available to rewards, shops and card transformations. The nine existing starter cards and the class-specific starting decks retain their identities and inherited gene behavior.

The expansion adds 188 original cards. Each card has a distinct complete rules combination within its class, original effect-based art and an upgrade. [The catalogue](card-catalogue.md) lists all 225 base cards. Sheets in `sheets/cards.json`, `power_effects.json`, `special_effects.json` and `pool_targets.json` are the authoring sources. `card_art.json` records the individual generation prompts and verified resource hashes.

## Knight builds

- **Block and retaliation:** Tower Guard, Bulwark and Hold the Line establish defense. Shield Slam converts current Block into damage; Battle Rhythm damages an enemy whenever Block is gained; Living Fortress preserves Block between turns.
- **Exhaust engines:** Reforge and Second Wind remove spent cards. Burnished Legacy adds Block on Exhaust, and Forge Memory draws on Exhaust. Arms Vault retrieves an exhausted card.
- **Blood and Strength:** Blood Price and Bloodied Blade trade HP for tempo. Forged in Pain turns card HP loss into Strength. Field Rations and Battlefield Recovery support the HP cost.
- **Multi-hit counters:** Iron Resolve and Tempered Steel prepare extra attack damage. Twin Cut, Sword Dance and Meteor Hammer use it across several hits, alongside the class's Counterguard mechanic.

## Mage builds

- **Arcane charge burst:** Kindle and Overcharge prepare charges. Arcane Reservoir strengthens them; Arcane Missile, Thunderchain and Spell Cascade deliver repeated hits.
- **Spell chains:** Rune Storm rewards five-card turns. Spell Threads adds damage per play, Astral Mantle provides Block per play, and the draw and Energy cards keep the chain moving.
- **Control and preparation:** Frost Ring, Frozen Spear and Crystal Shell handle enemy pressure. Spellweave prepares next-turn draw and Energy; Time Pocket retains the hand; Memory Prism retrieves a discarded spell.
- **Poison and debuffs:** Witchfire, Venom Rune, Toxic Cloud and Soulfire supply Poison. Toxic Theory converts applied debuffs into immediate damage.

## Ranger builds

- **Poison and precision:** Barbed Arrow, Razor Fletching and Black Arrow build Poison. Double Dose and Serpent Oil double it. Poisoned Arsenal adds Poison to unblocked attacks; Ambush and Assassinate reward marked targets.
- **Hunter Rhythm:** Cheap attacks such as Quick Nock and Hidden Knife build the three-attack rhythm. Finishing Flurry scales with attacks played, while Endless Quiver improves rhythm draw.
- **Discard and recovery:** Trail Sense, Reposition and Ranger Kit cycle cards. Escape Route gains Energy through discarding, Reclaim Arrows retrieves discarded cards, and Pocket Arsenal discounts the hand.
- **Planning and defense:** Watch the Wind and Scouting use Scry to plan upcoming draws. Camouflage and Deep Cover can be retained; Measured Breath keeps chosen cards; Ghost Steps adds Block per card played.

## Native integration

The larger pools restore ordinary Slay the Spire reward generation. The 0.1.9 reward-reroll replacement is removed. Question Card can offer four distinct rares from each class's sixteen-card rare pool. Shops, Prismatic Shard and other host reward modifiers use their native paths. Preflight rejects an undersized class pool, a missing shop tier, identical card mechanics within a class, duplicated illustrations or missing art.

Existing card IDs and family/save locations remain stable, so ongoing runs and manor progression can continue. New cards appear through normal rewards and shops. Balance across full runs will benefit from playtesting; the published build's regression checks cover pools, native rewards, shops, card creation/upgrades and representative combat effects.
