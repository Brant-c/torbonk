# Rendering clarity

Fullscreen now uses CanvasItems scaling: the selected resolution controls the
interface design size and windowed dimensions, while the actual window pixels
determine rendering resolution. Existing saved preferences are retained. Gameplay
uses 4× MSAA and 16× anisotropic filtering. This increases GPU work, especially on
4K screens; the resolution selector no longer reduces fullscreen GPU load.

Korriban (6 patches), Hutta (15), Dromund Kaas (4), and Balmorra (6) now sample the
existing source diffuse layers at their repeating world-space UV scale instead
of sampling only a flattened 1024-pixel bake. Source weights and vertex tint
still determine blending. Existing baked normal and roughness maps remain in use;
their fine relief is still limited by the bake. No mesh, collision, gameplay,
lighting, character art, or original archive is changed. The existing Eternity
Vault and Soa detail materials are left alone.

`scripts/stage_terrain_clarity.py` reproduces the materials from already extracted
April sources and existing selection/environment receipts. Run it with Python,
Pillow and NumPy after the map converters, then import the Godot project. Generated
materials live in `godot/Assets/TerrainClarity`; `Gameplay/TerrainClarity.cs` applies
them as surface overrides when a map loads. Scene recompilation is unnecessary.
Weight textures disable alpha-border processing because alpha is blend data.
Diffuse textures use mipmaps; each shader stays within 16 texture samplers.

Validation: .NET build passed (existing offline NuGet / unrelated C# warnings).
`TerrainClaritySmoke.gd` rendered all 31 materials with the Compatibility renderer
on the RTX 3070 Ti, verified six Korriban overrides and 4× MSAA, and saved matching
before/after images under `generated/terrain-clarity-*.png`. Visual inspection
confirmed sharper ground detail without black blend gaps. Receipt:
`manifests/terrain-clarity-smoke.json`.

`DisplayClaritySmoke.gd` also passed with isolated saved settings: a 1280×720
fullscreen interface produced a 2560×1440 image matching the desktop window,
with 4× MSAA. It reads the image pixels rather than the viewport texture's
logical size, which remains scaled. The real user's saved display file was not
changed. Log: `logs/display-clarity-pixels-console.log`.

The older D4 gameplay smoke reached the new six-patch application but failed its
first assertion, "Korriban alone opts into planet stages": other maps now have
PlanetRunPath too. That existing check is obsolete for the current workspace;
this change does not alter planet stages or that assertion. It is not reported as
a passing gameplay regression. Existing imported scenery also logs stale UID
warnings and successfully falls back to resource paths.

No performance benchmark or full playthrough was performed. These changes improve
clarity; they do not add detail to the original character or scenery textures.
