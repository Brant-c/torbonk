# Sniper and Concealment pass — 2026-10-29

This pass is a TorBonk balance adaptation using recovered SWTOR artwork, geometry,
animations and sound. It does not claim to reproduce original MMO balance.

## Sniper

Cover is grounded stillness for 0.6 seconds. Snipe, Ambush, Series of Shots,
Followthrough, Suppressive Fire and Plasma Probe require cover at activation.
Rejected casts consume neither a cooldown nor a gesture. Followthrough additionally
requires its existing precision-shot window. Rifle Shot, grenades, poison and
defensive actions remain available while repositioning.

Base damage: Snipe 48, Ambush 110, Series of Shots 27 per tick, Followthrough 65,
Suppressive Fire 23 per tick. Existing +15% positioning damage and applicable
talents remain in effect. These are initial tuning values, not measured parity.

Suppressive Fire marks a fixed area and can finish after leaving cover. Plasma
Probe is now a true persistent ground area: 24 damage per second for eight
seconds, not a target-attached DoT. Its original top/bottom probe geometry,
flame atlas and deployment audio play at that location. Both areas use the same
recovered Imperial emblem / green projector as Force Storm, including terrain
projection and cliff/hole clipping. Removal, interruption and restart release FX.

## Concealment

- Laceration: base 68 damage, 0.34 cleave radius, 2.6-second cooldown, full damage
  to every target in its cone. Tactical Advantage adds 50% damage when spent.
- Sever Tendon: 30 damage in a short knife cone, 70% slow for five seconds,
  seven-second cooldown. Available in Operative unlock choices.
- Carbine Burst: 25 damage every 0.3 seconds for 1.2 seconds in a short cone,
  six-second cooldown. Available in Operative unlock choices.
- Mass Debilitation replaces Advanced Cloaking: Debilitate becomes an uncapped
  0.55-radius attack centered on the player. Three-second stun; six-second mark
  adding 40% outgoing damage against affected targets. Bosses retain their stun
  immunity but still receive the mark. Debilitate now occupies an offensive slot.
- Counterstrike Training replaces Flanking: grants slot-free Blow for Blow,
  automatically activating near an enemy. Four-second window, 20-second cooldown;
  returns 150% of actual health loss to the original attacker. It does not prevent
  damage. Fully absorbed hits return nothing; partially absorbed hits return only
  health loss. Reflections bypass the enemy attack entry point and cannot recurse.
- Collateral Strike: +50% Laceration radius, +25% Carbine Burst radius and +30%
  Carbine Burst damage.
- Culling: +40% Laceration damage; Laceration and Carbine Burst gain another 50%
  against stunned targets or targets bearing Mass Debilitation's mark.
- Revitalizers remains intact. Existing internal talent IDs retain saved point
  allocations in their positions, with updated names, descriptions and effects.
  Concealment allocations prioritize the new control/burst abilities in early offers.

## Laceration source correction

Client-derived chain retrieved using the read-only SWTOR research skill:

1. `abl.agent.skill.concealment.laceration` (`16140940446259487977`),
   `ablEffectIDs` includes `0xE000FA74136A8F3F`.
2. That effect (`16141176440944758591`),
   `/effSubEffectEppDetails/1/effSubEffectEppSpec` =
   `epp.agent.laceration.cast_instant`.
3. Appearance (`16141066326246217900`),
   `/effAppearanceSchedule/0/effAppearanceActions/0/effAppStringParams` selects
   `cb_rifle_disk_left_1`; action 2 selects `abilities/spy/sp_lacerate_knife_cast`.
4. The exact April JBA was uniquely recovered and converted on the established rig.
   `agent-rework-source.json` records its original bytes/hash; the output hash and
   source timing are in `agent-animation-conversion.json`.

The separate `new_instant` prototype references a different recoverable animation;
the live ability chain above decides the final binding. The old Shiv fallback is
gone. The recovered knife FX supplies LeftWeapon, 180-degree geometry rotation,
0.011 offset, green trail colours and original white trail-fan texture. The
existing dedicated Laceration impact event remains distinct from Backstab audio.

