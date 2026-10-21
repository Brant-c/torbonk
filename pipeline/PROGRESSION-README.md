# SWTOR Survivors — B4

B5 now extends these systems to three automatic abilities and configurable
rarity strength. See [ABILITIES-README.md](ABILITIES-README.md) for current
behavior. The B4 notes below describe the previous milestone.

B4 adds XP pickups, levels and three-choice upgrades to the B3 survival world.
The gameplay HUD and selection screen use original April SWTOR textures, ability
icons and embedded Eurostile outlines, reconstructed with Godot controls.

The HUD now follows the supplied SWTOR screenshot: player portrait at the left,
target portrait at the right, paired health strips above one row of twelve
quickbar slots, and a thin gold XP bar beneath. The player portrait renders the
existing imported character in an isolated viewport; the target uses the same
model as the current B3 enemy and shows an original empty portrait plate when
there is no target. Only Force Lightning is populated; other slots remain empty
until actual abilities are implemented. Its attack remains automatic. Hover
the portrait, ability, bars or XP strip for details. The blue strip below health
shows Lightning readiness, since this prototype does not have Force costs.
The dock is compact and centered against the bottom edge. Wave/time/defeated
and Restart share one matching, opaque top strip. Original chrome is backed
with solid navy throughout buttons, panels and tooltips to keep the world from
showing through their interiors.

Launch from PowerShell:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

The launcher builds C# and opens `Gameplay/GameWorld.tscn`. Add `-Editor` to open
that scene in Godot. The project's default scene remains the original inspection
scene, so use this launcher or F6 on GameWorld for gameplay.

| Control | Action |
|---|---|
| WASD | Move relative to the camera |
| Mouse, or 1 / 2 / 3 | Choose an upgrade while the level-up screen is open |
| R, Restart, New Run | Clear the entire run, including a pending upgrade screen |
| F3 | Toggle retained collision/bounds diagnostics |
| Esc | Quit |

Lightning casts automatically. Defeated enemies drop blue skill markers. Walk
near them to collect XP; they move toward the player inside the magnet radius.
Three ordinary kills earn the first level. Selection pauses the scene tree,
including enemies, combat, wave timers, pickups and the run clock. UI remains
active. Overflow XP queues further choices individually.

| Upgrade | Effect per rank | Original icon |
|---|---|---|
| Crackling Power | +20% base Lightning damage | `sithsorcerer` |
| Forked Lightning | +15% base tick frequency | `forkedlightning` |
| Lightning Reach | +15% base acquisition range | `chainlightning` |
| Recklessness | +20% base recovery speed | `chargedgauntlets` |
| Mark of Power | +20 maximum HP; heal 20 | `markofpower` |
| Force Speed | +10% base movement speed | `forcespeed` |

The effects and upgrade names/arrangement are Survivors design. They do not
claim to restore SWTOR's original talents or balance. The April GOM directly
maps Force Lightning, Mark of Power, Force Speed and Recklessness to the icon
names shown. In particular, April Recklessness specifies `chargedgauntlets`,
despite a separate `recklessness.dds` also existing. The other two upgrades reuse
thematically appropriate original icons without claiming a verified talent link.

Effects derive from captured run-start stats rather than multiplying previous
results. Tick interval and cooldown equal base / (1 + total rank bonus).
Tick changes take effect at the next channel; cooldown changes at the next
recovery. R restores all six base stats, player health/position, waves, enemies,
XP, levels, ranks, pickups, elapsed time and pending choices.

Tune the six resources in `godot/Gameplay/Upgrades/`. `MaximumRank=0` means
unlimited; this keeps three distinct choices available throughout a run. A
custom finite catalog must retain three eligible entries to continue leveling.
XP defaults are 12 × 1.3^(level−1), rounded up; enemies drop 4 XP. Progression
and pickup settings are exported on `RunProgression` (created by GameWorld).
Pickups are capped at 128 rendered instances; later drops merge their XP into
an existing nearby pickup.

Reusable UI components are in `godot/Gameplay/UI/`: `SwtorSkin`, `SwtorPanel`,
`SwtorBar` (health/XP/resources), `AbilityIcon` (radial cooldown), `SwtorButton`,
`UpgradeCard`, `SwtorTooltip` and `SurvivorsHud`. State and gameplay effects live
in `RunProgression`, independently of the interface. The old text HUD is hidden
during normal gameplay and remains available with F3 and old validation flags.

Rebuild the UI assets with:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Convert-UI.ps1'
```

This uses the bundled Python runtime, Pillow and vendored fontTools 4.60.1. Pass
`-Python` to select another compatible Python with Pillow. Selection, limits,
font IDs and UI role mappings are in `ui-recipe.json`. Godot imports the generated
assets when opening the project. See `UI-PROVENANCE.md` for archive identities,
dependencies and conversion details.

Known limits: the HUD follows the supplied layout, with the three-column upgrade
choice adapted for Survivors. Original panel/corner pixels use nine-slicing; this is not an
exact recreation of SWTOR's complete HUD layout or Scaleform animation/state
machine. Font kerning is not converted; missing symbols can use Godot/system
fallback. The XP marker repurposes an original skill button glyph, rather than
claiming it was a native SWTOR world pickup. No resource pool,
persistent progression, original talent-tree logic or networking is
added. Visual comparison, resizing, tooltip interaction and gameplay balance
are left for manual review. B4 is the stopping point.
