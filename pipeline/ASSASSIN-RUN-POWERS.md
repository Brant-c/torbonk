# Assassin run powers — 2026-10-29

Assassin's former Combat Training granted 10% global damage and stopped at five
ranks. Most attacks had no repeatable damage power after that. This pass replaces
the global track with owned-ability packages and nine combination powers. It does
not change the persistent talent trees, other classes, enemy tuning, or ability
slot budgets.

## Repeatable packages

All values are additive percentages of the ability's base value per rank.
Damage also scales that ability's periodic damage. Cooldown bonuses increase
recovery rate, so 10% recovery means base cooldown divided by 1.1, not a flat
10% reduction. These powers have no rank cap and use Premium rarity.

| Power | Ability | Damage | Radius | Recovery |
|---|---|---:|---:|---:|
| Razor Tempest | Lacerate | 60% | 12% | — |
| Charged Blades | Voltaic Slash | 80% | — | 10% |
| Killing Edge | Maul | 80% | — | 12% |
| Sudden Violence | Spike | 100% | 15% | 15% |
| Unstable Voltage | Shock | 80% | — | 10% |
| Surging Detonation | Discharge | 70% | 12% | — |
| Ravenous Terror | Creeping Terror | 70% | — | 10% |
| Lingering Torment | Affliction | 70% | — | 10% |
| Crushing Agony | Crushing Darkness | 80% | — | 12% |
| Devouring Reach | Death Field | 70% | 15% | 10% |
| Gathering Darkness | Wither | 80% | 15% | 10% |
| Cruel Reprisal | Low Slash | 100% | — | 15% |

Devouring Reach replaces the previous Death Field power; Razor Tempest replaces
the standalone Wider Sweep. Existing finite Contagion and Static Blades remain.

## Combination powers

Each is learned once and only offered when the required abilities are owned.
Replacing a required ability suspends its interaction until the combination is
available again.

| Power | Effect |
|---|---|
| Voltaic Conduit | Voltaic hits recover Shock and Discharge by 1s, capped once per 0.5s. |
| Recurring Nightmare | Spike/Cloak recover Maul by 4s; opening Mauls gain another 50% damage. |
| Forked Lightning | Shock adds two jumps with 80% damage retained per jump; stacks with Chain Shock for five distinct targets. |
| Agony Resonance | Shock echoes a full tick of owned DoTs instead of a quarter tick, without consuming them; stacks with Raze. |
| Razor Wake | Lacerate spreads Terror, Affliction and Crushing Darkness; newly afflicted enemies take 75% Lacerate damage once per cast. |
| Deathmark Harvest | Death Field gains 30% damage per owned DoT on the target, capped at 90%. |
| Withering Blades | Wither recovers Maul and Voltaic Slash by 2s, capped once per 0.5s across all targets. |
| Shrouded Edge | Shroud/Deflection open 5s of +60% Lacerate/Wither damage; reapplication refreshes. |
| Quickening Decay | Terror, Affliction and Crushing Darkness tick 35% faster without shortening duration, including running and spread effects. |

## Offers

Normal offers prioritize one usable combination power and one scoped damage
package when enough slots and eligible candidates exist. Cards prefer different
owned abilities. Repeated ranks receive less selection weight, while remaining
available for focused builds. Early guaranteed core unlocks remain in place.

Health and speed may appear every fourth level, at most one per offer; each stops
after two ranks. Starter recovery also stops after two ranks. A small starting
kit gets extra unlock choices if needed to keep three cards available. Banishes,
rerolls, replacement and eligibility rules still apply.

## Validation

Run `./pipeline/Run-Assassin-Run-Powers-Smoke.ps1` from the repository root.
It uses isolated user data, checks the actual combat hooks, samples 320 offers
across starter/melee/DoT/defensive kits, and tests late ranks, resets and replacement.
The receipt is `pipeline/manifests/assassin-run-powers-smoke.json`.

898 checks passed. The existing Assassin relic smoke also passed 142 checks.
Build succeeded with existing warnings.

For the difficulty budget, three new ranks (one per named ability) were compared
against three hypothetical +20% global ranks, using automatic 60-second rotations
against two stationary targets, no relics, no persistent talents, and no new
combination powers. Baseline Lacerate remained equipped in each kit.

| Kit | Three global ranks DPS | Three scoped ranks DPS | Change |
|---|---:|---:|---:|
| Voltaic Slash / Shock / Maul | 45.50 | 51.19 | +12.5% |
| Discharge / Creeping Terror / Death Field | 50.02 | 50.32 | +0.6% |
| Lacerate / Wither / Spike | 25.28 | 27.24 | +7.8% |

These are controlled comparisons of upgrade value, not proof of full-run parity
between classes. Movement, enemy density, talent allocations and relic pairings
still affect outcomes. The new combination powers provide further payoff for
building around the abilities acquired during a run.
