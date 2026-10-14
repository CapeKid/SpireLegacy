# Extended earned-crown pacing and Ascension 10 (#146)

Follow-up to the [initial measurement checkpoint](balance-pacing-validation.md), starting from main `523ea49058cbf0784e8af281a3048d54796aa547`. No card stats, manor prices, or payout rules change.

The user clarified a completed Ascension 10 Knight run with full manor investment on Steam Deck/public-beta. The seed, exact build, and card/decision records are unavailable. This is user-reported human viability evidence, not a locally verified matched-seed result or an estimated win rate.

## Measurement method

- Extend the existing trait-free A0 earned-fund trajectories beyond twelve returns. Carry the recorded family, manor and Chronicle, and continue deterministic seeds `PACING146-N`. Use the same one-purchase-per-return order as the original checkpoint. Never inject new crowns or reset investment.
- This is one fixed purchase policy, not an optimized order or a human estimate of the time needed to complete the manor. Library precedes several cheaper later levels; trait bonuses and choosing different heirs are excluded.
- Compare Knight at A10 on seeds `BALANCE146-C` and `BALANCE146-D`, with no/partial/full manor on regular and public-beta. These are twelve fresh-family fixtures, using the existing planned pilot and defensive reward scoring. They are not expert human choices.
- The two long trajectories pair Knight with regular and Ranger with public-beta. Class and branch are therefore confounded: their purchase dates cannot rank class balance or isolate branch effects. Identical seed strings also need not produce identical native content across game versions.
- Validate completed receipts and the live durable family before extending. Check contiguous settlements, purchase order, crown conservation, class, generation, manor and settled ID. Reject failed/timed-out mutations and active or mismatched families.
- Archive previous launch logs before restarting a trajectory. The exporter validates unchanged completed prefixes and counts only the latest completed chain, avoiding double counting. Log counts describe the latest launch, not the whole historical chain.

## Fixture interruption and recovery boundary

The first Knight continuation at return 13 encountered `shuffle_ftue` during the enemy turn. The oracle's end-turn wait timed out. Inspection showed FTUE/Neow fixture settings were applied only when a request included `manorTier`; those settings are process-local. Restarting a family without re-injecting a manor tier could introduce tutorial modals. The opt-in oracle now applies the fixture settings on every start, without changing manor levels or ordinary gameplay.

The failed report and active private profile are preserved. Its timed-out turn is not retried. A new private fork can instead start from the last *completed durable family snapshot* recorded at return 12. This is explicitly labelled `forkedFromDurableReceipt`, and differs from an uninterrupted trajectory or a resume of the failed mutation. Earlier earned/purchased funds are preserved exactly; the failed return contributes no settlement or earnings to the canonical trajectory.

This technique operates only on recorded disposable profiles. It does not read, restore or mutate personal saves. Completed-family continuation and checkpoint recovery are separate modes.

After the fork completed return 36, a subsequent launch selected the original profile from the first inherited receipt rather than the fork's latest receipt. The completed-family guard rejected that launch on its read-only lifecycle check, before any run started. The launcher now selects the latest receipt's profile. The failed report remains preserved; the next batch uses a new evidence tag and the already completed 36-return fork, without replaying a mutation.

A later turn interruption terminated both native jobs. The stored Ranger report has 66 settled returns followed by unfinished return 67; the Knight report has 43 settled returns. `pacing_state.py --extract` derives separate completed checkpoints only when the last settlement, generation, crown balance and paid manor levels match an existing durable lifecycle receipt. It truncates the unsettled command tail, preserves the original files and hashes their provenance. It does not reconstruct a result from the journal, grant currency or resume the interrupted mutation. The next trials explicitly fork those checkpoints into new private profiles. Reusing the next deterministic seed in a new trial is distinct from replaying the old command against its interrupted save.

For export, verified stopped reports may be explicitly passed with `--stopped-report FILENAME`. Their raw hashes remain unchanged, their manifest status is `interrupted`, and their outcomes are excluded. Running reports remain rejected by default. Derived checkpoints are checked against the original receipt hash and settled prefix before being included.

The Ranger fork subsequently settled through return 83, then timed out waiting for a combat-turn response on return 84 (seed `PACING146-84`, Vantom fight). Its last successful response was turn 4; the last native play log entry was Long Reach during turn 5. The log also contains Godot worker-pool `Invalid Task ID` messages, without evidence establishing their causal relationship to the timeout. Return 84 has no verified settlement and earns no crowns in the canonical trajectory. Its failed profile/report remain preserved. A separate explicitly labelled private fork from the completed 83-return snapshot tests the same deterministic seed independently; no command is replayed against the failed save.

The separate trial passed that encounter and settled return 84 as a floor-33 loss, earning 63 crowns and purchasing Vitality level 3. The first timed-out attempt remains excluded. This observation does not establish a Long Reach defect or identify the timeout's cause; it also does not make the branched trajectory uninterrupted.

