# Marauder implementation and payoff pass

Marauder uses 17 recovered Warrior abilities and its own resource/animation variants. Juggernaut and other classes keep their existing tuning, equipment and animations. Vicious Slash is the sole free, replaceable starting attack; no separate Assault remains.

## Current combat

Untrained damage is now Vicious Slash 23, Rupture 18 plus six 7-damage bleed ticks, Sweeping Slash 24, Ravage three 22-damage hits, Annihilate 52, Deadly Saber six 6-damage ticks, Deadly Throw 38, Dual Saber Throw two 22-damage hits, Force Crush 10 plus six 8-damage ticks, Smash 58 and Force Charge 26. These are Survivors balance values, not native 2012 numbers. No generic +20% damage upgrades are offered to Marauder.

Rupture and Force Crush prefer fresh enemies and refuse to overwrite effects with more than 0.65 seconds remaining. Spreading has priority over hand-applying another DoT. Sweeping Slash innately spreads Rupture within its frontal cone; Blood Wake adds Deadly Saber and Force Crush plus a 12-damage impact on each newly afflicted enemy. Existing effects are not refreshed by repeated sweeps.

A successful spread applies 35% stronger periodic damage to newly afflicted targets, heals two health per new target (maximum eight), and grants three seconds of +35% weapon damage. The reward has a one-second shared trigger limit. Hungering Wounds adds an eight-health, three-second shield. This rewards positioning and seeding without adding baseline armor or passive mitigation.

Smash uses the orange Sith action-bar icon, has an 18-second base cooldown, and cannot fall below ten seconds with recovery bonuses. Fully Crush-empowered Smash can accelerate Predation with Decimate. Force Flow accelerates Dual Saber Throw instead of Smash.

## Presentation

Marauder has an instance-local animation library containing complete recovered dual-saber attack sequences with compressed playback timing. Gesture lifetime survives the initial damage event. Both actual equipped blades produce recovered red saber-texture trails. Bleed and Crush visuals follow afflicted enemies.

Predation, Berserk and Frenzy have sustained, bounded chest/wrist effects that follow the character and expire with their actual gameplay timers. Recovered dependencies include sw_predation_speed_loop.fxspec, sw_fury_loop.fxspec and their original electricity/fire/smoke texture atlases. Frenzy is a slotless conditional burst preparation utility: while learned Berserk is recovering, it readies Berserk and Deadly Saber on a 32-second base cooldown.

Dual Saber Throw sends render copies of both actual equipped hilt/blade meshes outward and back, restoring the held weapons on return or cleanup. Its frontal path hits up to six enemies twice; Forked Blades increases the cap to eight and removes secondary damage falloff. Deadly Throw has a single-saber flight. Flight is presentation; bounded ability ticks own damage. Two-saber flight is an adaptation of the recovered Saber Throw resources, not a claim of a distinct April 2012 Dual Saber Throw definition.

## Talents and progression

Existing pregame interface, 12-point budget, hybrid allocation, point gates and saved profiles remain. Three trees contain eight talents each. Every node now has a relevant recovered Warrior talent icon and exact source identity recorded in marauder.json. The blue shared Smash icon is replaced with the orange Sith artwork throughout Marauder.

Annihilation ties spread, stronger wounds, capped healing, charge application, extensions and Annihilate recovery together. Carnage ties attack acceleration, multi-target Ravage, periodic precision hits, Blood Frenzy follow-up strikes, Ataru Flurry, offensive windows and Berserk/Frenzy recovery together. Rage ties Crush charges, empowered saber follow-ups, controlled Smash bursts, movement recovery, throw cleave and execution pursuit together.

Six finite run choices replace generic damage selection: Blood Wake, Ataru Flurry, Saber Siphon, Sweeping Ravage, Forked Blades and Hemorrhage. They share their mechanic caps with pregame talents to prevent duplicate dead selections. Existing generic health, movement and cooldown choices remain. Early offensive offers, Banish/Reroll/Skip and ability-slot limits remain. The starter does not gain Marauder offensive talent procs or consume Deadly Saber charges.

## Evidence and limits

Source indexes: WARRIOR-ASSET-LIBRARY.md, manifests/warrior-library.csv and Assets/WarriorLibrary/catalog.json. manifests/marauder-presentation-evidence.json preserves selected source IDs, resource hashes, direct animation references, appearances and converted textures. manifests/marauder-assets.json records gameplay clip choices. Talent source identities distinguish recovered artwork from adapted mechanics.

Original native semantic animation graph selection, FXSpec/GR2 particle rendering, exact Wwise event routing and original server balance remain incomplete. The current implementation restores recovered artwork, source clips and equipment through the existing renderer; gameplay mechanics and throw flight are explicit Survivors adaptations. Exact native visual fidelity and large-population balance require interactive playtesting.

## Validation

Run Run-Marauder-Smoke.ps1. The payoff pass passes 107 checks covering base damage without generic upgrades, 24 distinct talent mappings, early offers, DoT priorities, stronger spread ticks and healing/weapon payoff, sustained character effects, Frenzy recovery, two throw hits, both weapon flights and return cleanup (including overlapping throws), Smash cooldown floor, original dual-wrist motion and blade trails, hybrid budgets and reset. Juggernaut regression passes 153 checks. Build succeeds with existing nullable/obsolete warnings and unavailable online NuGet vulnerability metadata.

