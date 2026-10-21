# SWTOR Survivors — initial investigation and plan

**Investigation date:** 20 October 2026  
**Decision:** retain Godot 4 with C#. Start with the local April-labelled assets and a bounded conversion proof. Authentic models, textures, skeletons, placements and some animations are promising inputs. Equivalent runtime shaders, terrain blending, FX execution and audio event behavior are separate engineering work.

This is an investigation report, not a claim that a SWTOR character has already been imported into Godot. No game implementation, importer installation, bulk extraction, emulator edit, server start or SWTOR client launch was performed. The new deliverables are research documents in the previously empty torbonk workspace.

## Evidence and scope

The following labels apply throughout:

- **Confirmed locally:** a file, executable version, source implementation, or decoded archive resource was inspected during this investigation.
- **Documented upstream:** a maintainer describes support or a relevant source implementation exists. This does not establish success on these exact assets and installed applications.
- **Proposed:** an engineering decision or acceptance target for review.
- **Unresolved:** the required dependency or conversion has not been demonstrated.

The corpus calls itself April2012 and contains the old 32-bit asset generation. The label is supplied by the existing project/operator; a historical retail manifest was not available to authenticate every archive or an exact patch number. Do not equate “April 2012,” “1.2.0,” archive modification dates, and every individual file's version.

The companion [asset evidence JSON](D:/SWTORClassic/torbonk/docs/investigation/asset-evidence-2026-10-20.json) preserves selected actual archive identities, decoded sizes, SHA256 values, GOM provenance, FX fields and placement records. GOM object IDs, model/material indexes, archive entry hashes and placement IDs are different namespaces. Keep 64-bit values as strings. Some field and enum labels come from the research tool's historical name map; their interpretation is less certain than the contained bytes and original node names.

The Azeroth Survivors download page could not be retrieved with the research browser. No build was downloaded or reverse engineered. The architecture below uses the survivors loop described in the request; it does not claim to reproduce uninspected details of that game.

## 1. Existing environment and available tools

### Applications and projects

| Item | Confirmed result | Implication |
|---|---|---|
| Godot | D:/WorkspaceVS/Godot_v4.7.2-stable_mono_win64_console.exe reports **4.7.2.stable.mono.official.ed1daf0bf**. Both .NET and standard builds are present. | Use the existing .NET build. No engine installation is needed. |
| C# environment | SDK **10.0.401**; installed .NET runtimes include 6.0.36, 8.0.31 and 10.0.12, with Windows Desktop runtimes. | An existing Godot project targets net8.0. Validate a new isolated project's build after review; SDK discovery alone is not a build test. |
| Blender | C:/Program Files/Blender Foundation/Blender 5.2/blender.exe reports **5.2.1 LTS**, build 9e2066aef7ef. | Keep this version for the initial compatibility test. |
| Blender add-ons | A background read-only module scan found **no SWTOR/GR2/JBA add-on modules**. Enabled modules include io_scene_gltf2. No preferences were saved. | The glTF exporter exists; SWTOR importers still need to be obtained and validated after review. |
| VS Code | Code.exe exists under the user's Local/Programs/Microsoft VS Code folder. | The user's integration was not changed. The inspected Godot settings do not themselves prove that external C# editing is configured. |
| Existing Godot projects | D:/WorkspaceVS/Arena-game, “Arena Combat Sandbox,” uses Godot.NET.Sdk/4.7.2, net8.0 and a C# main scene. D:/WorkspaceVS/BadBet also has project.godot. | Useful existing examples; neither was edited, built or adopted as SWTOR Survivors. |
| New workspace | D:/SWTORClassic/torbonk was empty at inspection. | Keep the standalone game and future pipeline here. |
| Capacity | D: had roughly **23.6 GiB free** at inspection. | Prefer selected dependency extraction. A second full archive copy plus expanded resources could consume this space quickly. |
| Hardware | GPU and RAM queries were unavailable under the current access restrictions. | Do not promise a crowd-size or frame-rate result before profiling. |

### Assets, extraction and related repositories

