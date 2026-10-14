# Enemy-caused co-op death and revival (#110)

This extends the [ready-input/host-loss audit](adversarial-coop-timing.md) with
native enemy-turn death and automatic victory revival. It does not complete #110's
physical-device, transport, exact embark-handoff or full-campaign requirements.

## Method

Fresh independent Windows lab pairs use Knight as the defending teammate and
either a Hero Complex Mage or Super Healer Ranger as the undefended teammate.
The Knight has Hero Complex and a fixture manor: vitality 5, smith 2, armory 2,
library 1, no garden. The other owner has no manor upgrades. This is an explicit
survival fixture, not an earned-upgrade campaign or balance measurement.

After normal native embark and a real map vote, both peers enter the native
`SlimesWeak` encounter through the synchronized debug-room loading path. HP,
monster stats, cards, energy and deck order are never assigned by the survival
probe. The defending player only plays normally drawn Knight Guard and playable
Slimed cards, paying their normal energy. The other player ends ordinary turns.
All damage and deaths come from native monster moves.

Once the other player is observed at zero HP, the survivor switches to normally
drawn Attacks, pays for them and wins. The driver observes the normal combat-end
revival, requires exactly 1 HP, checks owner serialization and cleared scopes,
then abandons through the real host action and verifies durable, once-only family
settlement. It never invokes `ReviveBeforeCombatEnd` or a healing command directly.
Knight, Mage and Ranger participate, but the death subjects are Mage and Ranger.

The two fixed seeds are `REVIVAL1` and `REVIVAL2`. The driver checks the actual
native seed in the receipt; intended input alone is not accepted as proof.
Every command is correlated to its request ID and exact DLL hash. Native
version/mod compatibility and checksum validation remain enabled. Room-entry
mutations are dispatched to both peers concurrently because native loading has a
peer barrier. Timed-out mutations must never be replayed.

## Fixture defects corrected

The earlier `coop-turn` bridge called `CombatManager.SetReadyToEndTurn` locally.
Each peer only marked its own player, so the first enemy turn could not begin.
Both branches reproduced that fixture failure. The bridge now requests the real
`EndPlayerTurnAction` through `ActionQueueSynchronizer`, matching the native End
Turn button. This affects opt-in tests, not ordinary player controls.

Native character-select initialization also clears `DebugSeedOverride`. Setting
the fixture seed before initialization silently produced random native seeds.
It is now set after initialization/selection and explicitly checked by the
driver. Earlier exploratory runs retain their actual generated seeds in private
receipts; they are not mislabeled as fixed-seed evidence.

The first harness check also caught the driver's evidence wrapper falling outside
its static direct-call safeguard. The final `send` function explicitly calls the
correlated transport and logs that exact reply before validation. The existing
harness safeguard remains unchanged.

## Evidence and limits

All four fixed-seed battles passed on regular v0.107.1 and beta v0.111.0 with
Spire Legacy 1.0.30 and BaseLib 3.4.7: 2392 correlated replies and 174 paid plays.

| Branch | Death subject | Enemy-caused death turn | Victory turn | Revival HP |
| --- | --- | ---: | ---: | ---: |
| Regular | Hero Complex Mage | 10 | 17 | 1 |
| Regular | Super Healer Ranger | 5 | 11 | 1 |
| Beta | Hero Complex Mage | 11 | 20 | 1 |
| Beta | Super Healer Ranger | 6 | 15 | 1 |

The Release build had zero warnings/errors; typed preflight validated 32 sheets,
2063 rows and 26488 cells; all 20 harness checks passed. All final lab processes
were stopped by verified executable/PID. The real Steam installation and saves
were untouched; nothing was published.

Regular stderr contained no errors. Beta logged two `Invalid Task ID` errors on
the host and one on the guest; cause and request attribution remain unresolved.
Both branches also emitted compatibility-renderer particle warnings during the
native revival VFX. The NCard double-free diagnostic did not reproduce here;
that does not prove it fixed. Exploratory random-seed successes remain separate
from the final fixed-seed evidence.

The [evidence manifest](adversarial-coop-survival-evidence.json) records the final
runtime source, DLL, game/mod/BaseLib versions, receipt counts, observed deaths,
victory HP, native turns, paid actions and stderr hashes. Full receipts, replay
logs and profiles remain private. No owned game files or decompiled code ship.

Successful required revival does not establish ordinary healing balance, human
win rates, Linux/Deck controller support, full campaigns or remote reconnect.
Debug-selected combat and deliberately idle teammates are stated interventions.
Unexpected diagnostics remain findings to investigate, not proof of a fix or
proof of upstream responsibility.

The user confirmed a Deck and PC are available, with one Steam account. Physical
Deck/LAN tests can be prepared separately, but that setup does not provide two
distinct Steam peer identities for a Steam-transport co-op test. It must not be
reported as Steam transport coverage. Exact embark loss and complete co-op
campaigns also remain open in #110.

## Reproduce

Build the committed runtime, then use fresh profile/evidence tags and independent
owned-game labs:

```powershell
./scripts/run-coop-audit.ps1 -Branch regular -Tag fresh-natural-regular `
  -Port 26021 -Pair -SurvivalChecks -Python <python-with-Pillow>
./scripts/run-coop-audit.ps1 -Branch beta -Tag fresh-natural-beta `
  -Port 26031 -Pair -SurvivalChecks -Python <python-with-Pillow>
```

Caps are 80 native turns, 1000 decisions and 15 minutes per battle. The wrapper
requires fresh profiles and stops only verified lab executable/PID pairs in its
cleanup path. It does not alter the real Steam installation, branch or saves,
and does not publish anything.
