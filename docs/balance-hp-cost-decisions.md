# HP-cost decisions in the balance pilot

The next stage records native reruns and conditional decisions: [balance-conditional-decisions.md](balance-conditional-decisions.md).

This stage follows merged PR #90. It changes the opt-in tactical pilot, not playable cards, traits, manor progression or release packages.

The pilot previously valued Offering's Energy and draw without considering its immediate HP loss. Native control diagnostics showed it could kill itself at 5 HP. The tactical policy now excludes verified immediate Ironclad self-damage when current HP is at or below the cost: Offering (6), Bloodletting (3) and Hemokinesis (2). Block does not count toward that budget because these owned models use unblockable self-damage. Other playable cards remain candidates, and a hand containing only a fatal cost produces no choice; the existing campaign loop then ends the turn.

Survivable costs remain available but receive an opportunity-cost penalty that increases as remaining HP approaches zero. This is a conservative heuristic, not an exact health-loss simulation. It reads the card's current base HP-loss variable, without predicting powers, relics, damage prevention or loss multipliers. It may reject a card that prevention would make safe and cannot guarantee survival with additional damage triggers. No blanket `HpLoss` filter is used: Haunt's variable describes a power rather than immediate self-damage.

Five new native fixtures complement the nine prior checks. Three compare fatal HP-cost cards against Strike with 50 Block; one confirms a healthy Offering actually loses 6 HP and survives; one checks that the pilot declines its only fatal card. The prior forced Offering death fixture remains, testing death-aware cleanup independently of card selection.

Validation results and immutable DLL details are recorded in MODLOG.md. Pure checks cover exact/lethal budgets, survivable costs, low-HP cards without costs and the increased near-death penalty. These checks do not establish optimal HP spending, potion decisions, archetype builds or character balance.

The prior six-run CSVs remain historical evidence of their original policy. No results are relabeled or recomputed under this policy. Next steps are HP-aware native control reruns, conditional finisher valuation, purposeful archetype rewards and potion decisions; these require their own effect checks before larger comparisons.

Tests use the existing independent private regular/beta labs, disposable profiles and verified PID cleanup. Installed game files, real saves and Steam branches are untouched.
