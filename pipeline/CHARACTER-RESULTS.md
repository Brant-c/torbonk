# Tasks 4–5 results — 20 October 2026

**Original modular Sorcerer skeletal playback is demonstrated in Blender 5.2.1
and Godot 4.7.2 .NET. Native cloth physics, separate JBA trajectory reproduction
and exact client shader parity are not established. Stop after Tasks 4–5.**

The assembled character uses the original `bmn` human head/body proportions,
`ss_a04` Sorcerer robe/skirt/gauntlets/boots/belt and their attachments. It contains
15 meshes, 15,343 triangles, 12,816 source vertices and 14,250 exported vertices
(attribute seams split vertices), eight source-derived materials and 24 embedded
PNG maps. The original 134-bone `bmnnew` rig plus 77 original CLO bones produce
211 exported joints. All source names, hierarchy, fingers and VFX sockets survive.

## Acceptance

| Check | Evidence / result |
|---|---|
| Source selection | 98 exact files, 20,547,866 decoded bytes; archive path, archive-entry hash, full archive SHA-256, decoded SHA-256 and dependencies recorded |
| Body-type compatibility | All selected registry entries support `bmn`; incompatible `bma` head `1000247` rejected in favor of `1000257` |
| Original skinning | No automatic/replacement weights. Direct original GR2-byte weight comparison and GLB roundtrip pass; exact errors in the roundtrip manifest |
| Bind integrity | Maximum neutral skin displacement `3.34e-8` source units; original imported bone-rest matrix difference at most `3.31e-6` |
| Animation mapping | Seven original JBA files; zero unmatched animated bone names; original facial translations/rotations retained |
| GLB | Self-contained 8,536,044 bytes; 211 joints, seven clips, exact source durations; finite accessors, valid normalized weights, expected triangles and images |
| Blender roundtrip | Five complete deformed-mesh poses per clip compared after reimport; maximum errors are in `character-roundtrip-validation.json` |
| Godot | All 211 bone names/parents and 15 skinned meshes checked; all eight material albedo/normal/roughness maps assigned |
| Godot pose comparison | Five poses per clip; maximum position error `4.69e-5` source units and rotation error about `0.00155` radians, below explicit `1e-4` / `0.003` tolerances |
| Visual execution | Original and portable Blender reference renders inspected; running Godot clips captured, including timed real-time frames, bind pose and bone debug views |
| Reproducibility / static regression | See `character-repeat-validation.json`; the runner reuses the complete doorway pipeline and full archive integrity pass |

Structural import alone was not treated as visual acceptance. Godot ran on the
OpenGL Compatibility renderer using the NVIDIA GeForce RTX 3070 Ti. Captures show
the source robe silhouette, intact hair, working finger poses, attached armor,
running/reaction motion, two-handed casting and a falling death. They establish
asset continuity and skeletal playback, not comparison against a running SWTOR
client. See `generated/character/` for the actual evidence.

## Original clips

All are under `/resources/anim/humanoid/bmnnew/` in
`swtor_main_anim_humanoid_bmns_1.tor`. Entry hashes are TOR path-hash identities,
not GOM IDs. The source manifest includes decoded hashes and archive offsets.

| Inspector name | Original file | Entry hash | Duration |
|---|---|---|---:|
| Idle | `cb_unarmed_idle_right_1.jba` | `1B1A1CAEFD281429` | 1.666667 s |
| Run | `mv_cb_unarmed_run_forward.jba` | `70653F6C21C19F45` | 0.600000 s |
| LightningStart | `fp_2h_elect_drain_start.jba` | `5207A573B09BB76F` | 1.066667 s |
| LightningLoop | `fp_2h_elect_drain_loop.jba` | `CF690A9CF59F4CA4` | 1.066667 s |
| Hit | `dg_unarmed_crit_damage_right_1.jba` | `E655DB63124D3226` | 1.600000 s |
| Death | `dg_death_1.jba` | `242DD315AE1260E2` | 2.333333 s |
| LightningEnd | `fp_2h_elect_drain_end.jba` | `EC5058E77CDCC85F` | 0.466667 s |

