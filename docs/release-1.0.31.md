# Spire Legacy 1.0.31

Public-release preparation for #162, based on merged main through PR #175. Requires Slay the Spire 2 and BaseLib 3.4.7. Native validation is scoped to the recorded regular v0.107.1 and public-beta v0.111.0 copies.

- Removes the crown-balance editor from the Manor, including its save handler. Family naming, controller keyboard, existing crowns and confirmed upgrade purchases are preserved. Developers retain the opt-in isolated test fixtures.
- Disables the historical playtest-crown system flag and rejects release packages that re-enable it.
- Ships a hash-pinned `asset-provenance.json` for the declared mod artwork and sounds. Packages contain no legacy content reader, extracted content, game assemblies, private references or save files.
- Restores the portrait manifest's generation record from the existing authoring guide. Original AI-generated illustration/style-reference records and MIT notices remain disclosed; technical checks do not constitute legal clearance.
- Includes the merged balance-pilot corrections and final bounded #146 assessment. Experienced human playtesting is separately tracked in #176/#177; no gameplay values or manor prices change in this preparation.

The Workshop item remains private pending the owner's public-release decision. Building these archives does not upload them, change visibility or update the installed/published mod. See [public-release readiness](public-release-readiness.md) for exact verification and remaining decisions.
