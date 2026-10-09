# 0.3.0 expanded-build verification

The final release registers 360 cards: 120 per class, with 117 reward/shop cards in each separate class library. Tests use the owned regular Windows Slay the Spire 1 build 10180494 and isolated game/family data.

| Release check | Result |
| --- | --- |
| Constructors, copies, targeting and queued effects | 360 cards; 3,843 assertions |
| Native card upgrades and trait-power construction | 360 upgrades; 63 traits |
| Native reward generation | 600 calls; 4,680 assertions |
| Native shops/class behavior | 2,019 assertions; three merchants |
| Live engine scenarios | 102 assertions across 11 scenarios |

The live scenarios play cards through the native card queue and wait for actions to finish. They verify per-card discard rewards, the manual-discard patch, once-per-turn Tailwind and its turn reset, exclusion of end-turn discards, exhaust-all-non-Attacks, exhaust-triggered area damage, exhausted-pile scaling/cap, native retained growth/Establishment, third-Skill Block and delayed Energy, third-Attack Vigor for the following Attack, Scry-triggered Block, complete hand refill and native Wound creation. Power factories also require a valid renderable flash icon.

The exact final production JAR passed 3,843 combat/card assertions, including native library registration and membership of every one of the 117 reward cards in each class. All 360 upgrades, 600 native reward calls / 4,680 assertions, 2,019 shop/class checks / three merchants, and 102 live engine assertions passed. All 135 new illustrations were generated individually with built-in imagegen and reviewed in labeled class sheets; all 360 resource images passed dimensions, unique-hash and reference checks. Windows, Linux and Workshop archives contain the same JAR and exclude game files and saves. Full three-act balance playtesting and a physical Deck playthrough of the new card expansion remain outstanding. The existing Deck content-reader/bootstrap and save paths are unchanged.

Release JAR SHA-256: `D5E6D9306F2C5044B7B250699F6B6A8B4CC908E990612254CADFACF2053BEFAD`. Native renderer samples: [Mage](../media/mage-cards-030.png), [Ranger](../media/ranger-cards-030.png). The native Ranger Question Card boss reward displays four distinct rares, including Forest Oracle and Venom Eclipse: [framebuffer](../media/boss-reward-030.png). The official uploader successfully updated existing private item 3816067184. Steam downloaded the same verified JAR. A subscribed-only launch with an empty local mods directory and fresh isolated data loaded 0.3.0, registered 360 cards, reached combat and produced four distinct Ranger boss rares with Question Card (Hunt Without End, Perfect Escape, Ghost Luggage and Snipe).

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
