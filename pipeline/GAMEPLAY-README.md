# SWTOR Survivors — playable gameplay

**B5 is current:** Force Lightning, Shock and Force Storm share the automatic
ability system and existing single action bar. See [ABILITIES-README.md](ABILITIES-README.md)
for launch, tuning, original asset provenance, validation and limitations.
The earlier milestone notes below are retained as historical context.

Normal launches now include **B4 XP, leveling, upgrades and original SWTOR UI**.
See `PROGRESSION-README.md` for current launch, tuning, asset provenance and limits.
The B4 verification is in `PROGRESSION-RESULTS.md`. The B3 systems remain. See
`SURVIVAL-README.md` for launch, tuning, limits and manual checks, and
`SURVIVAL-RESULTS.md` for verification. `COMBAT-README.md` and `COMBAT-RESULTS.md`
record the retained B2 systems and stationary validation fixture. The movement
and environment details below describe the retained B1 foundation.

Open the existing Godot 4.7.2 .NET project and run
`res://Gameplay/GameWorld.tscn` with F6, or launch it directly:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

Use `-Editor` to open the scene in the editor. The launcher builds the C# assembly
first. The doorway remains the project's main scene; all four earlier inspection
scenes and their launchers remain available.

| Control | Action |
|---|---|
| W / A / S / D | Move relative to the fixed camera yaw; diagonals are normalized |
| R | Restart player position, camera, health, combat, enemies and wave progression |
| F3 | Toggle source-derived collision triangles, crop limits and diagnostics |
| Esc | Close the prototype |

Combat attacks automatically. B3 adds chasing melee enemies and timed waves.
Collect defeated enemies' XP markers to level, then select one of three upgrades
with the mouse or 1 / 2 / 3. Progression resets on R; there is no persistence or networking.

## Authored components

Everything new at runtime is under `godot/Gameplay/`. No gameplay scripts are
attached to generated assets.

| Component | Responsibility |
|---|---|
| `GameWorld.tscn` / `GameWorld.cs` | Instance the existing Korriban scene and Sorcerer, wire components, configure input/lights/HUD and find the spawn floor |
| `PlayerController.cs` | CharacterBody3D acceleration, braking, gravity, floor snap, physical sliding, reset and fall recovery |
| `CharacterPresentation.cs` | Turn the wrapper, blend Idle/Run, and give original combat/reaction clips explicit visual priority |
| `ArenaBounds.cs` | Enforce crop limits, slope/drop limits and a nine-point capsule footprint against actual terrain collision |
| `CameraRig.cs` | Exponentially damped fixed-yaw follow, swept-sphere/ray obstruction checks, close-occluder hiding and bounded upward avoidance |
| `GameplayValidation.cs` | Opt-in acceptance tests, keyboard injection and rendered captures; inactive in normal play |
| `Combatant.cs` / `Health.cs` | Team identity, valid combat state, health, damage and death events |
| `TargetingService.cs` | Periodic nearest-hostile selection, stable tie-breaking and environment line of sight |
| `AutomaticLightning.cs` / `ChannelClock.cs` | Gameplay cast lifecycle, cooldown and deterministic damage ticks |
| `LightningPresentation.cs` | C# adapter for the unchanged original-source ForceLightning.gd API |
| `StationaryEnemy.tscn` / `.cs` | Reusable stationary hostile with original Hit/Death clips |
| `CombatValidation.cs` | Opt-in focused B2 checks in one rendered runtime |

The scene instances `res://Assets/Korriban/KorribanEnvironment.tscn` directly,
and instances the unchanged `res://Assets/sorcerer.glb` inside a presentation
wrapper. All seven clips remain available. Only Idle and Run are used by B1.
Animation libraries are copied in memory before setting locomotion loops. No
original mesh, bone rest, skin, material, GLB or animation file is edited.
The source animated face points toward -Z; the wrapper's yaw respects the
original retained Bip01 rotation. Animation playback never drives body movement.

## Configuration and units

All transforms retain source scale 1. Physical metres remain uncalibrated.
The player capsule is 0.185 units tall with radius 0.03, centered 0.0925 above
the feet; it represents the body rather than every robe/shoulder/hair vertex.
Movement defaults are 0.6 units/s, acceleration 3, braking 4, gravity 3,
48-degree walkable slopes, 0.045 floor snap and 0.0005 collision margins.

