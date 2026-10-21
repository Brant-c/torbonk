# D1 — Modular equipment and appearance foundation

> Superseded frontend/controls behavior: [D11-README.md](D11-README.md) adds
> persistent menu Collections and the shared 3D preview. Standard gameplay no
> longer exposes Gear Review; the explicit D1 inspection launcher still does.
> The review/validation notes below describe the original D1 milestone.

Implemented in `pipeline/godot/Gameplay/Equipment`. Only the existing Sorcerer is playable.

## Manual review

Normal development runs now show a **GEAR REVIEW [F5]** button at the upper right, below Restart. Click it for outfit, saber and chest-cosmetic buttons. `pipeline/Open-D1Equipment.ps1` opens Korriban directly with this panel already expanded:

- **F5:** expand/collapse the development gear panel.
- **F6:** cycle A (level-1 Inquisitor) / B (existing SS A04 cosmetic).
- **F7:** unequip / restore the working red lightsaber.
- **F8:** toggle a chest-only SS A04 cosmetic over the functional chest item.
- Existing WASD, Space, camera and combat controls remain active. Scroll to inspect more closely.

Review idle, running/casting, robe motion, hands and saber orientation. Stop here for manual visual approval. Review controls save no equipment or unlocks. Normal `Open-Survivors.ps1` uses outfit A and exposes the panel/keys in development builds; exported release builds require the explicit `--d1-equipment` flag. The menu uses the same advanced-class artwork UI as the other classes; it does not show a Sorcerer model. Class/map selection remains intact. Selection defaults describe the level-1 outfit. There are no chests or loot drops in D1.

## Existing architecture review

`CharacterPresentation.cs` owns the instance-local AnimationTree, locomotion, jump, upper-body casting masks, hit and death reactions. The D1 pose follow-up adds equipment-selected idle/run clip references to this existing tree. Its `Chest1`, `RootSpine`, `LowerBack` masks and imported BMN animation libraries are specific to this animation/rig profile; they are not assumed to work for all classes or creatures.

With the red saber visible, locomotion uses the original `cb_knight_saber_idle_1.jba` and `mv_saber_run_forward_01.jba`, already converted in B6. This replaces the fists-raised unarmed defaults. The weapon appearance's optional `Locomotion` data selects the source library/clip names; armor changes keep the same tree and cast owner. Removing or hiding the saber restores the base unarmed clips, and restoring it selects saber locomotion again. No new extraction, animation controller, attack or damage behavior was added. Original source hashes and the reused conversion are recorded in `manifests/d1-locomotion-source.json`.

The original character recipe selected SS A04 from appearance registries, not an original starter-equipment package. `GameWorld.tscn` and the launch adapter retain the verified Sorcerer GLB as the base rig composition. D1 hides its armor mesh groups and reuses its head, hair, original bmnnew skeleton and existing cloth-bone superset. There is one live skeleton and one AnimationPlayer. No health, movement, progression or ability object is recreated during a swap.

The previous right-hand-only `EquipmentAttachment` entry point remains usable by existing NPCs. A new overload accepts an explicit scene and attachment profile. D1 keeps the same original hilt, `RightWeapon` socket correction, scale and reconstructed red blade renderer. Main/off-hand bones and independent per-weapon offsets are data, not advanced-class branches.

## Equipment contracts

- `EquipmentDefinitions.cs`: game-local item IDs, separate optional original GOM identities, configurable slots, rarity/category tags, source/icon references, optional modifiers, cosmetic-only flags, body/rig/species/class restrictions, animation and attachment profiles.
- `EquipmentLoadout`: `TryEquip`, `TryUnequip`, `TrySetCosmetic`, `TryHide`, `TryPreset`, `TryApply`, `Changed`, `Current` and `StatModifiers()` are integration boundaries for future inventory/loot. Snapshots are copied. Operations validate before committing; rejected appearances retain the previous loadout and visible equipment.
- Functional items and cosmetic appearances are separate dictionaries. `StatModifiers()` only sums equipped functional items, once per occupied slot. D1 authors no gameplay modifiers and does not alter existing damage formulas. Future consumers should replace their equipment modifier group from this aggregate, rather than cumulatively adding event results.
- Item logic works without Godot scene conversion. An item with no converted appearance can leave its appearance reference empty and use the rig's authored visual fallback. An explicitly missing or incompatible visual is reported, never silently skinned to the wrong rig.
- `CharacterAppearanceAssembly` validates every skin bind by bone name, parent and rest transform, retaining inverse binds, weights, materials and source transforms. It caches armor mesh instances/resources, toggles visibility, and stages new weapon sockets before retiring old ones. It never advances or restarts animations. Extraction/Blender are offline only.
- `CoversSlots` supports inherited garment regions; `HidesBaseGroups` removes replaced geometry. Empty armor slots use rig-specific level-1 fallbacks because no naked-body set was recovered for D1. Those regions cannot be hidden into holes. Optional helmet/accessory slots may be empty; weapon visuals can be hidden. `BlocksSlots` supports authored equipment conflicts such as future two-handed weapons.
- Head, chest, hands, legs, feet, waist, wrists, main hand, off hand, two relics, ear and two implants are declared independently of any UI layout. Only chest/hands/legs/feet/waist and the existing main-hand saber have converted visuals. Head/accessory source candidates are documented but not converted.
- All eight frontend base-class IDs and sixteen advanced-class IDs are registered in `catalog.json`. Only Sorcerer has an enabled slot/weapon profile; future classes must supply explicit rules and compatible content. No default weapon rules, meshes or clips are inferred for them.

