# Warrior recovered presentation fixes — 2026-10-29

Addresses the four findings in `warrior-focused-review-2026-10-29.md` using the already extracted April Warrior library. No newly invented artwork or generated audio is used.

## Recovered evidence and decisions

Evidence is client-derived: `pipeline/godot/Assets/WarriorLibrary/appearance-parameters.json`, `fx.json`, `source-parameters.json`, and `pipeline/manifests/warrior-library-audio.json`. The existing library extraction manifests retain archive/resource provenance. These adapters do not implement the full original FXSpec or Wwise evaluator.

The bounded evidence subset is saved in `pipeline/manifests/warrior-defense-recovery-evidence.json`: 25 recovered resource records with archive hashes/decoded hashes and the four relevant Wwise graphs. The startup `fx_explosion_core_02_bw` texture was visually verified as a 4-by-4 atlas despite its filename; the adapter explicitly selects its frames.

- `epp.sith_warrior.endure_pain.cast_instant` requests `cb_buff_presence` and `abilities/sith_warrior/sw_endure_pain_cast`. Its particles use `glow/2x2_elec_blast_ring_alpha` and `glow/2x2_light_rays_07`. Both are attached at `vfx_aim01` in the recovered FXSpec. The adapter now uses these textures for the cast.
- `epp.sith_warrior.endure_pain.tick` requests `sw_endure_pain_pulse`. Its glow particles use `smoke/2x2_crystal_smoke`, a one-second lifetime and red/orange curves, attached to left/right shoulders, left/right hips and `vfx_aim01`. These replace the bounty-hunter hexagon shell and emit while the gameplay shield exists.
- Endure Pain's cast FXSpec requests both `Play_ability_sithwarrior_endurepain_cast` (event 3844945673; media 256217830/36875206) and `Play_ability_sithwarrior_endurepain_boom` (event 2086950725; media 157531335/10681804/12207239/80721193). Existing selected samples 256217830 and 10681804 now both play at activation.
- `epp.sith_warrior.enraged_defense.buff` contains an FXSpec request, **no animation action**. Removed `Basic/Saber` rather than guessing a different animation. Activation preserves locomotion. The recovered FXSpec supplies `glow/halo_crescent_01`, `glow/bw_blue_partial_ring`, and `fire/fx_explosion_core_02_bw`; these replace the generic glow with red/orange rings and startup flash.
- Enraged Defense buff event 1524628280 has two random containers. Samples 61363040 and 857325983 are representatives of the two containers, now layered at activation. The review incorrectly treated 857325983 as a healing impact sound.
- `epp.sith_warrior.enraged_defense.heal_proc` requests `sw_enraged_defense_heal_proc`, with `fire/4x4_flame_lick_02_bw` and `Play_ability_sithwarrior_enrageddefense_proc` (event 1215311663; media **622419195**). Successful gameplay heals now dispatch that flame and sound; unsuccessful heals do not dispatch it.
- Juggernaut Vicious Throw reuses the equipped recovered hilt/blade geometry flight adapter already used by Marauder. Its recovered `Warrior/cb_saber_throw_start` clip remains selected. Flight contact uses the configured 0.2-second damage windup; abort restores the held weapon immediately.

## Verification

Added runtime checks for Juggernaut weapon flight/contact/abort, Endure Pain shell identity and shield-bound expiry, both Endure Pain sound layers, animation-free Enraged Defense activation, sustained rings and successful-heal proc dispatch/audio/expiry. Existing Marauder checks remain enabled. Rendered captures and final test results are recorded after execution below.

- Build succeeds with existing nullable/obsolete warnings and unavailable NuGet vulnerability metadata.
- Marauder presentation: 242 checks passed, including a fresh rendered run after the atlas correction.
- Juggernaut gameplay regression: 199 checks passed.
- Broader Marauder regression stops at its unrelated `other classes retain catalogs` assertion before combat tests: the fixture expects 24 Assassin/Sorcerer talents, while the current catalogs both contain 27. No catalog or unrelated regression changes were made.
- Captures are `pipeline/manifests/warrior-juggernaut-{endurepain,enrageddefense,viciousthrow}.png`. Visual inspection identified and corrected the startup atlas issue. Automated audio checks verify playback dispatch; no listening-quality claim is made.
- Final rendered Juggernaut execution completed all 132 assertions with no assertion failure, and the corrected defensive captures were inspected. Its wrapper rejected the run because saving the unrelated Force Choke PNG failed once; a final headless run checks runtime cleanliness separately.
- Final headless presentation runner passed cleanly: Marauder **242 checks**, Juggernaut **132 checks**, with no runtime ERROR/SCRIPT ERROR entries detected by the runner.

## Fidelity limits

The asset identities and event membership above come from recovered data. Emission density, dimensions, ring motion, selected random audio variants, and mapping of Endure Pain pulses onto this game's temporary shield are bounded Godot adaptations. This does not claim pixel-identical native effects or native balance.
