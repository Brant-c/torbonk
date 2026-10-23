# SWTOR Survivors: Tasks 1–3 conversion proof

**D4 expands Korriban into Trials of the Sith:** six contiguous original terrain sectors,
additional original creature rigs, three major bosses, elites at levels 5/15/25,
level 1–30 stages, guarded caches and run rewards. Other planets are preserved.
See [D4-README.md](D4-README.md), [D4-INVENTORY.md](D4-INVENTORY.md), and
[D4-PROVENANCE.md](D4-PROVENANCE.md). Stop after the focused D4 smoke for manual review.

**D3 makes all eight Imperial advanced classes playable on Korriban.** See
[D3-README.md](D3-README.md) for launch instructions, class starters, original
asset provenance, presentation limits and the manual review checklist.
The focused C# build and 107-check Godot smoke pass; stop for manual review.

**D1.1 adds Main Menu → Collections → Appearances.** Sorcerer cosmetics save
between runs and application restarts. See [D11-README.md](D11-README.md) for
launch instructions, developer alternate-outfit unlocks, and manual review.
Normal gameplay no longer exposes the temporary D1 Gear Review controls.

The full game starts at the **B10.1 SWTOR main menu**.
Use `Open-Survivors.ps1`; see [B10.1-README.md](B10.1-README.md) for the
refined selection flow, UI provenance, validation and manual review.
The existing gameplay integration boundary is documented in [C2-README.md](C2-README.md).
`Open-Gameplay.ps1` still opens gameplay directly.

**B9 adds the original SWTOR cursor and free third-person orbit camera.**
Hold RMB or drag LMB in the world to orbit; use the wheel to zoom and Space to
jump. The camera anchors directly to the character with a wide pitch range.
WASD remains camera-relative and combat remains automatic. See [B9-README.md](B9-README.md)
for settings, launch and manual review, [B9-PROVENANCE.md](B9-PROVENANCE.md)
for cursor evidence and limitations, and [B9-RESULTS.md](B9-RESULTS.md) for validation.

Retained B8 gameplay includes **Sorcerer Death Field, Static Barrier and Dark Heal**.
See [B8-README.md](B8-README.md) for launch, tuning and focused validation, and
[B8-PROVENANCE.md](B8-PROVENANCE.md) for original resources and approximations.
The retained B7 boss, leveling, XP pickup and audio presentation remains documented below.
Launch, source provenance, tuning and manual review instructions are in
[B7-README.md](B7-README.md), with source/licensing notes in
[B7-PROVENANCE.md](B7-PROVENANCE.md). B6's roster and equipment are retained.
The historical conversion proof below and its inspection scenes are preserved.

Milestone A4's bounded original Korriban environment is documented in
[KORRIBAN-README.md](KORRIBAN-README.md) and [KORRIBAN-RESULTS.md](KORRIBAN-RESULTS.md).
Its standalone inspector preserves the original doorway, character and Lightning scenes.

Milestone A3 Force Lightning is documented in [LIGHTNING-README.md](LIGHTNING-README.md)
and [LIGHTNING-RESULTS.md](LIGHTNING-RESULTS.md). Its original-source VFX/audio
inspector is separate from the preserved doorway and character regression scenes.

Tasks 4–5 character work is documented in [CHARACTER-README.md](CHARACTER-README.md)
and [CHARACTER-RESULTS.md](CHARACTER-RESULTS.md). The static doorway remains the
regression baseline described below.

The original April-labelled Sith Academy doorway frame has been extracted,
imported into Blender 5.2.1, exported as a self-contained glTF 2.0 GLB, imported
into Godot 4.7.2 .NET, and rendered in an isolated inspection scene.
The repeated conversion produced a byte-identical GLB. No character, terrain,
combat or gameplay implementation was started.

## Open the result

