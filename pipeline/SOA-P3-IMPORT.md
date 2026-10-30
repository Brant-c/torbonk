# Soa room restoration — 2026-10-28

Operations → Eternity Vault → Soa → Set Course enters the restored room with the selected class. The level 1–50 combat run is described in `SOA-ENCOUNTER.md`. Boss navigation and difficulty selections are preserved.

Rebuild with `./Run-Soa-P3-Import.ps1`; add `-Preview` for the independent environment viewer. World composition: `godot/Gameplay/GameWorld_SoaP3.tscn`. Environment: `godot/Assets/Maps/soa_p3/MapEnvironment.tscn`.

## Restored map

Client-derived April area `4611686096259570491`, label `raid_eternity_vault`, room `05_Infernal_Prison`. The initial import selected only Y=-40..-34. The expanded recipe now selects every visible static area model in this room and retains complete ancestry, including one transform-only utility parent. Helper art, collision-only/debug meshes and occluders are excluded from rendering.

The restored selection contains 256 placements, 255 visible prop instances, 29 shared models and one original terrain patch. This includes all eight authored wall-ring placements, the full shaft, ceiling, upper entrance/interior architecture, statues and broken legs along the shaft, wall sets, floor tiles, rubble, lower obelisk bases, broken platform pieces, rocks, generator and tower. The environmental piston is resolved through its MAG → DYC → original model/material dependencies and imported in bind pose.

The upper encounter platform spawners are not instantiated, leaving the shaft open for a final-phase room. Source coordinates, rotations, scales and parent transforms are retained. The lower arena remains the playable area; restoring architecture does not turn the upper shaft into a traversal course.

Terrain presentation now uses the original full-resolution tiled diffuse layers, source blend weights, vertex tint and dominant-layer normal rather than a patch-resolution albedo bake. Tiling/normal adaptation uses the existing surface importer; native UV phase and shader parity remain unverified.

Terrain instance `4611686136141596212`: 67 by 71 grid, 9,240 triangles, terrain layers 6, 7 and 16. Authored position `(82.5999984741,-39.5453987122,69)`, footprint X=76..89.2, Z=62..76. The source final-phase marker `4611686102611670263`, tag `soa_teleport_round6`, is at `(82.8347015381,-38.7240982056,69.1054000854)`. Player spawn projects onto the terrain near `(82.5,-38.7309,69)`.

## Environmental presentation

`soa_room_source.py` retains the 11 original light records, their colors, intensities, transforms and flags. `RoomAtmosphere.gd` adapts them to Godot lights and replaces the generic outdoor lighting only for Soa. Spherical Godot ranges approximate the source ellipsoids. Directional forward-axis conversion and modest indoor ambient fill are renderer adaptations; native light maps, restrictions and lighting parity are not claimed.

Four authored rising-ember placements use the original `fx_embers_02.dds`, emitter interval/count, lifetime/trajectory ranges, hue keys and size-curve endpoints with additive rendering. Random distributions and curve interpolation are approximations. Native drag, buoyancy, motion splines, tint multiplication and exact particle orientation are not reproduced.

The lava pool FXSpec resolves to `fx_lava_pool_plane.gr2` and `fx_flowing_lava_pool.mat` (AnimatedUV). The two exact referenced textures `art/fx/texture/environment/fx_lava_pool_tile.dds` and `fx_lava_pool_tile_additive.dds` are absent from the selected April archives. The graph, speeds, tints and missing paths are retained in `room-environment.json`; this incomplete effect is not rendered. The original static lava decal remains. Lava burst effects, lava audio and piston animation state remain unresolved.

## Evidence and validation

`manifests/map-soa_p3-source.json` records 156 successfully retrieved resources (48,784,022 decoded bytes), exact archive/path hashes, SHA256 hashes and dependencies. Missing FX texture references are separately explicit in `godot/Assets/Maps/soa_p3/room-environment.json`; the successful-resource ledger is not a claim that every FX dependency exists.

`work/maps/soa_p3/selection.json` retains exact placements, ancestry and terrain. `environment.json` is the engine input. `map-soa_p3-godot-build.json` confirms scene build passed, 316 prop mesh instances and maximum world-matrix discrepancy 0.000000238419. Collision is derived from imported visible geometry, with 16 convex shapes and two flat mesh exclusions; native physics shapes are not claimed.

C# build, Godot import/build, centre-floor physics check and the menu-to-game Soa check passed. The gameplay check verifies all 11 lights, actual walking displacement and no recovery/falling during its bounded movement route. This is not exhaustive collision coverage of the entire island or upper architecture.

Rendered lower-arena and upward-shaft views were inspected: `manifests/soa-p3-import.png` and `manifests/soa-p3-shaft.png`. The destination preview uses the refreshed lower-room view. The map restoration is independent of the combat run now composed by `make_soa_encounter.py`.
