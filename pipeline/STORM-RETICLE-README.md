# Force Storm — original green player area indicator

Launch for manual review:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

Force Storm still unlocks through level-up choices and activates automatically.
Choose Gathering Storm to increase its area. WASD moves, R resets, Esc exits;
add `-Editor` to the launcher to open the existing gameplay scene in Godot.

## Original assets

The indicator uses April's **actual green player targeting projector**:

- `/resources/art/fx/fxspec/abilities/all/all_ui_targeting_reticle_green.fxspec`
  — TOR entry `0x3E1F337E4606A393`, decoded SHA256
  `4925ceb31006003abbc6f13036945065dbb953cfdcaac90191d4eee0361e2804`.
  Its `proj_outer_ring` selects `all_proj_selection_targeting.dds` and the
  explicit projector color `#.0,1.0,.0,1.0` (pure green).
- `/resources/art/fx/texture/symbol/all_proj_selection_targeting.dds`
  — TOR entry `0x110501EF90E6BD26`, decoded SHA256
  `9bab1fdbf6256d60eb823add87232c3518fcbf1f8fd63632c979686d7e1fa28c`.
  Original 256x256 pixels converted to RGBA PNG without resampling.
- The inner Imperial emblem reuses the already recovered
  `proj_empire_faction.png`, selected by
  `all_imp_target_reticle_gtaoe_persist.fxspec` (entry `0x552ED3D98AC183E4`).
  Its original red brightness channel uses the green player projector color.
  Existing B5 extraction/conversion receipts retain this texture's identity.

These are original SWTOR art resources, with a bounded Godot composition of
the green targeting ring and Imperial emblem. The named green Imperial
initialize/model variants in the historical filename map were checked but did
not resolve in the selected April archives. The existing Imperial persistent
FXSpec instead selects red/orange artwork and curves. We therefore use the
verified original green UI targeting projector and existing Imperial emblem;
we do not claim this is an exact native Imperial green persistent FX execution.
No generic substitute art is used.

Evidence is **Client-derived** from operator-labelled April 2012 archives;
the Godot behavior is checked separately. New recovery is limited to two
resources, using the existing read-only SelectedSource reader and Pillow
conversion. `scripts/prepare_storm_reticle.py` is repeatable within that bound.
`manifests/storm-reticle-source.json` records exact offsets, sizes, hashes and
dependencies; `godot/Assets/Abilities/storm-reticle.json` records the original
projector fields, PNG receipt and measured bright-ring radius. Archive hashes
are copied from the prior inventory; no archive hashing is performed here.
Archives and emulator content are unchanged.

## Runtime behavior

`StormAreaIndicator` is a presentation-only terrain-conforming textured mesh,
compatible with the project's existing OpenGL Compatibility renderer. It uses
terrain collision samples, a small fixed height offset, additive unlit color,
normal depth testing, no depth writes, and no shadows. It remains visible in
Korriban's lighting without drawing over foreground walls or characters.
Unsupported ground and large terrain steps omit triangles rather than bridge
holes. The mesh rebuilds only when its center or radius changes.

`OriginalAbilityPresentation` creates it at Force Storm's locked `GroundCenter`,
reads the live `AutomaticAbility.Radius`, and releases it on End/Release. It
outlives the caster gesture and remains during activation and active damage,
then disappears at the last tick or interruption. Existing abort, death,
reset and scene-exit paths use the same cleanup. Existing bolts and audio stay
in place; strike placement also reads the same live radius. No damage,
targeting, timing, progression, other abilities or UI behavior is changed.

The original ring's bright ridge maps to the effective gameplay radius, with
its soft halo outside it. The emblem scales proportionally inside. Gathering
Storm and its rarity multiplier already increase this live radius: one
Artifact rank changes 0.32 to 0.416 source units (+30%). A smaller gameplay
radius also shrinks the mesh immediately on its next presentation frame.
Damage-only power upgrades do not currently affect area, so they do not grow
the indicator. No separate power-to-size multiplier was introduced.

## Validation and limits

Focused validation uses `Run-Storm-Reticle-Smoke.ps1`: compile C#, import the
new local resources, and run one brief rendered GameWorld feature check.
It checks original green color, ground/center alignment, actual Storm damage,
visible green pixels, a real rarity-scaled Gathering Storm choice, larger
rendered area, shrinking, gesture-independent persistence, completion, abort,
death and reset. Results: `manifests/storm-reticle-smoke.json` and
`logs/storm-reticle-smoke-runtime.log`. A single review capture is written to
`generated/storm-reticle/active-upgraded.png`.

C# compilation and the completed focused rendered smoke passed (17 checks,
no runtime errors). One Artifact upgrade increased radius from 0.32 to 0.416;
the projected mesh width increased by the same 30%, and sampled visible green
pixels increased from 1,835 to 3,035. The single capture was visually inspected.
Compilation retains the existing NU1900 warning because NuGet vulnerability
metadata was unavailable; the restored dependencies built successfully.

The ring represents the horizontal footprint of the existing sphere damage
query; elevation, hostile membership and line-of-sight still determine hits.
Projection follows terrain collision rather than the original native projector
engine; very sharp slopes, holes or cropped ground can clip parts of the ring.
Source scale remains uncalibrated. Native particle/projector animation parity
and original reticle audio are outside this feature.

Stop here for manual gameplay/visual review. No comprehensive regressions,
archive hashing, screenshot suite or performance benchmark is part of this work.
