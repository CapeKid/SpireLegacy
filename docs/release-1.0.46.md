# 1.0.46 — C.I.P. hidden health

C.I.P. now hides the affected Heir's health bar and top-bar HP, including the native health bars in multiplayer panels. It no longer subtracts 8 maximum HP. Block, enemy health and other players without C.I.P. retain their normal displays. Health simulation, healing, damage and native saves continue normally. Existing runs retain their saved current and maximum HP; this release does not grant retroactive healing or rewrite saved HP.

The Muscle Weakness description now reads **Lose 1 Strength.** Its Strength penalty and 25% legacy crown bonus are unchanged, as is C.I.P.'s 25% crown bonus.

The owner authorized publication to public Workshop item **3816525393** (`SpireLegacy`) and private beta **3822395190** (`SpireLegacyBeta`). GitHub main is updated through a PR; Melty is not published.

Validation passed: release build, design preflight, archive checks, all 166,720 core assertions, and 108 native health-widget inspections across regular v0.107.1 and public-beta v0.111.0. All three classes were checked with and without C.I.P., plus vanilla controls. Checks include damage/healing, paid card play, Block/enemy visibility, room transitions and save/resume. Native checks used isolated solo profiles; no live multiplayer session is claimed. See cip-1.0.46-checks.json.

Publication verified on both existing Workshop listings: 539 downloaded files per channel match their staged packages, with the correct version, mod IDs and visibility. Both assemblies embed the updated C.I.P. behavior fields and Muscle Weakness text. See workshop-1.0.46-delivery.json.
