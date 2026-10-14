# Co-op ready input and active host loss (#110)

This is a completed local subset of #110, not completion of the device/campaign
audit. No production gameplay defect was demonstrated by these probes. The
changes add opt-in oracle commands, a repeatable driver and recorded evidence.
The original [co-op audit](adversarial-coop.md) remains the broader baseline.

## Native matrix

Four independent Windows ENet pair runs passed: regular v0.107.1 and beta
v0.111.0, each with a Knight/Mage pair and a native Ironclad/Ironclad control.
Heir runs used Hero Complex (`megahealth`) and Super Healer. All used BaseLib
3.4.7 and Spire Legacy 1.0.30. Exact source, DLL, seeds, 359 correlated receipts
and stderr hashes are summarized in
[the evidence manifest](adversarial-coop-timing-evidence.json). Full receipts,
game logs and disposable profiles remain private; no owned game files are shipped.

The scenarios exercise these native paths:

1. Ready with the actual Embark hotkey, then attempt to select a disabled other
   character. Godot refuses focus, emits no release and preserves the selection.
2. Unready with the actual Cancel hotkey, verify the remote frozen snapshot is
   revoked, then select another character and return to the intended character.
   Native A also activates Embark after character selection; the driver observes
   and revokes that ready state before attempting another selection.
3. Embark normally, vote into real combat and manually pay for one card per owner.
   Wait for synchronized exposed state and an idle native action executor.
4. Disconnect the host's real ENet service during active combat. Both peers lose
   connection and clear the mod's lobby handler, pending snapshots and owner scopes.
   Crowns, generation and unfinished family identity remain unchanged.
5. Explicitly host/join the native saved-run lobby with the original peer IDs,
   resume at the native checkpoint and vote back into combat. Frozen inheritance
   matches, including class, traits, family identity, banner and bonuses. Floor
   progress can differ from interrupted combat because the checkpoint precedes it.
6. Verify native player serialization and owner-specific card/relic eligibility,
   abandon through the real host action and check final handler/scope cleanup.

Recovery here is explicit saved-run recovery. No automatic host migration was
observed. This is not an active-session guest reconnect test.

`coop-ui-select` injects synthetic action events through `Input.ParseInputEvent`;
it never force-emits a release, calls `_GuiInput` directly or enables a disabled
button. Character controls use native focus routing; ready/unready use native
hotkeys. This establishes the game's UI gates, not physical Steam Input mappings.
`coop-disconnect` calls the actual service's immediate disconnect, not process
termination or cable loss. Both commands are dispatched only by the existing
isolated-profile `HEIR_TEST_MODE=1` bridge.

## Diagnostics and controls

Regular stderr had zero errors in all four peer logs. Beta Heir peers each logged
one `Invalid Task ID`; the beta native-character guest logged one as well, while
its host logged none. All behavioral checks still passed. The native-character
control keeps Spire Legacy and BaseLib loaded for the test bridge and briefly
selects a Heir in the lobby before reverting to Ironclad; it is not an unmodded
control. This weakens a Heir-card-specific explanation but does not establish an
upstream cause. Stderr has no request timestamps, so exact command attribution is
unresolved. The earlier NCard double-free diagnostic did not reproduce in these
shorter sequences; it must not be described as fixed.

Excluded fixture runs remain private: regular1 used an invalid trait ID;
regular2 used Select instead of native Cancel for Unready; regular3 collided with
the driver's `peer` argument; regular4 compared mutable floor progress against a
pre-combat saved checkpoint. These are test setup failures, not gameplay findings.

Validation also passed: Release build (zero warnings/errors), typed design
preflight (32 sheets, 2063 rows, 26488 cells), 164059 core assertions and 20 harness
checks. All eight final lab processes were stopped by verified executable/PID.
The real Steam installation, branch, Workshop item and personal saves were untouched.

## Reproduce

Use independent owned-game labs with their existing provenance and fresh tags:

```powershell
./scripts/run-coop-audit.ps1 -Branch regular -Tag fresh-ready-loss -Port 25901 `
  -Pair -TimingChecks -Python <python-with-Pillow>
./scripts/run-coop-audit.ps1 -Branch beta -Tag fresh-native-control -Port 25911 `
  -Pair -TimingChecks -NativeCharacters -Python <python-with-Pillow>
```

Compile the exact committed runtime first. The wrapper copies that DLL into the
isolated labs and the driver rejects mismatched hashes. Never replay a timed-out
mutation; inspect its request receipt and logs and restart with fresh profiles.

## Remaining acceptance work

#110 remains open for:

- Physical Deck/Linux input and Deck-to-PC Steam transport, including packet delay,
  loss and remote reconnect. No remote Deck is accessible from this workspace.
- Disconnect exactly during native embark handoff, abrupt process/transport loss,
  and supported live guest rejoin. Current disconnect occurs in settled combat.
- Natural enemy-caused multiplayer deaths and end-of-combat revival with Hero
  Complex/Super Healer; merely using those traits does not validate revival.
- Full co-op campaigns with independently earned Chronicle unlocks, real reward
  and shop callbacks, class resources, banners and per-device settlement.
- A genuinely mod-free native control for the beta diagnostics and more precise
  diagnostic/request correlation before assigning or fixing their cause.

For device runs, record both game branches/builds, mod/DLL/BaseLib versions, Steam
Input layout, seed, classes/traits and both peers' logs. Verify the ready/unready
sequence above using the Deck controls, then repeat guest loss, host loss and
explicit recovery without spending crowns or retiring an heir on mere disconnect.
Run natural-death cases separately from disconnect cases and preserve the original
peer identities during recovery. Do not mark this issue complete based on the
Windows loopback matrix alone.
