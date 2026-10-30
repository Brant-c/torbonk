# Sniper run powers — 2026-10-29

Sniper's run pool previously offered global Combat Training and generic range/
recovery powers bound to Snipe. Acquired core attacks had almost no dedicated
run upgrades. This pass removes those three generic tracks for Sniper and adds
14 scoped packages plus 12 combination powers. Snipe and Rifle Shot receive no
new power track. Slot budgets remain. Sniper now has a separate Lethality talent
set, detailed below.

## Cover requirements

Snipe, Ambush, Series of Shots, Followthrough, Suppressive Fire, Plasma Probe,
Orbital Strike, Explosive Probe and Sniper's Cull require grounded stillness for
0.6 seconds before activation. Followthrough also needs its precision-shot window.
The cover rule is checked before spending a cooldown or starting a gesture.
Active attacks can finish after moving. Operative's shared Cull remains mobile.
Quickbar summaries and detailed tooltips show the Sniper-specific requirement.

## Repeatable packages

All percentages add from the ability's base value per rank. Damage includes
periodic damage and every existing channel/area tick. Recovery increases rate
rather than subtracting a percentage of cooldown. Packages are Premium and uncapped.

| Power | Ability | Damage | Radius | Recovery |
|---|---|---:|---:|---:|
| High Caliber | Ambush | 80% | — | 12% |
| Sustained Barrage | Series of Shots | 80% | — | 10% |
| Finishing Salvo | Followthrough | 80% | — | 12% |
| Saturation Fire | Suppressive Fire | 80% | 15% | — |
| Heavy Ordnance | Orbital Strike | 80% | 12% | 15% |
| Plasma Saturation | Plasma Probe | 80% | 15% | — |
| Overcharged Payload | Explosive Probe | 90% | 12% | 10% |
| Interrogation Matrix | Interrogation Probe | 80% | 12% | 10% |
| Concentrated Toxin | Corrosive Dart | 100% | — | 10% |
| Corrosive Cloud | Corrosive Grenade | 100% | 15% | — |
| Lethal Barrage | Cull | 100% | 12% | 10% |
| Exploited Weakness | Weakening Blast | 100% | — | 15% |
| Fragmentation Cluster | Fragmentation Grenade | 80% | 15% | 10% |
| Close Quarters Payload | Overload Shot | 100% | — | 15% |

## Combination powers

Each has one rank. Every dependency must be owned for the power to be offered
and for its effects to operate. Replacement suspends combinations that lose a
required ability.

| Power | Effect |
|---|---|
| Crossfire Discipline | Ambush recovers Followthrough by 2s; Followthrough recovers Ambush by 3s, once per activation. |
| Penetrating Rounds | Ambush, Series and Followthrough gain two ricochets and retain at least 90% damage per jump. Stacks with Focused Barrage. |
| Targeted Suppression | Suppressive Fire and Plasma Probe gain 60% damage against victims of owned Interrogation Probe. |
| Coordinated Bombardment | Suppressive Fire activations recover Orbital Strike by 4s; Orbital gains 30% base radius. |
| Probe Overload | Explosive Probe consumes each victim's owned Interrogation Probe for its remaining scheduled damage, then reapplies to survivors. Unmarked survivors receive a probe. Foreign probes are untouched. |
| Overclocked EMP | EMP adds 6s recovery to learned Explosive Probe and Orbital Strike, alongside existing EMP/talent effects. |
| Probe Targeting | Series and Explosive Probe gain 50% damage against victims of owned Interrogation Probe. |
| Concentrated Volley | Cull gains 50% radius and full secondary damage. Its activation empowers the next Weakening Blast within 6s for 100% additional damage, consumed once for the whole activation. |
| Zeroing In | In cover, Dart/Grenade tick 35% faster, including running effects and spread copies, without shortening duration. Leaving cover restores normal frequency. |
| Virulent Weakness | Weakening Blast echoes one tick of each owned Dart/Grenade on its victim without consuming or refreshing them. |
| Contaminated Rounds | Ambush applies learned Dart to unpoisoned victims; against owned Dart, it echoes half a poison tick without consuming or refreshing it. Ricochets handle their own victims. |
| Entrenched Salvo | Entrench recovers Series/Suppression by 3s; its six-second window adds 40% damage to Ambush, Series, Followthrough and Suppression. |

