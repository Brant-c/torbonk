# EV surface restoration — 2026-10-28

The surface now uses the original sky mesh, converted turbulent cloud geometry,
cloud texture and alpha mask, lightning-cloud atlas, and explicitly timed local
snow, sparks, smoke and fire emitters. This remains a Godot adaptation, not verified
native-renderer parity.

## Deciding evidence and repairs

`trace_ev_surface_fx.py` traces 60 FX placements (58 surface, two sky), six root
FXSpecs, 53 resource references and 45 dependency edges. The exact source fields,
paths, SHA256s, model/material dependencies, particle specifications and sound-event
names are in `manifests/ev-surface-fx-trace.json`. All 52 archive resources resolve;
`/resources/engine/projector.ext` is an engine type reference, not a missing image.
The original illumination ramp and reflection DDS are also recovered for further
renderer work. Sound event names are retained; bank/audio resolution is pending.

The first cloud shader incorrectly sampled the mask's red channel, which is
uniformly white, and discarded the cloud texture's alpha. The source mask's soft
edge is entirely in alpha. The shader now multiplies both alpha channels and uses
the original cloud geometry at its FX scale (200,40,200), both authored sky FX
placements relative to the sky camera, and source projection size, tint, tiling,
scrolling and 20–80 degree angular falloff. The earlier sphere is removed.
Projector rotation-rate interpretation and dual-planar compositing remain adapters.

`EternityVaultWeather` now loads the traced local effects, including lightning
at both sky placements: 118 explicitly timed emitters rather than 104. It handles
source atlas frames, additive blending without fog-colored black cards, flat sky
billboards, box emission, source scale, and nested lightning color endpoints.
Distributions use their recovered bounds with uniform sampling; S/I/O spline
interpolation uses linear endpoints. Source acceleration units, gust-group
lifecycle, exact frame timing, and empty snow emission defaults remain unresolved.
These limitations prevent a claim of complete effect parity.

The terrain detail generator had three concrete adapter faults: alpha-border
repair modified RGBA blend data, normalized texture UVs did not align source grid
endpoints to texel centers, and one dominant normal map covered each entire patch.
All 17 patches now preserve unmodified blend data, align grid sampling, normalize
interpolated weights, blend each layer's normal and gloss-derived roughness with
its diffuse weight, and interpolate original tint on the terrain triangles.
Normal/roughness share one packed texture per layer, keeping the largest material
within 16 samplers. There is no speculative smoothing of source paint.

All source blend sums are 255 with no missing paint samples. Two outer copied-patch
paint edges differ in the source selection (5628→0039 and 0047→5634); these have
not been repainted. The rectangular floor pattern near spawn also occurs in the
original `ter_bel_city_floor01_snow_d.dds`, independently of the weight adapter.

## Retained earlier recovery

All 17 original terrain patches were already included. The 27 previously filtered
cliff, plateau and spire placements load at source transforms as distant scenery.
The playable arena and collision remain bounded. The selected surface scheme is
`exterior_ice_altermative`; source colors and angles inform the current lighting.
Its illumination lookup, reflections and original fog ramp still need faithful
renderer adapters. The snow-sky fade portal is traced with its OpacityFade material
but not rendered; native opacity-distance behavior remains unresolved. Hidden and
blockout helpers remain excluded.

## Reproduction and verification

Run-Map-Pipeline now stages the environment, traces FX, converts the source cloud
geometry, stages terrain layers, and imports/builds the map. Source extraction is
read-only against the April archives. Effects are staged in
`Assets/Maps/eternity_vault/authored-local-effects.json`.

- .NET build passes with seven existing warnings.
- `EvBlendDataSmoke.gd` verifies all 20 imported RGBA blend images byte-for-byte and
  all 17 mesh vertex-tint arrays against the source grid using mesh UVs, including
  the hole-trimmed patch. It also renders all 17 materials under Compatibility on
  the RTX 3070 Ti. Receipt: `manifests/ev-blend-data-smoke.json`.
- Rendered sky and ground exploration checks pass with zero player recoveries.
  Images: `manifests/ev-restored-sky.png`, `manifests/ev-restored-ground.png`.
- Direct gameplay scenes still log pre-existing directional-animation blend-point
  warnings and show a fallback character appearance. Those are separate from map
  rendering. No full raid playthrough, sound acceptance or performance benchmark.

Claims above are client-derived, with Godot import/render checks where stated.
No native-client visual match or complete restoration is claimed.

## Ground finish and opaque snow overlay correction

Following the supplied in-game image, terrain specular is reduced to 0.12 and
roughness has a 0.88 floor. This is requested matte presentation tuning; source
normal relief and layer blending remain in place.

The `hth_env_snow_decal` and `hth_env_snow_plops` material uses AlphaMode=Test,
AlphaTestValue=0.5 and UsesEmissive=False. Its diffuse alpha is uniformly 255;
opacity is packed in RotationMap red. The previous converter used diffuse alpha,
leaving the entire overlay polygon visible. The staged snow-overlay-cutout.png
now carries the exact RotationMap red channel in alpha (70,574 texels below the
source threshold). Runtime applies the original cutout threshold to both snow
placements, disables emission and softens their highlights. The model converter
also preserves the correct snow mask on future conversions.

Verification: forced .NET rebuild passes; rendered exploration check passes and
reports two repaired overlays. Generated overlay alpha equals source red bytes
exactly. Updated image: manifests/ev-matte-ground.png. Existing animation warnings
remain. The review image uses the development spawn camera, not the precise camera
and combat state from the supplied image.
