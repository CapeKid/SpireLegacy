# Manor controls and progression

This page retains the original tuning rationale below. Current Slay the Spire 2
benchmarks and adversarial results are in [adversarial-balance.md](adversarial-balance.md).

Full trait details are available by selecting **Read full traits** on an heir offer. Mouse clicks, keyboard navigation and controller A open a wrapped detail panel. Scroll or use Up/Down for long entries; Enter/Back closes it. Hover tips remain available.

**Name your family** opens an in-game editor with a physical-keyboard field and an on-screen keyboard. Use Clear/Backspace, type or Steam+X, then Save; Cancel preserves the existing name. Empty names are rejected. **Change banner** chooses Sun, Moon, Rose or Oak; its color and distinct emblem appear on a standard beside the heir during combat.

Click upgrades to stage levels. The manor shows pending levels, the total cost and crowns remaining after Save. Prerequisites can be satisfied by an earlier staged selection. **Save purchases** opens a review; only **Confirm save** spends crowns. Cancel returns to the pending cart; Clear selections removes it. Leaving with pending selections asks whether to discard them. A failed profile write cannot spend crowns or grant levels.

The public Manor has no crown-balance editor. Existing family balances and purchased upgrades are preserved. Developers can prepare disposable balances through the opt-in isolated test fixtures; these are not player progression or earned-fund evidence.

## Crown pacing

After qualifying combat progress, earn 2 crowns per completed non-Ancient map point, 1 crown per 25 earned, unspent gold, and 20 extra for victory. Starting gold is excluded. A non-abandoned combat death after a manual play earns one participation point. Apply trait bonuses after that calculation, capped at +100%. Treasury multiplies the capped payout by +5% per level. Settlement remains once per run; see [earned-crowns.md](earned-crowns.md).

| Example without bonuses | Crowns |
| --- | ---: |
| Death after 8 floors, 80 gold | 19 |
| Death after 17 floors, 100 gold | 38 |
| Victory after 50 floors, 200 gold | 128 |

Living Quarters starts at 80 crowns, Blacksmith and Armory at 120, Treasury and Healing Garden at 100, and Library at 350. Costs rise with levels. The 128-crown example buys one entry level, with insufficient crowns left for another. Buying every current level costs 2620 crowns before considering rewards earned along the way. Trait bonuses cap at twice the base; maximum Treasury then multiplies that by 1.2, for an overall maximum of 2.4 before final rounding.

## Card and class balance

The original reference audit read the owned Slay the Spire 1 library. The table below describes that historical pass, rather than certifying current StS2 parity. Current regular StS2 pools have 82 rewards plus 3 basics per class; public-beta has 85 rewards plus 3 basics. There are 34 shared family cards and 360 stable registered card identities, including historical cards outside the reward pools.

| Comparison | Native reference | 0.5.0 heir behavior |
| --- | --- | --- |
| Plain starter Attack | Strike: 1 energy, 6/9 damage | 1 energy, 5/7 damage, no free Block |
| Plain starter defense | Defend: 1 energy, 5/8 Block | 1 energy, 4/6 Block, no extra draw |
| Hybrid starter | Iron Wave: 1 energy, 5 damage + 5 Block | Shield Bash: 1 energy, 5 damage + 3 Block; 7 + 4 upgraded |
| Free draw/discard | Prepared: 0 energy, 1/1 exchange; 2/2 upgraded | Cold Read: same exchange; no positive-draw upgrade |
| Paid draw/discard | Acrobatics: 1 energy, draw 3/4, discard 1 | Battle Meditation: draw 2/3, discard 1/2; Sort Quiver: draw 3/4, discard 2/3 |
| Attack plus Vulnerable | Bash: 2 energy, 8/10 damage, 2/3 Vulnerable | Challenge: 2 energy, 6/8 damage, 2/3 Vulnerable |
| Make the current hand free | Bullet Time restricts further drawing that turn | Master Plan costs 3/2, draws no extra cards and applies native No Draw |
| Exhaust draw engine | Dark Embrace: 2/1 setup cost, 1 draw per exhaust | Secret Thesis uses the same setup cost; exhaust support is otherwise weakened |
| Hand cost reduction | Native cost manipulation is limited or has setup costs | Enchanted Ink reduces one/two random eligible cards by 1 for the current turn |

