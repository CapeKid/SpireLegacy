# Engine finishers and emergency Block Potions

The next stage records natural comparisons and a delayed-resource scoring correction: [balance-engine-comparisons.md](balance-engine-comparisons.md).

This stage follows merged PR #92. It improves the opt-in diagnostic pilot; production card stats, traits, manor/crown progression and release packages are unchanged.

## Finisher decisions

Known single-hit Block-derived, chain and exhaust-damage attacks now qualify for estimated-lethal priority using the existing native damage preview. The preview already includes their conditions, inherited class effects and the exhaust pile cap, so the pilot does not add those amounts again. Dormant Block/chain attacks with no estimated payoff are declined. Once other paid attacks establish the chain counter, or Block becomes available, their values are recomputed normally.

Reward scoring gives discounted prospective value to these engines. Block damage uses the average literal Block in the current deck and capped support coverage. Chain damage values cheap attacks as potential prior plays, capped at two and discounted; exhaust damage values cards that exhaust themselves or other cards, capped at three prospective exhausts. The description distinguishes Attack type from literal damage, and self-Exhaust from exhausting other cards. These are heuristic expectations, not measured activation rates or complete archetype planning.

Multi-hit, area-chain and other complicated effects retain their ordinary preview utility but remain outside lethal priority. Secondary/conditional Block, ethereal fuel, retention engines, cost changes and future draw sequences are still incompletely modeled. Do not infer that a low reward score proves a card is underpowered.

Native base/upgraded effect fixtures verify Shield Slam after 8 Block, Finishing Flurry after two actual paid attacks and Ashen Advance after 11 native exhaust commands. The last checks the ten-card exhaust cap. They check dormant decisions where relevant, target choice, paid damage and death. The Heir fixtures use a no-manor/no-trait Knight, including an injected Ranger finisher, to isolate mechanics; they do not represent a natural Ranger build or optimal rhythm play.

## Emergency potion decisions

After completing its card choices, the tactical campaign pilot recomputes visible attack intents. When their unblocked damage is at least current HP, it can consume an owned native Block Potion through the normal action queue. It checks ownership, queued/custom usability status and target validity, plus native combat-side/action/removal restrictions, and refuses open overlays. It waits for execution, then verifies removal from the belt. It reevaluates danger after each supported potion, with a bound based on the existing inventory. Healthy or already protected turns preserve potions.

The policy supports **Block Potion only**. It does not choose offensive, generated-card, trait-specific or other potions, optimize shop purchases, predict hidden intents, prevent reflected damage or guarantee survival when the Block gained is insufficient. Prevention modifiers, non-attack losses and before-use triggers can invalidate a simple incoming-damage estimate. Cards are chosen before the emergency check, so it also does not optimize spending HP instead of conserving it for incoming attacks.

Each combat-turn journal records potion ID, visible incoming damage, HP, previous Block and actual Block gained. The CSV exporter counts potion uses, treating older journals without that field as zero. Historical CSVs are preserved; these fields are for subsequent campaign comparisons.

Native fixtures verify emergency consumption, conservation at healthy HP, conservation with sufficient existing Block, disabled native potion permission and a full campaign step: actual Ironclad at 1 HP uses the potion and survives a native enemy turn, with consumption recorded in the campaign reply. No direct potion-effect or HP-restoration shortcut is used. The native permission is named `CanRemovePotions` on regular and `CanUseOrRemovePotions` on beta; it is resolved against the running branch.

## Validation and next comparisons

Exact fixture totals, branches and immutable DLL hashes are recorded in MODLOG.md. Pure checks cover supported/unsupported finisher rewards and emergency damage boundaries. Effect fixtures use durable enemies and injected fuel/potions; they are not natural campaign evidence. No new win-rate or numerical card-balance claim is made from them.

Next: rerun supported Knight/Ironclad comparisons with this policy and inspect actual potion use, then add purposeful deck/upgrade choices or investigate bounded card weaknesses supported by those runs and human feedback. Extend Mage/Ranger controls only after their corresponding mechanics are represented.

Tests use existing independently copied private regular/beta labs, fresh guarded profiles and exact verified PID cleanup. Diagnostics caught the beta Exhaust return-type change, a potion-holder getter requiring an open popup, and the beta permission rename. The fixture uses the existing branch-compatible Exhaust adapter; potion selection now uses inventory and native permission checks. Failed/obsolete diagnostics are excluded from final validation. Installed game files, real saves, Steam branches and Workshop listings remain untouched.