Probe Overload appears by name in the damage meter. Its secondary damage bypasses
ability proc hooks and consumes the existing periodic effect before applying
damage, using the same ownership-safe service as Acid Blade.

Sniper's ricochet contacts now enter the existing per-ability hit hooks, just as
its area contacts do. This also allows existing core-shot talents such as
Deployed Cover and Electrified Railgun to work on those chain contacts.

## Separate Sniper Lethality tree

Operative retains its nine existing Lethality talents and effects. Sniper has
nine separate definitions in the same visible Lethality tree, emphasizing cover,
ranged poison delivery and the Cull/Weakening Blast cycle.

| Sniper talent | Effect |
|---|---|
| Prepared Toxins | Dart lasts 2s longer; Dart/Grenade gain 20% additional damage in cover. |
| Corrosive Payload | Grenade gains 30% radius and 15% base damage. |
| Cull Discipline | Cull gains 15% base damage and 15% recovery rate. |
| Hemotoxic Rounds | Weakening Blast extends each victim's owned poisons by 2s, capped at normal duration +4s. |
| Lethal Alignment | In-cover poison ticks recover Cull by 0.75s and Weakening Blast by 0.5s, with a shared 2s cooldown. |
| License to Kill | Cull gains 35% base damage. |
| Toxic Precision | Each Cull contact echoes 25% of one tick of each owned Dart/Grenade without consuming them. |
| Targeted Contagion | Weakening Blast strikes a 0.30-radius group at full damage and spreads owned Dart/Grenade, excluding probes. |
| Toxic Focus | In-cover Cull grants an 8-health shield, capped once per 6s. Weakening Blast recovers Shield Probe by 1s, capped once per 2s. |

The shared `agent_lethality` tree identifier remains for UI grouping and offer
affinity. Sniper talent IDs use `sniper_lethality.*`; old Sniper profiles migrate
from `agent_lethality.*` by the same suffix/position before validation. All nine
saved selections and their point cost are retained; Operative profiles are untouched.
Recovered icons retain their provenance, with the new effects labelled as TorBonk
adaptations. Run `scripts/refine_sniper_lethality.py` after older Agent generators
and refinements to reproduce the catalog split; it is idempotent.

## Offers and opening kit

Sniper shares Assassin's offer policy: prioritize a usable interaction and a
scoped damage package when slots/candidates allow, prefer different abilities,
and reduce the weight of repeatedly chosen ranks. Existing talent-tree affinity
still guides both unlocks and powers; hybrids remain eligible.

Health and speed appear only every fourth level, at most one per offer, with
two ranks each. With only Snipe equipped, three offensive unlocks replace the
old starter-training filler. A kit with one acquired core attack gets that
attack's package and extra unlocks as needed to keep three choices. Invested
trees retain their guaranteed early core choices; banish/reroll exclusions apply.

## Validation and balance budget

Run `./pipeline/Run-Sniper-Run-Powers-Smoke.ps1`. The isolated test covers 240
seeded offers across starter, Marksmanship, Engineering and Lethality kits,
actual casts, all combinations, ownership, multi-target probe detonation,
replacement, expiry, restarts, and uncapped additive ranks. Lethality checks cover
legacy profile migration, class isolation, actual Weakening Blast spread, poison
extension caps, bounded shields/cooldown feedback, and leaving/reentering cover.

In automatic 60-second rotations against six stationary targets, three scoped
ranks (one per listed ability) were compared against three +20% global ranks.
Both retain baseline Snipe/Rifle Shot; neither uses the new combination powers
or persistent talents. These are upgrade-budget comparisons, not full-run parity.

| Kit | Three global ranks DPS | Three scoped ranks DPS | Change |
|---|---:|---:|---:|
| Ambush / Series / Suppression | 327.03 | 339.19 | +3.7% |
| Interrogation / Explosive / Plasma | 401.53 | 409.41 | +2.0% |
| Dart / Grenade / Cull | 181.39 | 183.73 | +1.3% |

Receipts: `manifests/sniper-run-powers-smoke.json`, existing Agent rules/cover
receipts, and the Assassin run-power receipt. Full-run feel, movement-heavy fights,
wide enemy spacing and larger mixed kits still need gameplay review.
