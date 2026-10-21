# SWTOR Survivors — Milestone A3 results

**Force Lightning presentation is implemented and demonstrated in Godot
4.7.2 .NET using original April-profile assets and decoded original audio.**
The original SWTOR effect executor and complete Wwise behavior are not ported.
The remaining fidelity differences are explicit below. Stop at this milestone.

## Delivered

- Reusable presentation component with start, continuous channel, graceful end
  and immediate interruption; configurable point or rig target.
- Four textured bolts attached to the original index/thumb bones, four target
  body arcs, eight original-socket particle emitters and bounded purple/white
  lighting. Geometry and shader execution are reconstructed in Godot.
- All 24 linked original Vorbis media decoded to PCM; original cast variants,
  channel layers and target aftershock audio integrated.
- Interactive inspector with the original Sorcerer, original target rig,
  three casting phases, all seven clip controls, target movement, orbit and zoom.
- Classified dependency manifest, byte identities, reproducible scripts and
  runtime evidence. Launch/repeat instructions are in `LIGHTNING-README.md`.

## Actual resource chain

`abl.sith_inquisitor.force_lightning` is GOM **0xE0009EB5CD7850CC**, in
`/resources/systemgenerated/buckets/132.bkt`, decoded node offset **189684**.
Its appearance string names `epp.sith_inquisitor.force_lightning.channel_loop`,
GOM **0xE0004B907DF52D6B**, bucket **435**, decoded node offset **95524**.
Those bucket bytes were selected and verified again. Historical decoded field
and enum names retain their separate provenance; node names and archive hashes
are not treated as interchangeable ID namespaces.

The appearance references the original start/loop actions and both FXSpecs:

| Resource | TOR entry hash | Decoded SHA-256 |
|---|---|---|
| `abilities/sith_inquisitor/si_force_lightning_caster.fxspec` | `0F00586C4A00B362` | `7d15279b1340f945e5705e7884f61497bd6e346e7546a2dbee2a93865368f366` |
| `abilities/sith_inquisitor/si_lightningdrain_bodyarcs.fxspec` | `2F93FE9521C60B9C` | `32069fa84ff69bb511531eb2f321bc3dda4bb8153b47cc9800a0643b452df62e` |
| `texture/electricity/512_singlearcb_bw.dds` | See manifest | `09123b2387a3aedbacb3e8d88e4279fa65b345a72b3023fffef808d8400c35b4` |
| `texture/electricity/electric_rings_bw_02.dds` | See manifest | `1d8afb288152f78fcb0c32aa4de03cc57d1b212c3384594aab47dbb1932454ac` |

FXSpecs are UTF-16 XML node data. The emitter/particle chain is actual text PRT v3:
`si_lightningdrain_bodyarcs.prt` → `si_lightningdrain_bodyarcs_p.prt` → the ring DDS.
It supplies a 0.1 s emission interval, one particle per emission, 0.05–0.25 s
lifetime, 0.016–0.03 size, additive blend, size/life curves and original hue keys.
Both DDS images are decoded directly, retaining their original pixel artwork.
Blender's image loader rejected the narrow 128×512 arc DDS; Pillow successfully
decoded its actual DXT1 bytes. The rings image is 256×256.

Caster bolts use `RightIndexFinger1`, `LeftIndexFinger1`,
`RightThumbFinger1`, `LeftThumbFinger1`; they converge on the target's `vfx_aim01`.
Target emitters use original elbow, shoulder, knee and hip sockets. Target arcs
connect original hand/foot sockets to FXSpec-named dummy endpoints. The original
`fx_dummy_cube.gr2` is retained as evidence of those proxy relationships; it is
not visible lightning art and is represented by transforms in the reconstruction.
Original `vfx_hand_left/right` are also used for presentation positioning.

The three cast clips retain their existing original durations: start **1.066667 s**,
loop **1.066667 s**, end **0.466667 s**. The inspector also retains Idle, Run, Hit
and Death. The end clip's matching-family identity is confirmed; its exact native
appearance transition was not independently proven in the earlier character work.

