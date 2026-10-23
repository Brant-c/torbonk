# D4 inventory — 2026-10-23, before implementation

Actual working tree audited, including the newer uncommitted maps, class kits, wardrobe and unlock work. Those changes are retained.

| System | Reusable now | D4 gap / selection |
|---|---|---|
| Terrain | Area `4611686022825732973`, `baseroom`; sectors `4611686022895042660`, `4611686022895042664`; XZ `[20.2,45.4,6.2,18.8]` (25.2 × 12.6 source units), 15,863 triangles | Add four directly adjacent authored sectors, retain original heights and holes |
| Scenery | 109 source records, 107 visible placements, 31 original models; statues, ruined walls/pillars, cliffs and cacti | Additional ruins, burial props, ancient crate, ritual brazier; camera/footprint-safe placement |
| Collision | Derived triangle shapes; terrain layer 1, props 2; capsule support ring, slope/drop checks, local steering and straight safe spawn corridors | Validate connectivity across added sectors, use existing convex clutter policy |
| Roster | Broodling (`klorslug` rig), Shyrack Forager (`dragonbat`), Crypt Looter (`bmnnew`); original idle/run/attack/hit/death | Recover tuk’ata (`dog`), ancient assassin droid (`assassin`), Ancient Beast (`terentatek`) from explicit April appearance links |
| Boss | Broodmatron, 650 base HP; 1.35 s locked-ground warning, eruption, recovery; one controller, boss frame, introduction/death events | Reuse controller for serialized level 5/10/15/20/25/30 milestones; droid and terentatek major encounters |
| Progression | 30 s waves, wave 5 boss; elite every 20 spawns from wave 2; XP 12 × 1.3^(level−1), three upgrade choices | Korriban-only stages/XP curve; retain wave cadence and other planet defaults |
| Rewards | Seven unique run relics and three instant powers; 35% elite chance, guaranteed boss; three slots, replacement UI, one actor/death payment | Guaranteed scheduled elites, stronger major choices, class-neutral relics, optional caches |
| Classes | Eight Imperial advanced classes, original outfits/weapons, starter + unlockable kit, shared ability lifecycle | No Sorcerer-dependent mechanics or global class retuning |
| Pipeline | Exact-path read-only TOR reader with decoded resource receipts; GR2/JBA family rig conversion, material maps, dense/sparse terrain decode, Godot scene compiler; typed Wwise HIRC/DIDX slicing | Bounded selected dependencies only; native MMO lighting, FX engine and animation graphs remain adapters |

Source selection evidence: `manifests/d4-definitions.json`. NPC → appearance → creature registry IDs: ancient protector `1002841/1002844`, Ancient Beast `1000158/1001462`, spitting tuk’ata `1000123/1001442`. These are April bucket-derived identities. Combat mechanics, region arrangement, rewards and tuning are Survivors designs.

Area ASSETS/ROOMS include all four numbered tomb sets, Academy rooms, original Korriban statues, sarcophagi, braziers, ancient containers and Sith chamber modules. A room number alone does not establish which named Sith tomb it represents. D4 will not label unverified numbered interiors as Ajunta Pall / Marka Ragnos / Tulak Hord / Naga Sadow, nor fabricate original topography. Use the actual contiguous Valley terrain with adapted precinct/wilds/forbidden burial scenery.
