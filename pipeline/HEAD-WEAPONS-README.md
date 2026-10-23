# Collectible heads and weapons

Four April headpieces (Warrior helmet, Inquisitor circlet, Agent mask, Hunter helmet) and eight new weapon models with matching source inventory icons are registered in Collections. Existing armor sharing applies to heads: Warrior/Inquisitor and Agent/Hunter. Each family earns its own headpiece at wave 11 on any playable map.

Reach wave 12 for High 01 weapons and wave 13 for High 02 weapons. Sith Warrior or Inquisitor earns single sabers; Inquisitor also earns double sabers. Bounty Hunter earns pistols; Agent earns rifles. Milestones persist across runs. These rewards are local design, not recovered MMO loot rules.

Single saber appearances are shared by Juggernaut, Sorcerer and Marauder; only Marauder receives a second saber. Double sabers are Assassin only. Pistols are shared by Mercenary and Powertech; only Mercenary receives a second pistol. Operative and Sniper share rifles. Selecting or resetting the main weapon mirrors the appearance to the left hand for dual wield classes. Combat gear, abilities and stats are unchanged.

Source evidence: `manifests/head-weapon-definitions.json` holds exact April GOM head definitions, `head-weapon-source.json` records unique TOR hashes, decoded SHA256s and dependency edges. `head-weapons-recipe.json` records selected models/materials. Conversion manifests are `head-weapons-conversion.json` and `wardrobe-head-*-character-*.json`. Weapon labels describe asset tiers rather than invented MMO item names.

Rebuild after the older wardrobe stages: bundled Python runs `scripts/prepare_head_weapons.py`; Blender runs `scripts/convert_collectible_heads.py -- --family head-FAMILY` for war, inq, spy, bh, and `scripts/convert_head_weapons.py`; bundled Python runs `scripts/stage_head_weapons.py`. Run `Run-Unlocks-Smoke.ps1` for isolated save, menu, compatibility and actual mesh/socket checks across all eight specializations. It also writes `work/collectible-SPECIALIZATION.png` previews.

Supported assets use the existing human male body type 2 rig and portable material conversion. Original helmet weights/binds are retained; enclosed helmets hide original hair. Source emissive maps are retained but the portable helmet bake omits emissive glow. Final visual acceptance remains a manual game review.
