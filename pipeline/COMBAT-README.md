# SWTOR Survivors — B2 automatic combat

Run the existing launcher:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

Use `-Editor` to open the project, then F6 on `Gameplay/GameWorld.tscn`.
The launcher compiles C# before opening Godot. The original doorway remains the
project's main scene, so F5 still opens that inspection scene.

WASD moves with the retained B1 controller and camera. Attacks are automatic.
R resets position, camera, player/enemy health, cast state and effects. F3 retains
collision/bounds debugging. Esc closes the game.

One hostile Sorcerer stands approximately 0.45 source units from the player.
Initialization chooses the first of four nearby authored offsets with supported
terrain, prop clearance at aim height and a clear targeting ray. This is a fixed
test fixture, not an enemy spawn system. Placement fails explicitly if none of
those offsets is usable.

The player automatically winds up, channels Lightning for three seconds, ends
the cast, waits for cooldown, then repeats if the enemy remains valid. The enemy
has 80 HP; each full channel deals 48 damage. It dies on the fourth tick of the
second channel. Its final original Death pose remains visible, with combat
disabled. Press R to repeat. The enemy has no AI, attacks or movement.

The HUD shows the selected target, HP, channel state, cooldown, damage-event
count and last combat event. The enemy also has a hostile/health nameplate.

## Configuration

Select the `GameWorld` root in `GameWorld.tscn` and edit **B2 Gameplay Tuning**:

| Export | Default | Meaning |
|---|---:|---|
| AcquisitionRange | 0.85 | Three-dimensional feet-to-feet distance in retained source units |
| TargetRefreshSeconds | 0.2 | Nearest-target acquisition interval |
| ChannelSeconds | 3 | Gameplay channel duration after windup |
| TickSeconds | 0.5 | Damage interval; first tick occurs after one full interval |
| DamagePerTick | 8 | HP per valid tick |
| CooldownSeconds | 1 | Cooldown after cast-end cleanup, including cancelled casts |

These are explicit prototype tuning values. The unresolved original SWTOR range
field is not used, and source units are not claimed to be calibrated metres.
The smoke test assumes these defaults.

Edit `StationaryEnemy.tscn` → `Health` → `Maximum` for enemy HP. The root exposes
team, combat enablement, aim height and stable target identity. Player HP defaults
to 100 in `GameWorld.AddCombat`. No enemy deals damage in this milestone.

## Components and rules

- `Combatant` supplies health, team and target identity independently of visuals.
  Neutral and same-team actors are excluded. Dead, disabled, queued-for-deletion
  and detached actors are invalid.
- `Health` clamps damage to remaining HP, ignores nonpositive/nonfinite damage,
  emits damage/death events, and supports explicit restoration.
- `TargetingService` scans the combatant group at the configured interval. It
  chooses nearest distance, then ordinal authored identity, then unique scene
  path for exact ties. Current casts retain their target; idle/cooldown scans can
  select a nearer enemy. Active-target validity is checked each physics step and
  again before every tick. LOS uses existing terrain/prop physics layers 1/2,
  including triangle back faces, independently of camera visibility hiding.
- `AutomaticLightning` owns start/channel/end/cooldown state. `ChannelClock`
  schedules ticks by integer interval index, including exact end-boundary ticks
  and catch-up after a longer physics step. Every tick rechecks combat validity.
  A cast never damages a newly acquired target halfway through its channel.
- `LightningPresentation` calls the unchanged `ForceLightning.gd` configure,
  start, stop and interrupt APIs. Neither effect visibility nor its phase signal
  can apply damage. The effect reuses the existing original textures, sockets,
  audio media and reconstructed playback behavior.
- Target death/tree exit immediately interrupts effects and audio. Range/LOS/team
  loss cancels on the next physics step before damage. A cancelled cast plays
  original LightningEnd without leaving an audio tail. Normal completion uses
  the A3 bounded fade. Caster death, explicit reset and scene exit abort cleanup.
- `StationaryEnemy` receives damage, plays Hit without continually restarting it,
  returns to Idle, and gives Death priority. Combat disabled after death prevents
  reacquisition. Its model has no player-blocking collision in B2.

Replacement NPC visuals can use the same health/target/combat rules. The current
Sorcerer presentation adapter expects the existing clip names and A3 skeleton
sockets; a different rig will require a clip/socket adapter. This milestone does
not convert or import another NPC.

## Animation policy and limits

The imported casting clips animate the full body. Layering them over Run would
require a carefully filtered AnimationTree and checks for root/pelvis/leg track
conflicts. B2 uses explicit full-body casting priority instead: cast-start,
loop and end temporarily override locomotion playback while the unchanged
controller continues to own position, collision and movement speed. Run/Idle
resumes after cast-end cleanup. A stationary caster faces its target; while
moving it faces actual travel as in B1. The hands' original sockets still drive
the Lightning endpoints.

This permits visible foot sliding and awkward casting while travelling sideways
or away from the enemy. No source tracks, skeleton, weights, root trajectories,
meshes or saved animation resources are edited. Animation libraries are copied
per instance. Gameplay timing uses physics time; the existing effect phases use
render time, so a small frame-dependent visual phase offset is possible.

Existing camera occluder hiding, small-scale collision, source terrain holes,
crop limits, shader approximations and uncalibrated units remain B1/A limitations.
There is no enemy AI, waves, spawning, XP, progression or arena expansion.

## Focused check and optional regressions

```powershell
# Compile, then one rendered combat smoke; no screenshots or conversion:
& 'D:\SWTORClassic\torbonk\pipeline\Run-Combat-Smoke.ps1'

# Optional comprehensive B1 and A1–A4 checks on existing converted assets:
& 'D:\SWTORClassic\torbonk\pipeline\Run-Gameplay-Validation.ps1' -Capture
```

The comprehensive command includes archive hashing and is intentionally not part
of routine B2 iteration. Conversion pipeline commands are unnecessary for B2.
The B1 validator retains its original fixture through its existing command-line
flags. Normal launches enable combat; inspection launchers remain separate.

The focused run writes `manifests/combat-smoke.json` and
`logs/combat-smoke-runtime.log`. It uses a real renderer/audio engine, inspects
active effect/audio state and bounded cleanup, and creates no captures. It does
not measure audible quality or replace manual visual gameplay review.

Manual checks:

1. Stand still and confirm original start/loop/end, Hit/Death and audible Lightning.
2. Move during a cast; check camera, collision, facing, and the documented foot sliding.
3. Walk away or behind original terrain/ruins; check damage and effects stop, then
   return to range/LOS and confirm casting resumes after cooldown.
4. Let the enemy die; confirm silence, no lingering bolts, no further damage, and
   normal movement. Press R during a cast and after death to repeat.
5. Toggle F3 and verify the B1 bounds/collision display still works.
