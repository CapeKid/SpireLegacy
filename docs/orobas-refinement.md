# Touch of Orobas and the Family Bloodline (#86)

Touch of Orobas now refines Family Bloodline into **Refined Bloodline** instead of falling back to a Circlet. It draws one additional card each turn. Class abilities, trait effects, purchased manor bonuses, frozen inheritance and the starting-relic grant guard remain active.

The upgrade uses BaseLib 3.4.7's `CustomRelicModel.GetUpgradeReplacement` extension. Native Touch setup, hover text, relic replacement and acquisition remain in use. Only a Family Bloodline replacement copies its saved family carrier. Native characters keep their own refinements. An already refined starter is excluded from another Touch offer; it cannot produce another empty Circlet refinement.

Refined Bloodline is a Starter relic, registered outside ordinary drop/shop rarities. It uses the existing original family/class artwork. Native saved-player binding accepts both starter IDs, so a refined heir keeps its class and frozen run through deserialization and save/resume. All broad `BloodlineRelic` consumers continue to apply to the derived model.

Baseline merged main is `42a7471`. A test-only probe at `994216e` / DLL `d0358f220473e0d2c0065103556822d76e9bc0c3ee82a99384f2c828d21d4323` reproduces the native Circlet mapping for all three classes on both owned branches. Fixed receipts record exact source/DLL identity in `orobas-refinement-evidence.json`.

Native checks cover Knight/Mage/Ranger with ordinary, Pacifist and Antique inheritance; real Touch acquisition and replacement; unchanged saved carrier fields; exact +1 owner draw modifier; player deserialization and real native save/resume; no repeated Antique grant; no repeat refinement offer; paid base/upgraded starters and table cleanup. A real Ironclad acquisition remains Burning Blood to Black Blood. Separate two-peer ENet tests refine both owners on both peers, verify owner-only draw and complete native round-trips, then exercise paid play and local durable settlement.

The initial co-op fixture obtained Touch while native `FinalizeStartingRelics` was still running. The engine consequently invoked Touch again as an initial relic and failed because its original starter had already been removed. The fixture now waits for playable combat before adding test relics. Production gameplay is unchanged by this timing correction; failed receipts are excluded. Co-op acquisition uses the native command on each peer as a synchronized fixture, not a natural Orobas event campaign.

Reproduction uses disposable profiles only:

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-regular-lab -Tag UNIQUE_REGULAR -Class knight -OrobasChecks
./scripts/run-balance-campaign.ps1 -Lab private/sts2-harness-beta-lab -Tag UNIQUE_BETA -Class knight -OrobasChecks
./scripts/run-coop-audit.ps1 -Branch regular -Tag orobas86_UNIQUE_OWNER_REGULAR -Pair -Port 25601
./scripts/run-coop-audit.ps1 -Branch beta -Tag orobas86_UNIQUE_OWNER_BETA -Pair -Port 25611
```

Build, core/preflight/harness checks and full native identities are documented in the adjacent evidence file. These are controlled integration checks, not physical Deck/Linux testing, balance win rates or a full multiplayer event campaign. No workshop publication, package refresh or personal saves are part of this PR. The new saved relic requires a build containing this model.
