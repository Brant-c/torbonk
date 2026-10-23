# D5 — Hutta underworld pass

## Ability effects under atmospheric fog

Additive lightning, particle glows, shields and projected indicators now explicitly disable fog on their effect materials. Unshaded materials still received fog previously, so black source texels could add the atmosphere color across the entire effect quad. The shared correction uses Godot's `fog_disabled` shader mode and `DisableFog` standard-material flag. Actual smoke, oil, opaque terrain and environment materials retain fog. Depth testing remains enabled so effects remain occluded by scenery. Source textures, ability timing and damage are unchanged.

Validation: C# compiles. `FogEffectsSmoke.gd` renders an additive black/white texture against a fogged surface at Hutta's .1 fog density; black texels differ from the adjacent background by only one 8-bit color step, while white texels remain bright. Receipt and single rendered frame: `manifests/fog-effects-smoke.json` and `.png`. Full artistic appearance still needs gameplay review.

## Manual review correction — perimeter, spawn and variety

The added perimeter wall instances have been removed from HuttaAtmosphere. Original source-placed scenery remains. The terrain support and drop checks still prevent walking onto unsupported terrain; this correction does not claim every camera view is free of exposed edges.

Spawn seed is now XZ (31.85,10.31), the area-weighted centroid of the rendered source triangles rather than the rectangular crop center. Runtime found supported ground there with 8/8 open ring samples, without needing a fallback search.

Added original Hutta Bog Snarler (appearance 0xE000DAE2E25DDCA7, model/material 1005718/1005719, dog rig) and Toxic Chemilizard (appearance 0xE0008A2311410362, model/material 1000131/1001449, lizard rig with source veractyl animation family, original .85 appearance scale). Their idle/run/attack/hit/death clips pass the converter's bone-matching checks. Both are ordinary melee enemies with no weapon attachment. Mixed into every stage at supporting weights, with the gunner weight reduced from five to three.

The gunner now uses the separately recovered Fa'athra gangster appearance 0xE00060C886B11DFC (bmn). The rusher uses that outfit with original unarmed idle/run/punch clips and no pistol. The gangster's source hair attachment requires an unsupported spring bone and is omitted, rather than binding it incorrectly. Original head, clothing, gloves, boots and belt are retained. Facial palette and physics fidelity remain limited by the existing converter.

Hutta gunner/droid firing cooldown is 2.2 seconds with a .9-second warning. Opt-in firing presentation restarts the original firing clip and shows a short reusable emissive bolt from the weapon socket along the locked attack direction. This is an authored visual adapter, not native SWTOR projectile simulation. Other planets do not enable this effect. Persistent pursuit from the subsequent shared aggro fix remains intact.

Evidence: `manifests/d5-variety-evidence.json`, extended `d5-source.json` and `d5-enemies-conversion.json`. Validation: zero C# errors; one focused rendered Hutta smoke passes with no reported runtime errors, covering the new central spawn, absence of added perimeter, seven roster entries, original animation resources and firing animation/strike. Manual assessment of animation appearance and combat balance is still required. The older delivery notes below describe the initial pass; this correction supersedes their perimeter and five-role appearance limitations.

Hutta now has a separate run configuration in `godot/Gameplay/Planets/Hutta/run.json`. Select Hutta normally, or launch GameWorld_Hutta with `--map=hutta`. Other planet scenes, class tuning, Collections and shared artifact catalogs were not edited.

## Delivered

