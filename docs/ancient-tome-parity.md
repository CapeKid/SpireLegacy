# Ancient Canopy upgrade investigation (#93)

Ancient Canopy being offered upgraded by **Dusty Tome** is the game's intended reward behavior. It is not a consequence of the Heir's starting deck. Dusty Tome chooses a non-transcendence Ancient card from the owner's unlocked character pool, shows an upgraded preview, then upgrades the granted card in its native acquisition callback. This also happens for Ironclad.

The owned regular and public-beta builds are checked directly. Each Heir class and an Ironclad control is tested with its intact starting deck and with every Basic removed from the disposable deck. Native offer choices, upgraded preview and acquisition remain valid in both cases. The normal Heir decks already contain Strike, Guard and a class signature, matching Ironclad's three starting identities (Strike, Defend and Bash). Removing them still does not affect the upgrade.

Ancient Canopy remains a Ranger Ancient reward; Dusty Tome grants Ancient Canopy+ when that is its selected reward. Its unupgraded form still exists and works normally wherever explicitly created. Both forms are tested through actual paid play: 14/18 immediate Block, 7 Block next turn, and Exhaust. These tests do not change the production reward or starting deck.

The card pool already has a shared family core alongside class specialties. A new shared starter would be a separate deck-design change and would not fix this reported behavior. This PR documents native parity and adds regressions instead of changing an intentional upgraded Ancient reward.

## Evidence

Baseline merged main: `42a7471`. Native parity fixtures use regular v0.107.1/public-beta v0.111.0, BaseLib 3.4.7 and Spire Legacy 1.0.29. `ancient-tome-parity-evidence.json` records the exact test source/DLL identity and successful counts. Complete correlated private receipts include seed `SPIRELEGACY-TOME-93` and both deck modes.

Per branch: 600 native offer/preview checks, ten actual upgraded acquisitions, eight real save/resume checks, serialized relic preview consistency, and paid base/upgraded Canopy effects and card-table cleanup. The two explicit Canopy acquisitions select a legitimate native option using the saved `AncientCard` property; this is a deterministic fixture, not a claim that ten random rolls all select Canopy. No runtime hooks, card definitions, starters or rewards are changed.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag tome93_UNIQUE_REGULAR -Class knight -TraitAuditChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag tome93_UNIQUE_BETA -Class knight -TraitAuditChecks
```

These are controlled native solo checks. Physical Deck/Linux input, full campaigns and multiplayer reward voting remain separate tests. No publication, merge or personal-save modification is performed.

The first control fixture waited for combat after resuming a native Ironclad debug fight. That fight does not replace the persisted map location, so the game correctly restored its original event room. The corrected fixture checks the saved relic and deck in that restored room; it does not claim combat resumption for the native control. Failed receipts are excluded from the final counts.
