# SWTOR Survivors — B3 enemy AI and survival waves

Normal gameplay now runs B3 in the existing bounded Korriban scene.

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

The launcher builds C# and opens the game. Add `-Editor`, open
`Gameplay/GameWorld.tscn` and press F6 to run from the editor. F5 still opens the
original inspection scene; previous inspection scenes and launchers are preserved.

WASD moves, Lightning attacks automatically, R restarts from wave 1 with full
health and cleared enemies/counters, F3 toggles collision/bounds, and Esc quits.
After death, movement, Lightning, enemy attacks and spawning stop. Press R to retry.
The HUD displays health, wave/countdown, active enemies/limit, defeated enemies,
Lightning status and spawn availability.

## Components

- `EnemyAgent.cs` owns Idle → Chase → Attack → Dead behavior, detection distance,
  movement speed, melee distance, attack interval and damage. It moves a parent
  `CharacterBody3D` and applies damage through `Health`. Range and environment
  line of sight gate melee attacks. Approaching melee range gives a 0.35-second
  windup; subsequent attacks use the configured interval. Death immediately
  stops AI and crowd collision. Player death stops every live enemy synchronously.
- `ArenaNavigation.cs` uses the retained `ArenaBounds` footprint/slope/drop probes
  and swept capsule checks against existing props. Local steering tries a small
  fixed set of directions every 0.18 seconds with stable left/right preference.
  Physics provides sliding, gravity and floor snap; unsafe post-slide movement
  is rejected. No navigation mesh bake, new terrain or global path graph is needed.
- `EnemySpawner.cs` samples a bounded ring around the player, rejects unsupported
  terrain, holes, steep ground, full-body collisions and overlapping live enemies,
  and requires a traversable corridor from the player. Failed placement retries
  later without relaxing safety checks. The active limit counts living enemies;
  defeated bodies retain their pose for four seconds and then leave the scene.
- `WaveDirector.cs` advances timed waves and adjusts spawning and new-enemy stats.
  A wave boundary never heals or changes existing enemies. Spawn attempts do not
  accumulate into bursts while capped or blocked. Reset clears all enemies,
  corpses, timers, wave state and defeat counters.
- `MeleeEnemy.tscn` composes the body/capsule, combat identity, `Health`, optional
  presentation and nameplate. The temporary visual instances the existing original
  Sorcerer. Idle, Run, Hit and Death use its existing clips. There is no converted
  melee swing clip; an enemy stands and faces the player during attacks. Damage
  timing is independent of animation and effects.

The player retains B1 movement/camera and B2 health, targeting, damage scheduling,
and the unchanged Force Lightning presentation. No incoming-damage animation
interrupts the player's full-body Lightning playback. Player Death takes priority.
`StationaryEnemy` remains intact for the existing B2 validation fixture; normal
launches use moving/spawned enemies. B1 validation flags retain movement-only play.

## Defaults and tuning

Select the `GameWorld.tscn` root and use **B3 Waves** and **B3 Spawning** exports:

| Setting | Default |
|---|---:|
| Wave duration | 30 seconds |
| Initial spawn interval | 2.8 seconds, first attempt after 0.5 seconds |
| Interval multiplier per wave | 0.88, minimum 0.75 seconds |
| Initial active limit | 3 |
| Active limit increase | 1 per wave, hard cap 10 |
| New-enemy health increase | +12% of base per wave, maximum 3× |
| New-enemy damage increase | +10% of base per wave, maximum 2.5× |
| Spawn ring | 0.55–1.05 source units from player |
| Corpse retention | 4 seconds |

The reusable director also exports `AdditionalActivePerWave` for other compositions.
Edit `MeleeEnemy.tscn` → `Actor` for detection (2.5), speed (0.22), melee range
(0.10), attack interval (1.2 seconds), damage (5), and `Actor/Health` for base HP
(40). The player has 100 HP. Existing Lightning defaults remain 8 damage per
half-second tick, a three-second channel and one-second cooldown after cast end.
All distances retain the existing uncalibrated source units.

EnemyScene can point to another compatible packed scene: a `CharacterBody3D`
root with a `CollisionShape3D` child and `Actor` (`EnemyAgent`) containing `Health`.
Presentation/nameplate are optional. A replacement visual using
`CharacterPresentation` must provide its expected skeleton and clip names;
different rigs can use another adapter. Match its body capsule to
`ArenaNavigation` radius/height. Gameplay code has no SWTOR asset paths; only
the world composition and enemy scene select the temporary Sorcerer visual.

## Verification and manual review

The B3 smoke builds C# and runs one bounded Godot session with rendering, without
conversions, screenshots, archive checks or historical regression suites:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-Survival-Smoke.ps1'
```

It briefly accelerates wave timing and disables outgoing damage to observe
pursuit, incoming damage, wave scaling, enemy death/cleanup and player death.
It then resets and verifies automatic Lightning against a new spawned enemy.
Results: `manifests/survival-smoke.json` and `logs/survival-smoke-runtime.log`.
Existing A1–A4/B1–B2 validation infrastructure is preserved and is not part of B3's
required run. See `SURVIVAL-RESULTS.md` for actual verification results.

Manual checklist:

1. Stay near the start; watch enemies approach, Lightning fire, Hit/Death clips,
   health changes, defeat count and delayed corpse removal.
2. Move around nearby ruins and terrain edges; check steering, collision, holes
   and safe spawn placement. F3 helps inspect existing collision and bounds.
3. Survive 30 seconds; confirm wave 2, faster spawning and a larger active cap.
4. Allow enemies to kill the player; confirm the death message and stopped combat.
5. Press R during combat and after death; confirm full health, wave 1, cleared
   enemies/defeats, restored movement and resumed spawning/Lightning.

## Known limits

Local steering can stall at deep concave ruins or disconnected terrain; it does
not plan long detours. The conservative spawn corridor check favors nearby open
ground and may pause spawning near obstacles or holes. Enemies outside detection
range idle until the player returns. Crowds use body collision and can queue in
narrow passages; the player's existing collision mask permits moving through
enemies. There is no crowd path planner or off-screen-spawn guarantee.

Original full-body casting can slide feet while moving. There is no authored
melee attack animation yet, and the Sorcerer is a temporary enemy visual. Existing
terrain/camera/material/source-scale limitations remain. Tuning and broader
navigation behavior need manual gameplay review; the short smoke is not a full
arena traversal or balance test.

**B3 stops here.** No XP, upgrades, bosses, persistent progression, new conversions,
arena expansion, networking or emulator/archive changes are included.