## Audio recovered

The correct bank is **`/resources/bnk2/abilities_imperialmelee_data.bnk`**, not the
unresolved `character_sithinquisitor.bnk` filename candidate. Its TOR entry hash is
**30583E2CDA806B52**, Wwise version **62**, bank ID **1839505760**, decoded SHA-256
**a0fc90e145da0b7d7b0abc537d5f8853761c59921f54d79e696e4cb12720d1bc**.
Discovery checked typed HIRC Event records, rather than treating filename-list or
raw substring hits as event membership.

| Original event | Event ID | Action → target |
|---|---:|---|
| `Play_ability_sithinquisitor_forcelightning_cast` | 2909241004 | 717691402 → random container 229586521 |
| `Play_ability_sithinquisitor_forcelightning_casterloop` | 1354101915 | 563421671 → layer container 368827915 |
| `Stop_ability_sithinquisitor_forcelightning_casterloop` | 243915777 | 151050253 → 368827915; 500 ms linear stop |

The source bank links three cast variants, fourteen channel-grain variants and
one bed. The body-arcs FXSpec additionally supplies the original shock-aftershock
play/stop events: six crackle variants plus a differently tuned transient layer
using the same fourteen shared grains. These are related original dependencies,
not substitute lightning sounds.

`abilities_imperialmelee_media.bnk` contains identical copies of the selected
media, but several HIRC sources actually name **abilities_creatures_media**,
**abilities_republicranged_media**, or **weapons_media**. Each selected source ID,
explicit bank ID, DIDX length, absolute HIRC offset and SHA-256 was checked against
those actual bank bytes too. Only the required ranges of those shared banks were
read; they were not extracted wholesale.

Pinned wwiser parsed the complete graph and init bank. Pinned vgmstream decoded
the selected Wwise custom Vorbis successfully. All 24 full media files retain
their decoded sample boundaries without trimming/normalization. Godot imports
them as **16-bit PCM**, and the actual imported sample buffers match their decoded
SHA-256s. No missing source was replaced with generic audio.

Recovered tuning used by the adapter includes cast pitch ±100 cents; channel
grains −6 dB and −560 cents with ±150-cent variation; bed −500 cents with
−100/+150-cent variation; aftershock crackle −9 dB with −5/0 dB variation and
−75/+100-cent pitch variation; target transient −6 dB with −4/+2 dB and
±150-cent variation. The crackle trigger interval is 200 ms with avoidance of
the previous two variants. The bed repeats its complete **240,096 samples at
44,100 Hz**; this is a container loop, not a fabricated internal loop marker.

## Validation

Final headless execution passed **69 checks**. Actual OpenGL Compatibility
execution on the RTX 3070 Ti passed **76 checks**, using the **WASAPI** audio
backend. The JSON results retain every individual check. Evidence includes:

- Required original caster and target socket names and the 211-bone imported rigs.
- Original start → loop → end transitions and configurable point targeting.
- Endpoints matching animated hands and moving targets throughout sampled poses;
  the hands themselves were checked to move, including continuously moving target
  samples. Maximum sampled endpoint errors remained below 0.0001 source units.
- Original channel audio active, graceful stop fade progressing, full-sample bed
  loop boundaries, and all 24 imported PCM hashes.
- Interruption during wind-up, active cast, Hit and Death; restarting during end;
  twenty repeated start/cancel cycles; target removal and timed automatic stop.
- No active beams, emitters, particles, lights or sound voices after cancellation
  or completion of the bounded graceful end.
- Five rendered images, including active bolts and a clean post-effect view.
  A real Godot master-bus WAV contains audible signal, no clipped samples, and
  an exactly silent final 100 ms after cleanup. This is engine-output validation,
  not a claim of subjective audio matching against a running SWTOR client.

All **26 converted audio/texture assets repeated byte-for-byte**. Selected source
resources and exact shared-bank media ranges were compared against the archives
again. The unchanged complete character and doorway runners also passed, with
byte-identical Sorcerer and doorway GLBs. All **91 baseline archives** passed the
full SHA-256 recheck. The expensive regression was run separately from the A3
iteration/capture runner; the repeat manifests record that distinction honestly.