The generic loadout/assembly contain no `sith_inquisitor` or `sorcerer` equipment branches. `EquipmentComposition` is the current content binding for the existing gameplay scene; future scene bindings choose their own rig and presets. A future frontend model viewer could call the same assembly with its own rig; this milestone uses artwork in the menu.

## Outfit evidence and limitations

**A: documented level-1 alternative, not a claim of verified complete spawn inventory.** Original April item prototypes for chest, legs and boots have `itmBaseLevel=1` and explicitly reference `ipp.class.inq.starting.cdps1.t1x1.*`. The matching hands and waist appearances complete the same authored appearance family. Their original item identities have not been established and remain null. No headgear or bracers are equipped. This is a simple tunic/trousers outfit, not a fabricated starter robe.

| Slot | Original item GOM identity (where verified) | Model / material registry IDs |
|---|---|---|
| Chest | `0xE000DD4FCFCC757D` | `1002328 / 1003772`, attachment `1002329` only |
| Legs | `0xE0002314C3EE1FEA` | `1003752 / 1004985` |
| Feet | `0xE000F141271037FA` | `1002314 / 1003770` |
| Hands | No verified item identity; IPP `0xE0005B86F5EF0C7C` | `1002325 / 1003771` |
| Waist | No verified item identity; IPP `0xE0008CC02F15778C` | `1002316 / 1003767` |

The direct IPP model/material/hue fields are preserved, including the original material-name/color-scheme naming differences. Palette scheme `1003284`, primary `1002161`, secondary `1002144` (boots `1002164`) are decoded from April prototypes. The pipeline does not choose additional collar/shoulder/shin attachments merely because they appear in a registry. Body type is `bmn`; skeleton is `/resources/art/dynamic/spec/bmnnew_skeleton.gr2`; original scale is 1. Original palette-baked portable materials retain the established pipeline's approximated specular/lighting behavior.

The level-1 weapon item `0xE000A3A1C47DBA5A` references `wep.melee.training.saber` and the training-saber icon. D1 records it but does not replace the working red saber or claim that red saber is the historical starting weapon. The red saber's local item has no invented original GOM ID.

**B: original SS A04 alternate.** Existing robe/skirt/gauntlets/boots/belt, original material and palette assignments, and all original cloth bones are retained. Original source records do not prove an exact gameplay level for this appearance. It is developer-only cosmetic content over the same functional items and cannot be selected as normal starting equipment.

Source receipts:

- `manifests/d1-definition-evidence.json`: deciding original item/IPP/class records, exact identity namespaces, bucket offsets and SHA-256 hashes. Decoder field labels retain their historical-name-map caveats.
- `manifests/d1-palette-evidence.json`: original palette fields and expanded color values.
- `manifests/d1-source.json`: 64 explicitly selected registry/model/material/texture/icon/bucket resources, archive hashes/offsets, decoded hashes and dependency edges. Archives were opened read-only; only selected resource bytes were hashed. No whole-archive rehash, emulator edit or broad extraction.
- `d1-character-recipe.json`: exact selected paths, registry XML, IPP identities, materials, per-slot palettes and attachment selection.
- `manifests/d1-character-conversion.json` and `d1-character-probe.json`: conversion settings, original hierarchy/bind data, cloth bones, weights, material references and sampled poses.
- `manifests/d1-icons.json`, `d1-staging.json`: icon conversion and staged artifact hashes. Original head/hair, skeleton, animation and cloth dependencies continue to reference the unchanged original character recipe/source receipts.

The preconverted outfit GLB SHA-256 is `9a6d61600179729b5e813dcb95c4a5474917a164fa4ac33fad2a395b72d00b50`. Previously verified GLBs, recipes and source receipts were not overwritten. The new conversion wrapper reuses existing conversion scripts in memory while redirecting all outputs to D1 paths.

## Validation

`Run-D1-Smoke.ps1` compiles C#, imports changed resources and runs one brief targeted rendered Godot session. `manifests/d1-smoke.json` records the result. Both presets loaded; idle/run/moving cast/hit/death, original hand/finger/VFX bones, saber motion and unequip/restore, single-slot cosmetics, rig fallback, incompatibility rejection and unchanged gameplay objects were checked. A synthetic in-memory modifier verifies cosmetic separation; it is not authored loot. No historical regression suites, archive rehashes, performance benchmarks or capture loops ran.

The initial checks needed correction for the original `LeftWrist`/`RightWrist` bone names and the source hit clip's 1.6-second duration. Final runtime passed. The smoke uses the existing pre-shutdown managed-collection cleanup workaround used by B10.1. The build retains the existing `StormAreaIndicator` nullable warning and a NuGet vulnerability-feed connectivity warning.

Manual visual judgment of clipping, palette appearance and cloth motion remains pending. No cloth simulation was added; original weighted cloth behavior is retained. Inventory screens, loot, persistence, unlocks, other playable classes and new attacks are outside D1.
