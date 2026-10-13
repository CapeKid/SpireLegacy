# Adversarial balance and progression audit (#60)

The audit starts from merged main `1780856`. It fixes an unbounded historical hand-exchange engine, corrects native Sly ordering for all hand exchanges, and gives Battle Meditation a useful paid draw budget. It also repairs obsolete audit values and misleading recovery descriptions. Card identities, rarity, reward membership, manor prices, payouts and release version remain unchanged.

## Findings and decisions

| Issue | Reproduction | Result |
| --- | --- | --- |
| [#141](https://github.com/CapeKid/SpireLegacy/issues/141) | Two upgraded Trail Resets plus native Reflex sustain twelve zero-Energy plays; a two-Block discard power gains 26 Block, then keeps gaining two per play. | Trail Reset Exhausts at both levels, preserving its 1/0 cost and hand exchange. It is a historical registered card outside current normal reward pools. |
| [#142](https://github.com/CapeKid/SpireLegacy/issues/142) | Separate Discard and Draw calls autoplay Reflex before the exchange redraw. Even after adding Exhaust, two resets gain six discard Block and leave a card, versus four Block/empty hand for native Calculated Gamble. | All three `refill_hand` cards use native `DiscardAndDraw`, which defers Sly autoplay until redraw finishes. Trail Reset, Quiver Repack and Empty Quiver now match native fuel destinations. |
| [#143](https://github.com/CapeKid/SpireLegacy/issues/143) | 102 existing `balance_changes.after` fields disagree with shipped cards, including Flame, Oath and reworked finishers. | Synchronize current fields, preserve historical `before` values, and check every existing field across all 360 identities during regeneration. |
| [#144](https://github.com/CapeKid/SpireLegacy/issues/144) | Recovery text prohibits “other recovery cards,” but the predicate accepts native Hologram. | Name Spire Legacy recovery cards/effects explicitly. Keep native eligibility and existing reciprocal-mod restrictions. |
| [#145](https://github.com/CapeKid/SpireLegacy/issues/145) | Paid Meditation draws 2/3 and discards one, leaving 1/2 cards; common Study leaves 2/3 at the same cost. Native Uncommon Acrobatics leaves 2/3 through deeper 3/4-card digging. | Meditation draws 3/4, discards one, and costs one at both levels. It matches Study's hand advantage while digging deeper and supporting discard engines. Its old free upgrade stays retired. |

Authoring overrides preserve both card adjustments. The all-class base/upgraded draw comparisons measure actual Energy payment, fuel drawn/discarded, hand size and Discard destination. The two reset copies now stop after two plays, Exhaust, gain four discard Block and leave no hand. Their serialized keywords survive reconstruction. Other hand exchanges keep their paid costs, current/imminent Block and Discard destinations.

## Native comparisons and intentional engines

The native oracle reads 19 owned StS2 models at both upgrade levels on regular v0.107.1 and public-beta v0.111.0. These are model budgets, not a substitute for reading conditional effects. Actual paid probes cover the draw/exchange/loop comparisons; prior card, power and retention audits cover the corresponding hook effects.

Numbers below mean base/upgraded, before traits, manor and combat powers.

| Budget | Owned StS2 reference | Heir choice |
| --- | --- | --- |
| Basic Attack/defense | Strike: 1 Energy, 6/9 damage; Defend: 1, 5/8 Block | Starters: 1, 5/7 damage or 4/6 Block. Full Smith/Armory supplies +2; class priming remains conditional. |
| Free draw/discard | Prepared: 0 Energy, 1/1 then 2/2 | Shared Cold Read matches these amounts. Every reusable literal zero-cost draw form has matching discard; conditional engine draw is audited separately. |
| Paid draw/discard | Uncommon Acrobatics: 1 Energy, 3/4 draw, one discard | Meditation now matches that budget. Common Sort Quiver draws 3/4 but discards 2/3. |
| Finite free resources | Adrenaline: 0 Energy, two draw, 1/2 Energy, Exhaust | Second Quiver retains that finite budget. Mana Fountain grants 2/3 Energy and Exhausts; paid Spellweave delays its 1 Energy/two cards. |
| Weak plus defense | Leg Sweep: 2 Energy, 11/14 Block and 2/3 Weak | Common Tripwire: 1 Energy, 3/5 Block and one Weak at either level. It trades raw defense/duration for affordability. |
| Delayed defense | Common Dodge and Roll: 1 Energy, 4/6 Block now and next turn | Uncommon Reinforce: 1, 5/7 now and 5/6 next turn. Its base is modestly stronger at higher rarity; the upgrade trades immediate versus delayed protection. |
| Prevent HP loss | Native rare Buffer: 2 Energy, 1/2 events | Rare Perfect Ward: 2, 1/2 events plus 3/5 Block, but Exhausts. It is finite defense rather than an unlimited power. |
| Retention | Regular Well-Laid Plans: Uncommon, 1 Energy, choose 1/2. Beta changes it to a rare 2/1 whole-hand Power. | Measured Breath remains a bounded 1/2 choice; rare Bookmark chooses 2/3 at cost one. Prior native flush checks verify fresh choices. |
| Recurring Energy | Rare Pyre: 2 Energy, +1/+2 recurring Energy | Perfect Timing: 3/2 setup, +1 recurring Energy, three HP cost. Its upgrade never doubles the resource. |

Multi-hit cards keep at most +1 printed damage per hit on upgrade. Smith and attack-scoped bonuses apply per hit, so a single-hit damage-per-Energy comparison cannot justify blanket multi-hit buffs. Likewise, Poison requires decay/setup measurements, retained growth requires holding and energy costs, and delayed protection requires reaching the next turn. The existing finisher, delayed-resource and conditional-card reports preserve those separate comparisons.

Some repeatable native combos are intentional. Two Flash of Steel cards sustain twelve paid zero-Energy plays: Knight/Mage deal 60/96 damage, while Ranger deals 78/132 with native Vulnerable from its third-Attack rhythm. After modeled legal combat-cost reductions, two upgraded Holograms gain 60 Block over twelve plays; Memory Prism/Hologram gains 30. Native cards keep their own rules. Discounted **all-mod** discard and Exhaust recovery pairs still refuse their recovery partner for every class on both branches. This distinguishes expected cross-color/setup engines from the prohibited reciprocal-mod cycle; it does not prove every possible relic/card-copy loop impossible.

The literal sweep covers 720 forms and identifies 66 resource/recovery/engine forms for focused review. [Design measurements](adversarial-balance-design.json) record sheet hashes, active membership and effective free-draw budgets. Historical identity count is not a per-class reward count: regular has 82 rewards plus three basics, beta 85 plus three, with 34 shared family cards.

## Progression pressure

Each branch has 36 controlled fights: four archetypes per class, each at no manor, full manor, and full manor with a difficult trait pair plus injected Family Seal. The four-card additions are asserted to belong to that class's current regular pool. Builds are Knight armor/exhaust/blood/Strength; Mage sequencing/exhaust/retention/delayed resources; Ranger discard/Poison/retention/Attack chains. The risk pairs are Vampirism/Super Healer, Crippling Intellect/Hero Complex, and Pacifist/Vegan respectively.

The fixture forces native Nibbits Normal with seed `SPIRELEGACY-AUDIT-60`, ordinary HP/Energy and A0, then runs a bounded defense-first pilot. It adds four cards directly, and risk cases add the relic directly; these are not natural acquisition or difficulty measurements. No case hits the 30-play budget. Twenty-turn survivors would be caps, not losses; the recorded twentieth-turn cases end in actual deaths. HP loss is net loss, including post-combat Garden/trait healing.

| Branch | Fresh manor victories / 12 | Full manor / 12 | Full manor with risks / 12 |
| --- | ---: | ---: | ---: |
| Regular | 4 | 12 | 7 |
| Public-beta | 5 | 11 | 9 |

These opening fights demonstrate useful manor pressure under this policy. They do not establish campaign balance, human win rates, optimal archetype play or that a twelve-fight full-manor sweep means the cards are overpowered. Branch encounters/RNG differ. Difficult traits change more than one variable, and the Family Seal is a deliberate additional intervention, so risk differences cannot be attributed to one trait alone.

Fourteen separate natural climbs use fresh family/save profiles, seed `SPIRELEGACY-AUDIT60-CAMPAIGN-1`, A0, no traits, tactical combat and deck-aware reward choice. They retain normal starters, enemies, rests, rewards and gold. No HP, Energy, cards or debug rooms are injected. Actual Ironclad has its normal starter relic and all three card-unlock epochs. Every climb terminates in natural death, with no cap or unsupported decision recorded.

| Class | Regular fresh/full terminal floor; crowns | Beta fresh/full terminal floor; crowns |
| --- | --- | --- |
| Knight | 6 / 28; 12 / 81 | 8 / 17; 17 / 50 |
| Mage | 6 / 15; 12 / 42 | 8 / 17; 17 / 50 |
| Ranger | 6 / 17; 12 / 48 | 6 / 7; 12 / 16 |
| Native Ironclad | 12; no family crowns | 17; no family crowns |

These pilot samples are a useful contrast to the forced opening-fight wins: full manor does not guarantee a completed climb. The pilot uses the first legal path, limited potion logic and no purposeful shop/upgrade solver; low survival also reflects those decisions. The cohort does not isolate a particular card as responsible. The Battle Meditation adjustment follows these initial measurements, and their original per-trial hashes/budgets remain historical evidence rather than campaigns with the final draw change.

[Campaign trials](adversarial-balance-campaigns.csv) and [314 card observations](adversarial-balance-campaign-cards.csv) retain actual offers, picks, paid plays and hand entries. They are deterministic-policy observations, not human pick rates.

## Crown target

The existing earned-progress policy is unchanged: after qualifying combat progress, two crowns per completed non-Ancient point, earned/unspent gold at 25:1, and twenty for victory. Abandonment gets no death-participation point; starting capital is excluded. Trait bonuses cap at +100%, then full Treasury multiplies by 1.2. Settlement rounds once and remains atomic/exactly once.

Current manor levels total **2620 crowns**: Living Quarters 750, Smith 320, Armory 320, Treasury 640, Library 350, Garden 240. The old 3580-total wording predated retired levels and is corrected. An eight-point death with 80 qualified gold pays 19; a 50-point victory with 200 qualified gold pays 128 before bonuses. Maximum multipliers make that specific victory example 307, not a universal payout cap. One participation point with no earned gold pays at most four in the tested trait/Treasury grid.

The playtest target is a weaker but viable fresh heir, substantial survival benefit from manor investment, and roughly **5–7 representative early returns** for the first 80-crown upgrade without handicap bonuses. The natural fresh samples pay 12–17, consistent with that target under this limited pilot. A 128-crown victory buys one entry level, rather than half the sixteen-level manor. Trait handicaps deliberately accelerate earnings. Longer-run purchasing feedback, Treasury return on investment and experienced-player pacing remain [#146](https://github.com/CapeKid/SpireLegacy/issues/146); these targets are not measured human guarantees.

## Verification and limits

Release builds pass with zero warnings/errors. Core checks pass **164,059 assertions**, including 14,560 compatible-pair/Treasury payout cases, 40,000 crown-conservation cases, atomic/fault/replay boundaries, paid card budgets and keyword/text regressions. Preflight checks 32 sheets, 2,062 rows and 26,477 cells. Disposable authoring regeneration preserves targeted stats, IDs, pools and every current audit field; the 20 harness checks pass.

[Validated native evidence](adversarial-balance-evidence.json) contains 260 measured cases across 24 stages plus fourteen natural climbs, totaling 7,485 correlated replies. It records every loaded DLL, game/BaseLib/mod identity, declared source, inputs, outcomes and log hashes. Stages use distinct diagnostic builds; the early Knight/Mage pressure stage predates the Sly correction, and those recipes never use a hand-exchange card. The corrected Ranger cohort has its own stage. Later launchers derive source identity from the DLL informational version instead of whatever checkout happens to be current.

Four final ENet pairs cover Knight/Mage and Knight/Ranger on each branch. Base/upgraded reset cards and Meditation resolve through paid native actions, leave other owners' Energy/hand/Block unchanged and retain enabled checksums. Meditation uses the actual local hand-choice completion and native remote-choice synchronization. Choice activation is synthetic native UI operation, not a physical controller test. Its effect fixture funds three Energy before each case; natural campaigns receive none.

Failed fixture attempts remain private diagnostic evidence: ordinary fuel did not sustain the initial loop assumption; a test used `vegan` instead of canonical `nomeat`; two probes copied the older DLL while compilation was running; and a co-op driver selected an obsolete visual holder after `skipVisuals` reset. Verified co-op selection requires current Hand membership and a newly opened choice task. None of those failed rows count as a pass, and no timed-out mutation was replayed.

Logs retain OpenGL compatibility particle warnings, native missing-save-backup deletion messages and some WorkerThreadPool `Invalid Task ID` diagnostics. Passing commands do not imply empty logs. Physical Steam Deck/Linux, native Steam transport and complete co-op/device campaigns remain [#110](https://github.com/CapeKid/SpireLegacy/issues/110). Multi-seed, higher-Ascension, experienced/winning campaigns and long-term purchase trajectories remain #146. All copied clients use disposable profiles and verified-PID cleanup; no release publication is part of this audit.

Run each driver with fresh tags; all relevant commands are guarded by the isolated test bridge:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_draw-budget -Class knight -BalanceAuditChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_matrix -Class knight -BalanceAuditChecks
./scripts/run-coop-audit.ps1 -Branch regular -Tag UNIQUE_ranger -Pair -BalanceChecks -Port 25890
python scripts/sts2-balance-design-audit.py
```

The documented evidence exporter selects the exact completed tags from this audit. It validates every reply, deduplicates request IDs, excludes the incomplete driver's duplicate Ranger rows and publishes compact measurements without game source, binaries, art, saves or credentials. MODLOG and generic generated coverage remain ignored.
