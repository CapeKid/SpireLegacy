# Spire Legacy — Beta

This private Workshop channel uses mod ID **SpireLegacyBeta**. The public channel keeps **SpireLegacy** and Workshop item **3816525393**. Both subscriptions can remain installed.

On Steam Deck, use the same Steam account as the item's owner and subscribe to the beta listing. In StS2's mod menu, keep BaseLib enabled, disable **Spire Legacy**, enable **Spire Legacy — Beta**, and restart. To return to public, reverse those two toggles and restart. Remove any manually installed local copy if it overrides the build you want to test.

Enable only one Spire Legacy build at a time. They intentionally share their assembly, card/model identities, character, patches, and family progression; a separate subscription ID is not a second independent character mod. Finish or abandon a climb before switching: native run saves record their loaded mod set. Back up family files if you want a fixed pre-test snapshot; see the installation guide shipped with the regular mod.

The beta contains the Mage Arcane Flow mechanic and six-card pool changes from test version 1.0.37. The public listing stays on 1.0.35 until the owner authorizes a public update. Regular and public-beta game branches are separate from these mod channels.

## Maintainer workflow

Build normally, then run `scripts/sts2-beta-package.py --output-root <fresh-directory> --expected-dll-sha256 <checked-hash>`. It creates a private `workshop/` workspace and a standalone beta ZIP. The beta DLL filename and manifest follow its new mod ID; the assembly/model identity remains stable. It does not upload.

Use the official ModUploader with the beta workspace only. First upload creates a separate item because this fresh workspace has no `mod_id.txt`. Record the returned beta ID and retain that marker. Future updates must target that beta ID, never public item 3816525393. Recheck `visibility: private`, the beta ID, and the payload hash before every upload. Do not copy the public workspace's marker into a beta workspace.