| Location | What was found | Version/coverage caveat |
|---|---|---|
| D:/SWTORClassic/swtoremu/Assets2012April | **92 TOR files, 18,412,344,088 bytes (~17.15 GiB)**. Humanoid/creature animation, modular character art, weapons, FX, audio banks, world data, global data and Korriban archives are present. | Strongest available baseline. Individual selected resources were decoded and hashed, not the entire corpus authenticated. |
| Same directory, Korriban archives | swtor_main_area_korriban_1.tor, **102,350,285 bytes**, and swtor_en-us_area_korriban_1.tor, **92,850,762 bytes**. | These are not a self-contained planet bundle: placement, shared architecture, FX and other dependencies live in other archives. |
| D:/SWTORClassic/swtoremu/Assets2012 | Five TOR files, **397,719,053 bytes** total. | A different prepared subset; exact version not established. Keep separate. |
| D:/SWTORClassic/swtoremu/AssetOriginals | One TOR file, **838,976,320 bytes**. | Exact build not established. The directory name is not version proof. |
| D:/SWTORClassic/swtoremu/_JPEXTRACT | Existing JSON/text research exports, Client.gom, DAT/NPC/ATYTHON material and reader captures. | Useful discovery hints. Match any selected export to current archive bytes before treating it as April evidence. |
| D:/SWTORClassic/assettest/resources | **Not present** at inspection, although older research instructions refer to it. | Do not build the plan around an already-complete extracted art tree. |
| D:/SWTORClassic/extracTOR | Existing source checkout and compiled WPF extractor. Built executable is under extracTOR/bin/Debug/net8.0-windows. | Reuse it; do not recommend installing another extractor by default. |
| Extractor configuration | Assets points to Assets2012April; Output points to the missing D:/SWTORClassic/assettest. A **119,616,362-byte filenames-all.txt** exists beside the executable. | This list includes newer names missing from April. It supplies candidates, not verified inventory. |
| D:/SWTORClassic/swtoremu | Active SharpServer emulator, original Server source, client, restoration tooling and research libraries. | Read-only reference for this project. No emulator files were changed. |
| D:/SWTORClassic/References/trespa-SwTor-1.3 | Older emulator reference source. | “1.3” in the folder name does not identify the local TOR set. Its README contains restrictive reuse notices; do not copy its code into the new game as an assumed dependency. |
| Other sibling directories | BrantClassic, shadowmick1, swtoremu2 and basestructs were identified at directory level. | Not exhaustively audited; no claim about their completeness or versions. |

No separate modern SWTOR installation was found in the inspected common Steam locations. That is a bounded discovery result, not proof there is no installation elsewhere. STE2 and audio converters were not located in the inspected tool/workspace folders or executable search path; no whole-machine absence claim is made.

### Reusable code and interfaces

The existing read-only research MCP is already working: it reports **291,033 GOM objects**, **867 unique script IDs**, and an inventory of **378,686 archive entries**. It supplies exact-path resource reads and decoded area/room records without the missing extracted tree.

Reuse the interface for investigation and export preparation. Do not make it, the emulator, its database or its server executable a dependency of the finished game.

The existing [tooling audit](D:/SWTORClassic/swtoremu/docs/re-tooling.md) and inspected source identify:

- **Tools/Hero:** archive access, compression and GOM structures.
- **Tools/tor_tools/TorArchive:** archive/path-hash access and area/room/asset readers.
- **Tools/tor_tools/GomLib:** typed GOM nodes, prototypes and higher-level content models.
- **Tools/Research:** a read-only wrapper around those libraries, including current resource and area access.
- **SCPTExtractor:** script-container research, not a model or animation converter.

A concrete limitation matters for environment conversion: the old RoomSpecification reader handles position and parent IDs, and AssetInstance.GetAbsolutePosition adds parent positions. That is insufficient for a rotated or scaled parent hierarchy. Use the current raw transform evidence or Jedipedia export, then compose full matrices; do not reuse the position-only helper as a world-transform solution.

The inspected extracTOR source opens input archives for reading, handles archive version 5 through zlib and version 6 through Zstandard, and extracts known filename matches. It uses FileMode.Create for output, so a fresh destination is required. Unknown names can be skipped by this implementation. Preserve unnamed entries in a separate inventory; “extraction completed” does not mean all archive entries received meaningful filenames. Its current local filename-list behavior also differs from older README descriptions of hashes.bin.

## 2. Recommended asset conversion workflow

### Baseline choice and newer-asset comparison

**Proposed baseline:** use one coherent April asset profile for the first character and arena. Use current maintained extraction/import tools against those inputs where the format is supported.

