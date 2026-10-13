# Earned crown settlement (#105)

Starting a climb and immediately abandoning it used to pay 3 crowns for 99 starting gold, or 5 crowns after quickstart merely entered floor one. The native settlement now supplies earned progress instead of reached floors and total carried gold.

The policy is:

- A completed combat qualifies the climb. Completed non-Ancient map points earn the existing 2 crowns each; the unfinished current point does not.
- A non-abandoned combat death after a completed manual card play earns one participation point. Abandonment kills the player internally, but is explicitly excluded from this death credit. An unplayed loss or untouched start earns zero.
- Only earned, unspent gold converts at the existing 25:1 rate. Native per-owner history supplies gains, spending, losses and net theft. Earned gold is conservatively spent/lost first, theft recovery cancels the theft, and conversion cannot exceed actual carried gold. Starting capital never becomes earned income.
- Existing victory, capped trait and frozen Treasury multipliers still apply. Exactly-once atomic settlement and owner guards are unchanged.
- Chronicle milestones retain the actual reached floor. Crown-paying progress and reached floors are now separate settlement inputs.

Native history already survives saves, including older active runs, so no new family fields or save schema migration are required. Replacing an orphaned family run without its native history no longer invents crowns from its cached floor; it still records the reached floor and advances the generation atomically. Historical crown banks are not retroactively reduced.

Baseline is merged main `6a58dc9`. Core checks pass 164,053 assertions, including a 40,000-case conservation grid, starting/Neow-gift eligibility, first-fight participation, spending beyond gains, losses, theft, remaining-gold caps, Chronicle separation and orphan replacement. Existing atomic settlement/fault/replay, trait cap, Treasury and co-op identity tests also pass.

Owned Windows regular v0.107.1 and beta v0.111.0 / BaseLib 3.4.7 / mod 1.0.29 each pass 42 native payout cases across Knight/Mage/Ranger and base/upgraded play contexts: normal/quickstart immediate abandonment, played abandonment, induced native first-fight death, paid native victory, earned gold, spending, loss, theft recovery, and 24 earned-progress save/resumes. Controlled victory fixtures lower enemy HP and generate paid starter cards/Energy; they do not claim ordinary balance outcomes. No debug room visits are used.

Three additional natural map comparisons per branch use the tactical pilot, ordinary enemy HP, ordinary gold, unmodified starting decks, no manor/traits and no debug rooms. They run to reached floor six or natural death and compare actual atomic payouts to independently derived native history totals. Regular Knight/Mage/Ranger each reach floor six and qualify four completed points / 53 unspent earned gold (10 crowns). Beta Knight/Mage reach floor six with four points / 48 earned gold (9 crowns); Ranger naturally dies at floor four with three points, including participation, / 30 earned gold (7 crowns). These are bounded seed samples, not full campaigns or win-rate evidence.

Exact source/DLL/game/BaseLib/mod identities, correlated reply counts and individual outcomes are in `earned-crowns-evidence.json`, validated by `scripts/sts2-crown-evidence.py`. Matrix and campaign stages use separate diagnostic builds; the later build adds an observation field and campaign driver, with unchanged production accounting. Failed compiler attempts are not native pass evidence.

```powershell
dotnet run --project sts2/tests/SpireLegacy.CoreChecks.csproj -c Release
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_regular_matrix -Class knight -CrownChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_beta_campaign -Class knight -CrownChecks
```

Use fresh tags and disposable profiles. Matrix/campaign tags select their corresponding suites. Physical Steam Deck/Linux, Steam transport and full multiplayer campaigns remain untested; the new gold extraction filters by player ID and existing pure owner/settlement checks pass. Native log diagnostics are reviewed separately from passing requests. No personal saves, installed branches, release packages, Steam Workshop or Melty publishing are changed. MODLOG and generated audit coverage stay ignored/local.

All four final native launchers exit successfully and their lab processes stop. Builds pass with zero warnings/errors; preflight (32 sheets / 2,062 rows / 26,477 cells), 20 harness checks, Python compilation, whitespace and generated coverage checks pass. Final logs retain renderer particle warnings, missing-backup deletion messages, two process-frame disconnect messages and one beta WorkerThreadPool `Invalid Task ID`. Their causes are not established or fixed here; resource/timing investigation remains #58/#110. Passing payouts do not imply clean engine logs.
