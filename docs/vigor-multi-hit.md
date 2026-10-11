# Vigor and multi-hit attacks

Baseline: merged main `fff78e1b1a6cfe4317dc713fddac3c07c1a5cf21` / Workshop 1.0.27.

Rapid Fire and 30 other multi-hit cards used a separate native `AttackCommand` for each hit. Native Vigor is consumed in `AfterAttack`, so only the first hit received the bonus. The paid regular-branch reproduction with 7 Vigor produced `[9, 2, 2, 2]` instead of `[9, 9, 9, 9]`; no Vigor produced `[2, 2, 2, 2]`. Native Twin Strike and BaseLib's attack helper group hits in one command.

`LegacyCard.OnPlay` now builds one attack using `WithHitCount`, preserving native single-target, random-target and area targeting. Vigor lasts through all hits and is consumed once. This shared path covers all 31 multi-hit definitions without adding a custom Vigor override or changing card numbers. Normal class damage modifiers remain inside each native hit's damage pipeline.

Other damage routes were reviewed: self HP loss, trait self-damage, Combust, Sadistic, Thousand Cuts, exhaust/discard damage and similar power triggers are explicitly unpowered damage. They should neither receive nor consume Vigor; this fix leaves those routes alone. Single-hit attacks already use one command. Vigor grants use the native power, including grants after an attack resolves.

## Native regression

The opt-in `VigorOracle` temporarily observes native `AfterDamageGiven` and `AfterAttack` hooks. Observation hooks are removed in `finally`; they do not install during ordinary mod startup. The probe removes existing powers and isolates inherited charge bonuses, refills disposable HP/energy, plays each card through paid manual play, and records individual damage, attack count, remaining Vigor, pile and table cleanup. It tests two live enemies for area/random targeting.

`scripts/sts2-vigor-check.py PRIVATE_PROFILE regular` (or beta) checks all 31 multi-hit cards, base and upgraded, paired with zero and seven Vigor. Controls include all three basic Strike/Guard pairs, native Twin Strike, Measured Cut (new Vigor survives after attacking) Tempered Steel (a skill stacks Vigor without consuming it), and three self-HP-loss skills (unpowered damage preserves Vigor). A real end-turn transition must complete for each class. It asserts each hit receives +7, one attack boundary, normal discard/exhaust cleanup and the expected leftover/new Vigor.

Owned builds: regular v0.107.1 and public-beta v0.111.0; BaseLib 3.4.7/mod manifest1.0.27. The fixed regular-built DLL is identical on both branches: `73ab3b3713e3d947df07e1cc0906715e53eb910e93885de00c297c829a53b60e`. Private evidence: `private/sts2-evidence/vigor-before-regular.json` and `vigor-{regular,beta}-final.json`. The beta before-fix probe exited when PC Steam closed and was not counted as a pass. The first broad fixture interrupted a combat tutorial while replacing its room; paid damage checks passed, but the resulting turn-loop errors invalidate a clean gameplay-loop claim. The corrected runner completes disposable tutorial flags through the native control before starting the Heir scenarios, then verifies end-turn progression. Initial evidence/logs are retained separately. Fixed tests use the game's supported `--force-steam=off` launch in independent copied labs with guarded fresh profiles; personal saves and installed game files are untouched.

Physical Steam Deck and multiplayer card-specific retesting are not performed in this fix. No Workshop/Melty update or version bump is included; publish after review/merge when requested.

## Final result

Both final suites pass 86 paired base/upgraded scenarios across 43 cards per branch (344 paid Vigor-fixture card plays total). All 31 multi-hit definitions receive +7 on every individual hit, with one attack boundary. Controls preserve unconsumed/new Vigor, energy charge and discard/exhaust/table cleanup. Each class completes a real next-turn transition. Rapid Fire base is `[2,2,2,2]` to `[9,9,9,9]`; upgraded is `[3,3,3,3]` to `[10,10,10,10]`. The corrected final logs contain the existing disposable save-file deletion diagnostics, with no tutorial/turn-loop or attack exceptions. Core3,265 assertions, 20 transport fixtures, 32-sheet preflight and the regular API release build pass.
