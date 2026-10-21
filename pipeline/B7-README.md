# SWTOR Survivors — B7: presentation and audio

B7 is implemented and ready for manual visual/audio review. B6's roster,
equipment, Korriban, combat, ability animations and progression balance remain
in place. This milestone adds presentation and encounter coordination only.

## Launch

Run the existing launcher from PowerShell:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

WASD moves, attacks are automatic, 1/2/3 or a click selects an upgrade, R restarts,
Esc quits. The launcher builds C# and opens the gameplay scene. `-Editor` opens
the scene in the editor. Normal wave-5 boss timing remains unchanged.

## What changed

- `BossEncounterController` owns Scheduled → Introducing → Active →
  Defeated/Failed, safe placement retries and one activation per run. The
  existing `BossEncounter` still owns Burrow Eruption. Its 18 damage,
  0.30 radius, 1.35-second warning, recovery and cooldown are retained.
- The boss gets a dedicated top health composition using recovered SWTOR
  window/bar artwork and Eurostile, accurate current/max values, smoothed fill,
  delayed loss and a short reveal. It is separate from the target frame and
  hides on defeat, failure or restart.
- Introduction lasts 1.2 seconds and holds only the new boss. The player and
  existing enemies keep moving. New ordinary enemies wait during introduction;
  during the active fight their default cap is 55% of the normal wave cap and
  spawn interval is 2.5× normal. Existing mobs are never removed by activation.
- Arrival/defeat notices announce the name and the existing 80-XP reward drop.
  The controller never awards XP. EnemySpawner retains its single death/reward
  path. Boss attack warning and impact now have separate graphics and original
  generic creature rock/ground audio adapted to the existing attack.
- Level-ups show original popup frame/gleam/arrow pixels and recovered original
  level-up audio before the upgrade panel. Combat pauses for the announcement
  and choice. Overflow XP advances through one banner/choice/confirmation at a
  time. Restart or death cancels pending presentation.
- XP is a small floating teal 3D mote with a recovered SWTOR loot glow, slow
  rotation/pulse and a 0.22-second collection trail. Awarding XP is immediate;
  the trail can finish during a level-up pause. Pickup cap, merge accounting,
  radius, magnet speed and XP values retain their existing rules.
- Upgrade cards have subtle hover feedback, sound and brief applied-state
  animation. Premium remains green, Prototype blue and Artifact purple.
  Confirmation holds are 0.34 / 0.48 / 0.65 seconds. Rarity probabilities and
  upgrade amounts are unchanged.

## Tuning and reuse

`godot/Gameplay/Enemies/BroodmatronEncounter.tres` is the reusable encounter
definition: scene, title, introduction duration, regular-spawn toggle, cadence
multiplier and cap fraction. WaveDirector exposes `BossDefinition` and the
existing `BossWave`. A future scene can use this controller by supplying another
definition; no additional boss is included here. Failed placement retries once
per second. Wave time continues during boss combat.

`godot/Gameplay/presentation-audio.json` centralizes cue sources, gains, pitch,
duration limits, retrigger intervals, category gains and voice budgets. Buses:
Music, World (ambient), Combat, UI, Reward and Boss. Music/World routing exists
for future or existing sources; this milestone does not add a soundtrack or
ambient loop. Original Lightning/Shock/Storm audio routes through Combat.

There are at most 12 presentation voices and 16 concurrently playing registered
combat voices. Existing effect-local pools remain bounded. Repeated pickup cues
are limited to one per 85 ms; suppressed sounds do not suppress XP, damage,
visuals or combat text. Priority level/boss cues reserve/reclaim presentation
voices and duck Combat by 9 dB, Music by 5 dB and World by 4 dB, with a short
attack and smooth release. Short cue tails fade. Death/restart clears voices,
loops, retrigger history and ducking, alongside the existing ability abort.

`RunProgression.LevelAnnouncementSeconds` controls the banner hold (1.75 seconds
by default). The original bitmap frame is preserved; layout, fades and timings
are authored. See [B7-PROVENANCE.md](B7-PROVENANCE.md) for the distinction between
recovered resources and reconstructed presentation.

## Verification and stopping point

`Run-B7-Smoke.ps1` compiles C#, imports local changed resources and runs one
brief rendered B7 session. The passing receipt is `manifests/b7-smoke.json`;
the log is `logs/b7-runtime.log`. It checks real pickup creation/collection,
queued banners and three-choice upgrades, confirmation and pause/resume,
boss initialization at 1280×720 and 1920×1080, attack synchronization, the single
80-XP reward, no duplicate boss, and cleanup after death/restart. The initial
fixture was adjusted to observe a boss drop before the nearby magnet collected
it; the corrected focused session passes.

C# build has no errors. The existing StormAreaIndicator nullable warning and
NuGet vulnerability-service availability warning remain. No historical A1–A4
suites, archive hashing, long wave benchmark or capture series was run.

Manual review remains for sound balance, cue edits/pitch, banner timing, card
feedback, telegraph terrain visibility and pickup readability during dense
combat. The warning remains a ground projection of the unchanged spherical
range/LOS damage check; terrain height changes can affect its readability.
Exact native Scaleform/Wwise behavior is not reproduced. Stop after B7.
