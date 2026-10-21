# B4 results — 2026-10-20

The HUD was subsequently revised to follow the user's screenshot. See
`HUD-REVISION-RESULTS.md` for the current layout and validation.

Implemented and stopped after B4 for manual review.

- C# build passed: zero errors. NU1900 reports unavailable NuGet vulnerability
  metadata; compilation used the existing restored dependencies.
- One brief rendered Godot 4.7.2 .NET run passed eight B4 checks: world/HUD
  initialization; death → XP pickup; collection → three distinct choices;
  scene pause; choice/rank/resume; selected stat effect; increasing requirements
  with queued overflow; restart from the paused screen restoring base stats.
- Receipt: `manifests/progression-smoke.json`. Runtime log:
  `logs/progression-smoke-runtime.log`. No script, UI or gameplay errors.
  The existing sandbox `user://` shader-cache warning remained; rendering worked
  on the RTX 3070 Ti. Headless asset import also reports its unavailable
  `user://` editor snapshot directory.
- Final original-art selection contains 41 preserved source resources, 21
  converted atlases, 260 exact subimage crops, nine icons and two fonts.
  The final metadata/import pass includes the GOM-verified April Recklessness
  icon (`chargedgauntlets`). No further gameplay smoke was run for that icon
  correction or the font bearing/timestamp conversion refinement.
- No historic regressions, bulk archive hashing or runtime screenshot captures
  were performed. Source archive metadata still matches the existing baseline.

Run `Open-Gameplay.ps1` for review. Test card hover/focus, tooltips, long-run
balance, movement/Lightning upgrades, death/restart and alternate window sizes
manually. `Run-Progression-Smoke.ps1` is available for a later focused rerun.
