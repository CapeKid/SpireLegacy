# Original class sprites

CapeKid's Spire Legacy 1.0.10 includes three original RGBA atlases under the mod's MIT license:

- `heir/characters/knight.png`: silver armor, crown, green scarf, sword and shield.
- `heir/characters/mage.png`: blue hood, violet robe, crystal staff and cyan magic.
- `heir/characters/ranger.png`: green hood, leather clothing, bow and quiver.

Created with the built-in OpenAI imagegen tool in reference/edit mode. The mod's original selection icon supplied style for Knight; the original Knight atlas supplied consistency references for Mage and Ranger. No extracted game artwork was used in or distributed with these assets.

Each 1448×1086 atlas has twelve authored poses. Row one contains four idle frames; row two contains four class attack frames; row three contains skill, hurt, defeated and victorious poses. Sprite regions, foot anchors and hashes are recorded in `sheets/character_art.json`. Runtime foot alignment keeps the character grounded. Mage's spell-release region extends 30 pixels into the neighboring empty gutter; the recovery frame excludes that gutter. Preflight checks meaningful alpha remains inside every region. A canvas shader discards faint alpha below 0.06 to prevent near-transparent generation residue from showing in adjacent frames.

Idle and attack are frame animations. Skill and hurt hold an authored pose briefly and return to idle; death and victory hold their authored poses. Native animation triggers and card-play hooks select these states without delaying gameplay. Rest sites and merchants use the selected class's idle animation. Trait sizes/colors remain supported. Family colors appear on the banner rather than tinting the entire illustration.

## Art direction and records

The original class atlases use compact fantasy proportions, clear silhouettes, dark contours and class-specific equipment. Original generation prompts and correction records have been removed from the current tree. These descriptions summarize the current art direction.

## 1.0.12 Mage death animation

`heir/characters/mage-death.png` is original built-in imagegen artwork, generated from the original mod Mage atlas as identity/style reference and then edited for slicing gutters. No game artwork is referenced or distributed. Four frames play at 5 FPS: fatal stagger, kneeling collapse, falling forward and settled defeat. Separate size normalization and ground anchors preserve the combat sprite scale; the final frame persists. Knight and Ranger retain their existing defeat poses. Rest-site sprites are twice their previous size; shop sprites are 1.75 times their previous size.

Generation prompt:

> Generate an original 4-frame DEATH ANIMATION sprite sheet for the exact Mage character in the reference, for Spire Legacy. Reference image is original mod artwork and character identity/style reference, not an edit target. Use same blue-purple hood, tiny golden crown, violet robe with golden trim, teal scarf, brown gloves/boots, wooden staff with cyan crystal. Clean bold cartoon outlines and cel shading, compact chibi proportions. Four equal square cells arranged 2 columns by 2 rows, transparent RGBA canvas. Each frame entirely inside its own cell with at least 15% transparent gutters on every side, no overlap. Reading order: frame 1 standing facing right, fatal stagger eyes closed, shoulders dropping, staff slipping; frame 2 collapsing onto knees, head bowed, staff tipping to right; frame 3 tipping forward toward ground, torso low, hands reaching floor, dropped staff nearby; frame 4 lying fully defeated face down, closed eyes, hood/cape settled, crystal staff resting horizontally beside body, consistent with reference fallen pose. Smooth consecutive collapse, SAME character anatomy/outfit/scale in all four frames. Ground baseline at cell-relative y=80%, all frames grounded, no scenery, no shadows or magic attack bursts, no gore, no text, labels or dividers. Crisp transparent original game sprites. Save as new mage-death atlas.

Production edit prompt:

> EDIT this Mage death atlas for production sprite slicing. Preserve the exact same original Mage identity, four consecutive collapse poses and reading order, 2 columns by 2 rows of equal square cells, cartoon outlines and transparent RGBA. SHRINK each entire frame including staff, cape and crown to 60% of its current size, centered horizontally in its own cell. Keep each frame's lowest ground-contact pixel at identical cell-relative y=80%. Leave at least 16% of each cell width fully empty on ALL sides of EVERY sprite; no pixels touching or crossing any cell boundary, including horizontal staffs in the bottom row. Ensure generous transparent empty gutters separate all 4 frames. Standing frame1, kneelingframe2, forward-fallingframe3, settled face-downframe4. Do not enlarge artwork to fill the canvas. No text, labels, gridlines, backgrounds, floor shadows or special effects. Exact square overallcanvas, clean transparency.
