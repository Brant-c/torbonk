# TorBonk — SWTOR Survivors

TorBonk is a Vampire-Survivors-style auto-combat game built in Godot from
original Star Wars: The Old Republic client assets. Models, animations, FX,
audio and UI are extracted from a locally held April-2012 client, converted
through Blender and imported into Godot; the game logic is new C#.

You pick a Sith Empire class on an SWTOR-style main menu, drop onto a bounded
Korriban map, move with WASD while your abilities fire automatically, collect
XP, choose level-up upgrades and fight a boss.

**Current state (D3):** all eight Imperial advanced classes are playable on
Korriban, each with its own original armor, weapons and starter ability.
Republic classes are preview-only. Collections → Appearances saves Sorcerer
cosmetics between runs. See [pipeline/D3-README.md](pipeline/D3-README.md) for
the class table and manual review checklist, and
[pipeline/D3-HANDOFF.md](pipeline/D3-HANDOFF.md) for D3 presentation fixes
that are written but not yet built or tested.

## Repository layout

| Path | What it is |
|---|---|
| `pipeline/` | Main workspace: conversion pipeline, PowerShell launchers and smoke checks, per-milestone READMEs/RESULTS/PROVENANCE notes. |
| `pipeline/godot/` | **The game.** Godot 4.7.2 .NET project (`SwtorAssetInspection.csproj`, net8.0, nullable enabled). Main scene: `res://Frontend/Scenes/MainMenu.tscn`. Gameplay lives in `Gameplay/`, converted assets in `Assets/`. |
| `pipeline/scripts/` | Python extraction, conversion, probe and receipt scripts used by the milestone pipelines. |
| `pipeline/manifests/` | Source receipts, hashes, tool lock (`tooling.lock.json`) and smoke-check results. |
| `character-selection/` | C1 frontend module (main menu, character and galaxy selection). Developed as a separate harness project; its `Frontend/` folder is integrated into `pipeline/godot/Frontend`. |
| `docs/investigation/` | The original 2026-10-20 feasibility investigation and asset evidence. |

Start with [pipeline/README.md](pipeline/README.md), which links each
milestone's notes from newest to oldest.

## Requirements

Everything is set up for Windows and PowerShell. The scripts assume the
repository lives at `D:\SWTORClassic\torbonk`; several READMEs and a few
scripts hard-code that path.

To **play and run smoke checks**:

- Windows with PowerShell 5.1 or later.
- .NET 8 SDK (`dotnet` on `PATH`). Builds use `--no-restore`, so the Godot
  NuGet packages must already be in the local package cache.
- Godot 4.7.2 stable **.NET (mono)** for win64, copied into
  `pipeline/tools/godot/` as `Godot.exe` and `Godot_console.exe` together with
  its `GodotSharp/` folder. `pipeline/Prepare-Tools.ps1` does this from an
  existing install (`-InstalledGodotRoot`, default `D:\WorkspaceVS`) and creates
  the `_sc_` marker that keeps editor settings self-contained.

To **re-run asset extraction and conversion** you also need:

- The April-2012 SWTOR client `.tor` archives (91 archives, about 17 GiB).
  They are opened read-only and are **not** in this repository.
