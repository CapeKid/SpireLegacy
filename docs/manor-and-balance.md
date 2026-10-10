# Manor controls and progression in 0.5.0

Full trait details are available by selecting **Read full traits** on an heir offer. Mouse clicks, keyboard navigation and controller A open a wrapped detail panel. Scroll or use Up/Down for long entries; Enter/Back closes it. Hover tips remain available.

**Name your family** opens an in-game editor with a physical-keyboard field and an on-screen keyboard. Use Clear/Backspace, type or Steam+X, then Save; Cancel preserves the existing name. Empty names are rejected. **Change banner** chooses Sun, Moon, Rose or Oak; its color and distinct emblem appear on a standard beside the heir during combat.

Click upgrades to stage levels. The manor shows pending levels, the total cost and crowns remaining after Save. Prerequisites can be satisfied by an earlier staged selection. **Save purchases** opens a review; only **Confirm save** spends crowns. Cancel returns to the pending cart; Clear selections removes it. Leaving with pending selections asks whether to discard them. A failed profile write cannot spend crowns or grant levels.

**PLAYTEST: Set crowns** is a temporary pre-release editor. It accepts whole numbers from 0 to 2147483647 and commits only on Save. Setting crowns clears pending purchases. Disable the playtest_crowns system to remove access before the full release. Existing family balances and purchased upgrades are not automatically reset.

## Crown pacing

Earn 2 crowns per cleared floor, 1 crown per 25 unspent gold, and 20 extra for victory. Apply trait/treasury bonuses after that calculation, capped at +100% combined. Treasury grants +5% per level. Settlement remains once per run.

| Example without bonuses | Crowns |
| --- | ---: |
| Death after 8 floors, 80 gold | 19 |
| Death after 17 floors, 100 gold | 38 |
| Victory after 50 floors, 200 gold | 128 |

Living Quarters starts at 80 crowns, Blacksmith and Armory at 120, Treasury and Healing Garden at 100, and Library at 350. Costs rise with levels. The 128-crown example buys one entry level, with insufficient crowns left for another. Buying every level costs 3580 crowns before considering rewards earned along the way. High-earning handicap traits accelerate progress but cannot multiply rewards beyond twice the base.

## Card and class balance

The reference audit reads the owned Slay the Spire 1 library, including costs, rarity, damage, Block, magic values, exhaustion and upgraded rules. Native Strike is 6 damage (9 upgraded); native Defend is 5 Block (8 upgraded). Heirs deliberately begin below those values. All 360 registered mod cards, including legacy cards accessible through saved decks/cross-color effects, receive the balance pass; ordinary pools still contain 75 per class with 30 shared and 45 unique cards.

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

Knight begins with 60 HP, Mage 52, Ranger 56. No class starts with bonus Strength, Dexterity or healing after fights. Counterguard adds 2 damage; Mage charges add 1 each, capped at two. Ranger retains its third-Attack rhythm. Living Quarters restores 20 HP at maximum; Blacksmith, Armory, Library and Garden supply damage, defense, draw and healing that the baseline lacks. Existing manor levels remain owned, and bonuses are snapshotted at the next run start.

These are deliberate baseline deficits, not scripted losses. A skilled player can still win without the manor. Full-run win rates, pacing feel and physical Deck testing of this revision need playtesting; the crown editor supports that work. Automated tests do not establish final balance.

## 1.0.13 targeted balance

Perfect Ward remains a 2-Energy Exhaust Skill, but prevents 1 HP-loss event (2 upgraded), matching native Buffer amounts with the additional Exhaust limitation. Perfect Timing now costs 3 Energy (2 upgraded), always grants +1 Energy each subsequent turn, and retains its 3 HP play cost. Its upgrade no longer doubles recurring Energy; native Pyre was the energy-power reference. Tripwire retains 3 Block (5 upgraded), but applies only 1 Weak at either level. Shared-core membership, card IDs, and existing saves are preserved. The balance authoring pass retains these adjustments.
