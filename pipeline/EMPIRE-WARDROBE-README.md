# Empire collectible appearances

Twenty new cosmetic armor pieces: five each from the April Warrior, Inquisitor, Agent and Bounty Hunter heroic families. Slots: chest, hands, legs, feet and waist. These are cosmetic overrides; functional equipment and combat statistics stay in their starter presets.

Warrior and Inquisitor share their two families. Agent and Bounty Hunter share theirs. Each class earns only its own family at waves 6 (feet), 7 (hands), 8 (waist), 9 (legs) and 10 (chest), on any currently playable map. Once earned, a piece can be worn by either class in its pair. Previously granted unlocks are retained. These milestones are local game design, not recovered MMO reward rules. Unlocks persist account-wide; equipped outfits remain per advanced class. Existing saves receive rewards for milestones they have already recorded.

Collections displays only unlocked, compatible appearances. Armor icons come from each corresponding April appearance's DDS resource, converted to PNG without tint or resampling. The 20 new pieces, 15 Warrior/Agent/Hunter starter pieces and five existing SS A04 pieces have explicit icons. Missing icons no longer borrow Inquisitor artwork.

Evidence: `manifests/empire-wardrobe-selected.json` contains exact GOM identities, fields, bucket hashes and selected attachment lists. `empire-wardrobe-source.json` records unique TOR entry hashes, decoded SHA256 hashes and dependency edges. `empire-wardrobe-ss-icons.json` supplies the SS A04 icon definitions; staging verifies their model and material IDs against the existing outfit. `empire-wardrobe-staging.json` lists converted icon outputs. The earlier `empire-wardrobe-definitions.json` preserves the initial selection; the selected file is the build input. The initial Hunter trench chest needs additional cloth bones; the build uses its supported t1x1 heroic family instead.

Rebuild with the bundled Python and Blender runtimes: run `scripts/prepare_empire_wardrobe.py`, run Blender with `scripts/convert_empire_wardrobe.py -- --family FAMILY` for war, inq, spy and bh, then run `scripts/stage_empire_wardrobe.py` with Pillow available. Stage after the older D1/D3 catalog generators. Run `Run-Unlocks-Smoke.ps1` for isolated save, unlock, menu, icon and per-class mesh checks; it never uses the normal player's collection save.

Current support is the existing human male body-type-2 rig. Original weights and skin binds are retained. Portable material baking omits emissive glow; original maps are retained. Native MMO shader/cloth parity is not claimed.