The current GR2 importer documents both 32-bit and 64-bit SWTOR mesh/skeleton input. Its JBA importer explicitly describes compatibility with **32-bit animation files before Game Update 7.2.1**. This is a practical advantage of retaining the older local animation corpus. [GR2 import implementation](https://raw.githubusercontent.com/SWTOR-Slicers/Granny2-Plug-In-Blender-2.8x/master/io_scene_gr2/ops/import_gr2.py), [JBA import implementation](https://raw.githubusercontent.com/SWTOR-Slicers/Granny2-Plug-In-Blender-2.8x/master/io_scene_gr2/ops/import_jba.py)

Newer assets could add revised skins, newer outfits and objects. ZG documents modernized 7.6 skin handling and a Jedipedia NPC JSON path, but also says skeleton gathering for that path is currently manual. These are reasons for a later comparison, not reasons to discard the local baseline. Mixing a newer mesh with an old animation requires explicit bone-name, rest-pose and weighting checks. [ZG maintained documentation](https://github.com/SWTOR-Slicers/ZG-SWTOR-Tools)

For Blender compatibility, ZG release **2.0.14** specifically fixes AnimatedUV for Blender **5.2 LTS**. The GR2 repository's advertised compatibility still emphasizes **5.0**. Therefore, the matched release pair is a credible candidate, but full registration, mesh import, JBA action creation and material operation on **5.2.1** remain untested. Test that pair first. Only consider a separate supported Blender version if a reproducible failure warrants it. JC Tools' documented 3.6-only path is not the default for this machine. [ZG release](https://github.com/SWTOR-Slicers/ZG-SWTOR-Tools/releases/tag/2.0.14), [Slicers tool catalog](https://github.com/SWTOR-Slicers/Essential-Tools-Downloads-and-Guides)

### Offline pipeline

Proposed stages, each with a recorded input/output contract:

1. **Inventory:** identify archive profile, entry hash, source path, decoded hash, duplicate variants and naming provenance.
2. **Resolve dependencies:** follow character appearance slots, model/material registries, GR2 material references, animation actions, area assets and FX/audio relationships. Retain unresolved edges.
3. **Extract selected originals:** use the existing archive reader or extractor into a fresh source-profile directory outside the Godot import root. Compare lengths and hashes with archive evidence.
4. **Assemble in Blender:** use pinned, matched GR2 and ZG tools. Import one static prop first, then the chosen character parts and exact skeleton. Keep original skin weights.
5. **Convert animation:** import the selected old JBA files to the matching armature, inspect deformation, name actions and export sampled clips. Preserve original action names and notes separately.
6. **Convert materials:** retain a faithful Blender reference. Generate exportable texture channels and Godot material descriptions from source parameters. Bake or translate shader behavior explicitly.
7. **Export glTF 2.0:** use GLB for small self-contained assets or glTF plus external textures for shared resources. glTF is the selected interchange format, not a native SWTOR reader.
8. **Import into Godot:** create wrapper scenes and resource mappings around generated assets; keep gameplay scripts outside imported scenes.
9. **Validate and cache:** compare bind pose, animation, dimensions, sockets, materials, transforms and dependencies. Key generated outputs by source hashes, tool versions and conversion settings.

Godot documents glTF 2.0 as its recommended scene format. Its direct .blend route invokes Blender to export glTF; it is not an alternative SWTOR conversion system. OBJ is suitable as an intermediate terrain mesh but cannot carry a character rig or animation. [Godot scene formats](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html)

The glTF exporter recognizes a defined material representation; it does not transport arbitrary SWTOR Blender shader logic. Export baked/translated channels or attach Godot shaders using preserved material metadata. Keep normals in linear data space and validate tangent orientation. [glTF exporter documentation](https://github.com/KhronosGroup/glTF-Blender-IO/blob/main/docs/blender_docs/scene_gltf2.rst)

### File organization and provenance

Proposed future layout under **D:/SWTORClassic/torbonk**; only the investigation documents exist now:

~~~text
docs/investigation/                 report and captured evidence
source-assets/april2012/resources/  selected immutable original extractions
source-assets/modern-<build>/       optional separate comparison profile
manifests/                         source identities and dependency graphs
conversion/                        future offline adapters and recipes
work/blender/                      intermediate reference/assembly files
generated/<profile>/               generated glTF, textures and audio
game/                              future Godot C# project
game/Assets/Generated/              staged engine inputs for the chosen slice
game/Content/                      authored Godot resources and wrapper scenes
~~~

Do not place the full source extraction inside res://. Keep large raw assets and rebuildable caches out of ordinary source control; decide storage policy before bulk conversion.

Each output manifest should record source profile; archive basename; archive-entry hash; canonical path; decoded SHA256; object/model/material IDs in their original namespaces; dependency edges; resolution status; importer/exporter version or commit; axis/scale settings; and generated output hashes. Never silently substitute a similarly named file from another profile.

Choose one measured conversion basis for meshes, rigs, clips, world placements, collisions and FX attachments. Calibrate against a character and a known placed prop. Do not apply an assumed scale factor independently at multiple stages. Preserve parent matrices and Euler convention evidence; negative/nonuniform scale requires explicit handling.

Jedipedia's reader accepts local TOR inputs and provides inspection/export views. Treat it as a dependency and visual-reference tool, not an assumed remote asset host. Its node JSON can describe relationships without containing the referenced art. Existing archive bytes remain the source of assets. [Reader](https://swtor.jedipedia.net/reader), [reader workflow guide](https://github.com/SWTOR-Slicers/WikiPedia/wiki/Jedipedia-File-Reader)

## 3. Asset compatibility matrix

“Direct” below means the final engine-ready output, not the SWTOR original.

| Asset/system | Local evidence | Route to Godot | Status and expected preservation |
|---|---|---|---|
| TOR archives | Actual selected entries decoded and hashed | Offline extraction | Confirmed archive access; no native Godot archive integration proposed |
| Character/NPC meshes | GR2 skeleton and appearance-slot data found | SWTOR GR2 importer → Blender → glTF | Upstream route documented; exact character conversion untested |
| Armor and weapons | Modular-art and weapon archives present | Resolve appearance/model/material slots; assemble weighted parts; export | Geometry likely reusable; exact first costume still unresolved |
| Skeleton and weights | bmnnew skeleton actual bytes verified | Import matching armature; preserve weights and required hooks | Skeleton import documented; local Godot deformation not tested |
| Idle/run/cast/hit/death | Six exact old JBA resources verified | JBA → Blender actions → sampled glTF clips | Promising old-format route; candidate selection is not visual acceptance |
| Newer-client JBA | No local modern sample verified | No demonstrated conversion route in this investigation | Current Slicers JBA path is explicitly old-format only |
| Character physics | CLO exists as a documented tool category | Recreate secondary motion in Godot or bake selected motion | Importer labels CLO nonfunctional; no authentic runtime cloth conversion established |
| Static props/buildings | Actual Sith door GR2 and placement verified | GR2 → Blender → glTF | Strong candidate for geometric fidelity |
| World placement | Korriban DAT records expose position, rotation, scale, parent and flags | Parse source or reader JSON → composed matrices → generated scene | Data access confirmed; Godot placement round-trip untested |
| Terrain | 51 heightmap-linked baseroom records; embedded VertexData | Validate STE2 on April DAT → mesh → glTF | Geometry path documented upstream; April compatibility and texture blending unresolved |
| DDS textures | Actual lightning and Korriban dirt DDS bytes verified | Decode/repack as needed; use PNG in portable glTF path | Pixel data reusable; packed channels require interpretation |
| MAT/palettes/shaders | Source material conventions documented | Material recipes and selected Godot ShaderMaterials | Requires translation; Blender appearance alone does not establish Godot parity |
| Force Lightning FX | FXSpec, texture, sound events and bone hooks recovered | Convert reusable art/data; implement beam/particle scheduling in Godot | Original ingredients preserved; runtime behavior must be recreated |
| Wwise audio | Audio TORs present; actual sound-event strings recovered | Resolve bank/event/media; decode selected audio → WAV/Ogg | Event-to-bank/media and successful local decode unresolved |
| Collision/navigation | Placement collision flags; importer has optional collision mesh path | Validate source collision; generate static collision/nav where needed | Flags are not collision meshes; complete original physics not established |
| Lighting | Source placement/FX parameters and upstream area-light limitations | Reconstruct lighting, fog, exposure and shadows in Godot | Art direction can be matched; identical rendering not demonstrated |
| Combat, AI, progression | Definitions can inform timing/content | New standalone C# systems | Must be implemented; no client/emulator runtime dependency |

The GR2 tool preserves mesh rigging and imports armatures, but supports SWTOR's particular format rather than arbitrary files with a .gr2 suffix. Its CLO option is explicitly not working. [Importer capabilities](https://github.com/SWTOR-Slicers/Granny2-Plug-In-Blender-2.8x)

### First character: what is actually traced

**Proposed pilot:** one human male body-type-2 Inquisitor/Sorcerer appearance. This is a bounded engineering choice, not a restriction on future character options. No full character creator is needed for the first slice.

Confirmed class/customization evidence:

- class.pc.sith_sorcerer, **0xE0006352A2EE37B3**, has the contained class-name value “Sith Inquisitor.” Do not infer role solely from its historical FQN.
- pcs.sith_inquisitor.male.human, **0xE000FDF2BD432F56**, contains 354 customization-slot entries and four skeleton-display entries.
- Its inspected slot **1025** contains model index **1000247** and material index **1583105**, under an interpreted head-slot label. These indexes have **not** yet been joined to a complete model/material file bundle.
- The actual skeleton **/resources/art/dynamic/spec/bmnnew_skeleton.gr2** exists: **21,464 bytes**, archive hash **DE65BCAEDA54D63B**, decoded SHA256 **9b7710d9f39087bef1939a20b449acada4f59462aa3481c96f1673c528d0729a**.

The head, skin/complexion, hair, eyes, chest/robe, lower body, hands, boots, belt, palette settings, robe/cape attachments, weapon mesh and its textures must all be resolved for the selected appearance. Body-part replacement rules matter: putting armor over a complete naked body can cause clipping. The costume, color values and weapon have not been selected or fully resolved in this phase.

As a separate appearance-reference example, Inquisitor Zyn's NPP **0xE000E471D70906AB** reports body type **bmf**, 14 appearance slots, human_imperial_low_male_01 and armor_cloth. It must not be combined with the pilot's bmn rig by name alone.

Skeleton lookup and retaining imported weights are documented Slicers workflows. The bmnnew skeleton is the documented male type-2 convention. [Skeleton and binding guide](https://github.com/SWTOR-Slicers/WikiPedia/wiki/Binding-a-character-to-a-posing-skeleton)

All clips below are **confirmed actual resources** under **/resources/anim/humanoid/bmnnew/** in **swtor_main_anim_humanoid_bmns_1.tor**:

| Slice role | Verified file | Archive-entry hash | Decoded bytes | Qualification |
|---|---|---|---:|---|
| Idle | cb_unarmed_idle_right_1.jba | 1B1A1CAEFD281429 | 34,881 | Candidate stance; preview required |
| Run | mv_cb_unarmed_run_forward.jba | 70653F6C21C19F45 | 26,049 | Candidate combat run; root motion/foot sliding not inspected |
| Cast start | fp_2h_elect_drain_start.jba | 5207A573B09BB76F | 26,049 | Name obtained from Force Lightning appearance schedule |
| Channel loop | fp_2h_elect_drain_loop.jba | CF690A9CF59F4CA4 | 26,177 | Name obtained from the same schedule |
| Hit | dg_unarmed_crit_damage_right_1.jba | E655DB63124D3226 | 34,369 | Candidate critical reaction; not necessarily the desired ordinary hit |
| Death | dg_death_1.jba | 242DD315AE1260E2 | 48,577 | Candidate death; preview required |

Separate cast-end, directional movement and ordinary hit variants remain selection tasks. The JBA guide notes problematic additive/ad_ clips and possible translation distortion, so choose ordinary full clips first and inspect the complete motion rather than accepting a single pose. [Animation guide](https://github.com/SWTOR-Slicers/WikiPedia/wiki/Applying-SWTOR-animations-to-a-skeleton)

### Force Lightning dependency trace

The deciding local chain is:

~~~text
abl.sith_inquisitor.force_lightning
  GOM 0xE0009EB5CD7850CC
  /resources/systemgenerated/buckets/132.bkt
  decoded node offset 189684
    → appearance string epp.sith_inquisitor.force_lightning.channel_loop
      GOM 0xE0004B907DF52D6B
      bucket 435, decoded node offset 95524
        → fp_2h_elect_drain_start
        → fp_2h_elect_drain_loop
        → abilities/sith_inquisitor/si_lightningdrain_bodyarcs
        → abilities/sith_inquisitor/si_force_lightning_caster
~~~

The ability's decoded fields include a 3-second channel, 6-second cooldown and line-of-sight flag. Some labels derive from the historical schema map; no gameplay behavior was executed. The decoded range value is **1**, whose meaning/units are not resolved here. Do not turn it into an assumed one-metre or one-unit game range.

The appearance has four schedules, animation-note trigger keys **1, 12 and 48**, and guaranteed trigger entries **2 and 3**. These require interpretation and timing reconstruction. The clips alone will not reproduce the effect.

The actual caster FXSpec is:

**/resources/art/fx/fxspec/abilities/sith_inquisitor/si_force_lightning_caster.fxspec**  
Archive hash **0F00586C4A00B362**, **44,852 bytes**, SHA256 **7d15279b1340f945e5705e7884f61497bd6e346e7546a2dbee2a93865368f366**.

It is **UTF-16LE XML-like node data**. The generic text endpoint rejected it; the complete bounded bytes were read and decoded for field inspection. This was not an unsupported-format absence.

Recovered references include:

- Texture **art/fx/texture/electricity/512_SingleArcB_bw.dds**, verified separately in the archive.
- Attachment bones **RightIndexFinger1**, **LeftIndexFinger1**, **RightThumbFinger1**, **LeftThumbFinger1**, plus **vfx_aim01**.
- Engine references **\Engine\zap.zzp**, **\Engine\light.lit**, and **\art\fx\geometry\fx_dummy_cube.gr2**.
- Events **Play_ability_sithinquisitor_forcelightning_cast**, **Play_ability_sithinquisitor_forcelightning_casterloop**, and **Stop_ability_sithinquisitor_forcelightning_casterloop**.
- Start/stop relationships and light/color parameters.

The body-arcs FXSpec is also verified: **85,896 bytes**, hash **2F93FE9521C60B9C**, SHA256 **32069fa84ff69bb511531eb2f321bc3dda4bb8153b47cc9800a0643b452df62e**. Its complete subordinate particle/material chain has not been traversed.

**Preserve the original bolt texture, compatible geometry, bones, motion, colors and recoverable timing. Recreate the effect executor, target-following beams, particles and lifecycle in Godot.** No FXSpec-to-Godot converter has been verified. The numeric string “250” in another schedule action remains uninterpreted, not an invented filename.

### Audio

The exact event names above are a useful starting point, but a filename-list candidate for character_sithinquisitor.bnk did **not** resolve in the April archives. Other plausible bank names also failed. Record these as failed candidates; do not claim the sounds are absent or that a newer filename proves an April bank exists.

Next trace: event name/hash → actual April bank event/action/sound graph → media IDs → embedded or streamed audio entries → decoded waveforms. Listen to cast/loop/stop together, preserving loop boundaries, gain and pitch metadata.

wwiser documents bank-graph exploration and event simulation; vgmstream documents wave decoding and notes that decoding media alone does not reproduce cue logic. They are fallback candidates after checking available local tools, not a claim that the specific April bank/codec is tested. [wwiser](https://github.com/bnnm/wwiser), [vgmstream usage](https://github.com/vgmstream/vgmstream/blob/master/doc/USAGE.md)

## 4. Korriban arena, blockers and unknowns

### Confirmed Korriban data

Korriban candidate area **4611686022825732973 / 0x4000000106296B6D** is corroborated by mapAreasDataProto's **kor_chat** entry and the actual area's header beginning “Area Specification for kor_rebuild_…”.

Its **/resources/world/areas/4611686022825732973/area.dat** is **337,017 bytes**, hash **089BCC9B923D72B7**, SHA256 **ce05e870ec10ecc70845684e2f5f77a27f79aa5c9fd983206310c94f89e77a63**. It lists **219 room entries** and **1,393 asset records**.

**Outdoor terrain is embedded in room data.** The baseroom.dat is **5,935,819 bytes**, SHA256 **467379eaf14e5f1902dcb3c9fc4b0f826e333c643360eae13fe620c20a313f82**. A bounded query found **51 records** linked to asset **4611686022825732977 → \engine\heightmap.hms**. The first inspected record:

- Placement **4611686022895030543**, source header line 49.
- Position **(-3.20000004768, -2.3601000309, -3.20000004768)**, line 56.
- Rotation **(0,0,0)**; scale **(1,1,1)**.
- Width/depth fields **33/33**; these are raw fields, not a proven metre dimension.
- Embedded **VertexData**, beginning with zlib-like 789C bytes; **TexWeightData is empty in this record**.

This means the terrain task is not simply “find heightmap.hms and convert it.” The room-instance payload and its other channels must be understood. Some serialized physics fields look implausible; preserve them without treating every decoded property as valid physics semantics.

The area references verified Korriban dirt texture **/resources/art/static/area/kor_korriban/terrain_map/ter_kor_dirt_d.dds**, plus rockyground and rock_purple candidates and several **bwa:** terrain-material names. These symbolic entries need material resolution. Do not silently replace them with a single repeated dirt texture and call the result faithful terrain.

sith_academy_entrance.dat provides a smaller architectural proof: **65 matching instances**. Placement **4611686022895032867** resolves to **\art\static\area\all_all\arch\all_sith_interior_hall_door_large.gr2** at area line 331, with position **(-29.4535007477, 1.79999995232, -0.0998999997973)** and rotation **(0,180,0)**. The actual GR2 is verified in the shared architecture archive. A banner sample has nonuniform scale, making it a useful transform test.

### Proposed reconstruction

Select a bounded outdoor patch from baseroom near a recognizable tomb or academy feature after previewing it. Include only geometry visible from the intended camera and a small decorative margin. Exact crop coordinates and terrain-instance IDs are not selected yet.

Use area asset IDs to resolve the exact GR2 props. Retain shared mesh instances, parent transforms and utility nodes needed by descendants. Importing an isolated room without its parents can misplace objects. Upstream Area Tools uses reader JSON and documents this dependency issue, a final axis rotation, incomplete terrain UV/texturing information and only approximate area-light generation. [Area Tools guide](https://github.com/SWTOR-Slicers/WikiPedia/wiki/ZG-SWTOR-Area-Tools)

Validate **STE2** on these old text DAT/embedded heightmap records before committing to it. Its documented purpose is terrain conversion; this inspection did not establish an April Korriban conversion. If it cannot read the sample, adapt an isolated offline decoder against the retained bytes, never modify the emulator's parser to fit the new game. [STE2 source](https://github.com/UltimaKaosXIII/STE2)

Collision should prefer verified source collision meshes where present, then generated static collision for the selected arena. Placement flags such as Collidable, IgnoreForPlayerCollision and PhysicsType inform selection but are not a collision-shape reconstruction. Bake a new navigation mesh for the game and verify slopes, stairs, blockers and spawn positions.

Lighting needs an authored Godot environment matching Korriban's warm ground, sky, contrast, fog and emissive accents. Preserve real light-placement data when useful; do not infer full color/intensity parity from positions alone.

### Material reconstruction

SWTOR uses packed, non-PBR texture conventions. The documented rotation map places normal components in alpha/green, with additional opacity and emission information; gloss/specular and dye/palette channels need separate treatment. Do not plug the green-looking texture directly into a conventional normal socket. [Material conventions](https://github.com/SWTOR-Slicers/WikiPedia/wiki/SWTOR-materials-and-texture-files)

Proposed policy: establish a neutral-lit Blender reference, then one Godot material recipe per needed shader family. Compare skin, cloth, metal, hair/eyes and stone separately. Start with translated/baked fixed-color materials for the chosen outfit, preserve palette inputs for later variants, and add custom Godot shaders where the visual difference justifies them. Normal signs, opacity, emissive strength and gloss conversion need visual tests; this report does not prescribe an untested universal formula.

### Deciding blockers

| Priority | Unresolved issue | Deciding next check |
|---|---|---|
| P0 | No exact SWTOR → Blender 5.2.1 → Godot proof | One prop and one weighted character round-trip with pinned tools |
| P0 | Complete pilot model/armor/material bundle missing | Join selected appearance indexes to actual archive paths; zero unresolved visible slots |
| P0 | Terrain geometry, UV/blend data and old-DAT tool compatibility | Convert two adjacent actual Korriban patches and match texture placement |
| P1 | Animation deformation, hooks and notes | Preview all six clips and verify Godot bone attachments; resolve cast end |
| P1 | Sound bank/media mapping | Resolve the named Lightning events and decode/audition exact media |
| P1 | FX execution differs from original client | Reconstruct one bounded Lightning graph using retained data and compare |
| P1 | Source shader parity | Compare required material families under controlled lighting |
| P2 | Full collision/lighting/cloth semantics | Validate only the slice's required behavior; document remaining differences |
| P2 | Hardware/performance envelope | Measure the actual Godot renderer and crowd load on this machine |

## 5. Proposed Godot 4 / C# architecture

Use the installed Godot .NET release. Nothing found establishes a major engine blocker. Keep runtime data and gameplay independent of SWTOR networking, scripts, GOM services and server processes.

| Module | Responsibility and boundary |
|---|---|
| RunSession | Run lifecycle, seed, pause, results and restart. Owns progression state; no asset decoding. |
| PlayerController | CharacterBody3D movement in a fixed physics tick, ground handling and facing. Movement authority stays in gameplay, not imported root tracks. |
| CameraRig | Elevated survivors camera, bounded follow and optional zoom; camera-space movement independent of the visual rig. |
| CharacterPresentation | Imported mesh/skeleton wrapper, AnimationTree/AnimationPlayer, clip map and attachment nodes. Preserve required hand/VFX hooks through export or supply explicit mapped sockets. |
| AbilityController | Automatic acquisition, range/LOS validation, cooldown, wind-up/channel/cancel states and target policy. Initial Lightning may stop movement during its channel; moving casts are a separate animation decision. |
| TargetingService | Spatial buckets or bounded nearby queries; stable tie breaking. Avoid every enemy scanning every enemy every frame. |
| CombatService | Health, team/filtering, damage ticks, death and explicit events. Damage timing has one authoritative simulation owner. Visual effects never decide damage. |
| EnemyDirector | Seeded spawn budget, arena-valid positions, wave schedules, elite/boss entries and cleanup. |
| EnemyBrain | Shared movement/attack state machine; direct steering and bounded path updates. Profile before choosing how many navigation agents run continuously. |
| ProgressionService | XP rewards, level thresholds, three-choice upgrades and stat modifiers. Authored Resource definitions; seeded choices where useful for replay/debugging. |
| WaveDirector | Time/kill progression, difficulty budget, boss transition and victory condition; delegates spawning rather than embedding it. |
| Presentation services | Pooled FX and audio, channel lifecycle, camera feedback and UI. Consume combat/ability events and clean up on interruption/death. |
| Content catalog | CharacterDefinition, AbilityDefinition, EnemyDefinition, UpgradeDefinition, WaveDefinition and ArenaDefinition resources with stable logical IDs. |
| Offline conversion | Source adapters, dependency resolver, conversion recipes and validation. Produces engine inputs and manifests; never runs during ordinary gameplay. |

Use wrapper scenes so reimporting glTF does not overwrite authored gameplay. Cache immutable meshes/materials; pool transient actors/effects when profiling justifies it. Avoid starting with a general ECS, complete MMO class system, server replication or full planet streaming.

The game should run from its packaged resources with the SWTOR client, emulator and conversion tools closed. Source identifiers remain provenance metadata, not a runtime lookup into the old client.

## 6. First vertical-slice milestone

**Milestone A — visual asset proof, before gameplay expansion**

Deliver one complete fixed Sorcerer appearance, six validated clips, one source-based Lightning presentation with recovered audio, one small Korriban crop and a provenance manifest. Demonstrate them inside an isolated Godot scene. This milestone answers how much authentic appearance survives the conversion.

Accept only when:

- Source geometry, textures, rig and clip identities are recorded and unresolved visible dependencies are listed as zero.
- Bind pose and complete clips show no collapsed limbs, detached armor, inverted normals or unexplained orientation changes.
- Required finger/weapon/VFX hooks remain usable.
- Two adjacent terrain patches align, with recognizable original texture blending rather than a placeholder.
- Selected placed props match their source transforms, including a rotated and a nonuniformly scaled example.
- Lightning starts, tracks a target, loops, stops and cancels cleanly, with its original texture and correctly resolved sound media.
- Side-by-side reference captures document material and lighting differences.
- A clean repeat conversion regenerates equivalent outputs without writing to source archives or emulator files.

If this fails, report the specific failing format/tool stage. Do not quietly replace the character, animation or Korriban terrain with generic assets.

**Milestone B — first playable survivors slice, after Milestone A**

A proposed five-minute run in that arena: move the Sorcerer, automatically channel Lightning at a nearby enemy, survive increasing waves, collect XP, choose at least three upgrades, face one elite/boss variant, then reach victory/death and restart. Start with one enemy visual rig and shared variants; additional creature rigs are later scope.

Acceptance: all core loops work offline; collisions prevent leaving the arena; spawn points are valid; dead targets and interrupted channels leave no stuck FX/audio; restarting clears state. Use **1080p, 100 active enemies and 60 fps** as a provisional profiling target to confirm against hardware, not as a current performance claim. Record frame times, renderer and hardware for review.

## 7. Ordered tasks and acceptance criteria

**These are future tasks for review. They have not been started as implementation.**

| Order | Task | Acceptance criteria / gate |
|---:|---|---|
| 1 | Freeze the slice's source profile and evidence | Record 92-archive inventory, selected decoded hashes, tool/name-map provenance and writable output roots. Keep other asset versions distinct. No emulator change. |
| 2 | Prepare isolated conversion tooling | Obtain the matched GR2/ZG release pair; record package hashes. In Blender 5.2.1, registration and relevant operators work without changing existing projects. Only investigate another Blender version after a captured failure. |
| 3 | Prove static prop conversion | Extract the verified Sith door to the new source tree, import/export it and open in Godot. Match dimensions, orientation, materials and optional collision. Hash-match original extraction. |
| 4 | Resolve the complete Sorcerer appearance | Choose fixed customization/outfit/weapon. Join all model/material/palette dependencies to actual files. Produce a complete manifest; no unlabelled current-client substitutions. |
| 5 | Prove skeleton and animation conversion | Bind original weighted parts to the matching bmn skeleton; import idle/run/start/loop/hit/death. Add a verified channel-end strategy. Godot playback and sockets pass visual review; preserve original names/notes. |
| 6 | Establish material recipes | Convert required skin/cloth/stone/metal/eye/hair families. Controlled comparison has no missing maps, broken alpha, inverted normal detail or unexpected color-space changes. Record approximations. |
| 7 | Prove April Korriban terrain | Test STE2 on actual embedded terrain records. Two adjacent patches agree in height/seams and texture alignment. Record fallback decoder needs if unsupported. |
| 8 | Build the bounded arena asset | Select the final crop; compose complete transforms, import required props, create collision/navigation and lighting. Source anchors remain traceable; no missing parent transforms. |
| 9 | Resolve and decode Lightning audio | Locate actual bank/event/media relationships; export audible cast/loop/end behavior with loop metadata. Candidate filename matches alone do not pass. |
| 10 | Reconstruct Lightning presentation | Reuse verified texture, rig hooks and recovered parameters. Target follows correctly; start/stop/cancel/death cleanup passes visual/audio review. Record any unsupported FX behavior. |
| 11 | Review Milestone A | Show the Godot scene, reference comparisons, dependency coverage and repeat-conversion result. Decide whether remaining visual differences are acceptable before gameplay expansion. |
| 12 | Implement movement, targeting and combat | Camera-relative movement, deterministic targeting, range/LOS rules and channel damage work together. Verify damage stops on cancellation/death and cannot double-apply through FX. |
| 13 | Add enemy waves, XP and upgrades | Complete one five-minute seeded run, three upgrade selections and an elite/boss transition. Invalid spawn positions and stale targets are handled. |
| 14 | Package and profile the playable slice | Run with client/emulator/tooling closed. Capture 100-enemy frame-time profile against the provisional target; document actual limits. Death/victory/restart leave clean state. |

The first implementation approval should cover **tasks 1–3 as a conversion proof**, followed by the bounded character/environment work. No broad game implementation is justified by archive access alone.

## Review conclusion

The evidence supports pursuing a recognizable SWTOR visual experience in Godot, especially original geometry, textures, old animation clips and authored prop placements. It does **not** yet support a percentage-fidelity claim or a claim that the whole visual stack transfers automatically.

The highest-value next result is a measured conversion proof with the local old assets and installed applications. Terrain blending, material response, Lightning's executor, sound-bank mapping and secondary physics determine the remaining fidelity gap. This investigation stops here for findings review.

