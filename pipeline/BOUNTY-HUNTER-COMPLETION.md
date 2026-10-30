# Bounty Hunter completion — October 30, 2026

Powertech and Mercenary now match the other Imperial advanced classes: six 3x3 talent trees (12-point budget,
`Gameplay/Talents/bounty_hunter.json`), runtime hooks (`Talents/PowertechRuntime.cs`, `Talents/MercenaryRuntime.cs`),
full kits and run cards (`Talents/PowertechProgression.cs`, `Talents/MercenaryProgression.cs`). Behaviour is a Survivors
adaptation; April data supplied names, icons, FX, clips and audio only.

## Kits

- Powertech: Flame Sweep starter; Incendiary Missile, Flame Thrower, Flame Burst, Rocket Punch, Immolate, Retractable Blade,
  Rail Shot, Explosive Dart, Electro Dart; utilities Energy Shield, Explosive Fuel, Carbonize (now every enemy within 0.42), Oil Slick, Jet Charge.
  Flame Sweep spreads Incendiary Missile and Immolate burns for every Powertech. Any fire hit on a soaked enemy ignites its Oil Slick.
- Mercenary: Tracer Missile starter (Heat Signature stacks, every 4th cast readies Rail Shot); Heatseeker, Unload, Rail Shot, Death From Above,
  Sweeping Blasters, Explosive Dart, Incendiary Missile, Fusion Missile, Power Shot; utilities Thermal Sensor Override, Kolto Missile,
  Kolto Shell, Concussion Missile, Jet Boost, Energy Shield. Explosive Dart and Fusion Missile spread burns/bleeds.
- Healing: Kolto Missile heals 1 per enemy, cap 4; Kolto Shell mends 1 HP at most every 2 s, 4 times per cast (+2 with its talent).
  Movement: Jet Speed talent and Jet Thrusters card, 3% each, total capped at 9%.

## Run cards

Uncapped rarity-rolled power cards (+60–90% base damage per rank) for every damaging kit ability; reach/spread/cleave cards
(radius 10–18% per rank, Fanning Embers +2 spread targets, Shockwave Knuckles cleave); one-time picks for every talent hook plus
card-only mechanics, gated on owned abilities and weighted by tree investment; Reinforced Plating (health, 3 ranks). Generic
damage/range/cooldown/health/speed cards are removed from both pools. The old Mercenary Tracer Lock card is retired; Chain Reaction
now triggers on Heat Signature.

## April provenance

- Icons: exact `ablIconSpec` of each named April node (`work/bh_icons_april.json`, receipts in `manifests/bounty-hunter-icons.json`).
- FX: FXSpec traversal from the epp records (`manifests/bounty-hunter-appearances.json`) into `Assets/BountyHunter/*.png`,
  `source-parameters.json`, `fx.json`.
- Animation: 14 bmnnew JBA stems named by the epp records, converted to `Assets/BountyHunter/casts.glb` (library `BH`).
  Rail Shot, darts, Kolto/Concussion Missile and Thermal Sensor Override reuse the already-recovered RangeBracer/Dart/FocusBuff clips.
- Audio: 61 Imperial-ranged and 2 Republic-ranged (Trooper kolto bomb, sticky grenade) Wwise events, `manifests/bountyhunter-audio.json`.
  All WAVs have real Godot imports. `Play_ability_bountyhunter_deathfromabove_activate` has no media in the April bank.

Not in April 2012 (2.0 content): Serrated Shot, Mag Shot, Electro Net and the Innovative Ordnance tree name. Substitutes:
Combustible Gas Cylinder burn, Rail Shot/Superheated Rail, Electro Dart; the tree keeps the April firebug (Pyrotech) nodes.

Regenerate with `scripts/generate_bounty_hunter_talents.py`, `scripts/prepare_bounty_hunter.py` (`--audio`, `--republic-audio`),
then Blender `scripts/convert_powertech_animations.py -- --bountyhunter`, then a Godot `--import`.

## Playtest pass — October 30, 2026

- Talent screen: Bounty Hunter trees use the shared 3x3 layout (the data was already 9 per tree).
- Mercenary starts with Power Shot + Rapid Shots (`rapidshots` icon); Arsenal's Tracer Missile talent converts the starter. Arsenal: Heatseeker, Tracer,
  Missile Blast (new, April `missileBlast`), Unload, Death From Above. Innovative Ordnance: Incendiary Missile, Fusion Missile, Explosive Dart,
  Sweeping Blasters, Rail Shot, Power Shot.
- Explosive Dart and Thermal Detonator stick on their target, then detonate (2 s / 1 s fuse, or on death). Death From Above: Imperial area
  projector, stand-still start, immune/rooted/lifted channel, authored length.
- Burns and Heat Signature render as triplanar coatings on the NPC's own meshes (`BountyStatusOverlay`); Bounty Hunter burns transfer on death.
- Advanced Prototype: Retractable Blade talent converts the Flame Sweep starter (Explosive Dart is offered early); Prototype Cylinders makes the dart bleed-only (+40% damage, +30% radius);
  Prototype Weapon Systems tunes Thermal Detonator.
- Thermal Detonator has no April player ability (2.0 content). It uses the April Bounty Hunter boss Sorno's ability icon (`thermaldetonator`)
  and `epp.npc.ability.tech.thermal_detonator`: `cb_unarmed_grenade_1` (D3/UnarmedGrenade), smuggler detonator FX and Republic-bank audio.
