# Original Heir icons

The active StS2 mod uses 66 original, hand-authored SVG icons: 63 trait icons (including two disabled historical traits) and three class badges. All 61 selectable traits have distinct artwork, including traits that previously shared an imported image. No game art, fonts, tracing or external image references are used.

The icons use compact silhouettes, dark rounded outlines, a violet/gold family frame and a consistent flat palette. Hearts, shields, books, wings, plants and equipment represent their corresponding traits; small modifiers distinguish related concepts. Each source SVG is also the shipped asset. Godot renders it directly into a texture on Windows and Linux, including Steam Deck and Proton. The icons require no content reader or asset cache.

Author and license: CapeKid, MIT. Reproduce the set with `python scripts/author-heir-icons.py`. `sheets/icon_art.json` records stable subjects, paths, dimensions, hashes and provenance. `scripts/preflight.py` checks full coverage, unique content, hashes and self-contained SVG structure. Native checks render every icon and inspect the actual trait/class UI.

Existing original card art, character portraits and class sprite atlases are unchanged. Historical `assets` sheet names remain as compatibility metadata for the archived StS1 source; the StS2 runtime does not resolve them against Rogue Legacy 2.
