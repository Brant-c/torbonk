# Soa final circular arena — import investigation, 2026-10-28

Imported into the Godot project as `res://Assets/Maps/soa_p3/MapEnvironment.tscn`.
Open `res://Integration/SoaP3Import.tscn` to inspect it independently.
Rebuild and inspect: `./Run-Soa-P3-Import.ps1 -Preview`.
This is the environment prepared for the requested second raid encounter. The normal raid route still ends at XRR-3; encounter transitions, Soa actors and combat are not connected by this import.

## Deciding source evidence

Client-derived April area `4611686096259570491`, label `raid_eternity_vault`, room `05_Infernal_Prison`.
The final-phase marker is instance `4611686102611670263`, tag `soa_teleport_round6`, position `(82.8347015381,-38.7240982056,69.1054000854)`.
The room has one terrain patch: `4611686136141596212`, authored position `(82.5999984741,-39.5453987122,69)`, grid 67 by 71, local height range 0 to 0.89453125, terrain layers 6, 7 and 16.
Its footprint is X=76..89.2, Z=62..76. Floor at arena centre is runtime-verified at `(82.5,-38.7329,69)`.
The marker is evidence for the lower arena height; native encounter execution has not been reproduced.

## What is brought in

`maps/soa_p3.json` selects this terrain and static placements in its footprint, source Y=-40..-34. It preserves source units, rotations, scales and instance IDs. The existing importer closes parent/child dependencies; these 92 selected placements have no parents. There are 15 unique models:

- `bel_arch_rakata_raid_infernal_room_wall01.gr2` (two lowest wall rings)
- `bel_arch_rakata_raid_infernal_platform_piece01.gr2` through `piece04.gr2` (15 broken floor pieces)
- `bel_arch_rakata_raid_energy_obelisk_base.gr2` (17 source placements, including the near-overlapping pair retained as authored)
- `bel_arch_rakata_statue.gr2`, `bel_arch_rakata_statue_broken_legs.gr2`
- `bel_env_boulder_overhang.gr2`, `bel_env_boulder_wall_curve_05.gr2`, `bel_env_boulder_wall_curve_06.gr2`
- `ord_env_boulders_sml_no_moss_01.gr2`, `ord_env_boulders_sml_no_moss_02.gr2`
- `all_item_caves_rubble_mound_ald_01.gr2`, `all_item_decal_lava.gr2`

Exact material, texture and terrain-layer dependencies are resolved from the actual model/material bytes. `manifests/map-soa_p3-source.json` lists all 83 resources (26,209,644 decoded bytes), archive identities, exact path hashes, SHA256 hashes and dependency edges. No unresolved resources. It includes the two source DATs, required models, materials and DDS textures; it is the authoritative bring-in list.
`work/maps/soa_p3/selection.json` retains the exact placement list and decoded terrain. `Assets/Maps/soa_p3/environment.json` is the staged engine input.

## Import boundary and later connection

The phase-one platform spawners at Y=-7.7 and phase-two platform spawners at Y=-23.7 are unnecessary for a direct final-arena start. Upper collision helpers, puzzle controllers, design markers, entrance/hallway, exterior, Adds room and mind-trap cells are outside this room-only import. Soa's model, rig, animations, effects, sounds, falling obelisks and mind traps belong to subsequent encounter work.

For encounter two, instance the imported environment in a second world composition and spawn near `(82.5,-38.7,69)` after ground projection. Keep the source coordinates or apply one shared root translation to geometry and all spawn anchors. Use the circular island as the playable boundary: the terrain patch's rectangle includes the surrounding lava. The inspection scene is deliberately independent of raid progression.

## Verification and limits

Godot import and scene build passed: 9,240 terrain triangles, 92 prop instances, 15 shared model resources, maximum placement matrix discrepancy 0.000000238419. A runtime physics ray hit the centre floor. `manifests/soa-p3-import.png` is a rendered overview inspected during this task.

Terrain uses the existing portable texture bake, materials use the existing Uber-to-PBR conversion, and lava is a static imported decal. Inspection lighting is authored. Native lighting, animated lava/FX, original physics and collision behavior across the entire arena are not verified. Collision is derived from visible geometry; existing conversion policies skip one flat mesh and use 16 convex prop shapes. The lower shell is intentionally open above the selected wall rings.
