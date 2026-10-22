# Spire Legacy — Beta

This [private Workshop channel](https://steamcommunity.com/sharedfiles/filedetails/?id=3822395190) uses mod ID **SpireLegacyBeta**, Workshop item **3822395190**. The public channel keeps **SpireLegacy** and Workshop item **3816525393**. Both subscriptions can remain installed.

On Steam Deck, use the same Steam account as the item's owner. In StS2's mod menu, disable **Spire Legacy**, keep BaseLib enabled, then exit the game. Subscribe to the beta listing and restart StS2 with **Spire Legacy — Beta** enabled. To return to public, reverse those two toggles and restart. Both listings can stay subscribed. Remove any manually installed local copy if it overrides the build you want to test.

Enable only one Spire Legacy build at a time. They intentionally share their assembly, card/model identities, character, patches, and family progression; a separate subscription ID is not a second independent character mod. Finish or abandon a climb before switching: native run saves record their loaded mod set. Back up family files if you want a fixed pre-test snapshot; see the installation guide shipped with the regular mod.

The tested beta changes through 1.0.45 have been promoted to the public listing with the owner's explicit approval. This separate private channel remains available for future tests. Regular and public-beta game branches are separate from these mod channels.

## Maintainer workflow

Build normally, then run `scripts/sts2-beta-package.py --output-root <fresh-directory> --expected-dll-sha256 <checked-hash>`. It creates a private `workshop/` workspace and a standalone beta ZIP. The beta DLL filename and manifest follow its new mod ID; the assembly/model identity remains stable. It does not upload.

Use the official ModUploader with the beta workspace only and explicitly target beta item **3822395190** (`--id 3822395190`). The initial upload created that separate private item; its target is recorded in `sts2/beta-channel.json`. Future updates must reuse it, never public item 3816525393 or a new item. Recheck `visibility: private`, the beta ID, and the payload hash before every upload. Do not copy the public workspace's marker into a beta workspace.
