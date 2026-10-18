# Spire Legacy 1.0.35

Custom-card choices now stay bound to the run that opened them. When the hand UI completes an old selection during cleanup or loading another run, its reply is cancelled before it can enter that run's choice synchronizer. This prevents the obsolete reply from becoming a new card-play diagnostic through `GetChoiceId` / `SyncLocalChoice` / `FromHand`. Current-run choices keep the native selection, synchronization and discard behavior; Cold Read's costs and effects are unchanged.

The Pacifist description no longer includes the redundant “No free Poison at combat start” sentence. Its gameplay effects are unchanged.

## Validation scope

The reported Steam Deck timing was not directly reproduced. Normal paid Cold Read worked in both published 1.0.32 and 1.0.34 on owned Windows regular v0.107.1 and public-beta v0.111.0, with the reported Ranger traits, including native save/resume. A controlled late-callback fixture with the original selection code reproduces the exact exception chain for both card forms after replacing the run/player collection. It uses the real hand UI and native synchronizer, with deliberate run-context fault injection.

Focused fixed-build results and the delivered binary hash are recorded in the accompanying validation record. The fixture invokes card effects directly for lifecycle faults; separate paid native plays exercise Cold Read and Cold Read+ through the real discard UI and verify the following card resolves. The regression also checks that active-run and unrelated native synchronization errors remain errors. No physical Deck retest or full campaign balance claim is made.

The Workshop package targets existing public item **3816525393**, with BaseLib required and both game branches allowed. Melty is packaged only.