Run this in PowerShell:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Inspection.ps1'
```

For the editor, add `-Editor`, open `Inspection.tscn`, select `OriginalDoor`,
press F to frame it in the 3D viewport, and press F6 to run the inspection scene.
The wrapper contains the actual GLB instance; the C# inspector creates its camera
and neutral lighting at runtime. Drag to orbit, scroll to zoom, use 1–4 for four
orthogonal views, and Space for automatic rotation. No SWTOR client or emulator
process is needed.

Rendered output: `generated/godot-inspection.png` and
`generated/godot-side-inspection.png`. The verified asset is an architectural
doorway frame; no door leaf or generic filler was added.

To open the maintained Blender shader reference with the local add-ons enabled:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --factory-startup --python 'D:\SWTORClassic\torbonk\pipeline\scripts\open_blender_reference.py'
```

Use Numpad 0 for the saved camera and F12 to render. For material inspection,
select the mesh and use the Shading workspace. `work/door-reference.blend` uses
the original DDS files and Slicers shader; `work/door-portable.blend` uses the
explicit export material. `generated/blender-reference.png` and
`generated/blender-portable.png` show them under identical Blender lighting.

## Repeat the conversion

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-Pipeline.ps1' -Capture
```

This checks the pinned tool hashes, retains the existing source baseline,
re-extracts only the five exact dependencies (existing originals are compared,
never overwritten), runs Blender, validates GLB and round-trip geometry, builds
C#, imports and validates in Godot, renders captures, and rehashes all 91 TOR
archives and the five originals. Whole-archive hashing reads about 17.13 GiB and
can take several minutes. `-SkipArchiveRehash` repeats the conversion and engine
checks without repeating that expensive read; the final proof run used this
option after a separate full integrity pass. The manifests explicitly distinguish
the two checks.

Exit codes are checked. A failing stage stops the runner and names its log.
`manifests/repeat-validation.json` compares the old and new GLB SHA-256. Source
inventory is deliberately not replaced on subsequent runs. If a legitimate new
source profile is selected later, give it a new isolated baseline.

If the local tool folders need restoration, run `Prepare-Tools.ps1` first. It
downloads only the two fixed GitHub release URLs when their packages are absent,
checks the recorded package hashes, unpacks them, and copies the existing Godot
4.7.2 .NET executable and GodotSharp directory into `tools/godot`. Internet access
is needed only for missing packages; .NET restore uses the installed package
cache on this machine. Do not regenerate `tooling.lock.json` to bypass a mismatch.
The runner checks all recorded add-on/runtime files and executables.

## Scope and storage

Everything authored or extracted lives under
`D:/SWTORClassic/torbonk/pipeline`. The investigation files remain unchanged.
Original TOR inputs are opened read-only; no emulator source/configuration,
existing project, extractor configuration, or global Blender preferences are
edited. The small private Godot copy uses `_sc_` to keep editor configuration
inside `tools/godot/editor_data`. Runtime logs are explicitly directed to `logs`.

The pipeline currently uses about 0.32 GiB including its private engine copy,
roughly 3.03 MiB of decoded originals, pinned packages and generated outputs.
No archive copy or bulk payload extraction is performed. Rebuildable folders
are excluded in `.gitignore`; the files remain locally available.

The inventory correction is exact: the previous report's **92 files and
18,412,344,088 bytes** include **swtor.exe (17,862,496 bytes)**. There are **91 TOR
archives, 18,394,481,592 bytes**. This is a counting correction, not evidence of
a missing archive. The source-inventory manifest records each archive's full
SHA-256, size and modification time.

## Files and evidence

| File | Purpose |
|---|---|
| `recipe.json` | Selected asset/profile, five-file/4 MiB extraction budget, geometry expectations |
| `manifests/source-inventory.json` | Full archive baseline; evidence-file hash; writable output root |
| `manifests/door-source.json` | Five exact resource paths, archive paths/hashes/offsets, decoded hashes, dependency edges |
| `manifests/tooling.lock.json` | Package URLs, exact versions, SHA-256s, unmodified add-on/runtime tree hashes, reader provenance |
| `manifests/zg-release.json` | Original GitHub release metadata for the matched package pair |
| `manifests/blender-probe.json` | Actual registration/import/material-operator compatibility results |
| `manifests/conversion.json` | Mesh/texture measurements, material recipe, import/export settings |
| `manifests/glb-validation.json` | GLB structure, accessor/image validation, output hash |
| `manifests/roundtrip-validation.json` | Positions, normals, UVs and winding compared after reimport |
| `manifests/godot-validation.json` | Actual Godot bounds, surfaces, textures, normals and triangle counts |
| `manifests/source-integrity.json` | Whole-archive and extracted-original SHA-256 recheck |
| `manifests/repeat-validation.json` | Repeat conversion result and whether archive rehash ran |
| `manifests/artifacts.json` | Generated GLB, image and Blender-file hashes and storage totals |
| `RESULTS.md` | Acceptance results, compatibility failures, limitations and remaining review |
| `logs/` | Reproducible command output, including initial failed attempts |

## Conversion contract

1. `source_pipeline.py inventory`: hash all TOR files once, freeze the operator-labelled
   April profile and the authoritative investigation evidence hash. This does not
   authenticate an original retail patch/manifest.
2. `source_pipeline.py extract`: scan TOR v5 index tables only; calibrate lookup3
   against the investigation's door entry hash; resolve exactly one matching
   entry per path; decode only selected payloads with bounded zlib; verify the
   investigation's door offset/size/hash. Parse GR2 v4's material-name table,
   then the material's three explicit texture inputs. Ambiguous, missing or
   over-budget dependencies stop extraction. No global filename list is needed.
3. `convert_door.py`: register pinned GR2/ZG modules in a factory-startup Blender
   session, import the unmodified model, process its named material, save a
   source-shader reference, and generate the explicit portable material below.
4. Export selected mesh only: GLB, Y-up, UVs, custom normals, tangents, materials,
   source metadata extras; no animations, camera, lights, collision or compression.
5. Validate/reimport, copy only the GLB and recipe into the Godot import root,
   build `Godot.NET.Sdk/4.7.2` / `net8.0`, and run the wrapper inspection scene.

The importer rotates the source Y-up coordinates +90° around X into Blender
Z-up, applying the rotation to geometry. glTF converts back to Y-up. Scale is
**1 source unit = 1 output unit**. Nothing establishes that this equals a metre.
Godot bounds are **0.764365 × 1.609608 × 0.909702 source units (X/Y/Z)**. No scale
factor of 8 or 10 is guessed. The source room placement `(0,180,0)` and position
are retained in the investigation, not applied to this isolated object. No
terrain/room placement or world-coordinate calibration was attempted.

The portable material preserves opaque, one-sided rendering. Diffuse RGB is
raised to 2.1 in linear space to match the maintained Uber shader, then encoded
as sRGB. Packed RotationMap yields tangent normal X from alpha and Y from
one-minus-green; Z is reconstructed, clamped and normalized. Its blue channel
is the emission mask. The gloss alpha's Phong exponent `1 + 63*A` becomes
approximate GGX roughness `(2/(3+63*A))^0.25`. Gloss RGB luminance supplies a
specular level. Source RGB specular is also preserved as a separate PNG and all
original DDS bytes are retained. PNGs remain outside Godot; the GLB embeds the
engine textures.

This is a bounded **static Uber-material adapter**, not a universal SWTOR shader
converter. View-dependent specular/emission coupling, colored specular response,
and source reflection parameters are not faithfully reproduced by this PBR recipe.
The original material's ReflectionIntensity 0.15, ReflectionBlurIntensity 0.3,
ReflectionSpecInfluence 1 and ReflectionContrast 0 remain in the preserved MAT.
The GR2 shader reference is itself an approximation of SWTOR, not a client capture.

For additional static assets, make a new recipe/evidence selection and manifest,
apply explicit budgets, resolve the actual GR2 material table, and retain the same
extract → Blender → portable-material → GLB → wrapper → validation stage contract.
The current adapter deliberately rejects unsupported formats/families rather than
substituting another model. Its current recipe covers one visible mesh and one
Uber material. Broader formats, multi-material support and other shader families
need separate bounded work and approval of scope; no additional assets are selected
by this implementation.
