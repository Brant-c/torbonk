# SWTOR Survivors — C2 integration

C2 connects the C1 selection module to the current B7 survival game in the
same Godot project and process. Stop here for manual review.

## Launch for manual review

From PowerShell:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Survivors.ps1'
```

Choose **New Run → Sith Empire → Sith Inquisitor → Sorcerer → Continue through
Appearance → Korriban → Play**. Continue advances each page. New Run resets
selection; Character Selection retains the session's choices. Back and Escape
retain their existing page-navigation behavior.

The default entry is `res://Frontend/Scenes/MainMenu.tscn`. Open
`pipeline/godot/project.godot` in Godot 4.7.2 .NET and press F5 for the full game,
or use `Open-Survivors.ps1 -Editor`. The existing `Open-Gameplay.ps1` is unchanged
and still launches `res://Gameplay/GameWorld.tscn` directly; F6 on that scene
also works. All original inspection scenes and launchers remain available.

In gameplay: WASD moves, combat is automatic, 1/2/3 or a click chooses upgrades,
R restarts the current run, F3 shows diagnostics, and Escape retains B7's quit
behavior. After death, **RETURN TO MAIN MENU** opens character selection's main
menu. Character Selection restores the last launched valid configuration;
New Run intentionally starts fresh. No disk save is added.

## Integration boundary

- Copied only `character-selection/Frontend/` into `godot/Frontend/`, retaining
  its scene/data/asset structure and `res://Frontend/` paths. C1's project,
  packages, caches, tools, research and source archives were not merged.
- Kept the existing C# project, assembly name, renderer, window settings, Jolt
  physics, input and gameplay launchers. Only `run/main_scene` changed in the
  main project settings. Explicit framework imports were added to the copied
  C1 scripts because the main project does not enable implicit usings.
- `Integration/MainMenu.cs` hosts the reusable `FrontendFlow`; the flow exposes
  compatibility and Play callbacks. The Ready screen and its Play action both
  validate. Faction, class, preview, appearance and galaxy presentation are
  otherwise retained. Stale integration-pending labels now describe availability.
- `SelectionGameplayAdapter` explicitly maps schema 1 frontend identifiers to
  the existing GameWorld, Korriban environment and Sorcerer model. It checks
  faction, base class, advanced class, map, preset and every appearance field.
  Serialized `Playable` / `PlayableStatus` never authorize a launch. Integrated
  configurations report `content_available` or `preview_only`; field names and
  selection identifiers are unchanged. These are not native GOM IDs.
- Only `empire / sith_inquisitor / sorcerer / korriban /
  sorcerer_april2012_bmn` is registered. Future gameplay compositions can be
  registered in the adapter without adding selection-specific branches to the
  frontend. New appearance implementations must extend its appearance validator.
- `GameNavigation` keeps only a one-shot immutable gameplay handoff and an
  independent last-selection snapshot. There is no autoload, persistent world,
  menu overlay over gameplay, second executable or large global manager.
- GameWorld consumes the handoff and verifies its scene's original environment
  and character resources against the registered composition. Direct development
  launches explicitly use that same original composition. R keeps the current
  world and configuration and uses the existing B7 reset path.

## Scene and audio lifetime

Normal Godot scene changes remove and free the outgoing scene. Menu UI sounds,
isolated preview viewport, model and input handlers belong to that scene.
Returning from gameplay first aborts abilities, stops spawning and the encounter,
cancels paused progression, clears reward/flytext/audio presentation and stops
the player. Freeing GameWorld removes all enemies, pickups, effects, portraits,
HUD layers and combat/ambience audio children. B7's existing audio cleanup resets
ducking; named B7 buses are reused, not duplicated. Menu input is visible and
unpaused, with keyboard focus restored. No menu soundtrack or new ambience was
added; C1 only contains its original interface cues.

## Preserved gameplay state

Before editing, the B7 chat **Polish boss encounters and level-up** was idle and
its latest turns were complete. Inspected the implementation and B7 documentation,
including follow-up tuning, rather than copying older gameplay from C1.

The retained state includes B7 encounter/health presentation, queued level banners
and upgrades, XP effects, original HUD, GCD, casting/movement blending, Storm's
green indicator, flytext, enemy roster and saber. It also includes the later
0.75-second starting spawn interval, 8 / +3 / 36 crowd caps, supplied XP sound at
−12 dB and layered Shock/Storm audio with finite impact tails. Shock and Storm
still unlock through the existing progression system; C2 changes no ability or
combat balance. See [B7-README.md](B7-README.md) for current gameplay details.

The only edited preexisting gameplay scripts are `GameWorld.cs` (configuration
handoff and menu teardown) and `UI/SurvivorsHud.cs` (game-over return button).
The saved gameplay scene and all other preexisting gameplay files were unchanged
against a local pre-integration comparison. No original assets were reconverted.

## Validation and limitations

C# compilation passed with zero errors. The preexisting NU1900 (NuGet
vulnerability service unavailable) and CS8600 in StormAreaIndicator remain.
Frontend import passed with no Godot errors. One rendered runtime session passed
all **20 integration checks**, also without Godot errors. See
[C2-RESULTS.md](C2-RESULTS.md) for the delivery receipt.

`Run-C2-Smoke.ps1` builds the existing project, imports local frontend resources
and runs a single bounded rendered integration session. Its receipt is
`manifests/c2-smoke.json`, with `logs/c2-runtime.log` and `logs/c2-import.log`.
It checks supported and blocked selections, real page/button navigation,
scene changes, preview release, restart, paused progression teardown, live boss
cleanup, fresh re-entry and the game-over return button. It is not a historical
asset regression, comprehensive combat suite or performance benchmark.

C1's eight base classes, sixteen advanced classes and six planets remain
browseable. Other classes use their original artwork; only Sorcerer has a 3D
preview. All appearance editing remains disabled. Other class/map/appearance
combinations cannot start gameplay. Galaxy positions remain schematic. Settings
only mute menu interface cues for the current menu instance. Broader settings,
class narration, persistence and additional gameplay content remain outside C2.

No export preset was introduced or export-tested. A future packaged build must
include dynamically loaded frontend scenes/assets and `Frontend/Data/*.json`,
alongside existing gameplay JSON requirements. Original asset provenance remains
in `character-selection/ASSET-PROVENANCE.md`; C2 copies those converted runtime
assets without changing their artwork or source provenance.

Manual review should cover visual layout at the existing 1000×800 window and
other sizes, preview framing/rotation/zoom, keyboard and mouse navigation,
audio balance, normal survival gameplay and death → R / Return to Main Menu.
The lightweight smoke does not establish full combat balance, long-run stability
or pixel-identical original SWTOR presentation.
