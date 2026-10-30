# Sniper spec identity and Concealment cleanup — 2026-10-29

Verified the current Sniper implementation and removed Carbine Burst from Concealment. Bounty Hunters were not changed.

## Sniper findings

All three specs have implemented identities in talents, preferred ability acquisition, repeatable scoped power cards, and mechanical run-upgrade cards. Each has nine talents. Spec investment biases offers; it does not permanently lock out hybrid choices.

| Spec | Identity | Legal four-offensive core | Examples of mechanical run upgrades |
| --- | --- | --- | --- |
| Marksmanship | Covered precision fire, ricochets, firing-chain recovery | Ambush, Series of Shots, Followthrough, Suppressive Fire | Crossfire Discipline, Penetrating Rounds, Entrenched Salvo |
| Engineering | Probe marks, remaining-damage detonation/reapplication, persistent area damage, EMP recovery | Interrogation Probe, Explosive Probe, Plasma Probe, EMP Discharge | Probe Overload, Targeted Suppression, Probe Targeting, Overclocked EMP |
| Lethality | Owned poisons, Cull echoes, Weakening Blast extension/spread, covered tick/recovery benefits | Dart, Corrosive Grenade, Cull, Weakening Blast | Concentrated Volley, Zeroing In, Virulent Weakness |

Snipe remains the free starter and Rifle Shot the basic fallback. Utility choices fit separately within two slots. EMP Discharge is an **offensive** attack: Engineering must trade another attack to add Orbital Strike; all five are not simultaneously free. Bombardment and contaminated-shot builds are alternatives/hybrids rather than mandatory additions to the cores above.

The current run-upgrade pool has fourteen repeatable scoped damage packages and twelve one-time mechanical interactions. Offer weighting follows the invested tree and owned-ability combination checks prevent drops for unusable interactions. The identity tests check actual generated three-card offers for twelve seeds per spec under legal four/two loadouts, including an owned power upgrade and an invested-tree mechanic.

The important gap is equipment: the accessories catalog has no dedicated Sniper/spec relic layer. Its explicitly Sniper-eligible item is the shared Skill Implant. Therefore the three identities are supported by **talents and run-upgrade rewards**, not by three complete sets of spec-specific equipment/relic drops. No new Sniper relic system was invented as part of verification.

Source paths: `Gameplay/Talents/agent.json`, `Talents/AgentRuntime.cs`, `Talents/AgentProgression.cs`, `Talents/SniperRunPowers.cs`, `RunProgression.cs`, and `Equipment/accessories.json` beneath `pipeline/godot/`.

Two Engineering probe tooltips incorrectly repeated the EMP description. Interrogation Probe and Explosive Probe now describe their own DoT/detonation role and the existing probe interactions.

## Concealment change

The requested blaster AoE is **Carbine Burst**. Any Operative build with Concealment investment now excludes it from acquisition and upgrade offers, cannot activate it, and disables a legacy/debug-equipped copy during recalculation. It is removed from Concealment's early core and ability affinity. It remains available to Operatives without Concealment investment; its shared assets are preserved.

Collateral Strike keeps both Laceration swipes' 50% cleave-radius bonus and moves the removed Carbine damage bonus to Laceration (+30% base damage). Culling keeps its existing +40% Laceration damage and +50% controlled-target bonus, now exclusively for Laceration. Stable talent IDs and costs are retained, preserving saved allocations. Tooltip associations and the existing control fixture were updated accordingly.

## Verification

- New spec identity suite: **75 Sniper checks passed**. Validated all nine talents per tree, legal core acquisition under four/two limits, distinct early acquisition, at least three usable mechanical rewards per core, spec-weighted rewards, generated-offer quality, and representative runtime talent effects.
- New Concealment suite: **32 checks passed**, including no Carbine acquisition/upgrades/firing, removal of legacy ownership, redirected Laceration bonuses, twenty-four generated offer seeds without Carbine, and retention of the non-Concealment option.
- Build passes with existing nullable/obsolete warnings and unavailable NuGet vulnerability metadata.
- Initial broader Sniper execution reached receipt writing but could not save because D: was full. Only disposable profiles created in this conversation and a reproducible intermediate bank XML dump were removed to recover enough room. Extracted source assets, archives, and provenance manifests were preserved. The final broad-run result is recorded below after completion.
- Final Sniper run-power/combat/Lethality regression: **772 checks passed**, including mechanical interactions, scoped damage growth, offer quality, poison ownership, live periodic effects, cover behavior, and equal-pick damage-budget comparisons.

Run the new checks with `pipeline/Run-Agent-Spec-Identity-Smoke.ps1`. `-ProfileRoot` can place disposable profiles on a different writable volume when the project drive is short of space.
