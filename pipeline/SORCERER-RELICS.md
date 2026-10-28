# Sorcerer endgame relics

All eight are permanently owned at +0, including migrated equipment-v1 profiles. Equip any two different relics through the existing equipment interface, independently of specialization. Normal credit upgrades (500 / 1500 / 3000) persist through +3. Only Sorcerers may equip them. Existing SWTOR tooltip cards list the effects for every rank.

| Relic | +0 / +1 / +2 / +3 |
|---|---|
| Eye of the Tempest | 2 / 3 / 4 / 5 independent Force Storms |
| Thundering Dominion | 40 / 60 / 80 / 100% secondary Thundering Blast damage to storm occupants, deduplicated across overlaps |
| Forked Lightning | 2 / 2 / 2 / 3 Chain Lightning casts, repeating the existing animation and chain targeting |
| Lightning Prison | 1.5 / 2 / 2.5 / 3 second movement roots, refreshed rather than added |
| Corrupting Storm | Successive ticks unlock Affliction, then Crushing Darkness at +1, then Creeping Terror at +2; +3 applies all three on each tick |
| Lingering Agony | 50 / 65 / 80 / 100% remaining owned periodic damage, radius 3 / 4 / 5 / 6 metres |
| Grip of Madness | 8 / 10 / 12 / 14% slow per distinct owned DoT, capped at 42% |
| Accelerated Torment | 50 / 65 / 80 / 100% faster ticks for 4 / 4 / 4.5 / 5 seconds; Shock cooldown 20 seconds |

Eye removes the normal storm cooldown while equipped, retaining global cooldown, gesture admission and storm capacity. This is necessary to overlap the baseline four-second storms with their normal eight-second cooldown. Each storm uses the existing channel clock, hit/talent logic and its own original presentation. Unequipping Eye or aborting combat clears detached storms.

Corrupting Storm applies the existing ability definitions even when those DoTs have not been learned; it does not unlock or equip their automatic casts. It applies missing effects without refreshing already active effects on every storm tick. DoTs retain their caster/ability/target identity and normal durations. Shock acceleration preserves duration and adds ticks; reapplication retains the remaining acceleration window. Death detonations snapshot remaining scheduled periodic damage with current damage modifiers and prevent synchronous recursive detonations. Radius uses the project's 0.1 world-unit per metre scale.

Five original DDS icons are reused from the existing April2012 extraction: darksiderelic1–3 and lightsiderelic2–3. Three DDS files were newly staged into Inventory; two were already there. These custom relics share some original icons. Exact source paths and SHA-256 values are in `manifests/sorcerer-relic-icons.json`; this is client-derived asset provenance, not a claim that these custom relic names existed in SWTOR 1.2.

Validation: `Run-Sorcerer-Relic-Smoke.ps1` uses isolated equipment persistence and checks ownership, restrictions, upgrades/reload, the four requested combinations, roots, slow removal, added periodic ticks, overlap deduplication and storm capacities at every rank. The existing Sorcerer/Assassin smoke suite also passes. Validation is headless; no manual visual review or new dedicated explosion artwork was added. No loot tables or other class equipment were expanded.
