# Heir Ancient cards

Each class has two Ancient cards, outside its ordinary Common, Uncommon and Rare reward pool. Dusty Tome uses the game's native selection, upgraded-card preview and deck acquisition. No Ancient card replaces an ordinary reward slot.

| Class | Card | Base | Upgrade |
|---|---|---|---|
| Knight | Kingbreaker | 2 energy; 14 damage. If you already have Block, gain 2 Strength. Exhaust. | 18 damage. |
| Knight | Royal Arsenal | 2 energy; 8 Block. Upgrade all Attacks currently in your hand for this combat. Exhaust. | 1 energy. |
| Mage | Arcane Conductor | 2 energy Power. Your first Skill each turn draws 1 card and grants 1 additional Arcane Charge, capped at 2. | 1 energy. |
| Mage | Elixir of Life | 2 energy; heal 6 and gain 8 Block. Exhaust. Inherited healing restrictions apply. | 12 Block. |
| Ranger | Ancient Canopy | 2 energy; 14 Block and 7 Block next turn. Retain; each retention increases this card's Block by 4 for this combat. Exhaust. | 18 initial Block; retention increases it by 6. |
| Ranger | Perfect Escape | 2 energy; draw 2 and gain 1 Buffer. Exhaust. | 1 energy. |

These rewards use six existing historical model IDs and their distinct original illustrations. All 360 historical cards remain registered, including cards already saved in an older Dusty Tome. Changing rarity does not change a model's save identity. Royal Arsenal only upgrades combat-hand cards; it never upgrades the permanent deck.

The reference is the installed game's Ancient cards, including Corruption and Relax: stronger specialist rewards with paid setup or one-use effects. Kingbreaker requires prior defense; Conductor triggers once per turn and respects the charge cap; Perfect Escape never gains another Buffer on upgrade. Ordinary reward counts remain 82 per class on regular and 85 per class on public-beta.

Verification uses `scripts/sts2-ancient-check.py` against isolated, owned-game profiles on both branches. It checks native Dusty Tome choices and acquisition, all six cards' paid base/upgraded plays, save round-trips, card-table cleanup, and Conductor's per-turn limit. Physical Steam Deck playtesting remains a separate acceptance check.