- Blender 5.2.1 under `C:\Program Files\Blender Foundation\Blender 5.2\`.
- The pinned Blender add-ons (`io_scene_gr2`, `zg_swtor_tools`).
  `Prepare-Tools.ps1` downloads them from fixed GitHub release URLs and checks
  their hashes against `pipeline/manifests/tooling.lock.json`.
- Python 3 for `pipeline/scripts/`.

`pipeline/tools/`, `pipeline/source-assets/`, `pipeline/work/` and
`pipeline/generated/` are git-ignored, so a fresh clone has none of them.
The converted Godot assets under `pipeline/godot/Assets/` **are** committed,
which is why playing does not need the archives.

## Assets and licensing

All game art, animation and audio here comes from SWTOR, which is owned by
Electronic Arts, and is used for a non-commercial fan project. Each
milestone's `*-PROVENANCE.md` and the manifests record exactly which original
resources were used and how they were adapted. Because converted and raw
client assets are committed, this repository should be private, and builds
should not be redistributed.

## Setup

```powershell
cd D:\SWTORClassic\torbonk
.\pipeline\Prepare-Tools.ps1            # once, or when pipeline/tools is missing
.\pipeline\Assert-Toolchain.ps1         # optional: verify pinned tool hashes
```

## Playing

```powershell
.\pipeline\Open-Survivors.ps1           # build C# and open the main menu
.\pipeline\Open-Survivors.ps1 -Editor   # open the Godot editor instead
.\pipeline\Open-Gameplay.ps1            # skip the menu and start gameplay directly
```

Choose **New Run → Sith Empire → base class → advanced class → Korriban → Play**.
WASD moves, Space jumps, hold RMB (or drag LMB) to orbit the camera, wheel
zooms, R restarts, and Main Menu returns to selection.

## Smoke checks

Each `pipeline/Run-*-Smoke.ps1` script builds the C# project, optionally
re-imports assets, runs one short rendered Godot session with an isolated
`APPDATA`, and fails unless the session writes a passing receipt to
`pipeline/manifests/<name>-smoke.json` with no script errors in its log. Logs go
to `pipeline/logs/`. `Run-D2-Smoke.ps1` and `Run-D3-Smoke.ps1` accept
`-SkipImport` to reuse the existing import. They need a real desktop session
because they render.

| Script | Milestone it covers |
|---|---|
| `Run-D3-Smoke.ps1` | D3: all eight Imperial classes, starters, periodic effects, splash (107 checks at last pass) |
| `Run-D2-Smoke.ps1` | D2: Chain Lightning, Overload, run slots, artifacts and run items |
| `Run-D11-Smoke.ps1` | D1.1: Collections wardrobe and cosmetic saves |
| `Run-D1-Smoke.ps1` | D1: equipment composition |
| `Run-C2-Smoke.ps1` | C2: frontend-to-gameplay integration |
| `Run-B101-Smoke.ps1` | B10.1: SWTOR main menu and HUD (`-HudReview` adds HUD color checks) |
| `Run-B9-Smoke.ps1`, `Run-B9-Jump-Smoke.ps1` | B9: cursor, orbit camera, jump |
| `Run-B8-Smoke.ps1` | B8: Death Field, Static Barrier, Dark Heal |
| `Run-B7-Smoke.ps1` | B7: boss, leveling, XP pickups, audio |
| `Run-B6-Smoke.ps1` | B6: enemy roster and equipment |
| `Run-Combat-Smoke.ps1`, `Run-Survival-Smoke.ps1`, `Run-Progression-Smoke.ps1`, `Run-Abilities-Smoke.ps1`, `Run-Refinement-Smoke.ps1` | Earlier gameplay foundations |
| `Run-Flytext-Smoke.ps1`, `Run-Audio-Polish-Smoke.ps1`, `Run-Storm-Reticle-Smoke.ps1`, `Run-Storm-Source-Smoke.ps1` | Presentation pieces |

The newest smoke is the one to run after a change. Older ones were last run
at their own milestone and are kept as historical regressions.

Conversion pipelines (`Run-Pipeline.ps1`, `Run-Character-Pipeline.ps1`,
`Run-Lightning-Pipeline.ps1`, `Run-Korriban-Pipeline.ps1`) re-extract from the
archives and need the full toolchain above. The `Open-*Inspection.ps1` scripts
open the standalone asset inspectors from milestones A1–A4.

## Development notes

- Milestones end with a manual review in the real game; passing smoke checks
  are not treated as acceptance of visuals, audio or balance.
- Builds currently report NU1900 (NuGet vulnerability feed unreachable offline);
  it is harmless.
- `character-selection/` is a development harness. Only its `Frontend/` folder
  is meant to be carried into the game; see
  [character-selection/INTEGRATION.md](character-selection/INTEGRATION.md).
