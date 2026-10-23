# Class ability presentation repairs

This pass fixes the functional gaps identified in the October 23 static review. Existing Survivors damage values, damage types, cooldowns and class pools are retained. Ambush and missile windups now delay their actual hit to allow a visible charge/flight; recovery is adjusted separately. Gesture-required activations wait for the actor instead of silently losing their animation. This changes activation timing when abilities compete.

## Implemented

- Shock's finite lightning tail is processed through normal instant-cast completion and lethal hits; abort/reset still clears it.
- Tracer Missile, Heatseeker Missiles and Incendiary Missile launch traveling luminous heads and smoke before their hit. Heatseeker launches three diverging paths converging on the target. Geometry, flight curves, scales and timing are Godot adaptations; this is not the native SWTOR particle simulator.
- Unload emits from both weapons each damage tick. Series of Shots fires visual/audio feedback each tick. Overlapping shot recordings have a bounded voice pool so later shots do not immediately truncate earlier tails.
- Unload preserves its recovered .533-second start clip before entering the firing loop and its .4-second end clip; its four existing damage ticks are retained.
- Ambush uses the recovered charge animation, a recovered April spy reticle, then the sniper firing clip and shot feedback.
- Weakening Blast now emits at the weapon associated with its rifle gesture instead of the left wrist.
- Shield Probe uses the recovered vfx_probe_mini01 model, hover audio and shield-absorption sparks/audio. It follows the caster and ends with shield expiry. Probe shading and placement are adapted; original complete material/movement graph is not executed.
- Force Scream retains its original frenzy gesture, removes unrelated saber swings, and adds a traveling wave and impact using the recovered source effect's artwork.
- Creeping Terror gains an animated target effect that follows the periodic instance's refresh/expiry/death lifetime.
- Force Shroud retains its original consumption-exit gesture and replaces the BH hexagonal bubble with a Resilience aura using recovered source artwork. Its complete force-ghost projector is not reproduced.
- Explosive Fuel retains its original Energy Shield gesture and now sustains wrist flames and the recovered fuel loop for the buff duration.
- Flame Thrower now has sustained channel audio with explicit completion/interruption cleanup. Its sound is an adaptation reusing the existing April Flame Sweep fire recording (13240087), not a verified Flame Thrower event binding. The named Flame Thrower FXSpec is absent from the selected April inventory.

## Evidence and corrected audit assumptions

The original review suspected several reused gestures. April GOM appearance schedules actually bind Force Scream to `cb_buff_frenzy`, Force Shroud to `cb_buff_consumption_exit`, and Explosive Fuel to `cb_buff_energy_shield`. Those bindings were already correct and are preserved. Force Shroud's original schedule binds `abilities/jedi_consular/jc_resilience_cast`, `jc_resilience_buff`, and `cinematics/force_ghost_projector`; the missing guessed `si_force_shroud_cast` filename is not evidence of a missing original implementation.

Evidence is preserved in `manifests/presentation-repairs-gom*.json`, including bucket identity/SHA256 and exact decoded paths. Field/enum labels remain historical fallback labels where marked by the research reader. Resource recovery uses unique path-hash lookup, verified original bytes and archive metadata, with source hashes/offsets and conversion receipts in `manifests/presentation-repairs-source.json`, `presentation-repairs-conversion.json`, and `presentation-repairs-geometry.json`. Audio recovery records typed event IDs and HIRC/DIDX media offsets in the melee/ranged audio receipts. The assets are original April inputs; the bounded Godot rendering is an adaptation.

## Verification

Run `Run-Presentation-Repairs-Smoke.ps1` for lifecycle checks across all eight Imperial profiles. `-Rendered` runs the same checks with actual rendering and captures each ability for review; `-Profiles mercenary,sniper` limits the run. Receipts are refreshed per profile and errors fail the runner. Tests cover clips, admission, damage, missile launch before impact, repeated shots, Shock's tail, target statuses, distinct shield lifetime, channel audio and reset cleanup. Visual captures and smoke success do not establish identical native SWTOR presentation or listening acceptance.

Remaining fidelity work: original probe materials/flight, complete particle graphs/projectors, full Wwise container layering/variation, verified Flame Thrower audio, and manual gameplay/listening comparison. No claim of full native-client parity is made.

### Completed checks (October 23)

C# compilation and asset import succeeded. The eight class repair receipts contain 203 passing checks, including lethal Shock/Unload tails. Sorcerer (31), Mercenary (22), and Sniper (25) also passed with rendering and saved captures; representative Shock, missile flight, Unload, Ambush and probe captures were inspected. These fixtures isolate effects and do not replace manual gameplay/pose/listening acceptance. Existing D3 starter regression passed 107 checks and Powertech regression passed 36. Runtime logs for the final fixtures contain no engine/script errors. Compilation retains nullable warnings and the offline NuGet audit warning.