Knight settled through return 67, then reached an unsupported `NCrystalSphereScreen` observation during return 68. This was a successfully answered observation, not a failed card mutation. The pilot now performs one ordinary cell/proceed action per request, selecting visible fog cells in row order and capping attempts at twenty. It does not inspect unrevealed item contents. Because the event charges gold and its exact UI save state was not verified, the unsupported attempt remains excluded; a new labelled fork starts from the completed 67-return family snapshot.

The separate natural return 68 finished at floor 23 but did not visit Crystal Sphere, so it does not verify that handler. Controlled event fixtures on regular and beta separately recorded three cell clicks, normal reward collection, Proceed and map travel. These fixtures deliberately enter the event and remain excluded from natural campaign and earned-crown counts. The first fixture setup used a mutable model where the room factory requires a canonical model; those two failed fixture reports are preserved and excluded. The corrected fixtures use source `f0d333134ff9e7e2609357eb880279f001c0bc1c`, DLL SHA-256 `e6c7687ac4788a1b91a0a59753b59846cf3428ab9c7973d4594df48b42f8f723`.

Knight then settled through return 73. During return 74, at Act 3/floor 35, the native Whispering Earring autoplay had installed its own selector while the player was still in Start phase. The bridge attempted to acquire the pilot selector before its existing in-Step readiness check, causing a nested-selector exception. The bridge now performs the phase check before acquiring any pilot selector and returns a read-only wait snapshot until Play phase. The failed attempt remains excluded; a new labelled fork carries the completed 73-return snapshot. Controlled fixtures separately grant the native relic to test that readiness boundary; they are not natural campaign evidence.

The readiness fixtures exposed a second regular-branch timing boundary: the native selector can remain active briefly after Play phase begins. The bridge also observes `CardSelectCmd.Selector` and waits for its release. It does not clear or replace the native selector. The combined event/autoplay fixture must restart into a separate run before entering the autoplay combat, because its event-exit proof may already have started another battle. The final regular/beta fixtures completed normal pilot turns after 25/26 read-only waits respectively. They use source `7bc3db8ec5f4063d887cd6370776b3dec2f743eb`, DLL SHA-256 `5a5e7eeec4ed16b06aaf1e51330bc1784cce81c5b6a70a8b4a3ea8ff8db14d22`. The natural Knight continuation was already running the earlier phase-only fixture DLL; that distinction remains in its receipts.

Ranger paid all sixteen upgrades by return 120: 2,653 crowns earned, 2,620 spent, 33 remaining. The stop flag in the initial PowerShell wrapper was inadvertently splatted as individual characters, so the test began an unnecessary return 121 before hitting the 8,000-receipt guard. That unsettled return is excluded. The verified full-manor lifecycle receipt at return 120 is exported as `full-manor-checkpoint`, rather than claiming that the original worker stopped normally at the milestone. The launcher now preserves the flag as a string array; tests exercise the actual assignment and native Python argument binding with the flag both enabled and disabled.

```powershell
./scripts/run-balance-campaign.ps1 -Lab private/YOUR-OWNED-LAB `
  -Tag YOUR-UNUSED-TAG -Class knight -PacingChecks `
  -ContinuePacingReport private/sts2-evidence/YOUR-COMPLETED-REPORT.json `
  -EndRun 36 -Python python

