# Assassin combat and synergy revision

## Loadout

Assassin has six learned damage positions plus one free starting position occupied by Lacerate. Lacerate can be replaced through the normal full-loadout replacement flow; its replacement inherits that free position. A traded Lacerate becomes a paid damage ability if learned again, preventing duplicate free positions. Reset restores the original free Lacerate and removes inherited flags.

Force Shroud, Force Speed, Force Cloak, Deflection and Low Slash are utility/control abilities outside the damage budget. Low Slash deals modest incidental damage (6), rather than consuming a damage position. Up to seven damage abilities and five utilities fit in the existing twelve-button quickbar. Tooltips explain these rules. Sorcerer budgets, slotless abilities, behavior and saved talents remain unchanged. Banish/Reroll/Skip, class eligibility and normal replacement controls remain in use.

## Starting balance

Lacerate: 12 base damage, 2.4-second cooldown, 0.24 radius. Previously its runtime tuning was 18 damage every 2 seconds. It is a modest filler/sweep and Discharge spreader rather than the preferred first action ahead of every ready core attack.

Spike: 24 base damage, 16-second cooldown, 0.22 radius. Previously 54 damage, 12-second cooldown, 0.4 radius. Darkswell can still increase its damage/radius through investment. Spike has a one-second vanish before the hit. Its brief stun, control lock and boss restrictions remain.

## Visual fixes

Spike now hides the actual character presentation subtree, including the attached weapon, and restores its prior visibility on reveal, interruption, offensive cloak break, expiry or reset. This replaces mesh-transparency-only stealth, which could leave opaque/custom materials visibly rendered. Gameplay cloak avoidance/pursuit behavior remains brief and bounded. Particles outside the character subtree remain visible during the vanish.

Wither now uses dedicated ground and attached-target emitters, rather than placing every recovered texture in one billboard burst. The selected April resources are `si_wither_debuff_target` and its `si_wither_dark_whisps`, `_aoe`, `_aoe_core`, impact rings and shockwave particle dependencies. An expanding horizontal shockwave accompanies dark wisp variants that rise around affected targets through the four-second weakening window. Atlas cells are treated as separate wisp variants rather than a rapid four-frame flash. Emitters follow their targets and expire on death, timeout or release. Source artwork remains original; the emitter implementation is a bounded Godot adaptation, not a full native FX runtime.

## Baseline interactions

- Lacerate spreads owned Discharge to additional enemies hit, even without a spreading talent. It preserves existing periodic tick timing. The Madness spread talent extends this to other owned DoTs.
- Shock echoes 25% of the scaled tick damage of existing owned DoTs, without consuming or extending them. Raze adds its existing 60% echo bonus.
- Voltaic Slash retains two charges for Shock (20% each), and also grants Discharge 10% damage per active charge.
- Death Field's six-second exposure grants 10% increased periodic damage at baseline. Deathmark adds 20% through the talent.
- Wither keeps its four-second 25% enemy attack weakening; affected enemies also take 10% more damage from Lacerate, Voltaic Slash and Maul.
- Ready Maul gets priority during a stealth opening; ready Shock gets priority while charged. Other ready core abilities precede Lacerate filler. These are opportunistic priorities, without tree locks or a mandatory fixed rotation.

Healing rates, cooldowns, control locks and defensive uptime limits are retained.

## Verification

`Run-Assassin-Refinement-Smoke.ps1 -Review` passes 73 baseline, 74 Madness and 74 Deception checks. Coverage includes actual hidden presentation visibility, interrupt/reveal restoration, Wither's attached/ground emitters and expiry, reduced burst tuning, baseline Discharge spreading, non-consuming Shock echoes, Voltaic/Discharge and Death Field/DoT interactions, all twelve concurrent abilities, free-position transfer, prevention of duplicate free positions, reset, automatic Maul priority, early offers and Banish/Reroll/Skip. Rendered stealth and Wither screenshots were inspected.

Sorcerer baseline/refinement and Assassin periodic-effect regression suites are retained. Long-run difficulty and damage balance still require playtesting.
