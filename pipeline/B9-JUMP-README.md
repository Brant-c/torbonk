# B9 follow-up — original SWTOR jump animation

Launch normally with `Open-Survivors.ps1` or `Open-Gameplay.ps1`, then press
Space while grounded. The existing jump height, gravity, horizontal movement,
collision and terrain safety rules are unchanged by this animation follow-up.

The original April male/body-type-2 single-lightsaber clips now drive the current
Sorcerer rig. Physics tells presentation when a jump takes off, starts falling
and touches the ground. Takeoff blends into airborne/falling poses, followed by
the original standing or running landing. Landing never waits for an animation
timer to decide whether the player is grounded. Starting to move during a
standing landing, or stopping during a running landing, blends back to ordinary
locomotion immediately. Another grounded Space press can interrupt landing.

The jump is a locomotion branch beneath the existing waist/chest/upper-body
casting layers. A stationary airborne cast no longer substitutes the full-body
standing cast over the jump legs. Existing Sorcerer bone masks, weights, gesture
ownership and animation timing remain. Hit/death reactions retain priority;
death/restart clear jump playback. Pausing freezes movement and animation.

## Original asset evidence

All paths below are under `/resources/anim/humanoid/bmnnew/` in the operator-labelled
April `swtor_main_anim_humanoid_bmns_1.tor`. Historical names were used for discovery;
each exact path matched one April hash entry, and decoded bytes were verified by
the existing read-only TOR pipeline and research MCP. These are actual original
single-saber locomotion clips, not the small `ad_*` additive decorators. The Godot
phase selection is an adaptation; the native Morpheme graph was not reproduced.

| Godot clip | Original JBA | TOR lookup hash | Source duration |
|---|---|---|---|
| Takeoff | `mv_saber_jump_start.jba` | `0x1E277FCF25685FA0` | 0.3 s |
| Airborne | `mv_saber_jump_mid_idle.jba` | `0x0BC697FA7DB96A88` | 2.1333 s |
| Falling | `mv_saber_fall.jba` | `0xE471E0169A8D32AE` | 2.1333 s |
| LandIdle | `mv_saber_jump_end.jba` | `0x5CF4381F0529515B` | 0.3 s |
| LandRun | `mv_saber_run_jump_end.jba` | `0x1AB84AF50C27A55B` | 0.7 s |

All five sources are 30 fps. `manifests/b9-jump-source.json` records exact archive
offsets, lengths and decoded SHA256s. Original bytes remain in ignored
`source-assets/april2012/`. No archive hashing/regression sweep was repeated.

`scripts/prepare_b9_jump.py` extracts only these five clips.
`scripts/convert_ability_animations.py --jump` uses the existing JBA importer,
Blender rig and 60 fps GLB sampling convention. It preserves the original bone
poses, axes and source clip endpoints. `godot/Assets/Jump/jump.glb` supplies a
separate `Jump` animation library; `Assets/sorcerer.glb`, character meshes and
equipment are not replaced. A retained original skinned mesh establishes joint
identity in the library GLB; its temporary imported scene is freed after copying
animation tracks, so it is never added to the visible character.

Tracks are rebound only to matching existing skeleton bones. The JBA world
trajectory is not applied and Godot root motion remains disabled: the capsule
and physics remain the sole owners of character displacement.
`manifests/b9-jump-animation-conversion.json` records generated output SHA256,
source durations, frame rates and conversion policy.

## Timing and tuning

CharacterPresentation exports:

- `JumpBlendSeconds = 0.06`: short transition between normal and jump locomotion.
- `TakeoffAscentFraction = 0.5`: fit the complete takeoff pose sequence into the
  first half of the configured physics time to apex (about 0.153 s with the
  current jump height/gravity). This retiming aligns the original 0.3 s clip
  with the shorter Survivors jump without delaying Space or changing physics.
- `FallAnimationGraceSeconds = 0.08`: ignore tiny losses of floor contact at
  respawn or during floor snapping. Explicit Space takeoff has no such delay.

Airborne/falling poses loop as needed. Descent is selected by actual vertical
velocity, and landing by actual collision contact. Source landing durations are
retained, with a short fade at their ends and movement-responsive interruption.
Original native timing/blending fidelity beyond this adapter remains for manual
review, particularly robe deformation and fast repeated moving jumps.

## Focused validation

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-B9-Jump-Smoke.ps1'
```

The runner builds C#, imports the new library and performs one short Godot
session covering actual Space jumps, source clip availability, leg articulation,
ascent/descent/contact transitions, pause, moving landing, casting layers, death
and restart. Receipt: `manifests/b9-jump-smoke.json`; runtime log:
`logs/b9-jump-runtime.log`. One closer airborne review frame is saved as
`generated/b9-jump/airborne-review.png`. No broad historical tests are included.

Validated on 2026-10-21: C# build passed (the two existing NuGet/nullable warnings
remain), and all **16 focused runtime checks passed** with no runtime/import
errors or warnings. The airborne review image was visually inspected. The first
run exposed a false falling pose during respawn floor settling; the grace-period
fix passed one repair rerun.

Manual review: jump standing and running, turn the camera during flight, start
moving during landing, jump during automatic casting and restart while airborne.
Check that feet/robe poses look natural, landing matches ground contact and
abilities continue automatically. Stop here for review.
