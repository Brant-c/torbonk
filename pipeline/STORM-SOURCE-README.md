# Force Storm — original lightning layers and moving casting

The simplified three-bolt visual has been replaced with the recovered April
Force Storm effect composition. The existing green terrain indicator, damage,
ticks, cooldown, GCD, fixed area and audio remain unchanged.

The subsequent full-visual pass adds the supporting caster particles and light
that were missing from the first lightning-only pass. Every drawable entry in
the four recovered Storm FXSpecs is now accounted for in
`manifests/storm-visual-coverage.json` (51 lightning, emitter/dummy and light
entries). This is coverage of the recovered visual layers, not an assertion of
pixel-identical native rendering.

`OriginalStormLightning.cs` reads the already-extracted
`Assets/Abilities/fx.json`. It uses:

* All five lightning entries in `si_forcestorm_lightningstrike`, including
  their 0/0.05/0.1/0.2/0.3-second delays, original widths, envelopes, longitudinal
  shapes, opacity curves, texture tiling and dummy endpoints.
* The four caster lightning entries in `si_forcestorm_cast`, with original
  left/right weapon, root and overhead dummy attachments.
* `si_forcestorm_caster_groundbolts_01`, the original ground flashes and the
  recovered hand/elbow electricity textures.
* The actual `4x1_tiling_electrical_bw` artwork and Storm's four-key
  `_fxTextureHueRamp`, replacing the shared Force Lightning recoloring.
  Each bolt samples one of the sheet's four alternate frames rather than
  compressing the entire sheet across a single ribbon.
* All 13 visible caster emitters: left/right ground smoke and glows,
  chest and both elbow tatters, both bicep/hand electricity emitters, and
  left/right spark trails. The two original strike flash/glow emitters also use
  their recovered particle settings rather than hardcoded glow substitutes.
* The source `FrontLight`, including its 0.2-second color cycle, range and
  intensity, restricted to the caster with a temporary render-layer mask.

`OriginalStormParticles.cs` reads the existing PRT parameters, respecting group
and entry delays, source count/rate, lifespan, per-life color and size curves,
texture-frame bitmasks, source hue ramps, alpha/additive blend modes, rotations,
trajectories and linked hand attachments. The delayed overhead `FirstStrike`
group now starts at its source 0.75-second cue. Invisible black driver particles
remain invisible; their child-FXSpec bolts are represented by the strike scheduler.

The existing `manifests/abilities-source.json` and
`Assets/Abilities/source-parameters.json` retain source paths, entry hashes,
FXSpec/PRT fields and original dependencies. Strike groups use the recorded
0.25-second FXSpec emission cadence and one-second particle lifespan. Endpoints
are terrain-probed and follow original skeleton sockets while moving. No
extraction, asset reconversion, source-JBA edit or archive hashing was needed.

The original `B5/StormStart`, `B5/StormLoop` and `B5/StormEnd` clips remain in
use. `MaintainChannelGesture` keeps the original channel pose for the duration
of the Storm; `GesturePriority` allows it to blend over a shorter offensive
gesture. This affects presentation only: other abilities, shields and healing
still activate independently. The existing upper-body masks preserve the
original running leg tracks, including root, pelvis, hips, knees and ankles.
The jump animation work already present in the checkout is preserved.

The original native `zap.zzp` renderer is unavailable in Godot. Ribbons render
the recovered parameters/artwork; curve easing, randomized dummy rotation and
emitter scheduling remain bounded adaptations. This is not a claim of exact
native engine reproduction. The previous arbitrary bolt heights, one-layer
width clamp and shared hue ramp are no longer used for Storm.
Texture-sheet playback currently advances at an authored 20 frames/second.
Particle distribution bias/easing, unit conversion for particle motion/size,
native light falloff/fade flags and invisible child-emitter nesting remain
documented approximations. Original source art and JBA files are unchanged.

Run the focused build/smoke with `Run-Storm-Source-Smoke.ps1`. Its receipt is
`manifests/storm-source-smoke.json`, log is `logs/storm-source-runtime.log`,
and single review frame is `generated/storm-source/moving-storm.png`. It checks
the original run-cycle leg poses against a second uncast rig, sustained casting
while moving, all 13 caster emitters plus two strike-flash layers, source light,
layered source lightning, independent attack damage and complete cleanup.
Launch normally with `Open-Survivors.ps1` and unlock Force Storm through leveling.
