# SWTOR Survivors — B6: Korriban enemies and boss

Launch the playable game:

```powershell
& 'D:\SWTORClassic\torbonk\pipeline\Open-Gameplay.ps1'
```

WASD moves, abilities attack automatically, R restarts, Esc quits. The existing
ability unlocks, upgrades, rarity, action bar, GCD, green Force Storm indicator,
floating combat text and death/restart flow remain in place. No melee player
ability was added. The red lightsaber is visual equipment.

## Roster

| Enemy | Role | HP / damage | XP | First wave |
|---|---|---:|---:|---:|
| K’lor’slug Broodling | Small natural melee swarm | 16 / 2 | 2 | 1 |
| Shyrack Forager | Fast flying silhouette, natural attacks | 30 / 3 | 4 | 2 |
| Crypt Looter | Armed humanoid with original electroblade | 55 / 5 | 7 | 3 |
| K’lor’slug Broodmatron | Larger soldier silhouette and boss bar | 650 / 8 | 80 | 5, once |

New enemies retain the existing per-wave health/damage multipliers. Broodmatron
also has an authored **Burrow Eruption**: an orange ground warning locks the
player's location for 1.35 seconds, resolves an 18-damage strike inside radius
0.30, and recovers before resuming pursuit. Move outside the ring to avoid it.
The encounter ends on boss death, player death or restart. Safe spawn placement
retries if blocked. Its one reserved slot can exceed the regular mob cap by one.

## Density and configuration

Regular living caps are 16, 23, 30, 37, 44, 51, 58, 65, 72, then 75. Initial
spawn interval is 0.28 seconds, multiplied by 0.88 each wave, minimum 0.10.
These are caps and spawn attempts, not a guarantee that enemies remain alive
at those counts. Manual tuning to 100 is available through MaximumActiveLimit.
No 100-enemy performance benchmark was performed.

GameWorld exports expose wave duration, cadence, cap increments, maximum cap,
boss wave, spawn ring and corpse duration. `Gameplay/Enemies/roster.json` holds
scene paths, unlock waves, initial weights and weight growth. Enemy scenes hold
stats, capsule dimensions, aim height, XP and visual selection.

Mob capsules retain terrain/prop collision but do not solidly block one another.
A small spatial grid supplies local soft separation. Each size uses matching
navigation clearance and terrain probes. The existing bounded local steering
and safe spawn corridor checks remain; deep concave ruins can still stall an
enemy. Spawning can pause near holes or obstructed terrain. This is not a global
path planner, a terrain expansion, or a guarantee of off-screen spawning.

## Reusable visuals and equipment

`EnemyPresentation.cs` maps five exported clip names and works with any imported
rig providing those clips. Every B6 GLB includes its own original skeleton and
idle, run, attack, hit and death animations. EnemyAgent, Health, targeting,
damage events, defeat counting and XP do not depend on those rig names.
The player's existing CharacterPresentation casting/movement layers are retained.

`EquipmentAttachment.cs` uses BoneAttachment3D on the original **RightWeapon**
bone; it does not edit fingers, hands or VFX sockets. Hilt and electroblade use
original mesh proportions at scale 1. An asset-derived correction reconciles the
established imported socket rest with the original GR2 rest (about 3.75 degrees,
no gameplay hand offsets). The hilt's emitter is on its original -Z axis.

The blade renderer is reconstructed: a 0.105-source-unit emissive core and red
outer shell, with crossed additive halos using the original April soft-tip
texture. The red-base particle specification supplies evidence of the original
red/white hue presentation. This is not execution of SWTOR's particle engine;
blade length, halo layout and warning ring are authored rendering choices.

## Original source provenance

Evidence is **Client-derived** from the operator-labelled April 2012 corpus;
this does not authenticate a retail patch manifest. Numeric namespaces remain
separate. `manifests/b6-research.json` retains definition IDs, decoded fields and
source bucket SHA-256s; `manifests/b6-source.json` records unique exact path/hash
lookups, archive offsets and decoded SHA-256s. Original archives and existing
extracted source bytes are never overwritten or modified.

