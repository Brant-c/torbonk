# Face color correction

Cycles writes emission-bake pixels in the target image's color space. The old
float bake target used sRGB, then character_materials.py applied another sRGB
encoding. A linear shader value of 0.214 became 0.735 instead of 0.500 in the
portable texture, washing out faces and hair.

The bake target now uses Non-Color, followed by one explicit sRGB encoding.
Skin diffuse composition is selected through its graph connection instead of
legacy Mix-node insertion order. Original head, eye and hair maps were rebaked
at source resolution. No upscaling or generated artwork was used.

stage_face_quality.py replaces only the three matching embedded base-color images
in existing game GLBs. It checks that every other buffer view stays byte-identical,
preserving mesh geometry, weights, normals and animations, including later additions.
It saves original files before writing and accepts --backup-dir for a drive with
space. The receipt is manifests/face-quality.json. Ten game assets were updated.
Future complete character conversions also receive the corrected albedo bake.

Validation: probe_face_color.py reproduces the old encoding and asserts that the
corrected bake preserves linear 0.214 and encodes once to 0.500. Godot reimport and
FaceQualityCapture.gd capture the original and corrected face with identical
camera, idle pose and lighting. This corrects color loss; original mesh detail
and the portable approximation of SWTOR skin/hair shading remain limitations.

## Facial proportions

A separate deformation problem came from enabling facial translations in JBA
imports. The supplied facial position tracks differ from this head's rest pose,
compressing the upper lip toward the nose and displacing the eyes. A matched
Godot idle-pose comparison with original facial positions confirms the distortion.

Character and combat animation converters now use the maintained importer's
ignore_facial_bones=True option (ignore facial translations; keep rotations).
stage_facial_rest.py removes only fc_ translation channels from existing GLBs,
leaving their source rest positions active. Facial rotation, blinking driven by
rotation, all body animation and binary buffers remain unchanged. Translation-
driven facial expressions are intentionally omitted until a compatible facial
retargeting path is established. This is a restoration of the source rest shape,
not an authored change to the nose or mouth mesh.

validate_facial_rest.py compares saved originals with the corrected GLBs and
checks all other channels and the full binary payload for exact equality. The
receipt in manifests/facial-rest.json covers 48 assets and 8,754 removed tracks.
Use a backup directory on a drive with space when running either script.
