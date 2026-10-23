# World loading screen

Play now displays original April SWTOR loading art before requesting the selected world in Godot's threaded resource loader. The blue bar reflects scene-resource progress through 85%, advances to 90% for scene creation, and reaches 100% after GameWorld finishes initialization. The overlay covers the first completed world frames, then removes itself. Scene instantiation and world setup still run on Godot's main thread and can briefly hold the indicator. There is no artificial timer or simulated download progress.

Original assets: loading-screen logo (slice 9), gold trim (12), blue fill (24), indicator (39), background tile and Korriban/Hutta/Dromund Kaas/Balmorra overlays and titles. The indicator rotates in Godot; the original Scaleform timeline is not executed. This is an adapted layout using original pixels.

Layout refinement: planet title overlays are omitted. Logo and loading frame share the canvas center. The single recovered gold edge is reflected to form an enclosed frame; the blue fill is a child of a clipped inner channel and cannot extend outside it.

`scripts/recover_loading_screen.py` reads the April archives without writing to them, matches historical candidate paths to unique TOR entries and records decoded SHA256 identities, archive offsets and slice rectangles in `manifests/loading-assets.json`. PNGs retain native RGBA pixels; Godot scales them for presentation. Additional atlas/slice images are retained for provenance and further refinement.

Run `./Run-Loading-Smoke.ps1` after importing assets. It exercises the actual menu handoff for all four destinations and checks that the overlay appears, the matching world finishes initialization and the overlay is removed. Rendered captures are saved in `work/loading-<map>.png`, with results in `logs/loading-<map>.log`.

Verified 2026-10-23: build succeeded; all four rendered transition checks passed. Korriban and Hutta captures visually inspected. Build retains existing nullable warnings and unreachable NuGet advisory-feed warning. Runtime reports ObjectDB objects remaining at shutdown in the test sessions; no transition or resource-load errors were reported. Failure recovery is implemented but was not fault-injected.
