# Design, generation and original-asset audit (#61)

Baseline: merged main `308847cf06ad73f3d1e8cfbf769f5db99fdc68b4`, mod 1.0.30. This audit covers the 32 authoring tables, the embedded `Design`/`CardDesign` access contracts, stable generated card/power identities and persistent class/trait/manor/relic/potion/Chronicle IDs, and deterministic asset/export writers. It changes contributor validation and text-art hashes, with no card, trait, manor or native C# behavior change.

Confirmed fixes:

- #149: preflight accepted negative, overflowing and boolean damage values. All tables now have checked-in typed contracts, required/unknown-field checks, exact booleans/integers, bounded values, JSON duplicate-property rejection, and contextual UTF-8/BOM/parser diagnostics. IDs and artwork paths are checked before filesystem access. Foreign starters, Ancient/expansion/dialogue references, manor prerequisite cycles and stale balance records are rejected. Artwork checks include nested atlases, frame bounds, manifests, provenance, corrupt PNG/SVG/WAV data, path collisions and external/active SVG references. Icon titles escape XML metacharacters.
- #150: an incomplete Ranger pool failed only after generated files were overwritten. The generator now validates first, preserves the saved-ID registry, writes deterministic UTF-8/LF output, rejects orphan files, and offers `--check`. Normal StS2 builds run this read-only validation before compilation. Source image exporters verify inputs before updating outputs; single-card export requires a declared ID before copying anything.
- #151: text-art hashes described the CRLF Windows working tree rather than Git's LF bytes. Generated SVG/scene/material assets now use explicit UTF-8/LF, Git pins their line endings, and manifest hashes describe those canonical bytes. Geometry, raster pixels and WAV audio are unchanged.

`Design.Row` uses `GetInt32`, `GetBoolean`, string and string-array accessors on cloned embedded JSON. `CardDesign` requires the enumerated numeric/boolean card fields and the negative-one draw-override sentinel. The schema matches these contracts before embedding; booleans cannot pass as Python integers. Card costs and hits have explicit bounds, and other integer authoring guards are intentionally broader than balance targets. Adding fields/mechanics requires deliberately updating the checked-in contract; adding content must preserve every existing saved identity. Removing an old identity requires an explicit save-migration decision, rather than regeneration alone.

Run `scripts/check-design-schema.py` with Python 3.10+ and Pillow, then build with `DesignPython` set when Python is not on PATH. The regression script uses disposable copies under `private`, including spaces/Unicode in the root and a different working directory. It exercises every required field and numeric type contract, then tests the actual loader/preflight/build gate, invalid-output preservation, stale models, BOM/Unicode, corrupt assets and repeatable authors. The compact results and exact build/hash evidence are in `adversarial-design-evidence.json`.

Writer boundaries:

| Writer | Inputs and output | Treatment |
| --- | --- | --- |
| `sts2-generate.py` | Validated sheets to 360 card / 41 power model identities | Active build gate; two-pass deterministic generation and saved-ID guard |
| `sts2-pool-expansion.py`, `balance_policy.py` | Current cards/pools plus curated overrides | Deterministic replay checked alongside the existing full-ID balance-regeneration test |
| `author-heir-icons/relics/potions/presentation.py` | Typed sheets plus original geometry/synthesis | Double generation, exact manifest/format checks, explicit UTF-8/LF |
| `author-heir-ancient-dialogue.py` | Original dialogue definitions | Double generation; class/Ancient/visit contracts and references checked |
| `prepare_card_art.py`, `prepare_ui_art.py`, `export_expansion_art.py` | Retained original PNG inputs | Deterministic export checked; AI generation itself is not reproducible from a prompt |
| `create_sheets.py`, `expand_traits.py`, `expand_card_pools.py`, `expand_build_choices.py`, `curate_shared_pools.py`, `generate.py`, `make_ui_assets.py` | Historical authoring/StS1 state | Retired reconstruction tools; do not run them against current sheets. They can replace current tuning, memberships or UI. Remaining archive/entry-point isolation belongs to #63 |
| `review_card_art.py`, `review_added_art.py`, `card_catalog.py` | Read sheets/assets; produce review/catalogue files | Review utilities, not a source of runtime or saved model identities |

Raster originals remain required authoring inputs. No source-game files or game extraction are needed for these tests. The ordinary compiled mod still requires no Python. Physical Linux/Deck execution, Workshop/Melty install/update layouts and duplicate-copy handling are not claimed here; those remain #62/#110. No publishing or personal-save changes are part of this audit. Native behavior is covered by the existing verified 1.0.30 regular/beta release baseline; this change adds no native gameplay hooks.

All eight authors are byte-identical on their second pass. Dialogue/card-art/UI-art JSON writers normalize CRLF to LF on their first pass; a separate parsed-value comparison verifies those three formatting changes do not alter design data. The other five writers reproduce their initial bytes exactly. A superseded slow mutation invocation was stopped by verified PID and is excluded from the final evidence.

The active Heir portrait was previously copied by the packager without a manifest hash. It is now declared in `ui_art` with its existing dimensions/hash and a preserve export mode, because the retained PNG is the available original. No portrait pixels change. The other six unmanifested `heir` files (`assets.json`, `host_assets.json`, the two background PNGs and two orb PNGs) are retired StS1 resources: no native StS2 reference or active package includes them; their archive treatment remains #63. All 510 active artwork resources now have manifests.
