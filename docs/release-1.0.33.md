# Spire Legacy 1.0.33

Family Manor no longer displays the full card-play exception or the **Copy card-play diagnostic** button after a failed card action. Card-play failures still write the existing diagnostic to the game log and preserve the original exception.

Unempathetic's description is shortened to **Each enemy starts with 4 Block.**

Double Dose now costs 1 Energy and applies 3 Poison twice (4 twice when upgraded), without Exhaust or Retain. It supplies a fixed amount of Poison and can be drawn again. Venom Reserve keeps its retained, one-use Poison multiplier and its cost-reduction upgrade. Existing saved card IDs remain compatible.

No progression, artwork or save formats change.

## Validation and delivery

The Release build and design preflight passed. Standalone, Workshop and Melty archives passed closed-file and byte validation against DLL SHA-256 `87b62964a9b5bdde2cb78d25925cb64b41fbaa4f3de3578f9752a0d48d92dec5`. NuGet vulnerability-feed lookup was unavailable during the build; compilation completed with zero errors.

Core checks passed 166,716 assertions. The regeneration regression preserves Double Dose, all saved identities and reward pools. On the owned regular v0.107.1 disposable native lab, both forms passed paid resolution, discard-pile destination, save-copy, replay and invalid-target checks. Fixed 6/8 Poison addition and Venom Reserve multiplication were also verified on an existing stack; those two comparison actions used explicit fixture autoplay. Receipts are private under `double-dose-20261017-profile`. The native-tested DLL was `236e9574732c8b18c918e3a01c24d039fc7f1a089d3f4ec193f1233534565102`; the final rebuild only updates the embedded Serpent Oil comparison reference. This is focused mechanic validation, not a full campaign balance assessment.

On October 17, 2026, the owner requested publication. Restarting Steam restored its connection, and the official uploader confirmed the 1.0.33 update to existing item **3816525393**. Steam SDK queries confirmed public visibility and unrestricted regular/public-beta access; a fresh Workshop download matched the release DLL hash above. No Melty upload was attempted.
