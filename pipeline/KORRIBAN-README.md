# SWTOR Survivors — A4 Korriban environment inspection

The standalone scene reconstructs the paired monumental statue approach in the
Valley of the Dark Lords from the original April-profile Korriban baseroom.
It contains two adjacent terrain patches, original blended ground textures,
107 visible prop instances using 31 shared source models, and two retained
environment-FX transforms. There is no gameplay or emulator dependency.

## Launch

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-KorribanInspection.ps1'
```

Add `-Editor`, select `KorribanInspection.tscn` and press F6 to inspect in the
existing Godot 4.7.2 .NET project. The doorway remains the project's main scene.
The existing doorway, Sorcerer and Force Lightning launchers still work.

| Control | Action |
|---|---|
| Left drag / wheel | Orbit / zoom |
| Middle or right drag | Pan |
| F | Switch between orbit and free-fly camera |
| WASD, Q/E; Shift | Fly camera horizontally, down/up; faster travel |
| Right drag in free-fly | Look around |
| Esc | Return to orbit |
| 1 / 2 / 3 / 4 | Overview / statue / terrain / original Sorcerer scale |
| Checkbuttons | Terrain, props, Sorcerer and derived collision overlay |

Only the camera moves. The Sorcerer is an idle, 1:1 inspection reference placed
on a decoded terrain vertex; its location is an inspector addition, not an
original NPC placement. Collision debug draws derived triangle shapes and is
not a reconstruction of SWTOR's physics or a gameplay controller.

## Rebuild

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-Korriban-Pipeline.ps1' -Capture
```

The runner checks the existing pinned toolchain; compares/extracts only selected
source dependencies; runs the unmodified STE2 heightmap reader on the two blobs;
converts 31 unique models; validates original GR2/GLB triangle corners; reconstructs
terrain and blends; imports assets; builds and reloads the reusable Godot scene;
tests visibility/collision controls; captures four actual views; verifies source
bytes; and compares generated assets against the previous conversion.

It also runs the unchanged full A3 runner, which includes the character/doorway
checks, .NET build, runtime checks and 91-archive SHA-256 pass. Use
`-SkipRegressions` while iterating A4, or `-SkipArchiveRehash` to run regressions
without the expensive whole-archive read. The run manifest records these choices.
Every subprocess checks its exit code and writes `logs/korriban-*.log`.

Conversion is offline when the existing tool folders are present. Reuse the
existing `Prepare-Tools.ps1` and `Prepare-Lightning-Tools.ps1` if restoring the
A1–A3 toolchain. The additional `tools/ste2-reference/Heightmap.cs` is the exact
public upstream file at the commit and hash in
`manifests/korriban-format-references.json`. Restore that file from its recorded
commit if missing; do not regenerate its hash to bypass a mismatch. The small
test harness builds into `work/korriban/ste2-bin` and `ste2-obj`.

## Reuse the environment

Instance `res://Assets/Korriban/KorribanEnvironment.tscn`. This generated scene
contains terrain ArrayMeshes/materials, shared original-model GLB instances,
the complete selected local transform hierarchy and derived static triangle
shapes. It contains no inspector camera, lights, character or gameplay scripts.
The inspector adds authored lighting and its Sorcerer reference independently.

The scene needs the generated `Assets/Korriban/` files. Do not edit generated
meshes, maps or placements by hand; change the bounded recipe/converter and
rebuild. All source placements retain source IDs as metadata. Raw authored
properties and relationships remain in the dependency manifest.

## Source and conversion contract

- Area `4611686022825732973`, room `baseroom`; terrain instances
  `4611686022895042660` and `4611686022895042664`.
- Terrain nominal footprint: X `20.2..45.4`, Z `6.2..18.8`, or `25.2 × 12.6`
  source units. Prop roots use the small explicit margin in `korriban-recipe.json`;
  original large statues/cliffs extend beyond the terrain rectangle.
- Original Y-up coordinates and unit scale 1 are preserved. Physical metres are
  uncalibrated. No independent model, character or terrain scaling is guessed.
- Parent transforms use `T * Ry * Rx * Rz * S`, matching the maintained area
  assembler's Blender ZXY convention. Godot's identically named Euler mode has
  different composition semantics, so the converter constructs the basis
  explicitly. World matrices are checked independently against Blender.
- Each patch has a 64 × 64 original height grid at 0.2 source-unit spacing.
  Border heights are original float32 values; interior heights are original
  uint16 fixed point divided by 512. Source hole bits suppress original missing
  regions. The selected tessellation fields are empty: fixed STE2 diagonals are
  used, and the native default remains unverified.
- VertexData versions 1 and 9, zlib/gzip wrapping, dense and sparse weights,
  April sparse-layout dense weight copies, vertex colors and DYD channels are
  decoded. Empty `TexWeightData` does not mean absent blending. These patches
  carry 127 × 127 source splat grids inside VertexData.
- All six used layers resolve through the original area table. Direct dirt and
  purple-rock DDS entries combine with `bwa:` sand, second sand, rough rock and
  ruins-floor MAT dependencies. Original maps and parameters are retained.
- Original weight values are preserved as lossless diagnostic PNGs. Portable
  1024² albedo/normal/roughness maps use bilinear weight sampling and the original
  diffuse, packed rotation, gloss and vertex-color data. Continuous world XZ UVs
  use source material UVScaling where available. Native UV phase/orientation,
  normal/tint semantics and shader parity are not established.
- All 30 selected materials are Uber. The established A1 diffuse-gamma,
  packed-normal and gloss-to-GGX adapter is reused for models. Native reflections,
  colored specular and lighting coupling remain approximations. No palette-based
  material family is required by this selection.
- The budget is 300 files / 160 MiB; actual selected originals are 151 files /
  45,339,962 bytes. No other room payload or planet terrain is extracted.

## Evidence

| File | Contents |
|---|---|
| `korriban-recipe.json` | Fixed source selection, limits, transforms and rendering policies |
| `manifests/korriban-source.json` | Every archive/entry/decoded hash, offset and dependency edge |
| `manifests/korriban-dependencies.json` | Exact area/room identities, layers, models and all selected authored placements |
| `manifests/korriban-format-references.json` | Pinned STE2 source and public reader format-reference provenance |
| `manifests/korriban-ste2-validation.json` | Actual unmodified upstream height-reader compatibility comparison |
| `manifests/korriban-model-conversion.json` | Per-model bounds, triangles, source/output hashes and full world matrices |
| `manifests/korriban-model-roundtrip.json` | All 31 original model geometry, UV, winding and normal comparisons |
| `manifests/korriban-terrain-conversion.json` | Per-patch raw blob identities, height/paint decoding, adjacency and limits |
| `manifests/korriban-godot-build.json` | Material/triangle checks and independent world-matrix agreement |
| `manifests/korriban-godot-validation*.json` | Reloaded scene, shared resources, toggles, collision and rendered checks |
| `manifests/korriban-source-integrity.json` | Every selected original compared again with archive payload bytes |
| `manifests/korriban-repeat-validation.json` | GLB/map/input-JSON determinism |
| `manifests/korriban-artifacts.json` | Output and capture identities |
| `manifests/korriban-validation.json` | Final A4 acceptance and linked full A1–A3 regression receipts |
| `generated/korriban/01..04-inspection.png` | Actual overview, statue, ground and Sorcerer views |
| `KORRIBAN-RESULTS.md` | Acceptance evidence, approximations and blockers |

The initial investigation and all A1–A3 scenes/scripts remain preserved. Source
archives are opened read-only; emulator files and extractor configuration are
not edited. A4 ends here, before gameplay.