- Expanded the existing factory room from six to fifteen original patches, at original transforms and scale. Footprint XZ is [18,47.8,-18.2,32.2], doubling the previous crop depth. 463 original placement records yield 462 rendered props and 90 model resources. No terrain tiling. Source collision and terrain holes retain the existing conversion policy.
- Recovered original `hut_skyscene01.gr2`. Hutta-only atmosphere uses the hut_main Area scheme's yellow/green fog and light colors. Native light ramps and fog curves are approximated. Original underworld wall geometry recovered from this room forms an authored industrial perimeter with derived convex collision. Terrain support/drop checks remain active for player and spawns.
- HUD planet title resolves from the existing map registry. Journey headings, completion, caches and ambient changes respect the selected planet. Korriban keeps its burial courts, colors and ambience.
- Five Hutta roles: rusher, ranged sentry, enforcer, chemical trapper and murder droid. Humanoids use the recovered Fa'athra security officer appearance; the droid uses its actual rogue assassin appearance. All have source idle/run/fire/hit/death animations. Ranged roles stop at an effective range and require line of sight; locked firing lanes give dodge time. No Korriban creature or tomb enemy scene is in Hutta's roster.
- Initial population 12 versus shared default 8; cap 48 versus 36; cadence .5 seconds declining to .22 versus .75 declining to .30. Values are Hutta scene exports. Ordinary HP is 42–110. Shared classes and other planets retain their tuning.
- Twelve serialized encounters at levels 3/5/8/10/13/15/18/20/23/25/28/30. Existing queue and recovery handle level jumps. Minor introductions are zero seconds. Major archetypes at 10/20/30 have burst lanes, close strikes or alternating discharge patterns and reinforcement summons. Existing boss frame and run artifact payout are reused. Names such as Gang Lieutenant and Cartel Champion are authored gameplay roles, not claims of canonical NPC identities.
- Trappers leave a warned three-second chemical danger circle with limited periodic damage. Four source plasteel crates offer XP, guarded run artifacts and healing. Run artifacts use the unchanged shared catalog and never enter Collections.
- Recovered SWTOR ranged audio is routed through a Hutta-only tuning file and the existing voice limiter.

## Provenance

Client-derived, operator-labelled April 2012 archive bytes. `manifests/map-hutta-source.json` records exact terrain, scenery, material and texture paths, archive identities and hashes; `map-hutta-model-conversion.json`, `map-hutta-terrain-conversion.json` and `map-hutta-godot-build.json` record conversions and transforms. `d5-source.json` and `d5-enemies-conversion.json` record rigs, clips, materials, source sky and exports. `d5-appearance-evidence.json` preserves decoded appearance slots and bucket hashes.

Security officer `0xE00006F84F0209FF` visual 0 references appearance `0xE000D440E4375377`, bms. Murder droid `0xE000065FA08D8198` references `0xE00046FB68EE42A3`, assassin, model/material 1000103/1001474. Historical decoder field labels are retained as evidence labels. The humanoid palette shader, facial layers and accessory attachments are not fully reproduced. Equipment uses the recovered shared pistol adapter; exact original weapon matching remains outstanding.

Sky source: `/resources/art/static/area/hut_hutta/sky/hut_skyscene01.gr2`. Lighting evidence: `/resources/world/areas/4611686019802841877/area.dat`, Area scheme, static_fog_colors .634/.658/.302 to .802/.574/.158, static_light_front .993/.968/.646. Audio adapts the existing original snipe-fire media 28023533, documented in `d3-fire-audio.json`; this is not claimed as a recovered Fa'athra-specific weapon event.

## Validation and limits

C# compiles with zero errors. Existing nullable warnings and unavailable NuGet vulnerability feed remain. Focused rendered smoke receipt: `manifests/d5-smoke.json`. It checks identity, supported spawn, original patch count, isolated roster, five role spawns, twelve encounter resources, guaranteed reward flags, cache sites and a real ranged telegraph/strike. It does not establish full boss victory/reward selection, exploration route connectivity, balance or visual acceptance.

This is an expanded factory-district adaptation, not a reconstruction of all Jiguuna, Rust Yards or Fa'athra's palace. Region names describe authored progression. All five roles share two recovered appearances; additional distinct gang outfits remain outstanding. Full Hutta ambient banks/voices, native chemical VFX, projectile simulation, dedicated steam machinery hazards, exact original weapon variants and complete native sky material behavior remain incomplete. Chemical circles reuse the shared terrain warning shader. No claim that every boundary/camera angle or cache route has been manually verified.

Stop here for manual Hutta gameplay review: walk the extended factory, inspect seams, walls and camera silhouettes, find the caches, and play melee and ranged builds through the encounter schedule. Other planets were not expanded.
