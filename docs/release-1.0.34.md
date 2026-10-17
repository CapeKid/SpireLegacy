# Spire Legacy 1.0.34

Knight, Mage and Ranger now leave a stale death pose when their HP becomes positive again, including automatic revival from Fairy in a Bottle. The visual frame update reconciles the custom sprite with the native creature's living state, without relying on a native `Revive` animation trigger. Returning to idle also restores the normal sprite atlas, scale and foot alignment.

Actual deaths still play and hold their class death animation. Attack, skill and hurt animations remain driven by native triggers. Potion effects, HP restoration, progression and saves are unchanged.

## Validation

The original bug reproduced with the real Fairy in a Bottle on all three classes: their HP returned to 16/18/15 for Ranger/Knight/Mage while the sprite stayed dead. The fixed release passed all three classes on owned regular v0.107.1 and public-beta v0.111.0 Windows copies. Checks cover automatic potion consumption, positive HP, idle recovery, normal scale and foot alignment, post-revival attacks, and completed/held death poses without a potion. Before/after screenshots were visually inspected. Both disposable clients exited and their original mod files were restored.

Release DLL SHA-256: `d15c6d3faaa96543f06e3636439e5386e4d10f3885d5c2d2e804f94ed4f642c4`. The Release build and design preflight passed with zero compilation errors; NuGet vulnerability-feed lookup was unavailable. Sanitized case results are recorded in `docs/revival-1.0.34-validation.json`. These are focused visual regressions, not full campaign or physical Steam Deck tests.

The Workshop package updates existing public item **3816525393** and keeps BaseLib required. Melty is packaged only.
