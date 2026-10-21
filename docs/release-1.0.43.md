# 1.0.43 — rare engine differentiation (private beta)

The three approved rare-card replacements keep their saved IDs, rarity, illustrations and reward pools. Their uncommon counterparts are unchanged, as are the nine 1.0.42 redesigns.

| Card | Energy / upgraded | Full rules text |
| --- | --- | --- |
| Eternal Bookmark | 2 / 1 | At the start of your turn, if you Retained any cards last turn, draw 2 cards. |
| Archmage Thesis | 2 / 1 | After you play 3 Skills in a turn, your next Attack that turn is played twice. Triggers once per turn. |
| Hunt Without End | 2 / 2 | At the start of your turn, gain 2 Vigor for each Attack you played last turn, counting up to 3 Attacks. Upgrade: 3 Vigor per Attack. |

Bookmark no longer grants Retain. Actual end-turn retention, whether intrinsic, selected or full-hand, qualifies; its payout is consumed before drawing, including when No Draw or a full hand prevents the draw. Stacked copies increase draw but do not create repeated payout actions.

Thesis no longer grants Block. It counts resolved Skills while active; replayed Skills count as plays. The ready effect adds one complete Attack play per stacked copy, claims readiness before resolution, and expires at owner turn end. Three additional Skills cannot retrigger it that turn. Arcane Flow retains its normal once-per-turn draw limit. Thesis's visible counter and triggered flag are saved independently of the class counter.

Hunt no longer grants initial Vigor or Vigor every third Attack. It counts resolved Attacks, including replays and Attacks played before the Power in its setup turn, caps the count at three, and clears that count before granting next-turn Vigor. An attack-free turn creates no new payout. Existing unspent Vigor follows native lifetime rules.

All counters and payout flags are owner-scoped and support saved-property serialization and independent clones. The existing bounded Measured Breath, Woven Incantation Block and Tracking Rhythm Vigor effects remain intact.

Target: private beta Workshop item **3822395190**, mod ID **SpireLegacyBeta**. Public Workshop item **3816525393** remains on **1.0.39**. No public upload or main merge is authorized.

Native fixtures use owned Windows copies and deterministic selectors. Physical Steam Deck input, new co-op gameplay and full-climb balance remain playtesting tasks.
