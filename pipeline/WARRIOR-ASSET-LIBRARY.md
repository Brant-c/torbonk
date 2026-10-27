# Sith Warrior April 2012 asset library

Asset preparation only. Existing Smash and gameplay files were not edited.

Recovered 808 definitions, 173 catalog entries (160 package members), 110 talent identities, 1824 original resources, 319 converted textures/icons, 32 animation clips, and 351 audio media records.

Start with `manifests/warrior-library.csv` for concise availability and `godot/Assets/WarriorLibrary/catalog.json` for exact identities, eligibility packages, effects, appearance links, dependencies, converted textures, audio events/media and gaps. `talent-packages.json` preserves original tree packages, rank/layout metadata is in `manifests/warrior-effect-tree.json`. Both Juggernaut Rage and shared Rage packages are retained.

Original bytes remain under `source-assets/april2012/resources`; `manifests/warrior-library-source.json` records exact archive/hash/offset/SHA256 provenance. Raw definition containers are preserved. `warrior-definitions.json` and external definitions preserve namespace identity. `warrior-appearance-tree.json` expands every appearance schedule, action, condition and parameter; `warrior-effect-tree.json` expands effect/status/talent/package relationships without summary-depth truncation.

Godot assets: `WarriorLibrary/casts.glb` is an additive animation library on the existing bmnnew rig; `Textures/` contains PNG icons and effect textures; `Audio/` contains PCM WAV media. `fx.json`, `source-parameters.json`, `appearance-parameters.json` and `audio.json` retain authored presentation parameters. Single/dual saber clips keep their original distinct names. Clips discovered from weapon-library paths are supporting assets, not proven per-ability assignments.

The internal Wounding Throw identity (localized as Deadly Throw) is confirmed as `abl.sith_warrior.wounding_throw` (0xE000963B0124FE67), with `traumaticslash` icon, four effect records and explicit Marauder base-package membership. Internal names such as `feint`, `push`, `resilience_form` and `ferocity_form` are retained without invented modern-name substitutions. The catalog includes archive-only records separately from package members.

## Remaining limits

- Source-complete namespace inventory includes unused/prototype records; package membership is explicit.
- Full effect and appearance trees saved; localization maps/tokens remain bounded in summary definitions.
- Semantic animation graph selection is unresolved; single/dual saber libraries are separate original clips, not inferred replacements.
- FXSpec/material/particle JSON and original GR2 retained; native particle renderer and GR2 effect mesh conversion remain pending.
- Audio uses the Imperial and Republic melee bank graphs; missing cross-bank events are listed.
- External package references include proficiencies and Assault; external presentation closure remains partial.
- No gameplay abilities or talent trees implemented; existing Smash implementation untouched.

Missing individual resources and audio events are enumerated in the summary and per-entry catalog. A recovered definition or converted library is not proof of native animation selection, particle rendering or Wwise runtime parity. All extracted sources, texture conversions, WEM originals and GLB hashes were verified.

Rebuild with `prepare_warrior_library.py`, then `prepare_ability_audio.main('warrior-library','WarriorLibrary',[])`, `finish_warrior_library.py`, Blender `convert_warrior_animations.py -- --warrior-library`, and `finish_warrior_library.py --report`, using the existing bundled Python/Blender tools. Research evidence JSON is the input snapshot; refresh it through the read-only April research tools if changing scope.

Localization string-group evidence also resolves feint to Force Camouflage, resilience_form to Soresu Form, ferocity_form to Juyo Form, push to Force Push, and force_push to Disruption. The catalog retains numeric string IDs and original internal names.

The GLB converts direct appearance-linked JBA clips and both basic saber attacks. The larger supporting single/dual saber library is recovered as original JBA and remains available for later targeted conversion after semantic action selection.
