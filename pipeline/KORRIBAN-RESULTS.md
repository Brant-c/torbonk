# SWTOR Survivors — Milestone A4 results

The original April-profile Valley of the Dark Lords statue approach is
reconstructed as a bounded, reusable Godot environment. Two adjacent original
terrain patches, original layered ground art, giant Sith statues, cliff/rock
formations, ruined architecture and excavation props are assembled automatically
from source placements. No gameplay was implemented.

## Source selection

The area is `4611686022825732973` (`kor_rebuild_test`, corroborated as Korriban by
the initial investigation); the selected room is `baseroom`.

| Original resource | Decoded SHA-256 |
|---|---|
| `area.dat`, 337,017 bytes | `ce05e870ec10ecc70845684e2f5f77a27f79aa5c9fd983206310c94f89e77a63` |
| `baseroom.dat`, 5,935,819 bytes | `467379eaf14e5f1902dcb3c9fc4b0f826e333c643360eae13fe620c20a313f82` |

Both match the verified investigation. Their TOR entry identities and offsets
remain in the source/dependency manifests. The profile is operator-labelled
April2012; it is not independently authenticated against a retail patch manifest.

Terrain instances `4611686022895042660` and `4611686022895042664` occupy a nominal
25.2 × 12.6 source-unit rectangle. Root prop origins are selected within the small
recipe margin; every selected descendant and required ancestor is retained.
The final hierarchy contains **109 placement records, 107 visible model instances,
31 unique models and 30 Uber materials**. Thirteen records have nonzero parents.
The two other records are original `engine/fxplaceable.fxp` transforms; their
native effects are not executed. No invisible utility is replaced with artwork.

The monumental statues retain source instances `4611686022895031560` and
`4611686022895031562`, source models `kor_vod_greatstatue.gr2` and
`kor_vod_greatstatue02.gr2`, source positions/180° rotations and unit scales.
The bounded extraction contains **151 files, 45,339,962 decoded bytes**, including
the two whole source DAT resources required to decode their embedded records.
Other room payloads and the rest of the planet's terrain/model dependencies are
not extracted. A complete raw-file count corrects the initial bounded query's
reported 51 heightmap matches: this baseroom has 46 unique heightmap instance
headers, all with VertexData. The earlier query count is not used as an asset
inventory. Only the selected two records are converted for the environment.

## Geometry and transforms

- Terrain contains **8,192 source grid vertices and 15,863 triangles**. Heights,
  original instance offsets, 0.2 grid spacing, even-grid centering and source
  hole masks are decoded. No substitute surface, smoothing, seam welding or
  manual terrain placement is introduced.
- Original border float32 heights are retained. Interior uint16 heights use exact
  `/512` fixed point. Each selected patch's payload and decompressed-byte SHA-256
  are recorded separately in the terrain conversion manifest.
- The shared edge has 64 height samples. Maximum X difference is
  `3.814e-7`, maximum Y difference `9.98973298e-5`, and Z difference zero, in
  source units. The Y discrepancy is already present in the original border
  floats plus original placement Y; it is retained and bounded explicitly at
  `0.00011`. There is no repair concealed by the adjacency check.
- The selected source tessellation fields are empty. A4 uses the same fixed
  diagonal as STE2 and the source hole bits. Native empty-field defaults are
  unverified; triangle connectivity is a decoded grid reconstruction, not an
  original serialized render-index buffer. Normals/tangents are derived from
  that grid. These topology/shading differences are distinct from preserved
  source vertex heights.
- The **unmodified upstream STE2 Heightmap.cs** at commit
  `5fe5751b59af6bfb7b653289dfa8e1a81f1e1f57` was compiled and actually invoked on
  the two selected decompressed blobs. Grid XZ agrees within `1.91e-7`.
  Its rounded multiplier `0.0019531548` differs from exact `/512`, producing
  maximum height differences `0.0003612058` and `0.0001513840`. The adapter uses
  exact fixed point; it does not copy that avoidable approximation.
- Source parent-child matrices include rotation, nonuniform scale and local
  translation. Blender independently composes `T * Ry * Rx * Rz * S`; Godot
  explicitly constructs the same basis. Maximum world-matrix component
  difference is about **3.28e-7**. Position-only parent addition is not used.
- Every one of the 31 source GR2 models is reimported independently and compared
  against its generated GLB. **Positions and UVs have zero measured round-trip
  error, and triangle winding survives**. Direct serialized normal component
  error is at most `0.000063956`; Blender custom-normal re-encoding on reimport
  reaches `0.287265°` on the tent. These small shading changes are measured,
  not labelled exact preservation. Model triangle totals and materials are also
  checked in Godot; automatic LOD generation and mesh compression are disabled.
