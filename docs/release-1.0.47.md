# 1.0.47 — Ghost Luggage recovery

Ghost Luggage is now a 2 Energy Rare Power (1 Energy upgraded): at the start of each turn, choose an Attack discarded during your previous turn, return it to your hand and reduce its cost by 1 for that turn. Only specific discarded copies still in the discard pile qualify. Played cards, hand flushing and enemy-turn discards do not qualify. Each stacked copy recovers one Attack; cancellation or a full hand consumes that recovery. X-cost cards retain their native X cost.

Light Pack remains the immediate defensive discard engine. Ghost Luggage no longer banks next-turn Block. The historical next-turn Block power identity is retained for compatibility, and the card keeps its existing ID and artwork.

The owner authorized publication to public Workshop item 3816525393 (SpireLegacy) and private beta 3822395190 (SpireLegacyBeta), with a PR to GitHub main. Melty is not published.

Validation passed: release build/design checks, all 166,720 core assertions, balance-regeneration checks and 44 native scenarios across regular v0.107.1 and public-beta v0.111.0. Base/upgraded tests cover recovery, recurring turns, temporary discounts, zero/X costs, cancellation, full hands, played/flushed/exhausted cards, distinct copies, stale discards, stacking, duplicate callbacks and the saved-property codec. These are isolated solo tests with controlled selectors; no live co-op session is claimed. See ghost-luggage-1.0.47-checks.json.

Published version 1.0.47 verified on both Workshop listings. All 539 downloaded files per channel match staging, including the exact tested DLL, embedded Ghost Luggage card/rules, correct mod IDs and public/private visibility. See workshop-1.0.47-delivery.json.