Select the player or camera nodes to adjust exported parameters in Godot.
For the camera, Height 0.9 and Distance 1.05 define a boom radius
`sqrt(Height² + Distance²)`; AngleDegrees 48 sets its elevation and YawDegrees 0
sets its horizontal direction. TargetHeight is 0.1 and FOV is 52 degrees.
Increasing Height or Distance moves the camera farther away at the selected
angle. FollowSharpness controls damping. Upward avoidance preserves yaw and
therefore input orientation; it can temporarily steepen the camera to 88 degrees
when geometry would otherwise pull the camera into the character. It returns
toward the configured angle smoothly. Set RaiseAboveObstructions false to use
only boom compression.

When a source prop would pull the camera within 0.45 units of the focus, the
camera temporarily hides that instance's meshes and excludes its bodies from
camera probes. Player collision stays active. Visibility is restored as the
view clears and when the camera exits the scene. This is an authored gameplay
visibility policy; it does not replace art or alter materials. Disable
HideCloseOccluders to inspect the purely physical camera behavior.

The shared project now explicitly selects the engine's bundled **Jolt Physics**.
The previous DEFAULT backend repeatedly exhausted the slide budget against the
same terrain triangle at this small capsule scale. Jolt resolves the reproduced
case without rescaling or rebuilding original assets. It requires no plugin.
Existing inspection scenes are regression-tested with this setting.

At runtime, existing terrain bodies use collision layer 1, props use 2 and the
player uses 4 with mask 3. Camera casts use mask 3; safety probes use terrain
layer 1, so a prop over a terrain hole cannot silently become replacement ground.
The generated scene's saved collision configuration is untouched.
Prop triangle shapes are copied in memory with back-face collision enabled,
so camera probes and player collision also detect the backs of the two-sided
source art. Their triangle data and saved source resources remain unchanged.

## Arena and known limits

The selected two-patch footprint remains X 20.2–45.4, Z 6.2–18.8 source units.
The controller keeps its footprint inside an additional 0.06 edge inset. A swept
centre/eight-point ring checks actual terrain triangles at intervals no larger
than half the capsule radius. Missing triangles, slopes over 48 degrees and
drops over 0.065 units are rejected; movement can slide along a valid axis.
The actual result of physical sliding is checked again. Unexpected falls recover
to the last supported location; R always returns to the authored spawn near
`(36, -17.136, 12.6)`.

This crop is sufficient for movement testing at preserved character scale, but
is not a finished arena. Source holes, steep rock faces and solid architecture
restrict traversal. Crop edges remain exposed where the original selected art
ends. No terrain is expanded, repaired, flattened or regenerated. Architectural
collision uses the existing visible triangle meshes, not original SWTOR physics.
There is no stair-climbing, jump, navigation mesh or broad-phase reachability map.

Camera avoidance is tested against an original ruin wall. Close blocking props
can disappear abruptly; visibility uses a hard hide rather than a material fade
on the Compatibility renderer. Terrain remains visible. Very tight terrain
crevices can still force a close, steep view; there is no guarantee of a full
unobstructed silhouette in every source crevice. The bounded
crop and baked terrain textures also become apparent when viewed closely.
Cloth/robe intersections, portable shaders, source root-trajectory fidelity and
uncalibrated physical units retain the documented A1–A4 limitations.

## Validation

For ordinary B2 development, the bounded optional check is:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-Combat-Smoke.ps1'
```

The comprehensive commands below remain optional manual regression tools.
B1 validation arguments deliberately disable B2 setup so the original movement
fixture and its animation assertions remain usable. B2 delivery does not rerun
these comprehensive suites.

```powershell
# B1 headless and rendered checks, original A1–A4 validators and all 91 archive hashes:
& 'D:\SWTORClassic\torbonk\pipeline\Run-Gameplay-Validation.ps1' -Capture

# B1 iteration without source/regression rechecks:
& 'D:\SWTORClassic\torbonk\pipeline\Run-Gameplay-Validation.ps1' -Capture -SkipRegressions
```

Without `-Capture`, B1 still runs actual physics and animation checks headlessly.
`-SkipArchiveRehash` skips only the expensive whole-archive SHA-256 comparison;
the selected original-resource checks still run unless regressions are skipped.
The runner reuses original validators and current converted assets. It does not
rerun conversion, rebuild Korriban, or generate more terrain.

Evidence is under `manifests/gameplay-*.json`, `logs/gameplay-*.log` and
`generated/gameplay/*.png`. `GAMEPLAY-RESULTS.md` records the completed run,
regressions, measured tolerances and remaining limitations. B1 stops here.
