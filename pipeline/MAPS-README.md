# Map sections: Hutta, Dromund Kaas, Balmorra

Three more playable survival maps, each a bounded section of an original April-2012 SWTOR
planet area. Terrain heights, splat blends, prop models, materials and textures all come from
the client archives; nothing is placeholder or generated art. Same conversion contract as
Korriban A4 (`KORRIBAN-README.md`): source units 1:1, Y-up, no repair of authored seams.

| Map | Area / room | Section | Terrain | Props |
|---|---|---|---|---|
| Hutta | `hut_main` / `terrain_large_factory_area` | Nal Hutta factory district | 6 patches | 303 placements, 69 models |
| Dromund Kaas | `dro_main` / `area_dark_temple_a` | Dark Temple approach | 4 patches | 479 placements, 86 models |
| Balmorra | `bal_main` / `sobrik` | Sobrik district | 6 patches | 20 placements, 6 models |

Area ids: Hutta `4611686019802841877`, Dromund Kaas `4611686019618382666`, Balmorra `4611686022220993772`.
Per-map selection (room, patch window, prop margin, budgets) is in `maps/<id>.json`.

## Rebuild

```powershell
.\Run-Map-Pipeline.ps1 -Map hutta     # or dromund, balmorra
.\Run-Maps-Smoke.ps1                  # B3 survival smoke on each map
```

Stages: `map_prepare.py` (read-only archive extraction, selection, materials) →
`map_models.py` (Blender, GLB per model) → `map_terrain.py` (splat bake to 1024² maps) →
Godot import → `BuildMap.gd` (saves `Assets/Maps/<id>/MapEnvironment.tscn`).
`map_scan.py` lists the terrain patches in every room of an area; `stage_map_art.py` extracted
Balmorra's codex illustration and planet surface (the other two were already recovered).
`make_map_world.py` writes `Gameplay/GameWorld_<Map>.tscn` from the Korriban world template.
Receipts: `manifests/map-<id>-*.json` (source hashes, conversion, Godot build, smoke).

## Differences from Korriban worth knowing

- `map_terrain_decode.py` extends the Korriban decoder: April sparse-layout weight rows are padded
  to a multiple of 4 bytes (Balmorra's 65×65 patches).
- Stray terrain layer 0 (Hutta) has no entry in the area texture table; its weight is dropped and the
  remaining layers renormalized. Some Dromund patches have slightly over-full sums; renormalized.
  Both are recorded per patch in `map-<id>-terrain-conversion.json`.
- One Hutta material is `OpacityFade`, converted as an alpha-blended diffuse.
- Spawn: Korriban keeps its authored point; other maps pick the most open standing ground using the
  same standing/walking tests as the enemy spawner (dense props made the footprint centre unusable).
- Dromund Kaas and Hutta are Imperial-only destinations; Balmorra is open to both factions.
  Today only Imperial classes are playable.
- Collision (`BuildMap.gd`): gameplay walks on terrain only, so walkable decks (bridges, planks, walkways, ramps) and clutter lower than a step (0.055) carry no collision; small rocks, boulders, debris, logs, crates and gravestones use convex hulls so the capsule slides instead of snagging. Counts are in `map-<id>-godot-build.json`.
- Imported textures and GLBs add roughly 600 MB to `Assets/Maps/`.

## Review checklist

1. Start a run and pick each destination on the galaxy screen; check terrain textures and props look right.
2. Walk the arena edges: bounds are the terrain footprint, holes and steep ground block movement.
3. Confirm the spawn spot feels sensible and enemies reach you.
