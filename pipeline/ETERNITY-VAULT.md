# Eternity Vault surface exploration

Launch through New Run → choose an implemented class → Eternity Vault → Set Course.
The imported geometry remains limited to the surface and visible entrance.
Normal launches now start the level 1–50 encounter described in
[ETERNITY-VAULT-ENCOUNTER.md](ETERNITY-VAULT-ENCOUNTER.md). The explicit
`--eternity-vault-smoke` option retains the earlier map-only inspection mode.

Client-derived source: area `4611686096259570491`, header `raid_eternity_vault`,
room `01_Surface`. The area header decoded SHA256 is
`96f2a37775f625425a1bbc71accdf6af3d8032bf696c49e8076dc8443184a15b`.
17 terrain patches, 298 selected placements and 55 unique environment models.
Source receipts and conversion/build reports are `manifests/map-eternity_vault-*.json`.

Requested location: `location.flashpoint.raid.eternity_vault`, game coordinates
`(-640, 307, 40)`. Configured Y-up source seed: `(-64, 4, 30.7)` (game coordinates
scaled by 0.1 with altitude moved to Y). Runtime projects that seed onto nearby
source terrain, rather than searching for a different open arena location.
The supplied `map_eternity_vault_world` and `map_eternity_vault_surface` identifiers
are retained in the recipe as references; they do not identify room geometry.

Rebuild: `./Run-Map-Pipeline.ps1 -Map eternity_vault`, then rebuild the C# project.
Recipe: `maps/eternity_vault.json`. Two copied terrain records retain stale embedded
origins; room placement transforms are used, and deltas remain in the selection evidence.
Collision and terrain materials use the existing map conversion pipeline. Native
SWTOR lighting, procedural vegetation and encounter logic are outside this import.

Validation: C# build passed (existing warnings); map build passed with 139,210
terrain triangles and maximum placement matrix error 0.00000763. Headless runtime
smoke passed: stable player at `(-64, 2.7965004, 30.7)` after ground projection,
zero recoveries, registered UI destination, and no spawner or wave director.
Visual appearance and manual movement still need an in-game review.

Visual refinement: editor-only `portal`, `util_collision_none`, and
`dbo_universal_superexclusion_test` faces are removed during this map's model
conversion, preserving the entrance door/stone materials. Terrain now uses the
original full-resolution diffuse layers, source blend weights, vertex tint and
a tiled dominant-layer normal instead of patch-resolution albedo baking.
The original `bel_raid_skyscene` is camera-centred using the source sky camera
offset. The generated falling flakes and global fog have been removed.
Menu icon: original Belsavis planet. Loading and preview: April
`/resources/gfx/loadingscreen/raid_eternity_overlay.dds` (the newer filename
`eternity_vault_overlay.dds` is absent from these archives). Exact selected
resource hashes are in `map-eternity_vault-refinement-source.json`.

The entrance is completed by the separate `Default`-room instance
`4611686142220981203`: `mag_doorrakatalg_doorlg.mag`, which resolves to the
original door GR2, `doorrakatalg_skeleton.gr2`,
`bel_arch_rakata_entrance_door_small_01.mat`, and `idle_close.jba`.
The original GR2 bind geometry is used directly at the exact authored transform
`(-64.1681976318, 2.79450011253, 39.620300293)`. This adds no encounter logic.
An operator-requested visual alignment offset of `(0.1681976318, 0, 0.4)` now
centres the bind-pose door farther inside the entrance at
`(-64, 2.79450011253, 40.020300293)`. The raw authored position is preserved
separately; the repeatable offset lives in `maps/eternity_vault.json`.
At the operator's request, `BuildMap.gd` adds an unlit black backing behind
this door to cover edge gaps. It follows the door's transform, has no collision,
and is kept separate from the original asset geometry.
The attempted JBA pose conversion produced angled panels and is not used; the
original bind geometry was visually checked as the assembled door. Animation
resources are retained as evidence, not claimed to be faithfully playing.
The editor portal material remains hidden; it is not the missing door asset.
The original entrance interior and grey fade portal from `01_Surface_Transition`
are included at their authored transforms to complete the visible entrance.

Weather: `eternity_authored.py` retains all 52 surface placements of
`fxtest/ed_env_raid_snow_test` and follows its two explicit 0.1-second
`raid_snow_cloud.prt` emitters to `raid_snow_cloud_p.prt` and original
`fx_smoke_09.dds`. The runtime reads source offsets, trajectories, life/size
ranges, scale/color curves, blending and distance fades. No generated sprite
art or guessed emitter frequency is used. Two snow-speck entries with empty
frequency fields are explicitly omitted pending native-default evidence.
Native random-distribution/spline semantics and buoyancy acceleration units
remain unresolved; the bounded adapter uses linear interpolation of recovered
curve endpoints and does not invent buoyancy acceleration. Overhead lightning,
sound and other FXSpec branches are not represented as complete native playback.
Raw dependency hashes: `map-eternity_vault-authored-source.json`.

Raised-platform movement: this map sets `GroundCollisionMask = 3` so imported
prop surfaces are valid ground as well as terrain. Other maps keep mask 1.
Physics smoke crossed both circular platforms centred at X=-61.3 and X=-66.7001,
Z=34, from 1.5 units to the left to more than 1.35 units to the right, including
entry and exit, with no recoveries. Repeat with
`--eternity-vault-smoke --eternity-vault-platform-smoke` on the direct launch.

Repeat runtime check after building:
`tools/godot/Godot_console.exe --headless --path godot res://Gameplay/GameWorld_EternityVault.tscn -- --advanced-class=sorcerer --map=eternity_vault --eternity-vault-smoke`
