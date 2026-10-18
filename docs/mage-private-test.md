# Private Mage test for issue #185

The local Git branch is `codex/185-mage-arcane-flow`. It has not been pushed. This repository is public, so a pushed branch or GitHub release would also be public. Version 1.0.37 is the current unpublished test build, including the Mage pool changes; the public Workshop item stays on 1.0.35.

## Steam Deck installation

1. Close StS2. Transfer `SpireLegacy-StS2-1.0.37.zip` from the local test output to the Deck, using your usual file transfer method.
2. In Desktop Mode, open Steam → StS2 → Properties → Installed Files → Browse. Extract the ZIP into that game directory. The result must be `mods/SpireLegacy/SpireLegacy.json`, `SpireLegacy.dll`, and `heir/` directly underneath the game directory. If a local `mods/SpireLegacy` already exists, move it outside the game directory first so you can restore it.
3. Keep BaseLib subscribed and enabled. You can keep the public Spire Legacy subscription: the native mod loader prefers the local mod with the same ID and disables the duplicate Workshop copy. Do not copy test files into `steamapps/workshop/content`.
4. Start normally in Gaming Mode. Confirm the enabled mod is **Spire Legacy 1.0.37**. Pick a Mage heir in Family Manor before starting a new climb. The class power should be **Arcane Flow** and describe card draw.
5. To return to public 1.0.35, close the game and move the entire local `mods/SpireLegacy` folder outside the game directory. Restore any previous local folder if you had one. Steam's subscribed copy becomes active again. Confirm the loaded version after restarting.

Your regular/public-beta **game** setting can stay as it is; it is separate from this local mod test. Local installation does not upload anything. Native checks cover Windows regular v0.107.1 and public-beta v0.111.0; this exact build still needs physical Steam Deck confirmation. Family progress uses the same folder as the public build, so back up the family files described in [the installation guide](sts2-port.md) before test runs if you want to preserve a starting snapshot.

## What to try

- Play one Skill, then an Attack: draw one card. Play two Skills first: draw two. Extra Skills cannot raise the charge counter above two.
- Charge another Attack that turn: it consumes charges but gives no second class draw. The allowance resets on your next turn; an uncharged Attack does not spend it.
- Leave a charge unspent at turn end: it should remain for your next turn.
- Try a multi-hit Attack or a replay: draw once for the card, rather than once per hit/play.
- Arcane Reservoir should still add its printed damage per charge to every hit, including charged Attacks after the draw allowance is used. Without Reservoir, Mage charges supply card draw rather than bonus damage.
- In 1.0.37, hold Ember Dart or Moonstone Lance for your charged turn; use Spell Notes to prepare the next draws. Try Rune Detonation with zero, one and two charges: only a full charge with the draw allowance ready refunds one Energy. Charge a second Rune Detonation afterward and confirm it pays its normal cost.
- Overcharge and Grand Thesis should bank Energy for the next turn. Use that Energy to play cards drawn by Arcane Flow; Overcharge pays 3 HP and both cards Exhaust.
- Judge whether one- and two-charge sequences are enjoyable across a full climb. Automated checks establish the mechanics, not final balance.

## Optional Workshop distribution

A separate [private beta Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3822395190) is now available with mod ID **SpireLegacyBeta**, allowing both subscriptions to remain installed. Enable only one build at a time and restart; follow the [beta channel instructions](beta-workshop.md). The public item 3816525393 is unchanged. Steam's [game-branch version ranges](https://partner.steamgames.com/doc/features/workshop/itemversioning) control compatibility with the game's branches; they do not make a Git branch private.
