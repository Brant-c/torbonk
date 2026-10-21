# SWTOR Survivors — Milestone B1 results

The original Sorcerer can move around the existing Korriban environment with
camera-relative WASD, acceleration/braking, turning, ground collision, gravity,
Idle/Run transitions and a fixed-yaw elevated follow camera. R resets the player;
F3 shows the derived collision geometry and bounds. **B1 stops here.**

Launch with `Open-Gameplay.ps1`, or open the existing Godot .NET project and run
`res://Gameplay/GameWorld.tscn` with F6. Full launch, configuration and validation
instructions are in `GAMEPLAY-README.md`.

## Delivered

The authored scene and C# components live under `godot/Gameplay/`:
`GameWorld`, `PlayerController`, `CameraRig`, `CharacterPresentation`,
`ArenaBounds` and an opt-in validation runner. The scene instances the existing
`Assets/Korriban/KorribanEnvironment.tscn` and unchanged Sorcerer GLB. Gameplay
code remains separate from generated assets. All seven original clips remain
available; B1 uses only Idle and Run and blends them on actual movement speed.

The player uses CharacterBody3D and a source-scale capsule. Physics owns all
translation; no SWTOR animation trajectory drives movement. Presentation turns
an outer wrapper while preserving the original -Z facing convention, skeleton,
materials, mesh scale and bone animation. No original GLB or animation is edited.

The environment retains its two original terrain patches, 107 prop instances,
31 shared prop meshes and 109 derived collision shapes. Source terrain holes,
steep slopes and crop edges are enforced by footprint/swept ground probes.
Actual physical sliding is checked again before accepting its destination.
Unexpected falls recover to the last supported position.

## Verification completed

- Headless integrated movement, collision, bounds, animation and reset checks pass.
- Actual OpenGL Compatibility gameplay runs and six rendered captures were made
  on the NVIDIA GeForce RTX 3070 Ti, using Godot 4.7.2 .NET and bundled Jolt.
- Physical W-key input moves forward relative to the camera. All four cardinal
  directions and a 90-degree camera yaw are exercised. Actual travel drives
  presentation turning; root motion does not move the imported wrapper.
- One-second movement at 30/60/120 physics ticks per second measured approximately
  0.524/0.519/0.516 source units, within the explicit 0.025-unit tolerance.
  Separate rendered runs measured approximately 30/60/144 FPS with consistent
  travel. Exact final readings are retained in the runtime JSON.
- Traversal remains grounded and inside bounds without ordinary fall recovery.
  An actual missing source triangle is rejected. The controller is driven into
  a crop edge and stops inside its footprint inset. A deliberately injected
  fall exercises recovery separately.
- An original `kor_vod_ruins_wall.gr2` placement physically blocks the controller.
  Camera obstruction, compressed clearance, close-prop visibility restoration
  and reset are exercised. A render-triangle sightline check supplements physics
  probes in the final visual test.
- Original A1 doorway and A2 character structural/Blender roundtrip/runtime
  validators pass, including original weights, all seven poses and material maps.
- A3 Lightning passes 69 headless and 76 rendered checks, including its existing
  engine-output audio validation. A4 model roundtrips, scene/resource/collision
  checks, source verification and rendered inspector pass. The original STE2
  reader compatibility test was also rerun without regenerating terrain.
- All 91 original TOR archives pass the existing complete SHA-256 baseline check
  with zero changes. Selected character, Lightning and Korriban source payload
  comparisons pass. B1 performs no emulator or extractor writes.
- Preservation comparison covers 337 pre-existing asset/inspection/pipeline
  files. The only intended edit to a pre-existing authored file is selection of
  bundled Jolt in `project.godot`; its remaining original configuration is checked
  against the original hash. Generated assets and inspection/conversion scripts
  remain unchanged. No terrain was expanded, rebuilt or regenerated.

The original conversion pipelines were preserved. Regression execution uses
their existing validators on the already converted assets; it does not claim a
new full conversion/determinism cycle. `manifests/gameplay-run.json` records the
full regression/archive pass. Final B1 runtime receipts and captures may be newer
than that pass because camera-only work was completed afterward.

## Implementation findings and limits

1. **Small-scale physics required Jolt.** With the previous DEFAULT backend, the
   0.185-unit capsule could repeatedly contact the same terrain triangle without
   advancing. The bundled Jolt backend resolves the reproduced case. Source
   scale remains 1. This is the sole existing project configuration change.
2. **Back faces matter for the camera.** Some original art is visibly two-sided
   while the saved derived collision was one-sided. Gameplay copies the prop
   shapes in memory and enables back-face collision, retaining exactly the
   existing triangles. Terrain and saved generated resources remain untouched.
3. **Camera visibility is authored.** The camera follows smoothly at fixed yaw,
   sweeps a sphere and ray against the environment, and can lift above a blocker.
   If a prop would force it into the character, that prop's mesh instance is
   temporarily hidden while its player collision remains active. It is restored
   as the view clears. Hard visibility changes can pop; there is no material fade.
   Tight terrain crevices and partial silhouette occlusion remain possible.
4. **This is a bounded movement test area.** The unchanged 25.2 × 12.6 source-unit
   crop is adequate at character scale but is not a finished gameplay arena.
   Holes, steep faces, solid architecture and exposed crop edges remain. A coarse
   diagnostic sampled 264 terrain points; 194 passed the slope/footprint guard.
   This is neither a reachability proof nor a navigation mesh. There is no jump,
   stair-climbing system or expansion of the surrounding world.
5. **Original presentation limits remain.** Physical metres are uncalibrated;
   the capsule does not enclose every robe/hair vertex. Cloth intersections,
   portable shader differences, baked terrain resolution and native trajectory
   limitations remain as documented in A1–A4. There is no foot IK or authored
   footstep audio, and no claim of original-client movement parity.
6. **Existing environment warnings remain.** Restricted `user://` shader-cache
   messages are nonblocking. The build succeeds; NuGet's unavailable vulnerability
   feed warning is not a completed vulnerability-audit claim.

## Evidence

| Artifact | Contents |
|---|---|
| `manifests/gameplay-validation.json` | Final headless controller/runtime checks |
| `manifests/gameplay-validation-visual.json` | Actual rendered run, frame-rate readings and camera/mesh checks |
| `manifests/gameplay-terrain-probes.json` | Coarse source-terrain support diagnostics |
| `manifests/gameplay-run.json` | Full regression and archive execution scope |
| `manifests/gameplay-preservation.json` | Preserved files and explicit project-setting exception |
| `manifests/gameplay-artifacts.json` | Authored files, original scene/GLB and capture hashes |
| `generated/gameplay/01..06-*.png` | Idle, running, traversal, debug, reset and architecture views |
| `logs/gameplay-*.log` | Build, runtime, source and A1–A4 regression logs |

No combat, targeting, damage, AI, enemies, waves, experience, bosses, networking
or progression is implemented. Further gameplay work waits for review after B1.