Native Hero emitter scheduling, material execution and Wwise container selection
remain Godot adaptations. Sever Tendon uses compatible recovered Shiv motion;
Carbine Burst uses recovered rifle full-auto start/loop/end. Blow for Blow reuses
recovered countermeasure artwork. These are explicitly adaptations, not newly
generated character animation or proof of exact native playback.

## Verification and regeneration

`Run-Agent-Expansion-Smoke.ps1` checks both class kits, all presentation bindings,
talent combinations and cover. Added checks cover rejected casts, persistence,
Imperial markers, new group attacks, boss-compatible damage marks, reflection
after absorption, expiry and reset. Rendered presentation checks with
`--agent-capture` save previews in `manifests/agent-<class>-<ability>.png`.

Final results: Operative rules 111, Sniper rules 100, Operative presentation 82,
Sniper presentation 61, cover 21: **375 passing checks**. Final build has zero
errors; existing nullable/obsolete warnings and offline NuGet advisory warnings
remain. Both rendered capture runs and both final rules runs contain no runtime
errors. Existing map-resource UID fallback and engine blend-point warnings remain.
Visual captures were inspected for the restored Laceration and plasma area.
Long-run damage parity and subjective combat feel still need gameplay review.

Apply `scripts/agent_rework_data.py` after legacy Agent generation scripts, then
convert the Agent animation library using `convert_agent_animations.py -- --agent`.
The existing source extraction receipts and other dirty workspace work are retained.

## Follow-up: Laceration audio and Explosive Probe presentation

The original Laceration Wwise graph layers three actions: a random contact layer
delayed by 200ms, a random slash layer, and a fixed main impact. The prior binding
played only the quiet contact file `142346317.wav`. A playing-voice assertion
therefore passed while omitting most of the actual event.

`scripts/mix_agent_repair_audio.py` now renders a representative selection from
all three recovered layers, with the source delays and relative gains. The main
impact is `213894810`, the slash selection `355335624`, and the delayed contact
`142346317`. No sound is synthesized. The finite PCM cue is 0.986 seconds long;
its RMS is 0.0792 versus 0.0237 for the old lone file. Per-voice Laceration gain
is -4dB, matching the effective level of the other knife attacks. Other ability
gains and user volume controls are retained. Random container history and native
bus processing remain adaptations. Original graph/byte receipts are retained in
`agent-audio.json`; layer hashes and clipping protection are recorded in
`agent-layered-audio-repair.json`.

Explosive Probe previously had only an unrelated generic explosion billboard and
empty audio bindings. `AgentExplosiveProbePresentation` now flies the recovered
`vfx_probe_mini01` toward the target with a red pulse, then emits the source FX's
short flash core, red expanding ring, 22 sparks, seven debris pieces and dirt/smoke.
It uses original `trail_misc_03`, `2x2_flares`, `bw_fx_2x2_sparknglow_01`,
`shrapnel_01`, `glow_ring_thin_innergrad_01` and `2x2volumesmokegradient_blur2`
textures. Deployment is bounded by windup; target loss/interruption/restart
cleans it up. One activation produces one burst and one explosion cue regardless
of target count. Damage, targeting and cooldowns are unchanged.

Original `sp_explosiveprobe_probe`, `_planted` and `_explode_02` confirm the model,
red pulse, particles and event names. Their exact extracted bytes are recorded in
`agent-probe-repair-source.json`. `recover_agent_probe_audio.py` recovers the
deployment pulse and layered explosion directly from the April ranged bank;
HIRC/DIDX offsets, event graphs and original media hashes are in
`agent-probe-repair-audio.json`. The layered explosion PCM retains representative
choices from all four random groups, with a measured 4.28dB peak reduction to
prevent clipping. Its in-game per-voice gain is -10dB.

Presentation checks now verify the exact audio cue/gain, deployment before impact,
multi-layer explosion and absence of duplicate detonations. With
`--agent-audio-capture`, the check drives actual ability casts and records the
combat bus through Godot's AudioEffectCapture, asserting audible, non-clipping
PCM. Measurements are saved in `agent-<class>-audio-output.json`; captured output
is saved in `agent-<ability>-combat-output.wav`. This tests actual mixed output,
rather than only checking that an audio voice reports Playing.