# Only when deliberately making a separately labelled private fork:
# add -ForkCompletedProfile
```

Bounds remain 20 minutes/1200 decisions per run, 50 plays per combat decision, and 45 seconds of continuous waiting. A capped/unsupported/failed run is incomplete evidence. EndRun is a recording bound, not proof of full-manor coverage. `-StopAtFullManor -EndRun 200` stops after the six upgrade lines reach their paid maximum levels, or records a return-cap outcome. The exporter distinguishes these outcomes and verifies all sixteen purchases for a full-manor claim.

Long batches append every completed command receipt immediately to a private JSONL journal. Full reports checkpoint every 25 receipts, at each durable return, and on exceptions. This reduces repeated whole-history writes. Journals are evidence, not a mutation replay or automatic crash-recovery interface. Detailed results and remaining gaps accompany the derived evidence export.

## Completed higher-Ascension comparison

Twelve trait-free Knight A10 fixtures used seeds `BALANCE146-C` and `BALANCE146-D`. Every fixture ended naturally at zero HP, with no victory. The cells below give terminal floors for C/D; these are paired observations, not estimates of win rates.

| Branch | No manor | Partial manor | Full manor |
| --- | --- | --- | --- |
| Regular v0.107.1 | 7 / 5 | 8 / 6 | 9 / 7 |
| Public-beta v0.111.0 | 7 / 6 | 7 / 7 | 13 / 7 |

The tested DLL was built from source `52cd89c`, SHA-256 `b13c50b1cdfa6a59e7125f39fc6798b06d2a08a0ac701705d8ad538431ae22d3`, with BaseLib 3.4.7 and mod manifest 1.0.30. The later pacing fixture correction uses a different DLL and is recorded separately in its receipts. These outcomes suggest survival improvement in this limited policy, but do not establish experienced-player difficulty or justify tuning cards on their own. The user's Steam Deck/public-beta full-manor A10 Knight victory is separate qualitative evidence.

The FTUE-corrected pacing DLL is source `07bcf006998747c5932bdcf9f041cd7e39a8b6bf`, SHA-256 `3f3cd3fe23b95bf619030eb285cef385c31bc459a28f6ed8d10ebe7f0f1d0d12`. Python/PowerShell receipt handling was subsequently improved without rebuilding that DLL. Each command records the loaded runtime artifact; newer tooling commits must not be presented as that DLL's source.

The Crystal Sphere fixture DLL is source `06a9266823ec104de8c81b5a973558bedd173e25`, SHA-256 `b8478fed248ee04451ac089be60a5e02e136b741e828af7b70e00832033c3a37`. It compiles against both owned game versions. Its event policy change is a separate measurement stage; ordinary gameplay, card stats, prices and payout rules remain unchanged.

## Final earned-purchase measurements

The [derived export](evidence/balance146-long-term/manifest.json) contains 34 completed standalone campaigns, 200 settled returns and 20,542 decisions. Earlier completed prefixes are superseded, not counted twice. The [controlled UI/relic fixtures](evidence/balance146-long-term/sphere-ui.json) are separate from these counts. Offered, picked and played cards are retained in `cards.csv` and `decisions.jsonl`; hand-entry observations are not claimed as Draw-hook counts.

| Trajectory | Settled returns | Paid upgrades | Earned crowns | Spent crowns | Remaining crowns |
| --- | ---: | ---: | ---: | ---: | ---: |
| Knight, regular | 80 | 16 | 2,649 | 2,620 | 29 |
| Ranger, beta | 120 | 16 | 2,653 | 2,620 | 33 |

Knight stopped normally at full manor with the corrected flag. Its final measurement stage uses source `ebb006d3ad50d06a9a9cb74825f8634148ed0c77`, DLL SHA-256 `0babb0eb02ba9d839f3ee85984b5ccf53267f0522c823bdd933e92af22c22e62`. Ranger's full-manor result is the verified checkpoint described above. Both trajectories have zero winning campaigns. Each purchase below is an actual paid receipt; gaps count settled returns since the preceding purchase.

| Upgrade | Cost | Knight regular return (gap) | Ranger beta return (gap) |
| --- | ---: | ---: | ---: |
| Vitality 1 | 80 | 5 (5) | 7 (7) |
| Armory 1 | 120 | 12 (7) | 16 (9) |
| Smith 1 | 120 | 18 (6) | 23 (7) |
| Garden 1 | 100 | 21 (3) | 30 (7) |
| Library 1 | 350 | 34 (13) | 48 (18) |
| Vitality 2 | 115 | 38 (4) | 54 (6) |
| Smith 2 | 200 | 44 (6) | 66 (12) |
| Armory 2 | 200 | 50 (6) | 74 (8) |
| Garden 2 | 140 | 53 (3) | 80 (6) |
| Vitality 3 | 150 | 56 (3) | 84 (4) |
| Vitality 4 | 185 | 61 (5) | 91 (7) |
| Vitality 5 | 220 | 67 (6) | 101 (10) |
| Vault 1 | 100 | 70 (3) | 103 (2) |
| Vault 2 | 140 | 73 (3) | 107 (4) |
| Vault 3 | 180 | 76 (3) | 113 (6) |
| Vault 4 | 220 | 80 (4) | 120 (7) |

The observed first-upgrade timing matches the documented 5–7-return target in this limited pilot. The later Library gap demonstrates the effect of front-loading an expensive upgrade. Neither the full-completion dates nor the higher-Ascension losses establish human difficulty or justify changing prices/cards. These are loss-heavy, branched fixtures with the limitations stated above.

## Validation and remaining scope

The subsequent [A10 Mage/Ranger checkpoint](a10-mage-ranger-validation.md) adds twelve matched regular-branch class/investment trials across two seeds. It records early losses and explicitly distinguishes configured shop/potion/upgrade support from decisions that were actually exercised.

The final fixture build passes design preflight and compilation with zero warnings/errors, 20 harness checks, 17 continuity/launcher tests and 14 exporter tests. Native Crystal Sphere and separate-run Whispering Earring fixtures pass on both game versions. The export checks unchanged prefixes, checkpoint hashes, unique command/settlement IDs, paid level order and crown conservation. Failed, interrupted and unsupported attempts remain visible in the manifest and contribute no unverified earnings.

Issue #146 remains open. These measurements complete the paid full-manor curves and add controlled A10 Knight comparisons, but do not establish experienced-player win rates, current-build winning campaigns with recorded decisions, controlled higher-Ascension Ranger/Mage archetypes, or representative hard-trait/relic campaign combinations. The user's reported A10 Steam Deck Knight win supports qualitative viability; its unknown seed/build/deck prevents treating it as a matched fixture. No gameplay balance change is warranted from these pilot losses alone.
