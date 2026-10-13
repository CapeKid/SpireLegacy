# Heir presentation

Knight uses warm gold, Mage uses lavender, and Ranger uses green. Original family crests appear on the energy counter, card energy icons, map marker and auxiliary character icons. Existing selection portraits and combat sprite sizing are preserved.

Played cards trail a class-colored ribbon and crest. The ribbon lives outside the native crest fade/scale container, so it is visible immediately and keeps a constant world scale. Combat sprites follow the native body facing, including both sides of the crab fight; co-op rest-site sprites follow their native slot flip. A family-chevron transition replaces the Ironclad selection wipe. Architect attacks use original sword strokes, arcane rays or arrow volleys for the active class. Rest/shop sprites breathe subtly without changing their baseline size, react to rest/smith choices and their owning player’s shop purchases, and keep the existing class death animation.

Attack, cast, death, selection and transition cues are original synthesized PCM audio. They respect the game's master and SFX volume settings. Yummy Cookie has a shield, star or arrow icing variant for the owning heir; its upgrade effect is unchanged. The active run's saved Bloodline snapshot determines identity even after settlement selects the next heir.

`scripts/author-heir-presentation.py` reproduces the 51 original MIT vector, audio, scene and material files, and `sheets/presentation_art.json` records their provenance and SHA-256 hashes. Packaging verifies each file and includes it in both distribution routes. No game artwork or audio is included.

Native gameplay containers and hidden Spine positioning scaffolding remain in use. Heir-only hooks replace the visible identity and audio; native characters retain their own resources and behavior. Native checks cover regular/public-beta resource loading, actual energy/map/cookie widgets, paid card resolution, sound routing, transition loading and room responses. Physical Steam Deck visual/audio acceptance remains pending.

See [the presentation audit](adversarial-presentation.md) for paid-play, room/ending, resource-lifetime and owner-isolation coverage and explicit device/renderer gaps. Missing/corrupt illustrations produce path-bearing diagnostics; temporary decoded image buffers are disposed, and disposed cached textures are recreated.
