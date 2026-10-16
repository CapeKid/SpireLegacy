# Public-release preparation (#162)

Version 1.0.31 removes the player-facing crown editor and stages audited standalone, Workshop and Melty archives. It does not publish a release or change the existing private Workshop item's visibility.

## Crown controls

The active StS2 Manor has only a family-name editor. The crown button, numeric keyboard and crown-write save handler are removed, rather than merely hidden. Existing balances and purchased levels are preserved. The retired `playtest_crowns` sheet row remains for historical IDs, with `enabled=false`; packaging rejects a re-enabled flag. The archived StS1 source is not an active release path.

Developers can still seed disposable profiles through `HEIR_TEST_MODE=1` and the existing isolated native fixtures, which enforce private test-profile paths. Those fixtures are not earned-crown evidence. Normal launches do not subscribe the test bridge. There is no public Manor toggle or balance editor to enable.

## Asset provenance and package boundary

The native package copies only assets declared by seven manifests: cards, UI, characters, icons, relics, potions and presentation. It validates their bytes against recorded SHA-256 hashes, checks raster/vector/audio structure, and rejects old extracted-asset filenames. Closed package validation rejects undeclared files, game executables/assemblies, historical readers and saves. The Melty bundle additionally contains the pinned MIT-licensed BaseLib dependency; its PCK is a dependency resource, not the game's PCK.

Each mod directory includes `asset-provenance.json`: exact shipped asset paths/hashes, generation or authoring method, source manifest/guide, declared author/license and whether the record mentions game-style references. Card/UI illustrations were generated with imagegen; vectors, presentation geometry and synthesized sounds have authoring records; sprite generation and atlas assembly are documented in [character sprites](character-sprites.md). The current portrait prompt was already recorded in [its authoring guide](heir-portrait.md), and is now restored to the manifest. No artwork is replaced or given invented historical provenance.

Historical RL2 extraction metadata remains in source/embedded design compatibility records. Those names are not packaged RL2 images or a runtime extraction dependency. Runtime artwork loads from the mod's own declared files; RL2, Python and a reader/cache are not prerequisites. The owned StS2 game supplies its native encounters and engine assets at runtime, through normal mod APIs, rather than being redistributed by this package.

Provenance records are declarations plus byte/format verification, not independent proof of authorship. Some generated illustrations used Rogue Legacy or owned StS2 art as style/framing references; those private reference files are not shipped. Matching manifest hashes and excluding extracted files do not establish legal clearance for a public listing. The owner must accept the recorded provenance and presentation before making that release decision; this audit does not promise immunity from IP claims. MIT notices and AI-art disclosure are retained.

## Release decision

The existing Workshop configuration deliberately remains private. Publishing publicly requires an explicit owner decision and a separately authorized upload/visibility change. This preparation does not request or use publishing credentials. No personal family save is changed, and there is no balance wipe or migration.

Human build/progression playtests are #176/#177; [the final #146 assessment](balance146-final-assessment.md) states the existing evidence limits. Device/co-op coverage previously waived under #110 is not silently reintroduced or claimed as tested. Package layout checks for Linux/Proton do not substitute for physical Deck execution. Melty uses a Windows mapping and has a separate installed-launch verification boundary.

The final validation record accompanying this change distinguishes tested package/UI behavior, source-declared provenance and outstanding owner/device judgments. Preparation alone does not close these remaining judgments or make the item public.
