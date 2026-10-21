# SWTOR Survivors — A3 Force Lightning inspection

The inspection uses the existing original Sorcerer, original animation clips,
two original lightning textures, recovered FXSpec/particle parameters, and 24
decoded original audio media files. The SWTOR effect executor and Wwise mixer
are reconstructed in a bounded Godot presentation component. Read
`LIGHTNING-RESULTS.md` for the fidelity limits; this is not native-client parity.

## Open

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-LightningInspection.ps1'
```

Add `-Editor` to open the editor; select `LightningInspection.tscn` and press F6.
The existing doorway main scene and original character inspector are preserved.

| Control | Action |
|---|---|
| F or Space / Start | Original cast-start, then continuous channel |
| G / Stop | Original cast-end; 500 ms audio fade and bounded visual cleanup |
| X / Interrupt | Immediate effect, particle, light and audio cleanup |
| Moving target | Automatically move the target during the channel |
| Arrows, PgUp/PgDn | Move the target horizontally and vertically |
| 3 s channel | Demonstrate the ability's recovered 3-second channel duration |
| 1–7 / clip buttons | Inspect all seven original clips; cancels the effect first |
| S | Original character bone overlay |
| Drag, wheel | Camera orbit and zoom |
| FX scale | Inspect the unresolved FX-to-rig unit relationship, 0.10 by default |

The second character is another instance of the original Sorcerer asset with
its original rig, facing the caster. It is an inspection target without AI,
health, damage, collision, acquisition rules or combat targeting.

## Reproduce

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-Lightning-Pipeline.ps1' -Capture
```

The runner verifies the pinned existing toolchain and the additional audio tools,
scans bank metadata for typed event IDs, extracts only the selected resources,
parses the bank graph with wwiser, selects the linked DIDX media, decodes with
vgmstream, converts DDS pixels, imports uncompressed PCM into Godot, and runs
behavior/alignment/sample-integrity checks. `-Capture` additionally runs the real
renderer and captures five images plus the actual Godot master-bus audio.

It then runs the **unchanged complete character and doorway pipelines**, including
their repeat GLB checks, Blender roundtrips, .NET build, Godot pose tests and full
91-archive SHA-256 pass. Finally it verifies source bytes and generated hashes.
All 26 converted audio/texture files must match a previous run byte-for-byte.

`-SkipRegressions` skips the established character/door checks when iterating only
the new presentation. `-SkipArchiveRehash` omits the expensive whole-archive pass
when regressions run. Both choices are recorded explicitly in the repeat result.
The milestone's final validation includes a full regression and archive pass.

`Prepare-Lightning-Tools.ps1` obtains the two pinned packages if needed. It checks
their SHA-256s before unpacking. No global installation is required. Tools:
wwiser v20260808, vgmstream r2117, and the bundled Python/Pillow 12.3.0 runtime.
`lightning-tooling.lock.json` pins the actual executables/packages. The new texture
decoder uses Pillow because Blender rejected the selected narrow DXT1 DDS image;
that failed attempt is retained in the investigation history/logs.

No audio files are normalized, trimmed or compressed during Godot import. Every
imported PCM sample buffer is checked against its decoded WAV sample hash.

## Reuse the presentation

Instantiate `ForceLightning.gd` as a Node3D and add it to the scene tree, then:

```gdscript
var presentation = load("res://ForceLightning.gd").new()
add_child(presentation)
assert(presentation.configure(caster_skeleton, caster_animation_player,
    target_node, target_skeleton))
presentation.start_channel()
# Later:
presentation.stop_channel() # graceful end
presentation.interrupt()    # immediate cancellation
```

For a point target, configure without a target rig/node, then call
`set_target_point(world_position)`. Update that point as needed. A rig target uses
the original `vfx_aim01`; the four caster ribbons use the original index/thumb
bones each frame. The target's own original limb sockets drive its arcs/emitters.
The component emits `phase_changed` and exposes `snapshot()` for inspection.
`automatic_stop_seconds=0` channels until explicitly stopped; the scene's 3 s
option is a demonstration timer. Missing caster attachments/clips fail setup.

Call `interrupt()` before changing the actor to unrelated animation or removing
the presentation. The inspection wrapper does this for clip, bind, hit and death
changes. A removed caster/assigned target automatically cancels a running effect.
All transient visuals are owned by the component; it stops audio on tree exit.

This API deliberately has no dependency on future damage, enemy or targeting
systems. It only receives presentation inputs. The imported GLB is not edited.

## Evidence and files

| File | Purpose |
|---|---|
| `manifests/lightning-dependencies.json` | Confirmed, unresolved and unsupported dependencies, exact identities and limits |
| `manifests/lightning-source.json` | Selected archive entries, offsets, archive and decoded SHA-256s, extraction budgets |
| `manifests/lightning-gom-evidence.json` | Original ability/appearance trees and schedule evidence |
| `manifests/lightning-visual-conversion.json` | All recovered FXSpec/particle fields and texture conversion hashes |
| `manifests/lightning-audio-discovery.json` | Typed event discovery and bounded bank coverage |
| `manifests/lightning-audio-graph.xml` | Selected events, actions, containers, parent mixers, tuning and stop fields |
| `manifests/lightning-audio.json` | Event/media joins, exact shared-bank ranges, decoded WAV and PCM hashes |
| `manifests/lightning-godot-validation*.json` | Headless and rendered alignment, lifecycle and PCM checks |
| `manifests/lightning-runtime-audio-validation.json` | Actual Godot output amplitude and silent cleanup tail |
| `manifests/lightning-source-integrity.json` | Original bytes compared again against archives |
| `manifests/lightning-repeat-validation.json` | Converted asset determinism and explicit run scope |
| `manifests/lightning-artifacts.json` | Converted files, component/scripts, screenshots and runtime audio hashes |
| `godot/ForceLightning.gd`, `godot/Lightning.gdshader` | Reusable presentation component and source-texture shader |
| `godot/LightningInspection.*` | Interactive wrapper around the unchanged original character inspector |
| `godot/Assets/Lightning/` | Two converted original textures, 24 original PCM WAVs and recovered parameters |
| `generated/lightning/` | Actual runtime images and audio recording |
| `scripts/*lightning*.py`, `Run-Lightning-Pipeline.ps1` | Reproducible bounded conversion/verification |

Source archives and emulator files are read-only inputs. No bulk archive
extraction, gameplay or environment reconstruction is part of this milestone.
