# Tasks 1–3 results — 20 October 2026

**End-to-end static conversion demonstrated. Stop at Task 3.**
The verified original Sith Academy doorway frame is visible in a running Godot
4.7.2 .NET scene with camera, lights, orbit/zoom controls, and its source-derived
textures. No character, terrain, ability, combat or gameplay work was started.

## Acceptance

| Task / check | Result |
|---|---|
| 1: Isolated source profile/output roots | Passed; all implementation under `torbonk/pipeline` |
| 1: Archive inventory and integrity | Passed; all 91 TOR files SHA-256 hashed before and after; zero changes |
| 1: Evidence preservation | Investigation unchanged; exact evidence SHA-256 recorded |
| 2: Pinned tools | GR2 4.2.1 + ZG 2.0.14, matched release packages; package and unpacked-file hashes recorded |
| 2: Blender 5.2.1 compatibility | Registration, GR2 import and named Uber material processing passed, with caller-side context workaround |
| 3: Original extraction | Five files, 3,177,576 decoded bytes; exact investigated door hash matched |
| 3: Blender → glTF 2.0 | Passed; bundled glTF exporter 5.2.40; self-contained 1,503,732-byte GLB |
| 3: Godot import / C# | Passed; Godot.NET.Sdk 4.7.2, net8.0, SDK 10.0.401; engine loaded .NET 10.0.12 |
| 3: Geometry and orientation preservation | 576 triangles; all positions and UVs exact in Blender round-trip; triangle winding retained |
| 3: Normals | Maximum round-trip angular change 0.0273963°, below explicit 0.1° tolerance; Godot normals finite and unit length |
| 3: Dimensions | Godot X/Y/Z = 0.7643650 / 1.6096082 / 0.9097019 source units; dimension error 0 |
| 3: Textures | Original diffuse/rotation 1024², gloss 512² loaded; embedded portable albedo, normal, roughness, emission and specular images validated |
| 3: Rendered inspection | Passed on OpenGL Compatibility, NVIDIA GeForce RTX 3070 Ti, driver 617.14 |
| 3: Repeat conversion | Passed; GLB byte-identical after complete re-extract/import/export repeat |
| Original shader / physical metre calibration | Still unverified; explicitly outside demonstrated fidelity claims |
| Optional collision | Omitted; no collision/physics acceptance claim |

Blender imported 813 vertices. glTF/Godot contain 822 vertices because interchange
attributes require vertex splitting. Triangle-corner positions, normals, UVs and
winding were compared, so the count increase is not substituted or extra geometry.
The structural GLB check is a project validator, not full Khronos conformance
certification. The final repeat skipped rehashing the 17.13 GiB archives; a separate
full before/after SHA-256 pass passed and is retained in `source-integrity.json`.

## Exact identities

Model canonical path:
`/resources/art/static/area/all_all/arch/all_sith_interior_hall_door_large.gr2`

Source archive:
`D:/SWTORClassic/swtoremu/Assets2012April/swtor_main_art_area_all_arch_b_1.tor`

Archive entry hash **0xF2E8A6152575480C**, entry offset **337914888**, data offset
**337914924**, compressed **12049** bytes, decoded **29504** bytes, zlib/TOR v5.

| Original | Decoded SHA-256 |
|---|---|
| Model | `d4708978fdf34720f310795db695776f156e4515917ff08fc2fa7d871374db17` |
| kor_acad_higharch.mat | `46dd67d9d5e183045d9a747a7730306081d5e5f974973879511a94d0c78296f1` |
| kor_acad_higharch_d.dds | `7eb442d0f4fe29223052cbf71ce28f795459fd51315d065d4e6b24e3ad2c0ddc` |
| kor_acad_higharch_n.dds | `780dfa2cf89567d075bb2ef99223586a181171389ed57aff8c690bb947ac98a1` |
| kor_acad_higharch_s.dds | `ddf96f31b19e7ca85301548006567a8cf6d57893a43fa45f95788996dae5175c` |

All four dependencies are exact entries in `swtor_main_area_korriban_1.tor`.
Full canonical paths, entry hashes, offsets, lengths and dependency-edge semantics
are in `manifests/door-source.json`; each was unique in the April index.

**GLB SHA-256:**
`09503643b43ae7123a42d0b050cbc79d883a030a9d5a0a6c4c7423daac4bd58f`

Blender **5.2.1 LTS**, build **9e2066aef7ef**. Godot
**4.7.2.stable.mono.official.ed1daf0bf**. No alternative Blender version was needed.

