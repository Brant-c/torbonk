# EV1A Equipment

C toggles Character, I toggles Inventory; both windows can remain open. Equipment on the main menu opens both. Escape closes them; each pane also has a recovered blue X anchored at its top right. Right-click a bag item to equip it (first empty compatible slot, otherwise the first matching slot); right-click an equipped item to return it to the bag. Drag from the bag onto a matching character slot or character pane, and drag from Character back onto Inventory to remove it. Invalid or cancelled drops make no changes. Equipped accessories do not consume bag grid cells. Slots appear left of the model in earpiece, implant 1, implant 2, relic 1, relic 2 order. Hover for descriptions, compatibility and comparisons; selecting an owned item shows only its upgrade controls.

Equipment uses a separate atomic user://equipment-v1.json profile, shared across scenes. Owned items and ranks and Credits are shared; equipped slots are saved per advanced class. Initial demonstration inventory contains eight accessories and 10,000 Credits, seeded only when no profile exists. No drops, rewards, combat effects or appearance changes are added. Corrupt/unsupported saves are preserved and writes disabled with a visible notice.

Upgrade costs are configurable in godot/Gameplay/Equipment/accessories.json (500/1500/3000). Ranks cap at +3. Equipment is restricted to the appropriate class and slot, and each owned instance occupies at most one slot in a class loadout.

Presentation reuses CharacterPreview and its saved Collections assembly, Eurostile fonts, recovered menu frames, item frames and tooltip skin. Original accessory icon DDS files were copied unchanged from source-assets/april2012/resources/gfx/icons. Item names/descriptions are representative demonstration labels, not recovered item record text; layout uses the existing recovered skin rather than a pixel-exact reconstruction of the original paperdoll window.

Close artwork: bwaui_buttons_266.png, recovered blue X. Red close artwork is not used.

Validation: native mouse-event drag tests cover equipping and removal; right-click handlers, drop compatibility and bag exclusion are also checked. dotnet build and Integration/EquipmentSmoke.tscn cover class and slot rejection, ownership, unique instances, both implants/relics, +3 cap, credit deduction, insufficient funds, unequip and disk reload. Render reviewed at 1600x900. Existing unrelated build warnings remain.


UI polish: close icons moved 4 design pixels right and 7 down. Pane frames follow their content minimum height, with Inventory growing/shrinking when upgrade controls appear. Tooltip delay is zero. Native drag sources fade to 35% opacity and restore on rejected/cancelled drops. Smoke checks verify fading during real pointer drags, restoration after a rejected drop, and the immediate-tooltip setting.

Equipment tooltips now use a dedicated SWTOR-style layout: icon and rarity-colored header, upgrade rank, item category, description, right-aligned class requirements, equipped comparisons, and upgrade cost footer. Optional Stats and Effects arrays on the accessory catalog populate Total Stats and Equip sections only when present. No ratings, binding rules, level requirements, currencies or effects are invented. Tooltips read current profile data when opened.
Tooltip first-hover fix: item cells return an empty native tooltip string and draw the item card directly in the equipment canvas. This completely bypasses the default PopupPanel and its outer frame. Cards remain transparent while text/container layout settles, then appear at their final size; exiting, dragging, refreshing or closing dismisses them. Regression checks assert no native popup, invisible unfinished layout, and stable dimensions from the first visible frame.

