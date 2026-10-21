# B9 — SWTOR cursor and free third-person camera

Launch the existing frontend:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Survivors.ps1'
```

Choose Sith Sorcerer and Korriban. `Open-Gameplay.ps1` still opens gameplay
directly. The shared native cursor also applies to faction/class selection,
customization, galaxy selection and dialogs. No original client or emulator runs.

| Input | Behavior |
|---|---|
| Hold RMB, move mouse | Capture/hide cursor and orbit yaw/pitch; release restores cursor at its starting position. |
| Hold LMB, drag in world | Orbit after a four-pixel threshold; character facing remains independent. |
| Wheel up/down in world | Smooth zoom in/out, bounded by configured limits and actual obstruction clearance. |
| WASD | Existing camera-relative movement; character faces actual movement. |
| Space | Jump while grounded; movement continues in the air. No double jump or held-key repeat. |
| R | Existing restart; releases capture. |

An orbit must start outside UI. HUD tooltips/icons, buttons and upgrade cards
take priority over both drag starts and zoom. Once an orbit owns capture, its
motion and release are consumed before GUI processing so dragging cannot hover
or activate menu controls. Level-up pauses, death, focus loss and scene changes
release capture. Resuming a run requires a fresh drag. Character previews retain
their existing local drag/zoom controls.

Camera yaw persists during movement and restart; it never follows character
facing by default. The camera anchors directly to the interpolated character,
and user pitch responds immediately in both directions. Automatic camera lifting
is disabled. Collision can still shorten the camera boom to avoid geometry.
Automatic combat and targeting do not read mouse motion. Jumping uses the
existing capsule, gravity and terrain-footprint guards; it does not allow crossing
the crop boundary or terrain holes. Original April single-saber jump animations
now follow takeoff, ascent, descent and actual landing, with standing/running
landing variants. The existing Sorcerer gesture layers stay above jump locomotion.
See [B9-JUMP-README.md](B9-JUMP-README.md) for provenance and animation tuning.

Select `CameraRig` in `godot/Gameplay/GameWorld.tscn` to tune these exports:

| Setting | Default |
|---|---|
| MouseSensitivity | 0.18 degrees per physical mouse pixel |
| DefaultPitch / MinimumPitch / MaximumPitch | 48 / -80 / 85 degrees |
| DefaultZoom / MinimumZoom / MaximumZoom | 1.383 / 0.35 / 2.5 source units |
| ZoomStep / ZoomSharpness | 0.12 source units per wheel step / 10 |
| FollowSharpness / ObstructionRecoverySharpness | 0 (direct anchor) / 8 |
| AutoCenter / AutoCenterDelay / AutoCenterSharpness | false / 3 seconds / 1.5 |
| InvertMouseY / LeftDragThreshold | false / 4 pixels |

These distances use preserved SWTOR source units, **not metres**. Default framing
matches the former B1 camera envelope. Smaller sharpness values ease more slowly.
FollowSharpness = 0 disables the extra following delay; a positive value restores
optional follow smoothing. PlayerController exports JumpHeight = 0.14 source
units, with launch velocity derived from that height and the existing Gravity.
Collision retains mask 3 (terrain 1, props 2), the sphere sweep, centre-ray
fallback, optional raised obstruction angles and source-prop hiding. Terrain is never
hidden, and camera exclusions do not change player collisions. Retraction is
immediate for safety; outward recovery is smoothed.

`Integration/SwtorCursor.cs` is the sole cursor autoload.
`Assets/UI/Cursor/cursor_theme.tres` configures the native art and hotspot.
The gold 32×32 arrow is used at native size across UI scales, with its recovered
(8,3) hotspot. Buttons use the original gold hand at (13,1); disabled buttons use
the original blue hand at (13,2). Each state keeps its own native hotspot. A
separate pressed image is not identified, so clicking retains the hover/default
artwork. The earlier cyan engine-default texture is no longer bound to the theme.
No settings menu expansion is included.

The correct artwork and hotspots were extracted read-only from the installed
SWTOR client's native Windows resources, not from an April TOR archive. Original
April byte identity remains unverified. See B9-PROVENANCE.md and
`manifests/b9-native-cursor.json`; `scripts/prepare_b9_native_cursor.py` reproduces
the selected resource extraction without loading or executing the client.
Tight corners may still cause a fast safety retraction or temporary prop hiding.
Native OS cursor rendering and high-DPI feel require manual review.

Focused check, when needed:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Run-B9-Smoke.ps1'
```

This compiles C#, imports changed resources and runs one brief viewport-input
session. It does not run archive regressions or camera benchmarks. See
`manifests/b9-smoke.json`, `logs/b9-runtime.log` and B9-RESULTS.md.

Manual review checklist:

- Check cursor appearance, click alignment and consistent size across every
  frontend screen, Settings, disabled appearance buttons and gameplay.
- Orbit a full circle with each mouse button; check stationary facing, release
  position, wider pitch limits and WASD movement in each direction after rotating.
- Press Space standing and moving; check takeoff, air movement and landing. Hold
  Space and press it while airborne to verify there are no repeat/double jumps.
- Zoom near ruins and slopes; check wall/terrain clipping, player framing,
  temporary prop restoration and smooth outward recovery.
- Hover tooltips, click upgrade cards, then resume; verify a fresh drag works.
- Alt-tab while dragging; restart, die and return to the menu; verify visibility.
- Review zoomed-out enemy/boss warnings and Force Storm's ground indicator.

Stop after B9 for manual review. No ability, enemy, planet or combat balance work
is part of this milestone.