## Fidelity differences and unresolved work

1. **Original art and parameters, reconstructed execution.** `Engine/zap.zzp`
   and `Engine/light.lit` do not resolve as transferable archive resources. Godot
   draws camera-facing textured ribbons, interpolates recovered shape/envelope/
   colour keys, regenerates bounded control points, and draws short-lived source
   texture billboards. Native spline modes, control-point travel/rotation,
   probability distributions, UV randomization and exact shader/bloom response
   are not reproduced. A complete SWTOR particle engine was not attempted.
2. **Cue timing is not proven.** Appearance animation-note keys 1, 12 and 48 and
   guaranteed schedules 2/3 are retained. Their exact note timestamps remain
   unresolved. The adapter uses an explicit **0.35 s reconstruction cue**, then
   the original clip durations. It channels continuously until requested to end,
   or uses the optional recovered 3 s demonstration timer. The ability range
   value `1` is not interpreted as a physical unit or targeting rule.
3. **FX units are not calibrated to native playback.** The imported rig remains
   unchanged. An exposed scalar, **0.10 by default**, maps FX widths/radii/ranges
   to this inspector. It is a reviewable reconstruction choice, not evidence of
   SWTOR's physical units. Target proxy semantics, source offsets and light
   positions are approximated where the original runtime's rules are unavailable.
4. **Lights differ.** Original colours, intensities and periods (caster 0.2 s,
   target 0.03/0.087 s) are preserved. Bounded Godot omni lights replace the native
   light system, including the source target spot lights. Native selective object
   masks, spot cones, fog, bloom, photometry and source fade semantics differ.
   The curve suffix values are interpreted as cycle times by the adapter;
   their exact native executor semantics are not independently verified.
5. **Particle texture selection is unresolved.** The source ring DDS visibly has
   four shapes, while the PRT contains TextureNumber=15, RowSize/ColumnSize=1 and
   AnimationSpeed=0. The full original image is used; no unproven atlas selector
   or generated replacement is introduced. Native curve easing/distributions and
   particle-local motion are retained as evidence rather than asserted equivalent.
6. **The Wwise mixer is not reproduced completely.** Original media, layer tuning,
   full sample boundaries, target crackle triggers and the 500 ms linear stop are
   retained. The channel grain container's **1500 ms CrossFadeAmp** is currently
   a repeating shuffled Godot playlist without native overlap. It avoids adjacent
   repeats but uses a fixed 14-item shuffled cycle, unlike Wwise's random/history
   algorithm. Pitch varies per channel activation rather than per grain. The
   target transient's native CrossFadePower/container semantics, bus/LPF effects
   and spatial attenuation are not equivalent. These parameters remain in the
   graph so later work can implement them without rediscovery.
7. **Cancellation is deliberately bounded.** Beams and particles are removed
   immediately on graceful stop, caster lighting fades for up to 0.3 s, and sound
   ends within 0.5 s. Immediate interruption clears all of them. The source cast
   one-shot is marked NEVER for its own stop trigger; the reconstruction also
   bounds that tail on graceful cancellation to keep this standalone component
   clean. This is an explicit behavior difference.
8. **No native-client comparison.** Captures establish actual Godot rendering,
   tracking, original-resource continuity and cleanup. No running April SWTOR
   visual/audio reference was captured. The earlier character cloth, shader and
   root-trajectory limitations remain unchanged. The April profile is operator
   labelled, not authenticated against a retail patch manifest.

The source archives, original emulator and extractor configuration were not
modified. No combat damage, enemy AI, progression, map reconstruction or other
gameplay was started. This milestone ends here for review.

Tool references: [wwiser](https://github.com/bnnm/wwiser),
[vgmstream usage](https://github.com/vgmstream/vgmstream/blob/master/doc/USAGE.md),
[Godot AudioStreamPlaylist](https://docs.godotengine.org/en/stable/classes/class_audiostreamplaylist.html).
