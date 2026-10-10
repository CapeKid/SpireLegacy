# Original class sprites

CapeKid's Spire Legacy 1.0.10 includes three original RGBA atlases under the mod's MIT license:

- `heir/characters/knight.png`: silver armor, crown, green scarf, sword and shield.
- `heir/characters/mage.png`: blue hood, violet robe, crystal staff and cyan magic.
- `heir/characters/ranger.png`: green hood, leather clothing, bow and quiver.

Created with the built-in OpenAI imagegen tool in reference/edit mode. The mod's original selection icon supplied style for Knight; the original Knight atlas supplied consistency references for Mage and Ranger. No extracted game artwork was used in or distributed with these assets.

Each 1448×1086 atlas has twelve authored poses. Row one contains four idle frames; row two contains four class attack frames; row three contains skill, hurt, defeated and victorious poses. Sprite regions, foot anchors and hashes are recorded in `sheets/character_art.json`. Runtime foot alignment keeps the character grounded. Mage's spell-release region extends 30 pixels into the neighboring empty gutter; the recovery frame excludes that gutter. Preflight checks meaningful alpha remains inside every region. A canvas shader discards faint alpha below 0.06 to prevent near-transparent generation residue from showing in adjacent frames.

Idle and attack are frame animations. Skill and hurt hold an authored pose briefly and return to idle; death and victory hold their authored poses. Native animation triggers and card-play hooks select these states without delaying gameplay. Rest sites and merchants use the selected class's idle animation. Trait sizes/colors remain supported. Family colors appear on the banner rather than tinting the entire illustration.

## Generation prompts

Knight:

> Create one original game animation sprite atlas for Spire Legacy's KNIGHT heir. Reference image is STYLE ONLY: match the Rogue Legacy 2 inspired compact chibi medieval cartoon, large helmet/head, thick clean dark outlines, flat cel shading. New full-body silver armored knight with small gold crown crest, emerald green scarf, sword and shield, facing RIGHT. Transparent RGBA background. Exactly 4 columns by 3 rows of equal-size cells in a rectangular 4:3 canvas, no dividers, text, labels, shadows on ground or background. Each sprite fully inside its cell with generous padding, feet fixed at same baseline and character same size; no spill between cells. Row1 cells1-4: four subtly different breathing idle poses. Row2 cells1-4: coherent attack sequence, sword windup, forward step, right-facing sword slash with short pale slash arc, recovery. Row3: cell1 defensive shield-raised skill pose; cell2 recoiling hurt pose; cell3 fallen defeated pose; cell4 sword raised victorious pose. All 12 sprites must be this exact SAME original character and consistent proportions/equipment/colors. Full body visible in all cells. Clean production sprite artwork readable at 200px tall, no pixel art, no realistic rendering. Save the generated sprite atlas and report its path.

Mage:

> Create one original game animation sprite atlas for Spire Legacy's MAGE heir. Reference image is the companion KNIGHT atlas: match its compact chibi proportions, bold dark contours, cel shading, right-facing direction, 4x3 grid and sprite size. Replace the character with an original medieval MAGE, oversized hooded head, midnight blue pointed hood with small gold crown crest, violet robe, teal scarf, leather boots, long wooden staff with glowing cyan crystal. NOT armor, sword, or shield. Transparent RGBA background. Exactly 4 columns by 3 rows of equal-size cells, rectangular 4:3 canvas, no text or grid dividers. Same character/equipment/color/proportions all12cells; feet same baseline and entire sprite inside each cell with ample padding. Row1 four subtly different idle breathing poses, staff held. Row2 four clear casting ATTACK animation poses: lift staff preparing, draw magic toward crystal, thrust staff RIGHT releasing small bright cyan flame trail within cell, recovery. Row3 cell1 skill pose with magical circular ward around raised free hand; cell2 recoiling hurt pose; cell3 fallen defeated pose; cell4 victory staff raised. Fullbody everycell; comic Rogue Legacy2-inspired clean hand-drawn game sprite style, not pixels, not realism, highcontrast readable silhouette. Draw an original Mage atlas, do not include the knight. Save the PNG and report path.