Follow-up validation: Operative presentation/audio 85 checks, Sniper
presentation/audio 67 checks, all passing. Actual Laceration combat-bus output
measured RMS 0.0186 / peak 0.244; Explosive Probe RMS approximately 0.029 / peak
0.176, both below clipping. Rendered deployment/detonation previews are isolated
from earlier abilities' lingering effects. Build succeeds with the existing
seven warnings and no errors. This follow-up changes presentation only.

## Two-swipe Laceration and talent-directed Agent offers

Following the requested two swipes on every cast, Laceration now has two actual
contacts at 0.2 and 0.8 seconds, each dealing 34 base damage and playing its own
layered impact cue. Total base damage stays 68 per activation. Both contacts share
one Tactical Advantage spend and its damage bonus. Interruption cancels the
remaining contact; tick acceleration cannot add extra contacts. The gesture owns
the full 1.2-second sequence. Two copies of the recovered Laceration animation are
joined using the existing original-animation composition helper, then fitted to
that duration. Two trails and separate overlapping audio voices match the two
contacts. This requested sequence is a TorBonk adaptation; the April base cast and
separate proc appearance do not establish two unconditional original MMO hits.

Collateral Strike remains at its existing position in Concealment and retains
saved allocations. Its description now explicitly states that the +50% cleave
radius applies to both swipes, alongside its Carbine Burst bonuses. The previous
talent only changed stats and could not trigger another animation or sound.

Sniper previously forced Suppressive Fire and Ambush into early offer slots
regardless of talent allocation. Agent progression now follows the same pattern
already used for Marauder and Juggernaut: invested talent trees guide opening
abilities and increase the weight of related later offers.

| Investment | Preferred opening kit |
| --- | --- |
| Marksmanship | Suppressive Fire, Ambush, Series of Shots, Followthrough |
| Engineering | Interrogation Probe, Explosive Probe, Plasma Probe, EMP Discharge, Orbital Strike |
| Lethality | Corrosive Dart, Corrosive Grenade, Cull, Weakening Blast |
| Concealment | Laceration, Debilitate, Carbine Burst, Sever Tendon, Backstab, Hidden Strike |

Levels 2–3 reserve up to two eligible core unlocks; levels 4–6 reserve one and
leave more room for upgrades. Owned, banished, excluded and conflicting abilities
retain their normal eligibility rules. Tied talent investments interleave both
cores. Off-tree options keep nonzero weight for hybrids. Blank talent profiles
receive a varied offensive opening instead of being forced into Marksmanship.
Later related choices gain weight `1 + 0.4 * invested points + 0.1 * direct affinity`.

Validation covers actual generated offers for five Agent tree/class combinations
across twelve seeds each, reroll exclusions, later upgrade space, hybrid ties and
blank profiles. Sequence checks verify two damage contacts and sounds, unchanged
total damage, one resource spend, haste safety and interruption cleanup. Final
results: Operative rules 199, Sniper rules 219, Operative presentation/audio 86,
Sniper presentation 65, cover 21: **590 passing checks**. Real Laceration output
contains both contact events and remains below clipping (RMS 0.0261, peak 0.244).

## Core-ability refinement and distinct follow-up

Laceration's requested two-contact sequence now joins two different recovered
motions: `cb_rifle_disk_left_1` and `cb_rifle_attack_1`. The latter is selected by
`epp.agent.laceration.proc` (`16141145342582886653`) at
`/effAppearanceSchedule/0/effAppearanceActions/0/effAppStringParams`. Its impact
appearance points to `abilities/trooper/tr_stockstrike_impact`; the apparent
Trooper name is the original reference. The second motion is retimed to the first
motion's length before composition, preserving two equal playback segments. The
knife is hidden and the equipped rifle restored for the original follow-up motion.
Source bytes are recorded in `agent-core-refinement-source.json`; conversion
timing/output hashes are in `agent-animation-conversion.json`.

