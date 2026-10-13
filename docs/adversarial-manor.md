# Family Manor input and accessibility audit (#57)

Baseline main: `be412a0`. The audit fixes [#133](https://github.com/CapeKid/SpireLegacy/issues/133) and [#134](https://github.com/CapeKid/SpireLegacy/issues/134), using owned offline Windows game copies, fresh private family/native saves and BaseLib 3.4.7. Regular v0.107.1 and public-beta v0.111.0 use Spire Legacy 1.0.29. [Issue-specific proof](adversarial-manor-evidence.json) records the tested source/DLL hashes, native command observations, layouts and complete deduplicated stderr diagnostics. Original game files, saves and screenshots remain private.

## Findings and changes

The name editor immediately focused its text field, but its delayed layout callback then focused Save. Navigation also assigned identities by enabled-control index; rebuilding the page could restore an unrelated control. Controls now have stable identities, each subpage remembers its focus and scroll position, and the editor's deferred focus targets the actual text field. Chronicle entry identity remains stable when its label changes from NEW to Read.

In a native two-peer lobby, an 80-crown purchase staged before Ready could still be confirmed afterward: the host's bank fell from 123 to 43. The selection page checked Ready during rendering, while the confirmation callback did not. Purchase staging, confirmation and heir selection now recheck both Ready and active-run restrictions at activation. Removed or closed-page controls ignore queued callbacks. A lobby/run modal closes when its owning screen changes; it cannot remain over a newly embarked climb.

The old Steam Deck document described the retired StS1 content reader and mouse layout. It now starts with the current StS2 installation and controller controls, clearly labeling the old instructions as historical. Co-op documentation explains activation-time restrictions and lobby-modal closure.

## Native coverage

- Open from Heir selection using the native Y action, with Ascension enabled. Navigate with native directional/Select/Cancel events and shoulder paging; held directions repeat.
- Type `AB c` entirely through controller-selectable on-screen keys, save it, and test the maximum 24-character family name. The editor retains text-field focus after layout; return restores the parent page.
- Read a Chronicle entry, select its motto, verify persisted state, then return with B to the correct Read entry and Chronicle launcher.
- Change the banner, reject a negative crown-editor value, and save a playtest bank. Family state agrees with its persisted file.
- Stage an upgrade without spending, review and cancel, keep a pending selection on close, then confirm once. Duplicate activation of the removed upgrade control stages one upgrade. Discard-and-close leaves the confirmed bank and levels unchanged.
- Render every trait definition in valid batches of at most three heirs, with long heir/family names. Descriptions remain complete labels, labels are not focusable, and shoulder paging reaches the lower content.
- Native display settings exercise 1280×800/16:10, 1280×720/16:9, 1200×900/4:3 and 1920×1080. Label/button/editor horizontal bounds remain within the viewport. Regular/beta 1280×800 and beta 4:3 screenshots were inspected: long descriptions wrap and the fixed Back button remains visible.
- Start actual Knight, Mage and Ranger combats on both branches. Manor browsing pauses solo, purchases/heir selection are unavailable, and closing restores the previous unpaused state. These are short native run/UI integrations, not completed campaigns.
- Two native ENet peers per branch verify unchanged 123-crown banks when Ready races a rendered confirmation, unready/banner selection, automatic modal closure on embark, active-run restrictions and a guest card action while the host browses. Co-op browsing stays unpaused and does not mutate the guest crown bank.

Release build, 164,053 existing core assertions, design preflight and all 20 harness checks pass. The audit does not alter crown prices, card balance, trait effects or frozen-run ownership rules.

## Fixture corrections and deferred coverage

An early all-traits fixture tried to persist more than three offers. The existing family-data validator correctly rejected it and preserved the original file. The corrected fixture uses valid three-offer batches in fresh profiles. An early resize-only probe retained native 16:9 settings; final probes apply native aspect settings and capture actual window/viewport dimensions. Neither was a production defect.

Controller events are injected into the game's input pipeline; callback-style presses are explicitly represented in the receipts. This does not certify physical Deck buttons, touch-to-controller switching, hardware reconnects, Steam virtual-keyboard dismissal, Linux/Proton or Steam transport. Those remain device validation under #110. Arbitrary UI scales, non-English locales, full campaigns and every lobby/disconnect timing are deferred. Current wrapped-layout and focus results must not be presented as passes for those cases. All native tests used disposable saves; no installed game branch, personal progression or Workshop/Melty upload was changed.

## Reproduce

Build against owned game assemblies. Use independent copied private labs prepared according to [the native harness guide](adversarial-harness.md), then run `scripts/run-balance-campaign.ps1 -ManorAuditChecks` for each branch and `scripts/run-coop-audit.ps1 -Pair -ManorChecks`. Supply fresh tags, the lab path, class and Python runtime for solo; supply branch, tag and distinct port for co-op. Launchers refuse reused profiles, isolate native/family saves and stop only verified lab executable PIDs. `scripts/sts2-manor-evidence.py` validates the recorded completed audit tags and exports this issue's bounded proof. It does not track the generic generated coverage map.