Idle, Run and LightningLoop repeat in the wrapper; the other clips hold their last
pose. Hit remains the investigated **critical** reaction, not an assertion that it
is the ordinary SWTOR damage reaction. The start/loop animation identities were
traced to the Force Lightning appearance schedule in the original investigation.
The end clip is an actual matching-family file; its exact appearance-schedule
transition is not separately established. No unrelated animation was substituted.

## Conversion findings and limits

1. **Original cloth bones required a narrow adapter.** The two original `.clo`
   files are XML, while the pinned maintained CLO operator reads binary OLCB and
   explicitly labels its cloth simulation nonfunctional. It cannot import these
   files. The adapter reads only their original names, parents and inverse rest
   transforms; no new skin weights or speculative simulation are generated.
   The chest/leg LOD probes retained missing cloth weights and were not used.
   Source originals and failed-probe logs are retained.
2. **CLO cached local rest disagrees with the rig.** The maximum absolute matrix
   component discrepancy between cached parent-relative CLO rest and inverse-bind
   rest composed against `bmnnew` is about `0.122704`. The manifest records each
   bone's discrepancy. Inverse bind is used to preserve the mesh's neutral pose,
   and local rest is derived against the actual source parent. Cloth follows those
   parents during animation; native gravity, collision, constraints and wind are
   absent. Skirt folds and equipment can intersect during motion/death. Native
   cloth fidelity remains unresolved despite correct skinning and conversion.
3. **Source animation timing needs explicit handling.** Original clips use mixed
   15/30 fps. All action times are rescaled to 60 fps without changing duration.
   The maintained importer places key sample `i` at `duration*i/num_frames`; its
   final sample is held through the full source duration. A zero-time export flag
   avoids a one-frame start delay. Native interpolation/sample endpoint semantics
   are not proven; the quantitative tests compare the maintained import to the
   exported and Godot representations.
4. **Separate world trajectory is not applied.** The pinned JBA `build()` imports
   skeletal bones but never uses `jba.world_space`. Skeletal `Bip01` position stays
   effectively zero across these clips, with its source 180° turn retained. Run
   also has zero separate world-translation travel. Death has a decoded auxiliary
   first-to-last delta of roughly `(-190.78255,-26.90829,0.000145)` in raw JBA units;
   raw samples are retained. Applying that stream and verifying its units/basis
   would need separate work. The pinned reader also strides world rotations by
   eight bytes while advancing the block by six bytes per frame, so world-rotation
   decoding is not asserted. Original JBA bytes are preserved. Death plays and
   deforms correctly as an in-place skeletal clip; full native root trajectory
   reproduction is an explicit blocker.
5. **Materials are an approximation.** All selected maps/palettes are original.
   The reference retains the maintained SkinB/Eye/HairC/Garment graphs. Portable
   albedo is baked before view-dependent specular/flush lighting; normals and
   gloss-to-roughness are translated. Hair's source rotation-red transparency
   needed inversion for glTF opacity. Source anisotropic hair, skin lighting,
   colored specular/reflection and emission coupling are not equivalent to PBR.
   Lighting differs between Blender and Godot; no pixel/client shader parity claim
   is made. No replacement artwork was generated.
6. **Compiled palettes, not guessed XML replacements.** The three human palette
   registry paths are absent. Actual April GOM palette values and their bucket
   hashes were resolved instead. Absent numeric hue/brightness fields use zero;
   all explicit saturation, contrast and specular colors are preserved. The
   original profile is operator-labelled April2012, not authenticated to a retail
   patch manifest.
7. **Environment messages.** The existing restricted-profile thumbnail/shader-cache
   warnings remain nonblocking. Blender and Godot successfully write their actual
   output files inside this workspace. No original archive, emulator source,
   extractor configuration, pinned add-on source or global preferences were edited.

`logs/character-probe.log`, `character-lod-probe.log` and
`character-cloth-probe.log` preserve early binding failures. Later conversion and
validation logs show the final successful path. `character-facial-probe.json`
records why retaining original facial translations was usable for this selection.

Launch and repeat commands are in `CHARACTER-README.md`. No terrain, Force
Lightning VFX, ability audio or gameplay work follows this milestone automatically.