The second impact uses a distinct finite original-media mix, combining an alternate
recovered slash with recovered contact samples. The exact native follow-up sound
event is not established by the appearance's dummy audio reference; this cue is a
labelled compatible adaptation, not a claimed exact native event. Its composition
and hashes are in `agent-layered-audio-repair.json`. The first motion's original
white trail texture now follows actual blade geometry as short fading ribbons,
rather than appearing as a rectangular billboard. Both contacts retain their
existing damage/timing; the follow-up no longer repeats the first motion or cue.

Operative admission now gives the requested relative priority:
Debilitate > Backstab > Laceration > Shiv > Corrosive Dart. Hidden Strike retains
an opening-only position between control and Backstab; other equipped attacks
fit ahead of the two fillers. Concealment's early learning preferences now start
with Debilitate and Backstab. Manual Dart selects a target with no poison or at
most two seconds remaining instead of repeatedly refreshing fresh poison.

Venomous Blades replaces Revitalizers at the same saved talent ID/position. It
replaces the starting manual Dart with Laceration at the same offensive slot cost.
Each living target hit by Laceration receives the full owned Dart poison, using
the retained hidden source without another slot or cast. Subsequent contacts
refresh that single poison without restarting its tick clock. Hidden-source
poison remains valid through periodic ticks and applicable spread mechanics.
The Dart unlock is excluded while this talent is active. Replacement runs through
the ordinary recalculation/restart path; it does not re-equip a traded Laceration
on every stat update.

Other starter-focused talents now support the core kit:

- Meticulously Kept Blades buffs Backstab, Hidden Strike and Laceration; its extra
  cleave belongs to Backstab instead of Shiv.
- Tactical Opportunity uses core knife activations for openings and lets Backstab
  generate Tactical Advantage, reducing reliance on Shiv.
- Acid Blade applies poison through Backstab even without a separately equipped Dart.
- Focused Barrage replaces Sector Ranger's Snipe/Rifle Shot upgrades with two extra
  Series of Shots ricochets and +30% Suppressive Fire area.
- Entrenched Firepower's cover bonus applies to Ambush, Series of Shots,
  Followthrough and Suppressive Fire; Deployed Cover also triggers from those core
  attacks instead of Snipe.

Interrogation Probe now displays its recovered drone with three independently
pulsing electric links to the target, using its exact
`4x1_tiling_electrical_white_01` texture. The native Engine zap primitive is adapted
to textured Godot ribbons. Its original sound is retained. Refresh, removal,
target death and caster death remain owned by the periodic effect lifecycle.
The preview uses a visible humanoid as the status-effect recipient.

Run `scripts/refine_agent_core.py` after the older Agent data-generation scripts;
run `mix_agent_repair_audio.py` for both distinct impact cues. Stable talent IDs
preserve saved point allocations. Validation includes actual controller admission,
slot-neutral replacement, locked-source poison ticks, multi-target application,
starter-free talent effects, different original animation poses/audio cues,
probe links and rendered captures. Final receipts are recorded in
`agent-core-refinement-validation.json`.

## Acid Blade poison detonation

Acid Blade now makes each Backstab victim's existing caster-owned Corrosive Dart
burst for its remaining scheduled poison damage, then applies a fresh full-duration
Dart if the target survives. An unpoisoned target only receives the Dart. Other
casters' Darts and other poison types are untouched. The burst is target-specific;
Backstab's cleave independently handles every victim without multiplying explosions
onto neighboring targets. Venomous Blades' Laceration application/refresh remains
unchanged and can prime targets for Acid Blade.

The periodic service consumes the exact owned effect before publishing burst
damage, preventing death reactions or other hooks from consuming it again.
Remaining damage uses the saved tick amount and remaining tick schedule with the
same current damage modifiers as normal poison. The new Dart restarts its tick
clock and has only one instance. Burst damage is credited as Acid Blade in the
ability meter. Lethal bursts do not reapply poison to corpses.

Every detonating target gets a green burst using recovered SWTOR explosion artwork;
one recovered corrosive sound cue plays per Backstab activation. Checks cover
first application, partially spent poison, full reapplication, fresh tick timing,
expired poison, ownership isolation, other poison types, lethal bursts, talent
gating and actual multi-target Backstab damage. Gameplay and presentation receipts
are recorded in `acid-blade-validation.json`.