Most direct damage, Block and repeated payoff amounts are reduced; upgrades no longer amplify several resource effects at once. Some engine cards retain native-like effects so build strategies remain functional. Before/after statistics and reference categories are recorded in sheets/balance_changes.json; the catalogue describes current cards.

Knight begins with 60 HP, Mage 52, Ranger 56. No class starts with bonus Strength, Dexterity or healing after fights. Counterguard adds 2 damage; Mage charges add 1 each, capped at two. Ranger retains its third-Attack rhythm. Living Quarters restores 20 HP at maximum; Blacksmith, Armory, Library and Garden supply damage, defense, draw and healing that the baseline lacks. Manor bonuses are snapshotted at run start. Version 1.0.14 retires excess Blacksmith, Armory and Garden levels with full crown refunds; unfinished runs retain their existing bonus snapshots.

These are deliberate baseline deficits, not scripted losses. A skilled player can still win without the manor. Full-run win rates, pacing feel and physical Deck testing of this revision need playtesting; isolated developer fixtures can prepare comparison profiles. Automated tests do not establish final balance.

## 1.0.13 targeted balance

Perfect Ward remains a 2-Energy Exhaust Skill, but prevents 1 HP-loss event (2 upgraded), matching native Buffer amounts with the additional Exhaust limitation. Perfect Timing now costs 3 Energy (2 upgraded), always grants +1 Energy each subsequent turn, and retains its 3 HP play cost. Its upgrade no longer doubles recurring Energy; native Pyre was the energy-power reference. Tripwire retains 3 Block (5 upgraded), but applies only 1 Weak at either level. Shared-core membership, card IDs, and existing saves are preserved. The balance authoring pass retains these adjustments.

## 1.0.14 balance audit fixes

- Guardian Angel prevents one HP-loss event at either level, costing 2 Energy (1 upgraded). Perfect Ward costs 2, prevents 1/2 events and now supplies 3/5 Block, giving the cards different upgrade choices.
- Every multi-hit card adds at most 1 damage per hit on upgrade. Knight/Mage priming and Arcane Reservoir now add damage to every hit of the next Attack, following native attack-bonus behavior (#130). Their charge limits and once-per-card consumption remain unchanged.
- Discard draw/Energy/Block cards reward at most once per copy per turn. The claimed turn is saved on the card, and nested reward triggers cannot claim it twice. The native discard POWER remains capped at the first discard per turn.
- Astral Mantle always grants 1 Block per card played; its upgrade lowers setup cost from 2 to 1.
- Blood to Mana upgrades Energy 2 to 3; Mana Fountain 3 to 4; Escape Route 1 to 2. Kindle gains 2/4 Block alongside its charge gain. Overcharge retains the normal charge cap, draws 1/2 cards, loses 3 HP, and now Exhausts.
- Time Pocket retains the hand for the promised 1/2 turns: its power decrements each owner-side turn end rather than disappearing immediately.
- Cosmetic crown bonuses and Methodical's free crown bonus are removed. Bookish offers +1 draw for -10 HP without crowns; Crippling Intellect offers +1 draw for -18 HP and +50% crowns, giving a distinct progression tradeoff.
- Treasury multiplies the trait-capped payout, so it works for Hero Complex, Fragile and Pacifist. Rounding occurs once at settlement; normal trait-only payouts remain unchanged.
- Blacksmith, Armory and Garden cap at two levels; Living Quarters and Library retain their bonuses. Vampirism heals 3 instead of 4. Full Garden with Vampirism/Super Healer heals 10 instead of 16. Retired purchased levels are refunded at their old purchase prices once, with atomic persistence and no changes to an active run snapshot.

Core checks cover every active card's effective upgrade, migration/refund persistence, payout caps and healing combinations. Opt-in native checks exercise real multi-hit damage, discard serialization, retention duration and upgraded card behavior on both game branches. Full-run win-rate tuning still requires playtesting.
