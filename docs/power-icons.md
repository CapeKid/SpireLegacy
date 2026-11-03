# Power icon audit

Eternal Bookmark (`retained_draw`) inherited the Strength sword from the shared custom-power fallback. The same fallback affected unrelated draw, Block, Energy, Retain, recovery and triggered-damage powers. Castle Heart was the only custom exception.

Both the status-bar and tooltip icons now use one explicit source:

| Effect | Native artwork |
| --- | --- |
| Draw (Eternal Bookmark, Rune Familiar, Arcane Conductor, Second Quiver) | Draw Cards Next Turn: card with a plus sign |
| Block (Woven Incantation and other Block engines) | Plating: metallic helmet, also used by Castle Heart |
| Block next turn | Block Next Turn |
| Energy (Arcane Meter, Tailwind, Berserk) | Energy Next Turn: lightning bolt |
| Retain / card planning | Well-Laid Plans or Retain Hand |
| Recovery (Ghost Luggage, Prophecy Return) | Rebound: return arrow |
| Next-Attack bonus | Vigor |
| Repeated Attack | Echo Form |
| Triggered damage | Inferno: fiery impact |
| Class charges / Reservoir | Focus |

Native power aliases reuse their corresponding native artwork. A new custom power must receive an explicit icon assignment rather than silently inheriting Strength. Artwork conveys the effect category; each power's existing description still specifies its triggers and timing. No mechanics, IDs, save fields or card text change.

Validation: Release build and design preflight passed. The isolated native test bridge loaded both icon sizes for all 51 generated power identities on regular v0.107.1 and public-beta v0.111.0 (204 texture-path checks). It rejects unrelated Strength icons and explicitly checks Eternal Bookmark's small and large draw paths. Exported icons were visually reviewed; Block engines use the metallic helmet after that review. See [power-icons-checks.json](power-icons-checks.json) for paths, dimensions and tested DLL hashes. This fix has not been published to either Workshop listing.