| Selection | Original definition / appearance | Model and rig |
|---|---|---|
| Broodling | `npc.location.korriban.mob.klorslug.klorslug_broodling` (16140931363754601544), `npp.creature.klorslug.larva` | creature model 1006619, material 1006620; `klorslug_klorslug_a02.gr2`, `klorslug_skeleton.gr2`; source scale 0.6 |
| Shyrack | `npc.location.korriban.mob.shyrack.shyrack_forager` (16140955883146077542), `npp.creature.dragonbat.shyrack` | creature model 1003627, material 1003628; `dragonbat_shyrack_a01.gr2`, `dragonbat_skeleton.gr2`; source scale 1.15 |
| Crypt Looter | `npc.location.korriban.class.sith_warrior.slaying_the_beast.crypt_looter` (16140937307513069069), `npp.location.korriban.mob.humanoid.tomb_robber` | bmn modular model/material pairs: boot 1000540/1001143, chest 1000543/1001305, hand 1000374/1150780, leg 1000544/1001727; `bmnnew_skeleton.gr2`; head/hair among the appearance's source variants |
| Broodmatron | `npc.location.korriban.class.sith_warrior.arm_yourself.klorslug_broodmatron` (16140995455988527881), `npp.creature.klorslug.soldier` | creature model 1000128, material 1001446; `klorslug_klorslug_a01.gr2`, `klorslug_skeleton.gr2`; source scale 1.1 |
| Looter weapon | NPC melee item `itm.npc.electroblade.low03.a01_v01` (16141166261790294400) | `/resources/art/dynamic/weapon/model/electroblade_low03_a01_v01.gr2`; material dependencies from original GR2 table |
| Player hilt | Original Sith low03 art selection; no specific named item asserted | `/resources/art/dynamic/weapon/model/saber_sithlow03_a01_v01.gr2`; original GR2 material table |
| Blade halo | `lightsaber_red_base.prt` → `lightsaber_red_base_p.prt` → texture | `/resources/art/fx/texture/weapon_melee/dynamic_saber_tip_core_white.dds` |

Creature models live under `/resources/art/dynamic/creature/model/`; skeletons
under `/resources/art/dynamic/spec/`. K’lor’slug clips are actually stored under
`/resources/anim/creature/slug/`, not under its rig name; dragonbat has its own
animation folder. Both use `cb_ready_1`, `mv_run_forward`, `cb_attack_1`,
`dg_damage_1`, `dg_death_1`. All selected JBA bones matched their target rigs.
Looter uses original bmnnew `cb_knight_saber_idle_1`,
`mv_saber_run_forward_01`, `cb_saber_attack_right_1`, original hit and death.
The complete exact paths and conversion measurements are in `b6-recipe.json`
and `manifests/b6-conversion.json`.

## Verification and limits

`Run-B6-Smoke.ps1` builds C#, imports only local changed assets and runs one brief
rendered session. Its receipt is `manifests/b6-smoke.json`, runtime log
`logs/b6-runtime.log`. It exercises every enemy's safe spawn, movement, damage,
flytext and death, the animated weapon sockets, saber initialization, boss
telegraph/strike/recovery/death, scheduled boss spawn, player death and restart.
One gameplay image is retained in `generated/b6/gameplay-review.png`.
Historical regression suites, whole-archive hashes and long stress tests are
deliberately excluded.

Portable PBR materials approximate SWTOR Creature/Garment shading. Looter uses
the recovered modular outfit with the established head/hair visual; NPC palette
randomization, complexion variants and exact native garment hue evaluation are
not reproduced. Original attachment meshes and their skin weights are retained.
Flying shyracks use ground-based navigation with their original flight animation;
there is no free aerial pathfinding. JBA world trajectories are not applied;
movement is owned by gameplay, so attack/death root travel is approximate.
Balance, dense crowd overlap and equipment appearance need manual gameplay review.

Stop here for B6 manual review; no new terrain, classes or unrelated systems.
