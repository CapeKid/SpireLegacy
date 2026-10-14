# Extended earned-crown pacing and Ascension 10 (#146)

Follow-up to the [initial measurement checkpoint](balance-pacing-validation.md), starting from main `523ea49058cbf0784e8af281a3048d54796aa547`. No card stats, manor prices, or payout rules change.

The user clarified a completed Ascension 10 Knight run with full manor investment on Steam Deck/public-beta. The seed, exact build, and card/decision records are unavailable. This is user-reported human viability evidence, not a locally verified matched-seed result or an estimated win rate.

## Measurement method

- Extend the existing trait-free A0 earned-fund trajectories beyond twelve returns. Carry the recorded family, manor and Chronicle, and continue deterministic seeds `PACING146-N`. Use the same one-purchase-per-return order as the original checkpoint. Never inject new crowns or reset investment.
- Compare Knight at A10 on seeds `BALANCE146-C` and `BALANCE146-D`, with no/partial/full manor on regular and public-beta. These are twelve fresh-family fixtures, using the existing planned pilot and defensive reward scoring. They are not expert human choices.
- Validate completed receipts and the live durable family before extending. Check contiguous settlements, purchase order, crown conservation, class, generation, manor and settled ID. Reject failed/timed-out mutations and active or mismatched families.
- Archive previous launch logs before restarting a trajectory. The exporter validates unchanged completed prefixes and counts only the latest completed chain, avoiding double counting. Log counts describe the latest launch, not the whole historical chain.

## Fixture interruption and recovery boundary

The first Knight continuation at return 13 encountered `shuffle_ftue` during the enemy turn. The oracle's end-turn wait timed out. Inspection showed FTUE/Neow fixture settings were applied only when a request included `manorTier`; those settings are process-local. Restarting a family without re-injecting a manor tier could introduce tutorial modals. The opt-in oracle now applies the fixture settings on every start, without changing manor levels or ordinary gameplay.

The failed report and active private profile are preserved. Its timed-out turn is not retried. A new private fork can instead start from the last *completed durable family snapshot* recorded at return 12. This is explicitly labelled `forkedFromDurableReceipt`, and differs from an uninterrupted trajectory or a resume of the failed mutation. Earlier earned/purchased funds are preserved exactly; the failed return contributes no settlement or earnings to the canonical trajectory.

This technique operates only on recorded disposable profiles. It does not read, restore or mutate personal saves. Completed-family continuation and checkpoint recovery are separate modes.

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
