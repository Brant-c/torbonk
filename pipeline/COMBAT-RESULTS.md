# SWTOR Survivors — B2 results

B2 is implemented: automatic nearest-hostile targeting with environment LOS,
health/team/death components, deterministic gameplay damage, the unchanged A3
Lightning presentation, configurable channel/cooldown, and one stationary hostile
instance of the original Sorcerer. The existing playable Korriban scene launches
with combat enabled. R resets the complete fixture; WASD, camera and F3 remain.

Combat implementation lives in `godot/Gameplay/`. `PlayerController`, `CameraRig`,
`ArenaBounds` and the saved gameplay/environment scenes are unchanged.
`GameWorld` wires B2 components and shows combat status. `CharacterPresentation`
adds explicit full-body combat/reaction priority while retaining B1 locomotion.
`StationaryEnemy.tscn` is a separate reusable stationary damage-receiver scene.

## Completed validation

- C# build succeeds with zero compilation errors and no C# compiler warnings.
  The existing NU1900 warning remains because the NuGet vulnerability feed was
  unavailable; no successful package audit is claimed.
- One rendered Godot 4.7.2 .NET run passed **35 focused checks**, producing 2,450
  frames on OpenGL Compatibility with the NVIDIA GeForce RTX 3070 Ti. No screen
  captures were generated. This is smoke coverage, not a performance benchmark.
- Integer tick scheduling produces exactly six ticks over three seconds at
  30/60/120-Hz-sized steps, a fractional step, and a single oversized step;
  advancing at an already-consumed boundary creates no duplicate tick.
- Health clamps lethal damage and emits one death event; invalid/dead damage is
  ignored. Stable identity tie-breaking, friendly/neutral/dead filtering, range
  exit/entry and LOS blocking/restoration passed. LOS tests use temporary test
  blockers on the same terrain/prop masks as the original environment.
- Actual original start/loop/end animation names, enemy Hit return to Idle and
  Death priority passed. The rendered channel had eight beams and six active
  audio voices when sampled. Engine playback state was checked; audible quality
  and detailed pose fidelity are left for manual review.
- Movement remained grounded during a cast. A complete channel dealt 48 HP in
  six events; the second channel killed the 80-HP enemy at ten total events.
  Death stopped damage, effects and audio. Range loss, target removal, caster
  death, reacquisition/repeated casting and reset completed with bounded cleanup.
- Lightweight retained-B1 checks verified grounded movement and crop-boundary
  rejection. Full movement/camera scenario validation was not repeated.
- The existing restricted-user-folder shader-cache message appeared once and
  did not prevent rendering. No additional runtime initialization/script errors
  were reported.

Machine-readable results: `manifests/combat-smoke.json`.
Engine output: `logs/combat-smoke-runtime.log`.

## Regression scope and remaining limits

A1–A4 conversion outputs, original assets/archives, `ForceLightning.gd`, inspection
scenes/scripts, project renderer/physics settings and original regression runners
were not edited. No archive rehash, conversion, terrain regeneration, emulator
write or comprehensive A1–A4/B1 regression run was performed. Compatibility is
supported by reuse and the focused integration run, not a new full regression
pass. Existing comprehensive validation remains an optional manual command in
`COMBAT-README.md`; the B1 runner keeps its movement-only fixture.

Casting deliberately takes full-body visual priority while movement continues,
so feet can slide and travelling casts may look awkward. There is no upper-body
layering. Original terrain/camera/material limits remain. The temporary hostile
does not block player movement, retaliate or pursue; its final death pose stays
visible and disabled. The player's death path is exercised by the smoke test,
but the normal fixture contains no source of incoming damage.

Launch/configuration and a short manual checklist are in `COMBAT-README.md`.
**Stop after B2.** Enemy AI, spawning, waves and progression are not implemented.
