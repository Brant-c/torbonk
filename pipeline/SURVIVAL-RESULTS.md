# SWTOR Survivors — B3 results

B3 is implemented in the existing Korriban gameplay scene: reusable chasing
melee enemies, safe bounded spawning, timed survival waves, incoming player
damage/death, restart and a minimal survival HUD. Original Sorcerer visuals are
temporary and replaceable. Existing targeting, Health, automatic Force Lightning,
player movement/camera and presentation components are reused.

New components are `EnemyAgent`, `ArenaNavigation`, `EnemySpawner`, `WaveDirector`
and `MeleeEnemy.tscn`. `GameWorld` composes them for normal launches and preserves
the existing stationary B2 fixture under `--combat-smoke` and movement-only B1
fixtures under their existing flags. No existing test runner was removed or rewritten.

## Verification completed — October 20, 2026

- `dotnet build pipeline/godot/SwtorAssetInspection.csproj --no-restore --nologo`
  succeeded with zero errors and no C# compiler warnings. The existing NU1900
  package-audit warning remains because the NuGet vulnerability feed could not
  be reached; a successful vulnerability audit is not claimed.
- One lightweight rendered Godot 4.7.2 .NET smoke session completed successfully
  on OpenGL Compatibility with the NVIDIA GeForce RTX 3070 Ti. All **10 checks
  passed**: bounded supported spawn outside player collision, pursuit, melee
  damage through player Health, wave cap/cadence/stat scaling, enemy death and
  defeat counting, delayed corpse removal, player death stopping combat/AI,
  no subsequent incoming damage, complete restart, and automatic Lightning
  damage against a newly spawned enemy after restart.
- The smoke temporarily used three-second waves and disabled player outgoing
  damage while observing melee. Enemy and player lethal damage were explicitly
  applied to keep lifecycle verification short. It restored normal wave timing
  and enabled Lightning after restart. This does not establish long-run balance.
- The runtime log contains the existing restricted shader-cache folder message.
  No additional startup, script or gameplay errors were reported. No screenshots
  or benchmark measurements were taken.

Machine-readable receipt: `manifests/survival-smoke.json`.
Runtime log: `logs/survival-smoke-runtime.log`.
Repeatable focused runner: `Run-Survival-Smoke.ps1`.

No comprehensive A1–A4/B1–B2 regression suites, archive hashes, conversions,
emulator operations or arena expansion were performed. Existing converted
assets, inspection scenes and previous test infrastructure remain in place.

## Review limits

Local steering can stall at concave or disconnected geometry and crowds can
queue in narrow spaces. Spawn placement deliberately waits when no safe nearby
corridor exists. There is no melee swing clip, global path planner or crowd
avoidance system. Player full-body casting retains B2's movement/foot-sliding
limits. Whole-arena obstacle traversal, appearance and conservative default
balance remain manual review items.

Launch, tuning and the short manual checklist are in `SURVIVAL-README.md`.
**Stopped after B3.** No XP, upgrades, bosses, persistent progression, networking
or additional asset conversion has been started.
