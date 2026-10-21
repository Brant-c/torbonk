# SWTOR Survivors — Tasks 4–5 character inspection

The original April-profile human male type-2 Sorcerer is assembled and animated
in Blender 5.2.1 and Godot 4.7.2 .NET. The authored inspector is separate from
the generated asset. This is an asset inspection milestone; no gameplay, AI,
terrain, combat or ability effects are implemented.

## Launch

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-CharacterInspection.ps1'
```

Click an animation button or press **1–7** for Idle, Run, LightningStart,
LightningLoop, Hit, Death and LightningEnd. **Space** pauses/resumes; **R** restarts;
**B** shows the bind pose; **S** draws all bones through the mesh. Drag to orbit,
scroll to zoom, and use Left/Right while paused to step by 1/30 second.
Non-looping clips hold their final pose. There are no lightning particles or beams.

For the Godot editor:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-CharacterInspection.ps1' -Editor
```

Open `CharacterInspection.tscn` and press F6. `Assets/sorcerer.glb` is generated;
`CharacterInspection.tscn` and `CharacterInspection.gd` are authored wrappers.
The small inspector uses GDScript inside the existing .NET project; the original
C# doorway inspector and its main-scene setting remain available.

For Blender with the pinned add-ons enabled for this session:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --factory-startup --python 'D:\SWTORClassic\torbonk\pipeline\scripts\open_character_reference.py'
```

`work/sorcerer-reference.blend` contains the original DDS files, original material
parameters and palettes, assembled meshes, rig and seven actions. The saved action
is Idle. Choose the armature and an action in the Action Editor, set the playback
range to that action's range, and play. `work/sorcerer-portable.blend` contains the
export material approximation. Both use a 60 fps scene. Numpad 0 selects the saved
camera. No global Blender preferences are saved.

## Reproduce

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-Character-Pipeline.ps1' -Capture
```

This checks the shared pinned toolchain, resolves the selected original files,
assembles the original rig/weights, imports all seven JBA clips, converts materials,
exports GLB, verifies its structure and duration, reimports it to compare weights
and deformed vertices, checks Godot poses/materials, and captures actual playback.
It then runs the existing complete doorway pipeline, including its C# build,
geometry checks and repeat hash, and rehashes all 91 original TOR archives.
Use `-SkipArchiveRehash` to omit that expensive final full-archive read.

Every stage checks its exit code and writes `logs/character-*.log` or the existing
doorway logs. An existing character GLB must repeat byte-for-byte. Do not regenerate
the tooling lock to bypass a mismatch. Existing extracted originals are compared
with archive bytes and never overwritten when changed.

The source reader reuses `source_pipeline.py` unchanged. It scans only archive
index metadata, then decodes exact selected paths. The character manifest includes
98 files, 20,547,866 decoded bytes, including appearance registries, three selected
GOM buckets, and four cloth/LOD investigation inputs. It does not extract a full
character archive or use filename-list matches as proof of presence. Archive
payloads and the emulator are read-only inputs.

## Selection and conversion contract

- **Body:** `bmn` modular models with `bmnnew_skeleton.gr2`; source scale stays 1:1.
  No physical metre conversion is assumed. Equipped modular parts replace body
  regions; a duplicate naked torso is not added beneath the outfit.
- **Head:** model ID `1000257`, material ID `1583105`, original human Caucasian
  a01 head. The investigation's `1000247` is a `bma` head and is excluded.
- **Outfit:** original `ss_a04`, c01/s01. Chest `1142760`, skirt `1142831`, gauntlets
  `1142609`, boots `1142516`, belt `1142643`, plus all registry-listed attachments.
  No exact localized in-game item/set name is asserted from the art abbreviation.
- **Hair:** model `1000620`, material `1027364`; skin palette `1437794`, eye palette
  `1001086`, hair palette `1000998`, complexion `1001008`. Clean-shaven, no optional
  tattoo/paint, helmet or weapon. The head's eye material occupies its second
  original material slot; the registry's override index is `-1`.
