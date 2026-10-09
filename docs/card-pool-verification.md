# 0.2.0 card-pool verification

The release JAR was built from strict preflight and tested in the owned Slay the Spire 1 regular Windows build 10180494 (v2.3.4 / 12-18-2022), with isolated game and family saves.

| Check | Result |
| --- | --- |
| Owned Ironclad reference | 75 cards: 3 basic, 20 common, 36 uncommon, 16 rare |
| Each heir class | Same 75-card count and rarity split; 72 reward/shop cards |
| Card constructors, copies, upgrades, targets and native action queues | All 225 cards; 1,945 assertions |
| Native reward entry point | 600 calls; 4,680 assertions |
| Native merchants, class loops and starter genetics | 2,019 assertions; three merchants |
| Card upgrades and trait loading | 225 upgrades; 63 trait-power constructors |
| Original card images | 225 distinct verified 250 x 190 resources; 188 new illustrations |
| Windows, Linux and Workshop archives | All contain the same release JAR and exclude game files and saves |

Reward cases cover every class, ordinary and boss rooms, no count relic, Question Card, Busted Crown, both count relics, and Question Card plus Prismatic Shard, over 20 seeds. Boss choices retain rare rarity and the relic-adjusted count. The removed RewardPools replacement is absent from the clean release JAR. The actual Ranger Question Card check rendered four distinct rares: Perfect Escape, Master Plan, Perfect Timing and Storm of Arrows. [Boss reward screenshot](../media/card-pool-boss-reward.png).

Combat checks cover Block-based and conditional damage, stable HP costs on upgrade, recurring draw-power costs, retrieval upgrades, combo previews versus the native card queue, and Hunter Rhythm marking both enemies for an area Attack, including a native Cleave supplied by Prismatic Shard. Live combat also exercised Poison doubling, area attacks, Block per card played, Mage charge preparation and the native Scry selection interface. [Scry screenshot](../media/card-pool-scry.png).

All artwork was generated individually using built-in imagegen with the existing class artwork as style references. Full originals are saved under art/originals; prompts, exported paths and SHA-256 hashes are in sheets/card_art.json. Labeled reviews: [Knight](../media/card-art-knight.png), [Mage](../media/card-art-mage.png), [Ranger](../media/card-art-ranger.png).

These checks verify implementation and integration. Full three-act balance playtesting and a fresh physical Deck playthrough of 0.2.0 remain outstanding. The creator already verified the earlier startup/controls update on Deck with both Steam Linux Runtime and Proton.

## Workshop delivery

The official uploader updated private item 3816067184. Steam downloaded the same release JAR (SHA-256 D446392B864A2F0CCD5E818E8E66C3344037B1A96C1A31B2BE928F8FA8084C67). A subscribed-only Windows launch with an empty local mods folder and fresh isolated data prepared all 66 owned sprites, registered all 225 cards, reached combat and generated four distinct Ranger boss rares with Question Card.
