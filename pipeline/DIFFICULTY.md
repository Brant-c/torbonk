# Planet difficulty and phase recovery

Korriban and Hutta opt into the `Difficulty` settings in their `run.json` files.
Other worlds retain the legacy wave rules. XP rewards and level requirements
are unchanged.

## Enemy stats

At spawn, each enemy snapshots the player's level. Its reference level is
the first stage containing that species, or its scheduled milestone level
for an encounter. Levels below that reference never weaken the authored
stats. Summoned species outside the roster default to reference level 1.

For `n = max(0, player level - reference level)`:

| Stat | Multiplier |
| --- | --- |
| Maximum health | `1.08 ^ n` |
| All outgoing damage | `1 + 0.045 * n` |
| Movement speed | `1 + min(0.25, 0.01 * n)` |
| Basic attack frequency | `1 + min(0.15, 0.005 * n)` |

Damage scaling happens once in `EnemyAgent.DealAttack`, including ground
patterns and boss strikes. Warning durations and special-attack cooldowns
keep their authored timings. Existing enemies never gain health or change
stats when the player levels. Planet stats do not also multiply by wave stats.

## Population

The living population limit is `min(100, 16 + 3 * (level - 1))`. One slot
at the hard ceiling is reserved for the scheduled encounter. Bosses,
summons and cache guards all count; even required spawns cannot exceed 100.
Corpses do not consume the living limit. Spawn placement still requires
supported terrain and a reachable corridor.

Regular spawn intervals interpolate from 0.5 seconds at level 1 to 0.15
at level 30. Failed placement retries remain bounded. There is one spawn
attempt per interval, with no accumulated burst after a pause.

## Recovery

Boss defeat and stage unlocks offer a 20-second recovery period. Stage
unlocks during a boss wait until defeat; unlocks during recovery update the
destination without extending the timer. Recovery suppresses ambient
spawns, summons and cache guards, freezes the wave clock, and holds the
next encounter. Existing enemies continue fighting.

The tracker shows destination, compass direction, distance in world units,
and remaining recovery time. Stage focus points resolve to supported
standing positions. Hutta stages now use distinct exploration destinations
near the existing caches. Within 0.65 world units, **Begin next phase** can
end recovery early. Reward and upgrade choices do not spend recovery time.
Spawning returns over eight seconds, initially at four times the usual
interval. Finishing all milestones ends the run as before.

## Verification and tuning

Run `pipeline/Run-Difficulty-Smoke.ps1` for focused checks on both planets;
add `-Rendered` to exercise the renderer. Checks cover spawn snapshots,
reference levels, special damage, population reservations, recovery,
pending encounters, restart, and a real animated population of 100.

The proposed stat curves are initial balance settings. A short automated
crowd check is not a sustained performance benchmark or a complete
30-level balance playthrough. Compare combat seconds per level, kills
per minute, typical enemy survival time and incoming damage across classes
before changing XP growth. In particular, test both single-target and
area-damage builds with the larger population.