- Shared GLB mesh resources are reused by the scene's prop instances. Collision
  shapes are derived static triangle geometry for inspection. Original source
  collision shapes, physics flags and navigation behavior are not reconstructed.

## Terrain materials

The empty `TexWeightData` fields did **not** mean unblended ground. VertexData
version 1 contains a dense index/weight table. Version 9 contains sparse blends
and an April-specific dense copy with padded rows. Both selected patches preserve
127 × 127 splat samples. Color maps and DYD density channels are also decoded.
Both payloads parse through their documented tails rather than dropping unknown
bytes after the height grid.

The selected area table uses original dirt (layer 1), purple rock (3),
`ter_kor_sand_trodden` (18), `ter_kor_sand_trodden2` (19), `ter_kor_rough` (20),
and `ter_kor_vod_ruins_floor` (2). Direct DDS and `bwa:` MAT references all resolve
uniquely in the April archives. All used diffuse, rotation, gloss and auxiliary
material inputs are retained. **Every layer's source weight values agree exactly
along all 127 samples of the shared texture edge.**

The portable bake uses all original layers, their bilinear source weights and
original vertex-color tint. Raw weight/color PNGs are retained alongside the
1024² albedo/normal/roughness maps. Source material UVScaling values are used;
the direct legacy entries specify scale 2. A continuous world-XZ projection
keeps the selected patch boundary coherent. Original DDS data, MAT parameters
and raw source paint remain available for future shader work.

Rendering limits are explicit:

- Native terrain UV phase, orientation, color-tint transfer and terrain-normal
  conventions have not been compared against a running April client. Continuous
  XZ projection and multiplicative tint are reconstruction choices. The portable
  atlas has finite resolution and different filtering/mips from native layer
  sampling. The evidence establishes source-derived layered artwork and paint,
  not native pixel parity.
- Uber normals, gloss and specular use the existing bounded A1 material adapter.
  View-dependent coupling, reflections and colored specular are approximate.
  There is no unresolved palette family for this selection: all selected MATs
  derive from Uber.
- Original DYD densities and names remain in source evidence; native procedural
  grass/pebble scattering is omitted. Original explicitly placed rocks and props
  are present. The two original environment-FX nodes retain their transforms
  without native FX execution. Area audio and distant scenery are outside this
  bounded reconstruction.
- Warm directional light, ambient/fill light and a simple authored sky approximate
  Korriban. These are inspector additions, not source lighting/sky shader parity.
  Large statues and cliffs intentionally extend beyond the terrain crop; open
  edges expose the bounded extraction. No surrounding filler is generated.
- Physical metre calibration and native rendering comparison remain unverified.
  The existing Sorcerer is shown at its preserved scale 1, without a guessed
  character-to-environment rescaling. It is an inspection reference, not a source
  NPC placement or movement controller.

## Validation and preserved work

The environment builder verifies source model triangles/materials, all selected
placement matrices and scene serialization. The inspector reloads the generated
scene, checks its terrain/mesh/collision coverage and shared resource reuse,
exercises terrain/prop visibility and collision overlays, and produces four actual
Godot views. The OpenGL Compatibility renderer runs on the existing RTX 3070 Ti.
The restricted-profile shader-cache warning is unchanged and nonblocking.

The final A4 repeat compares **211 asset files with zero changes**, including
31 GLBs, original-weight/portable/imported PNGs and the environment input JSON.
Generated Godot scene IDs and rendered images are hashed
as artifacts, with no requirement that they repeat byte-for-byte. Refer to the
machine-readable run/repeat manifests for the exact execution scope.

The complete unchanged A3 pipeline was run during A4, including real rendered
Lightning/audio validation, character poses/roundtrips, doorway geometry, .NET
build, repeat asset hashes and the full **91-archive SHA-256 check**. All passed.
The final A4-only repeat uses `-SkipRegressions` after that complete regression;
these separate executions are recorded honestly. Every selected A4 original is
also compared again with the current archive payload bytes.

No original TOR archive, emulator source/configuration, extractor configuration,
global Blender preference or existing A1–A3 conversion/inspection script was
modified. Existing documented A1–A3 fidelity limitations remain unchanged.

**A4 stops here.** There is no conversion blocker for this bounded environment.
Native terrain shader/default tessellation parity, DYD/FX execution, complete
source lighting and physical-unit calibration remain fidelity blockers. No
combat, character movement controller, enemies, progression or waves follow
automatically. Launch and rebuild commands are in `KORRIBAN-README.md`.

Format references: [STE2 source](https://github.com/UltimaKaosXIII/STE2),
[Jedipedia reader](https://swtor.jedipedia.net/reader). Source snapshots, hashes and
their limited use as decoder evidence are recorded in
`manifests/korriban-format-references.json`.
