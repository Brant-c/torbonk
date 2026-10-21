# B9 results — 2026-10-21

Implemented and stopped for manual review.

- Corrected the initial cyan engine-default selection after user review. The
  active cursor now uses the installed SWTOR client's native gold arrow, gold
  interaction hand and blue unavailable hand. Each image uses its recovered
  hotspot. Original pixels/alpha remain unchanged; source executable and resource
  offsets/hashes are in `manifests/b9-native-cursor.json`. These bytes are not
  claimed as April TOR assets. The rejected cyan candidate evidence is retained
  and explicitly marked retired in B9-PROVENANCE.md and the older manifests.
- Retained one persistent native cursor controller and reusable theme for
  frontend and gameplay, now with separate per-state hotspots. Pressing keeps the
  current hover/default artwork; no separate original pressed image was identified.
- Extended CameraRig with independent yaw/pitch, RMB capture, LMB drag threshold,
  smooth bounded zoom, configurable sensitivity/inversion/follow and optional
  auto-centering (off). Kept B1's initial elevated framing, sphere/ray collision
  probes, collision masks and temporary prop hiding. Following now anchors
  directly to the interpolated character (FollowSharpness = 0), pitch responds
  immediately in both directions, and the default pitch limits widen to -80/85.
  Automatic camera lifting is off; collision still prevents geometry clipping.
  Wheel zoom also works while orbiting. Safe retraction remains immediate;
  outward recovery eases.
- Added Space jump through the existing PlayerController capsule and gravity.
  JumpHeight exports 0.14 source units; ground-footprint traversal checks continue
  in the air. Only grounded presses jump; pause clears pending requests. Existing
  horizontal speed and movement-facing logic remain. The subsequent animation
  follow-up adds original April single-saber launch/air/fall/landing clips below
  the existing casting layers; see B9-JUMP-README.md for focused validation.
  No ability damage, targeting, animation layering, models, waves, boss tuning,
  progression or frontend class/map data was changed by B9.

C# build passed with zero errors. The pre-existing NU1900 unavailable NuGet
vulnerability-service warning and StormAreaIndicator nullable warning remain.

The final brief Godot 4.7.2 runtime smoke passed **37 checks**, using actual viewport
input through the frontend → Korriban → frontend flow. It covers initial cursor
and framing, RMB capture/rotation/release, LMB threshold/orbit, pitch limits,
camera-relative movement with persistent yaw, smooth zoom and both limits, HUD
tooltip priority, release over UI, real upgrade pause/selection/resume, focus
loss, death, restart and scene teardown without a duplicate cursor controller.
The follow-up also checks gold hand/disabled-state hotspots, direct pitch and
character anchoring, zoom during capture, Space takeoff, air movement, landing,
no double jump/repeat and no jump queued during a paused upgrade presentation.

An initial smoke exposed cursor texture retention during shutdown and redundant
fixture mouse-enter notifications. Both were fixed. The user-requested camera,
jump and gold-cursor follow-up passed one updated runtime smoke with **no
runtime/import errors or warnings**, including renderer teardown.
`manifests/b9-smoke.json` is the final receipt. `logs/b9-runtime.log` and
`logs/b9-smoke-console.log` contain the final run and build output. Future runner
invocations also retain `logs/b9-runtime-console.log` to catch late teardown
errors after Godot closes its own log.

No historical asset regressions, archive rehashing, broad gameplay suites,
benchmarks or screenshot series were run. Archives and emulator remained
read-only. Existing concurrent frontend edits were left in place.

Manual checks still needed: native cursor appearance/hotspot on high-DPI
displays; every frontend step/dialog and disabled control; sustained orbit/zoom
beside ruins and slopes; obstruction hiding/restoration; enemy/boss warning and
Force Storm readability. Native April runtime cursor selection was not observed.
Use the short checklist in B9-README.md, including Space jumping and wider camera
pitch. Work stops at B9 plus these user-requested corrections.
