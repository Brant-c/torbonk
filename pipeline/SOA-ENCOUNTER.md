# Soa — Infernal Prison run

Choose Operations → Eternity Vault → Soa, select Normal/Hard/Nightmare and Set Course. This starts a finite level 1–50 run in the restored final circular room. The existing class kits, level-up choices, equipment, artifacts, XP pickups, reward presentation and restart path are reused.

| Level | Content |
| --- | --- |
| 1–9 | Primeval Manka Patriarch and Rakata Eternal Guardian waves, adding pressure gradually |
| 10 | Ancient Pylon Guardian: the Proto Acklay resolved from selector D of `enc_boss_puzzle_guardian_2` |
| 11–24 | Rakata Eternal Watchers and Proto Acklay join the jungle roster |
| 25 | The Infernal Council: sixteen named members, at most three active concurrently |
| 26–39 | Mixed jungle/prison pressure |
| 40 | Soa's Image: translucent blue projection with locked lightning and ground warnings |
| 41–49 | Final mixed waves |
| 50 | Soa, the Infernal One: larger original model, alternating warnings and a temporary breakable shield; defeating him completes the run |

Council roster: Luthro the Kinslayer; Kahesh, the Reborn; Jekhop, the Bowed Lord; Alarii, Sister of Sorrow; Fahren Jaj, the Laughing Lord; Yshaar Kael, Betrayer of Adas; Za'heen Kaur, the Bowed Lord; Jael, the Silencer; Serrod the Messenger; Kyyrah, Jewel of Xo; R'hoteb, the Bowed Lord; Doruk, the Uncaged; Wei'val the Lesser; Wei'val the Elder; Cyrisop, the Exiled; Keryha, Sister of Sorrow.

## Source identities and adaptation

The requested A/F jungle selectors resolve to `jungle_cat` / `jungle_rakata_warrior`. D in the requested puzzle-guardian encounter resolves to `jungle_acklay`, not its A-slot guardian. The authored early boss therefore uses the source Proto Acklay appearance.

`manifests/soa-npc-research.json` retains the first NPC/appearance evidence. `soa-npc-expanded-research.json` adds the eight `difficulty_3.boss.soa.temple_boss_*` prototypes. Jekhop is exact NPC `0xE00026F40146C07C`, linked to string ID `2512190795939840`; the original spelling is Jekhop. His prototype reuses the Yshaar appearance, as the source specifies. The other extended Council members likewise reuse the source-linked appearances rather than requiring sixteen unique meshes.

Thirteen rigged actor models are converted from April GR2/JBA dependencies: Manka, Rakata Guardian, Rakata Watcher, Acklay, Soa and eight Council appearances. Modular parts, selected attachments, eye overrides, cloth bind bones and all five gameplay animation labels are retained. Garment color is baked through the maintained SWTOR shader with original primary/secondary palette XMLs. Skin/hair customization, native cloth simulation and exact native shader lighting remain approximations. Weapons use the existing game weapon/socket presentation; Council sabers have red blades. Manka uses the existing original Manka vocal clips with the shared NPC vocal mix reduced by 6 dB.

`soa-encounter-source.json` records successfully extracted original dependencies. `soa-enemies-conversion.json` checks weighted bone compatibility and animation bone matches. `soa-roster-review.png` provides the rendered art inspection.

Wave mixtures, level thresholds, stats, loadouts, attack patterns, image tint/transparency, shield timings and the three-at-a-time Council structure are authored Survivors mechanics. This is not the original MMO encounter or its per-player Council assignment puzzle. No earlier-floor destruction/platform transitions are added.

Rakata Watcher ground attacks use the quieter, non-priority `ev_guard_warning` / `ev_guard_impact` mix: -9/-18 dB cue gain, 1.5/1-second shared cooldowns and 0.65-second clips. This avoids the loud +9 dB boss warning previously reused by every Watcher and limits overlapping attack sounds. Visual warnings and attack timing remain unchanged.

## Lifecycle and difficulty

The Council uses the shared boss controller with an optional member-scene list. Regular mob pressure continues alongside Council members. It holds the same milestone until every member is defeated, follows the current living member in the boss HUD, withholds its guaranteed boss reward until the last member, and clears its subscriptions/pending members on restart. Existing single-boss definitions keep an empty member list.

Soa locks warnings before damage. His three-pattern rotation uses the existing projected-ground telegraphs; the final form periodically gains an 8%-maximum-health shield lasting six seconds if not broken. Normal/Hard/Nightmare use the existing operation health, damage, spawn pressure, warning duration, radius and follow-up rules. All eight implemented class selections remain available.

## Rebuild and verification

- `Run-Soa-Encounter-Pipeline.ps1`: extract selected actor dependencies, convert rigs/animations/palettes, compose run/scenes and import/build. It preserves the map restoration pipeline.
- `Run-Soa-Encounter-Smoke.ps1`: default checks every implemented class on Hard. Choose `-Difficulty Normal` or `Nightmare`; use `-Classes sorcerer` for a bounded check and `-Rendered` for captures.
- `Run-Soa-P3-Import.ps1`: independently rebuild the room geometry/environment.

The combat check exercises early roster spawning, all five animation labels, level progression to 50, all four milestones, every Council member, the concurrent-member limit, delayed group completion/reward, image/final distinction, warning-before-strike, the final shield, finite victory and restart. These are implementation checks, not a claim of fully playtested balance.

Validation results: Hard progression/lifecycle checks passed for Sorcerer, Assassin, Juggernaut, Marauder, Sniper, Operative, Mercenary and Powertech. Additional Normal and Nightmare checks passed. The existing XRR-3 run passed its regression check; its old fixed-tick smoke assertions were updated to accommodate the already-existing difficulty-dependent multi-strike cannon cycle. Menu selection/launch and the NPC vocal playback check also passed. Source roster and rendered image/final-form captures were inspected.

Continuous encounter pacing: Soa and XRR-3 now preserve existing mobs across every stage/boss transition on Normal, Hard and Nightmare. No timed recovery or reinforcement ramp follows a boss, and regular spawning continues through its introduction. Old-stage mobs die naturally while new spawns use the current roster. Population limits include the boss/Council members; occupied encounter slots are not reserved twice. All six operation/difficulty combinations passed `Run-Continuous-Encounter-Smoke.ps1`. Level-up/reward choices and boss attack windups retain their existing behavior.
