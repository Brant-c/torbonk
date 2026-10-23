# Blue drop symbols

The original `bwaui_buttons.gfx` atlas slices are reused without recolouring:

- XP pickup: `bwaui_buttons_298.png` (star / upward symbol).
- Ordinary item reward: `bwaui_buttons_135.png` (open inventory box).
- Major-boss and exploration-cache reward: `bwaui_buttons_242.png` (diamond).

These are application role assignments approved from the recovered icon comparison, not claims about native SWTOR XP or loot bindings. Exact slice regions and source hashes remain in `manifests/ui-conversion.json` and `ui-source.json`. No source assets were changed or generated.

XP's previous constructed octahedron and tinted flare are replaced by an unshaded camera-facing sprite; bobbing, pulse, attraction, value, merge cap and collection flight remain. Item rewards already open automatically. Their markers now appear at the reward source when an actual offer is created; this does not add a collectible item or change reward timing/chances. Markers freeze with gameplay during reward selection and fade after three gameplay seconds. Reset clears them. Cache rewards use the player's position at cache claim.

Validation: C# build passes. `--drop-symbol-smoke` checks the XP texture and exact collection amount, real enemy reward paths at tiers 1 and 2, marker categories, reward choice/claim and reset cleanup. Runtime log: `logs/drop-symbol-smoke.log`. `manifests/drop-symbols-world.png` shows a deliberately staged comparison of the three icons in the actual game renderer. Tests use isolated APPDATA at `work/drop-symbol-userdata`.
