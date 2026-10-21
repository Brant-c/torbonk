# D1.1 — Main-menu Collections and cosmetic wardrobe

D1.1 is complete and ready for manual visual review. No loot, chests, artifacts, new classes, planets, or equipment statistics were added.

## Launch and review

From `D:/SWTORClassic/torbonk`, run:

```powershell
.\pipeline\Open-Survivors.ps1
```

Choose **Collections → Appearances**. Sith Inquisitor / Sorcerer is selected initially. Select an equipment slot, then an unlocked appearance to equip, preview, and save immediately. Drag the character to rotate; scroll to zoom; double-click to reset the view. **Reset Slot** and **Reset Outfit** restore the starter visuals while retaining unlocks. Escape or Main Menu closes Collections.

For alternate-outfit visual review:

```powershell
.\pipeline\Open-Survivors.ps1 -WardrobeDev
```

The explicit `--wardrobe-dev` argument exposes **DEV · Unlock SS A04** only inside Collections. It unlocks the five existing alternate cosmetic pieces (chest, hands, legs, feet, waist). These remain unlocked on later normal launches. Normal play now unlocks the same pieces through run milestones; see `UNLOCKS-README.md`. Starter pieces and the existing red lightsaber are unlocked initially. Head has no converted content and stays disabled. Weapon has only the existing red-saber appearance; no training-saber substitution is claimed.

New Run retains the existing faction → class → advanced class → appearance summary → map → Ready flow. Collections adds no mandatory step. The appearance summary and Ready previews now use the saved wardrobe. All sixteen advanced-class IDs and eight base-class IDs are selectable in Collections, but only Sorcerer has a working model/equipment profile. Others show their own recovered artwork and an unavailable message.

## Implementation

- `Frontend/Scripts/CollectionsScreen.cs` hosts the Appearances category, frontend class navigation, equipment slots, explicit Equipped / Unlocked / Locked / Incompatible labels, reset actions, and the developer unlock button. Only authored compatible converted slots can be selected.
- `Gameplay/Equipment/CosmeticCollection.cs` stores schema version 1, unlocked appearance IDs, and separate `base_class/advanced_class` profiles with a default-loadout ID and cosmetic slot overrides. An absent override means the authored default for that slot. All sixteen profiles have independent dictionaries.
- `CharacterPreview` reuses `EquipmentComposition` and `CharacterAppearanceAssembly`, just like gameplay. Its recovered saber idle, socket, blade, zoom, and rotation are retained. Armor changes affect the same live preview rig without resetting its animation.
- `EquipmentComposition` restores compatible saved cosmetics over the unchanged functional starter items. Appearance assembly still stages/validates meshes and attachments. Failed visual staging leaves the corresponding starter region intact. The save has no functional items, stat modifiers, ability, progression, or run fields.
- F5–F8 / Gear Review are absent from standard gameplay, including debug builds. Existing D1 inspection controls require explicit `--d1-equipment`; `Open-D1Equipment.ps1` remains the opt-in inspection launcher. Those controls do not write wardrobe saves.

The save is `user://cosmetic-collections.json`, normally under `%APPDATA%/Godot/app_userdata/SWTOR Survivors - Static Asset Inspection/`. Writes use a temporary file and atomic replacement. A failed save reports an error and restores the previous visible selection. Loading validates schema, known IDs, converted resource availability, slot, class, body, rig, and animation compatibility. Unknown, wrong-slot, locked, or incompatible overrides fall back to the authored starter. Bad JSON or unsupported schema loads defaults. No save field can grant gameplay power.

## Recovered UI assets

Reuses the existing converted April assets; no archive extraction or source modification:

- `Frontend/Assets/UI/bwaui_standard_window_1.png`: frames and headers.
- `Frontend/Assets/UI/bwaui_standard_window_{9,18,24}.png`: buttons, selection/category states, and highlights.
- `Frontend/Assets/Slices/bwaui_character_create_panels_9.png`: character preview frame.
- `Assets/UI/slices/bwaui_buttons_381.png`: recovered square icon frame reused for equipment cells and empty appearance icons. This is a Survivors reuse of recovered artwork, not a claim of native SWTOR inventory placement.
- `Assets/Equipment/{chest,hands,legs,feet,waist}.png`: the existing recovered starter icons. Alternate pieces and red saber use the empty recovered icon frame and explicit names because no matching icons were established in D1; no unrelated icon is passed off as theirs.
- Existing recovered class/advanced-class art, Eurostile fonts, tooltip frame, and frontend sounds.

## Lightweight validation

`Run-D11-Smoke.ps1` compiles C#, imports changed resources, opens one brief rendered wardrobe session, then restarts the application once with the same isolated user-data directory to verify persistence and the gameplay handoff. It never modifies the normal player's collection. The restart runs without the developer unlock flag.

Passed receipts: `manifests/d11-save-smoke.json`, `manifests/d11-load-smoke.json`. Review capture: `work/d11-collections.png`.

Verified menu entry, locked/default states, immediate selection, resets retaining unlocks, class switching, all 16 profile keys, disabled head, preview rotation/zoom, application restart persistence, original New Run navigation, saved outfit in gameplay, absent Gear Review, inert F5–F8, unchanged functional starter items and modifier aggregate, original health/movement/damage/cooldown/shield/ability defaults, original saber socket/idle, invalid saved-ID fallback, and retention across run reset.

Build passed with the existing StormAreaIndicator nullable warning and NuGet vulnerability-feed connectivity warning. The first build encountered a transient DLL write lock; rebuilding with shared compilation disabled succeeded. No historical regressions, archive rehashing, performance testing, or repeated rendered suites were performed. Stop here for manual review of mixed-piece clipping, original palette appearance, robe motion, and navigation at the preferred window size.
