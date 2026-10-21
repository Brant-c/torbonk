# C2 integration receipt

Implemented in `pipeline/godot`; stopped at C2 for manual review.

- Confirmed the prior B7 chat was idle and its latest turns complete before
  editing. Preserved the current reduced spawn pressure and later audio fixes.
- Integrated only C1's transferable `Frontend/`. Main menu is the default entry.
  Main project/physics/render/input settings and direct gameplay launcher retained.
- Added an explicit validated selection adapter, a minimal scene handoff and a
  game-over Return to Main Menu button. Unsupported selections cannot launch.
- Compiled the existing C# project successfully, zero errors. Existing NU1900
  and StormAreaIndicator CS8600 warnings remain. No new compiler warnings.
- Imported copied frontend resources successfully; no Godot import errors.
- One brief rendered session passed **20 checks**. Receipt:
  `manifests/c2-smoke.json`; runtime: `logs/c2-runtime.log`. No Godot runtime
  errors. No historical regressions, archive hashes, reconversions, benchmarks
  or comprehensive gameplay suites were run.
- Verified actual menu page/button navigation, Sorcerer 3D preview, valid Play,
  unsupported class/map blocking and forged flag rejection; only one of the
  catalog's 16 advanced classes × six maps resolves to gameplay.
- Verified current B7 world initialization, HUD/abilities and spawn defaults,
  in-place R restart, return while progression is paused with a live boss,
  freeing gameplay/preview/audio nodes, restored menu focus/selection, fresh
  subsequent gameplay and the real game-over Return button.
- A pre-integration local comparison confirms the only changed preexisting
  gameplay scripts are GameWorld.cs and UI/SurvivorsHud.cs. Saved gameplay scene,
  other gameplay files, character/environment resources, C# project and
  Open-Gameplay.ps1 remain unchanged. The only project setting changed is the
  default entry scene.

Launch the full game with `Open-Survivors.ps1`, or open the existing project in
Godot 4.7.2 .NET and press F5. Use `Open-Gameplay.ps1` or F6 on GameWorld for direct
development gameplay. Instructions and known limits: [C2-README.md](C2-README.md).

Manual visual, input and listening review remains. Other classes/maps and
appearance editing remain unavailable; packaged export and long-run gameplay
were not tested. No later milestone has been started.
