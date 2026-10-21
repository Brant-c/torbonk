# D3 — Eight Imperial advanced classes

Launch from `D:/SWTORClassic/torbonk`:

```powershell
.\pipeline\Open-Survivors.ps1
```

Choose **New Run → Sith Empire → base class → advanced class → Korriban → Play**. All eight Imperial classes have playable profiles. Republic remains preview-only. WASD moves, Space jumps, attacks activate automatically, R restarts, and Main Menu returns to selection. Restart keeps the selected class and clears run progression. No additional planets, Republic gameplay or full rotations are included.

| Advanced class | Starter | Weapon | D3 damage / cooldown / source-unit reach |
|---|---|---|---|
| Juggernaut | Smash | Single red saber | 42 radial / 4 s / 0.34 radius |
| Marauder | Rupture | Two red sabers | 14 initial + 7 each second for 6 s / 3 s / 0.32 range |
| Assassin | Lacerate | Double-bladed red saber | 22 radial / 2 s / 0.28 radius |
| Operative | Corrosive Dart | Blaster rifle | 6 initial + 9 each second for 8 s / 4 s / 1.1 range |
| Sniper | Sniper Shot | Sniper rifle | 32 instant / 2 s / 1.5 range |
| Mercenary | Tracer Missile | Two blaster pistols | 38 primary + 40% splash / 3 s / 1.3 range, 0.22 splash radius |
| Powertech | Flame Sweep | Blaster pistol | 17 radial / 1.8 s / 0.31 radius |
| Sorcerer | Force Lightning | Existing red saber | Existing D2 tuning and ability roster retained |

Numbers are Survivors tuning. All seven starters use the existing shared GCD and independent cooldowns. No movement lock or cast bar was added. Damage occurs on the next combat update; visible weapon meshes have no damage authority. Recovered gestures continue for their original 1.07–2.27-second durations through the existing presentation lifecycle. This recovery can set the practical minimum cadence when upgrades shorten a cooldown below the gesture duration. While standing the full source gesture blends in; while moving the existing waist/chest/upper-body masks retain locomotion. Hit/death reuse original compatible BMN reactions, with full-body death priority. Native animation notes, aim-offset graphs and exact impact-frame synchronization are not reproduced.

`Gameplay/classes.json` is the class registry: stable ID, faction/base class, appearance, equipment preset, rig/body, animation library, locomotion, presentation adapter, starter, statistics, ability categories, talent pool and artifact effects. Selection and gameplay validate this registry. `EquipmentComposition` keeps D1 assembly, skin-bind validation, source materials and socket equipment as the foundation; no independent character or combat controller was added. The base GLB supplies the common human head/hair and original BMN skeleton; its Sorcerer armor is hidden and replaced by each profile's own original modular outfit. The same assembly powers pregame previews and Collections.

`PeriodicEffects` owns one effect per caster/ability/target. Reapplication replaces damage snapshot, resets the duration and starts a fresh tick interval; it never stacks. Separate enemies retain separate instances. Effects outlive range and LOS after application, expire naturally, stop on target/caster death, and clear on restart/menu exit. Each applied tick uses Health → combat events → existing flytext. Damage upgrades scale both initial and periodic damage. Splash collects distinct valid targets around impact and applies the primary amount only once; secondary targets receive their own smaller events. No knockback or extra secondary mechanics are added to the new starters.

New-class level choices contain shared health/speed, starting-ability damage/cooldown, range or radial-area improvements, and starter mastery. Sorcerer unlocks/mechanical talents are absent from their pools. The existing four-offensive/two-utility limits, rarity multipliers and single twelve-slot SWTOR bar remain. Compatible shared run artifacts and three fallback power items remain available; lightning/storm/death-field-only effects are excluded. Collections saves only cosmetics. Starter visual defaults and compatibility are class-aware; no new cosmetic unlock progression is introduced.

## Original assets and evidence

Evidence is **Client-derived** from the operator-labelled April archives; Godot load/damage checks are **Behavior-verified in the Survivors adapter**, not claims of native MMO equivalence. Original archives and emulator files were not edited. No whole-archive hashing or bulk extraction was performed.

* `manifests/d3-definitions.json`: exact April IPP and ability identities, bucket paths/offsets and decoded resource hashes.
* `manifests/d3-appearance-evidence.json`, `d3-impact-evidence.json`: expanded original appearance actions, animation semantics, FX roots and event strings.
* `manifests/d3-source.json`: bounded model/material/palette/icon/JBA recovery with exact TOR identity, offsets and hashes. `d3-fx-source.json` contains FX dependencies; ranged and Lacerate source receipts identify the additional HIRC banks.
* `d3-{war,spy,bh}-recipe.json`, `manifests/d3-*-character-{probe,conversion}.json`: registry IDs, original meshes, palettes, skin binds, preserved skeleton/cloth bones, materials and conversion outputs. `d3-weapons-recipe.json` and its conversion receipt identify weapon models/materials.
* `manifests/d3-{fx,ranged,lacerate}-audio.json`: original named Wwise event → typed HIRC graph → exact DIDX/media offset → decoded PCM. Only selected media slices were decoded; media banks were not bulk extracted.
* `manifests/d3-delivery.json`: selected-source receipt index and converted output hashes.

