# 1.0.48 — Phoenix Forge cumulative Exhausts

Phoenix Forge now deals 14 damage to ALL enemies for every three cards you Exhaust (18 upgraded). Partial progress carries across turns, and multiple groups can trigger in one turn. The 2 Energy cost, upgrade, card/model IDs and artwork are unchanged. The Power tooltip shows progress toward the next burst.

All of the owner's Exhausts count, including Ethereal cards. The counter is reset before damage resolves to keep nested Exhaust effects safe. The existing saved counter field is retained for compatibility and normalized into a three-card cycle. Other exhaust Powers remain unchanged.

Castle Heart armor now uses the native armor (Plating) icon in both its status badge and large tooltip. Its Block, armor amount, cost and effects are unchanged. Its cost-only upgrade no longer falsely highlights unchanged Block in green. The shared upgrade handler now skips zero-value upgrades, while real stat upgrades retain their highlighting.

The owner authorized publication to public Workshop item 3816525393 (SpireLegacy) and private beta 3822395190 (SpireLegacyBeta), plus a PR to main. Melty is not published.

Validation passed: release build/design checks, all 166,720 core assertions, 20 native card scenarios and 720 card upgrade-preview checks across regular v0.107.1 and public-beta v0.111.0. Phoenix Forge checks cover base/upgraded bursts, partial progress across turns, multiple bursts, stacked damage, Ethereal Exhausts and the saved counter codec. Castle Heart checks verify native armor textures, unchanged Block/armor, a cost-only preview, and upgrade flags across all 360 card definitions per branch. These are isolated solo tests; no live multiplayer session is claimed. See phoenix-castle-1.0.48-checks.json.

Published version 1.0.48 verified on both Workshop listings: all 539 downloaded files per channel match staging, including the exact tested DLL, embedded rules, correct mod IDs and public/private visibility. See workshop-1.0.48-delivery.json.
