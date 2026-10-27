# Basic attacks and shared Affliction

Sorcerer starts with Force Lightning alone and has no automatic saber attack. The other playable Imperial classes start with their existing class ability **and** an automatic basic weapon attack. Basic attacks use no offensive/utility slot and no shared global cooldown. Ready class abilities are checked first; weapon attacks only fill gaps while no class ability is casting. Their low-priority gesture can be preempted, cancelling an unfinished weapon hit while preserving its recovery timer.

| Class | Basic attack | Existing starter |
|---|---|---|
| Sorcerer | None | Force Lightning |
| Assassin | Saber Strike, double-bladed saber | Lacerate |
| Juggernaut | Assault, single saber | Smash |
| Marauder | Assault, two sabers | Rupture |
| Operative | Rifle Shot | Corrosive Dart |
| Sniper | Rifle Shot | Sniper Shot |
| Powertech | Rapid Shots, one pistol | Flame Sweep |
| Mercenary | Rapid Shots, two pistols | Tracer Missile |

Saber attacks require melee reach (0.34); Agents use rifle reach (1.5), Hunters pistol reach (1.1). A Sorcerer will not swing a saber at a distant target. No chase or movement behavior was added. Basic attacks deal 10 damage with a 1.5-second recovery and a short 0.12-second windup; these are TorBonk tuning, not restored MMO damage/resource rules. Shared applicable damage bonuses scale them. Both initial attacks return on run reset, and both appear on the action bar.

Basic hits deliberately do not trigger ability-hit talent mechanics such as Static Blades, Rage stacks or Target Lock. Otherwise free filler would change those talents substantially (in particular, permanently refreshing Affliction through Static Blades). Ordinary damage bonuses still apply. This boundary can be revisited deliberately as part of later class balance.

Assassin gains the existing **Affliction** unlock alongside Creeping Terror, Discharge, Death Field and Force Shroud. Sorcerer retains Affliction and its Thundering Blast interaction. The shared Affliction uses the same caster-owned periodic damage, refresh, expiry and original icon. The existing recovered electrical gesture is loaded for Assassin. Learning it still consumes an offensive slot; the basic attack does not. Assassin now has more offensive unlock options than available slots, so a run must choose its combination.

Six original April weapon clips were recovered through the existing bounded reader and converted into `Assets/BasicAttacks/casts.glb`: single saber, dual saber, pole saber, rifle, pistol and dual pistol. `manifests/basic-attacks-source.json` records exact archive/hash/offset provenance; the conversion and animation manifests record the selected clips, original timing and GLB hash. Presentation reuses recovered weapon audio and blaster effects. The mapping and combat timing are authored adaptations; native SWTOR action-graph and multi-hit/resource parity are not claimed.

Run `Run-Basic-Attacks-Smoke.ps1 -SkipImport` after importing the new library. It checks all eight classes separately: correct two-attack starts, original animations, free slot accounting, starter priority, filler during a running global cooldown, weapon damage, preemption, range, reset, and unlock-pool exclusion. Assassin additionally verifies Affliction application/ticks and exclusion of basic hits from Static Blades refreshes. `Run-Sorcerer-Smoke.ps1 -SkipImport` retains the Thundering Blast and Death Field regressions with two starting attacks.

Validation: all eight class fixtures passed (128 checks), Sorcerer/Assassin ability regression passed (38), Assassin kit regression passed (39), and Powertech regression passed (36). A rendered Mercenary fixture passed its 15 checks without runtime/shader errors; the captured dual-pistol pose was visually inspected. Source clips are present for every weapon family. Full encounter pacing/balance remains a manual gameplay review.