Matched packages from the official [ZG 2.0.14 release](https://github.com/SWTOR-Slicers/ZG-SWTOR-Tools/releases/tag/2.0.14):

| Package | SHA-256 |
|---|---|
| io_scene_gr2.zip (87,903 bytes) | `06e2188affe114dd78f58ab11ba47b948ce5caf9d3c7a402aecf0c834e85b488` |
| zg_swtor_tools.zip (17,486,684 bytes) | `2d268f1e288d82348a2d5ee086c0c1c479d366202faceacd6ab1df720d08635e` |

No upstream code was patched. Original licenses are retained in the unpacked
packages. Relevant operators were tested; skeleton, JBA, character and area tools
were not executed and have no compatibility acceptance result here.

## Failures and workarounds

1. **GR2 callback in a background context.** Calling ZG's
   `bpy.ops.zgswtor.process_named_mats()` directly in a factory-startup background
   session prints `AttributeError: 'NoneType' object has no attribute 'type'`
   from GR2 `types/node.py`, `update_alpha_mode`, on `context.space_data.type`.
   The operator misleadingly returns FINISHED. The pipeline supplies an existing
   VIEW_3D area with `bpy.context.temp_override(area=area)` while invoking it.
   The rerun has no callback exception and all three maps are assigned.
   Initial output: `logs/blender-probe-initial-context-failure.log`.
2. **Blender export temporary directory.** Default tempfile placement under the
   sandbox's AppData temporary directory caused OpenImageIO write failure and
   `PermissionError [WinError 5]` during glTF image encoding/cleanup. Setting
   `tempfile.tempdir` to `pipeline/work/temp` fixes export without changing the
   exporter. Initial output: `logs/blender-export-initial-temp-failure.log`.
3. **Godot global profile initialization.** The original installed console engine
   launch with `--headless --editor --import` could not create its global AppData
   editor directories, then reported `.NET: Assemblies not found` and crashed
   with signal 11. Log: `logs/godot-import.log` was superseded by successful runner
   output; a captured excerpt is preserved in `logs/godot-import-original-failure.txt`.
   A private copy of the same executable, console launcher and GodotSharp files,
   with a local `_sc_` marker, imports successfully. No other engine installation
   was changed. Runtime `--log-file` points into pipeline/logs.
4. **Nonblocking restricted-profile messages.** Blender .blend saves can report
   failed thumbnail writes to `C:/Users/brant/.thumbnails`, and startup/default
   materials can report relative-path warnings. Both .blend files and renders
   were saved. Godot rendering can report that `user://` shader caching is
   unavailable; rendering and screenshots still pass. No global-profile write
   permission is requested as part of this proof.
5. **NuGet audit network unavailable.** Restore/build used the installed packages
   and succeeded. NU1900 warns that vulnerability-feed access was unavailable.
   This is not a missing dependency or a successful vulnerability-audit claim.

The parser intentionally rejects TOR versions other than 5, unsupported material
families, missing/ambiguous path matches, and extraction budget overruns. It
does not silently change source profile or introduce generic replacement assets.

## Visual findings and remaining review

Saved Blender reference, portable Blender and Godot captures were inspected.
They show the same tapered architectural frame, an open doorway, intact UV
details, and the blue/grey source surface. No missing-map magenta or large
conversion-induced normal inversion was seen. Godot is darker than the Blender
studio render because the inspection lighting differs; these captures establish
visibility and asset continuity, not pixel parity across engines.

Two source triangles have a first-corner normal opposing their face normal; the
same diagnostic count remains after GLB reimport. They were not repaired or
claimed to be introduced by export. DDS compression placed 1,579 normal pixels
outside the reconstructed XY unit disc; the conversion clamps reconstructed Z
and normalizes the tangent vector. The material recipe and measurements remain
explicit in `conversion.json`.

Before treating this as an accepted fidelity reference, the user should run the
inspection, orbit all sides, compare the two Blender files under the same
lighting, and compare to an original-client/reader reference if available.
Physical metres, the two inherited normal disagreements, exact original shader
response (including reflections/colored specular), source collision and visual
acceptance remain open. These do not prevent the demonstrated pipeline from
displaying the exact selected original asset.

The GLB includes `KHR_materials_specular` with its source-derived specular-level
image. Godot's albedo/normal/roughness/emission assignments are checked; textured
specular equivalence is not validated and must not be assumed from extension
presence alone.

See `README.md` for launch commands, full repeat procedure, material equations,
scope limits and the corrected 91-TOR inventory. No Tasks 4+ work is authorized
or started by this result.
