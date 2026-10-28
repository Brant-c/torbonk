# Assassin presentation fixes — 2026-10-28

Implemented the confirmed gaps from the Assassin presentation review, plus contact timing sampled from the existing clips. Existing class progression, relic mechanics, damage amounts and cooldowns are retained.

## Movement and weapon presentation

- Assassin now loads original polearm forward/back run cycles and the same in-place lateral adaptation used for Warriors. The actor can continue facing an enemy while the legs follow actual travel direction. Running cadence follows movement speed; ordinary attack legs remain available to running and jumping.
- Reused the bounded equipped-blade ribbon renderer for both ends of the polearm. Lacerate, Maul, Voltaic Slash, Low Slash and revealed Spike receive trails from actual visible weapon geometry. Hidden weapons do not generate ribbons.
- Recovered two explicit JBA resources into a separate `AssassinLocomotion` library. Source hashes and conversion provenance are recorded in the Assassin locomotion manifests. Original archives and existing libraries were not replaced.

## Contacts and sustained effects

- Lacerate and Discharge now emit contact effects on each successfully damaged enemy, including lethal contacts.
- Discharge uses a ground-oriented area ring instead of the oversized upright central electrical card. Each affected enemy also gets electrical impact feedback and a refreshable sustained status. Spreading Discharge carries that status; refresh, expiration, death and source removal clear it.
- Chain Shock and Voltaic Fury now use an arc interface supported by the actual Shock renderer. Secondary links survive beam redraws, each link has an impact flash, and all links expire or release with the renderer.
- Shock flashes use the additive particle shader, removing the dark rectangular backdrop seen during rendered inspection. This shared correction also applies to Sorcerer Shock.
- Assassin target contacts now run after positive damage admission, rather than through the earlier class-mechanics hook. Existing Wither target attachments and lethal hit tails remain intact.

## Attachments and defensive lifetimes

- Wither's cast particles follow the left-hand socket. Spike's source particles follow the weapon socket. Moving attachments retain their layer offsets; dedicated target impacts follow the target while it is alive and retain their last contact position after death.
- Deflection's emission window follows the talent-adjusted duration and active protection state, including reset and death.
- Force Shroud retains its configured gesture through the short recovery while preserving moving/jumping legs. The shield aura still follows the actual shield.
- The alpha billboard shader now preserves model scale, allowing Wither's wisps to grow as requested by their renderer.
- Assassin recipes retain up to eight recovered visible layers, including Maul's previously omitted layers. Non-rendering black placeholders are skipped. The total live particle budget remains 72; new contacts evict the oldest particles instead of disappearing when the budget fills.

## Contact alignment

Standing pose samples at 0.2, 0.4 and 0.6 seconds confirmed that the previous immediate damage preceded the visible swing or release. These are game-adaptation contact markers, not a claim to have recovered the original April animation-event graph.

| Ability | Contact after activation | Total action duration |
| --- | ---: | ---: |
| Lacerate | 0.50 s | 1.40 s |
| Maul | 0.55 s | 1.60 s |
| Voltaic Slash | 0.55 s | 1.60 s |
| Low Slash | 0.55 s | 1.60 s |
| Wither | 0.30 s | 1.20 s |
| Discharge | 0.50 s | 1.60 s |

Recovery was shortened by the added windup so total action lengths remain unchanged. Saber contact sounds move with the contact phase. Spike retains its existing one-second vanish/reveal behavior. No speculative replacement was made to Lacerate's ending-segment clip or the documented Spike/Low Slash source mappings; those still have limited evidence for exact original graph parity.

## Verification

- Build and Godot asset import succeeded. Existing compiler warnings and environment-asset UID fallback warnings remain; no runtime errors appeared in the final presentation captures.
- New Assassin presentation suite: **409 checks passed**, including the final rendered run. Covers every nonempty Assassin gesture across five travel directions, feet and root motion, enemy facing, blade ribbons, contact timing, action duration, per-victim contacts, lethal contacts, status spread/refresh/expiry, chain and relic arcs, extended Deflection, reset, Shroud gesture lifetime, hand following, retained Maul layers and particle cleanup.
- Existing Assassin refinement suites: **73 baseline, 74 Madness, 74 Deception checks passed**.
- Assassin relic suite: **142 checks passed**.
- Shared regressions: **154 Sorcerer, 242 Marauder, 117 Juggernaut presentation checks passed**.
- Inspected fresh close-up movement, contact-pose, Discharge, chain-Shock and Wither captures. Target fixtures are visible capsules; these captures establish local presentation behavior, not exhaustive acceptance across every enemy rig and combat density.

Run `pipeline/Run-Assassin-Presentation-Smoke.ps1 -SkipImport` for the focused checks, or add `-Capture` for the hidden native renderer and screenshots. Both use a fresh isolated test profile with valid Chain Shock and extended Deflection talents.