- **Palettes:** scheme `1002880`, primary hue `1000768`, secondary hue `1000760`.
  Human skin/eye/hair palette XML paths in the registries have no April archive
  entries. Their actual compiled `dynamic.*` GOM objects supply the values;
  decoder evidence and the exact hashed bucket resources are preserved.
- **Binding:** original GR2 vertex groups and byte weights are retained. No
  automatic weights, generated mesh or replacement animation is used. All 134
  `bmnnew` bones survive, including fingers, `vfx_hand_left/right` and sockets.
  Another 77 original XML CLO bones retain their source names and parents.
- **Rest transforms:** the maintained importer supplies `bmnnew`. Longer display
  tails avoid edit-mode quantization without changing bind heads/orientations.
  CLO rest uses inverse `m_rootToBone` transforms. The cached parent-relative CLO
  transforms disagree with this rig; their discrepancies remain in the manifest.
- **Animation:** maintained GR2 4.2.1 JBA import, facial translations retained,
  `delete_180=false`, no translation cleanup. Actions are rescaled to a common
  60 fps timebase, sampled each frame, slid to time zero, and held through their
  original source duration. Godot imports at 60 fps with optimizer/compression,
  automatic LOD generation and immutable-track removal disabled.
- **Materials:** original Garment/SkinB/Eye/HairC graphs and palettes are saved in
  the reference. Portable albedo evaluates palette color before view-dependent
  lighting; SkinB includes complexion/age/facepaint color composition. Packed
  normals are converted as in the doorway adapter. Roughness approximates the
  original gloss exponent. Hair opacity is **1 minus rotation-map red**. Guild
  insignia is the source blank-black texture. View-dependent skin response,
  anisotropic hair, colored specular, reflections and exact shader parity remain
  approximations.

The source JBA skeletal root retains its original 180° turn. Animated characters
face Godot **-Z**, while the bind pose faces **+Z**. The selected run is in place;
its skeletal root and decoded separate world-translation stream have zero travel.
JBA world trajectories are logged but not applied by the maintained importer.
Death has a nonzero separate trajectory. World rotation decoding also has an
unresolved upstream stride inconsistency. See the results before treating this
as a native root-motion reproduction.

## Evidence and outputs

| File | Purpose |
|---|---|
| `character-recipe.json` | Fixed selection, registry XML records, material/palette joins, paths and IDs |
| `manifests/character-source.json` | All source archive identities, entry hashes/offsets, full archive and decoded SHA-256s, dependency edges |
| `manifests/character-palette-evidence.json` | Read-only GOM palette trees with bucket identity and provenance |
| `manifests/character-conversion.json` | Bone hierarchy/rests, mesh/weight summaries, import/export settings, trajectories and five pose samples per clip |
| `manifests/character-glb-validation.json` | Skins, joints, normalized weights, finite accessors, exact durations, embedded maps and GLB hash |
| `manifests/character-roundtrip-validation.json` | Direct original GR2 weight comparison, GLB reimport weights/rest vertices, deformed vertices at five poses per clip |
| `manifests/character-godot-validation.json` | Actual Godot hierarchy, material assignment, clip duration and bone-pose comparison |
| `manifests/character-playback-captures.json` | Timed frames captured during real-time playback |
| `manifests/character-repeat-validation.json` | Character repeat hash, doorway regression and archive-rehash status |
| `manifests/character-source-integrity.json` | All 98 extracted originals rehashed; archive metadata and investigation evidence checked |
| `manifests/character-artifacts.json` | Generated/reference output and script hashes; exact investigation-resource matches |
| `manifests/source-integrity.json` | Existing whole-archive integrity check, shared with doorway regression |
| `generated/character/` | Source/portable Blender renders, Godot poses, bind/skeleton views and real-time playback frames |
| `CHARACTER-RESULTS.md` | Acceptance evidence and remaining limitations |

The doorway launch and repeat commands in `README.md` still work. Stop after
Tasks 4–5; terrain, ability VFX, audio behavior and gameplay require later work.

Optional `scripts/make_character_previews.py` uses Python/Pillow to combine the
actual runtime captures into `inspection-contact-sheet.png` and three playback
GIFs. It does not create character imagery or alter the source assets. Core
conversion and validation use Blender's bundled Python and need no Pillow.