Warrior uses `ipp.class.war.starting.mdps1.t1x1.*` (original SW A01 medium appearance). Agent uses `ipp.class.spy.starting.tdps1.t1x1.*` (SP A01 uniform). Bounty Hunter uses `ipp.class.bh.starting.rdps1.t1x1.*` (GE A16 heavy appearance), including its selected chest attachment **1003353**. Chest/hands/legs/feet/waist use each definition's actual model, material and primary/secondary palette IDs. These are documented original **starting appearance-family alternatives**, not claims that the complete native spawn inventory has been established. Headgear/bracers are not invented. Inquisitor retains the documented D1 level-1 family. All currently use the established human male BMN body and common head/hair.

Weapons use original `/resources/art/dynamic/weapon/model/` resources: `saber_sithlow03_a01_v01.gr2` (existing), `dualsaber_low01_a01_v01.gr2`, `rifle_low01_a01_v01.gr2`, `rifle_sniperlow01_a01_v01.gr2`, and `blaster_low01_a01_v01.gr2`. Exact historical starter weapon item relationships are unverified. These are original class-appropriate low-level alternatives. Original RightWeapon/LeftWeapon bones and the established axis correction drive the sockets; scale remains 1. The double saber has a distinct original hilt, two reconstructed red blades, and a tip offset derived from its original model bounds. Blade glow remains the existing Godot reconstruction using recovered artwork.

Smash maps April `SaberSpecial2Attack1` to compatible original `cb_saber_sp2_attack_right_1.jba`; this semantic-to-filename association is source-derived, not native animation-graph proof. Rupture and Lacerate directly name `cb_2saber_sp2_attack_right_13.jba` and `cb_pole_blade_dance_end.jba`. Flame Sweep directly names `cb_bounty_flame_sweep.jba`. Mercenary uses compatible `cb_2pistol_bracer_fire_big_1.jba`. Operative and Sniper use original rifle fire clips; Dart's April appearance requests a bracer gesture, deliberately adapted to rifle presentation for this milestone. Sniper Shot preserves the requested name and instant moving shot while drawing its icon/audio identity from April **`abl.agent.snipe`**; Corrosive Dart maps to **`abl.agent.toxic_dart`**, icon `toxicdart`. Tracer Missile's original icon is `unexplodedmortarshell`, Lacerate's `spinningstrike`, and Flame Sweep's `flamingsweep`. These source names are retained in the evidence rather than replaced with guessed modern names.

FX roots include original `sw_smash`, `sw_impale_impact_01`, saber trail artwork, `bh_flamesweep_bracer_flames_01`, `bh_fusion_missile_fire`, `bh_tactical_nuke_explosion` and `sp_toxic_dart_muzzle_flash`. The bounded Godot adapter displays recovered textures/atlas frames with one-shot original PCM; it does **not** execute the complete SWTOR FXSpec/particle/projector/lighting system. Rifle tracers/missile paths are reconstructed line presentation, not a recovered native projectile simulation. Flame Sweep is a moving radial fire burst rather than a stationary channel. Wwise random/layer containers, exact weapon fire-note timing and native spatialization are approximated by selected original PCM. Manual review must assess these fidelity limits, muzzle alignment, off-hand/staff orientation, animation transitions, cloth clipping and audio balance.

## Focused validation and manual review

`Run-D3-Smoke.ps1` builds C#, imports changed assets and runs one short rendered Godot process with isolated user data. It starts a Mercenary gameplay composition and checks all eight selection adapters, actual modular armor/main/off-hand loads, original idle/run/attack/hit/death clips, previews, ability damage, independent periodic instances/refresh/expiry, exact primary/splash amounts, per-hit/per-tick flytext, no presentation-caused duplicate damage, upgrade pools, unchanged cosmetic saves, restart and return to menu. Receipt: `manifests/d3-smoke.json`; logs: `logs/d3-*`. No historical A1–A4 suites, screenshot comparisons, benchmarks or long simulations ran. Build retains the existing nullable warning in StormAreaIndicator and unavailable NuGet vulnerability-feed warning.

Manual review:

1. Choose each Imperial advanced class, verify its preview and Korriban launch, and check that the action bar shows the assigned starter.
2. Review idle/run/attack/hit/death, moving attacks, both dual weapons and both staff blades, and source armor palettes/clipping.
3. Check close-range triggers, independent Rupture/Dart ticks, missile splash, flames and original audio at normal camera distance.
4. Gain a level and an encounter reward; verify useful class-compatible choices. Restart and return to the menu.

Stop at D3 for manual acceptance of presentation and gameplay feel.