Ranger:

> Create one original game animation sprite atlas for Spire Legacy's RANGER heir. Reference companion Knight atlas supplies STYLE AND GRID ONLY: compact chibi medieval large head, thick dark outlines, clean cartoon cel shading inspired by Rogue Legacy2. Original RANGER wearing forest green hood with small gold crown crest, ochre leather tunic, dark green scarf/cape, brown gloves and boots, wooden recurved bow and back quiver; facing RIGHT. No armor, sword or staff. Exactly 4 columns by3rows equal cells in4:3transparentRGBAcanvas. Same character equipment proportions colors throughout. STRICT generous 15% empty transparent padding INSIDE each cell, no pixel crosses cell boundary; all feet at same cell-relative baseline. Row1:4small breathing idle variations bowheld. Row2:4coherent right-facing bow Attack frames: reach for arrow, nock arrow/draw string, release arrow with veryshort trail fullywithin samecell, recover bow. Row3:cell1 defensive skill crouch with cape raised;cell2 recoiling hurt;cell3 fallen defeated on ground;cell4 victory bow raised. All12 sprites fullbody visible in theircells. Balanced consistent character scale matching knightatlas. No text, labels, borders, gridlines, scenery or groundshadows; actualalpha background. NOT pixelart or realistic. Save original Ranger PNG and reportpath.

## Final atlas edits

Knight:

> EDIT this Knight sprite atlas for production animation slicing. Preserve exact same character and 12 poses and row order, the4columns3rows layout, artwork style and alpha. Main correction: REDUCE every entire pose including weapons/effects to 60% of its current size, centered in each equal grid cell. Make wide transparent empty GUTTERS all around EVERY pose: at least 18% of cell width/height empty on EVERY side. All12 poses must be completely separated by transparent gaps, absolutely NO pixels touching neighboring cell borders; sword arcs and death equipment included. Align standing boots at identical cell-relative y=80% and torso/head positions consistently for idleframes. Each cell square, exact4:3overallcanvas. Empty space is crucial, sprites will be enlarged in game. Do NOT fill canvas with enlarged sprites. No text labels dividers orgroundshadows. Keep realtransparentbackground. Save correctedatlas.

Mage:

> EDIT this Mage sprite atlas for production slicing. Preserve the same original character, all12poses and row order,4columns3rowslayout, cel-shadedartstyle and transparency. SHRINK each entire pose including staff, magic and effects to60% of current size, centered in its equal square cell. Wide completely empty transparent GUTTERS on EVERY side of EVERY cell, at least18% of cellwidth/height. No pixel touching or crossing any cell boundary, including cyan flame projectiles and ward. Four standing idlefeet identicalcell-relative baseline around85%cellheight. Consistent character scale across entireatlas. Make3rdattackframeflame short enough tofitinsamecellwithpadding. No labels,gridlines,shadows,scenery. Exact4:3canvas, transparentRGBA. Emptyspace critical: doNOT enlargespritestofillcanvas. SavecorrectedPNG.

Ranger:

> EDIT this Ranger sprite atlas for production slicing. Keep exact same originalgreenhoodedRanger,12posesandroworder,4columns3rowslayoutandcartooncelshading. ShrinkEVERYentireposeincludingbowarrowscapequiver/effects to60% of currentsize,centerinits squarecell. Wide emptytransparentGUTTERS aroundEVERYsprite:atleast18% cellwidth/height clearonall4sides,no pixel touching/crossingcellboundary. Shortenreleasedarrow/trailsofitsinsideitsowncell. Allstandingbootsalignedaround85%cellheight,consistentcharacterheightacrossposes. Entire4:3canvasistransparentRGBA.Noenlargementtofillemptyspace,no labels,text,gridlines,groundshadoworbackground. SavecorrectedPNG.
