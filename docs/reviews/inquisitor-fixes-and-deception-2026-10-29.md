# Inquisitor presentation fixes and Deception identity — 2026-10-29

Implemented the five findings from `inquisitor-focused-review-2026-10-29.md`, plus the requested Deception and Sorcerer movement changes. Existing unrelated working-tree changes were preserved.

## Deception: Discharge, control, melee burst

Discharge remains the main Deception DoT. Any saved Deception investment grants slotless Electrocute automatically, including after a run reset. Electrocute is not added to the Sorcerer roster. Its automatic targeting requires a living enemy carrying the player's own Discharge and avoids targets already exposed by Electrocute.

- Deception Discharge: periodic base damage 6 instead of 3, base cooldown recovery from 9 to 5.4 seconds, base periodic duration from 4 to 6 seconds. Existing clean-target admission remains: a ready Discharge does not repeatedly overwrite every already-afflicted target.
- Surging Charge: increases Discharge's direct and periodic damage by 60% and adds another two seconds of DoT duration. It does not turn Discharge into a direct-damage-only ability.
- Electrocute: 16 energy damage, 0.6 range, 12-second cooldown, 0.3-second contact, no ordinary slot or shared cooldown. Ordinary targets are stunned for three seconds. Existing elite/boss resistance remains authoritative. Every surviving victim is exposed for six seconds, granting 35% additional Maul/Voltaic Slash damage. The first successful contact also opens a four-second Maul ambush window. It does not consume, detonate or replace Discharge.
- Conductive Field (existing Saber Conduit talent ID): retains Lacerate's 35% radius bonus and lets Electrocute hit three enemies total, at full damage, in a 0.28 splash radius.
- Darkswell retains its Spike benefits and makes Electrocute recover four seconds of Maul cooldown once per activation.
- Crackling Blades (existing execution talent ID) retains Maul cleave and adds another 20% Maul/Voltaic Slash damage against exposed victims.
- Early Deception ability offers now favor Discharge, Voltaic Slash and Maul. Discharge and Electrocute have automatic admission priority before the melee follow-up. Existing Shock/Voltaic charge mechanics remain.
- Added a scoped repeatable Electrocute power package. The Assassin quickbar fits more than twelve learned abilities in the same available width so the additional control is not silently hidden.

This takes inspiration from Concealment's control-to-burst rhythm. The DoT and electric melee remain Deception's own mechanics. Numeric balance and exposure are authored Survivors tuning, not claims of original-server balance.

## Sorcerer stationary starts

Only Sorcerer Death Field and Force Storm require standing still on the ground to begin. Admission checks actual velocity, requested input and autorun; pressing movement into a wall does not qualify as standing. Rejected starts spend no cooldown. Eligible stationary area spells take priority over ordinary ready attacks without preempting an already-owned gesture or bypassing shared cooldown rules.

Movement after initiation is allowed, including during the remaining windup. Active fields/storms retain their previous persistence rules. Assassin Death Field has no stationary-start requirement. Tooltips state the new rule.

## Presentation repairs

- Thundering Blast launches after its two-second cast, tracks the target for a 0.25-second outbound phase, then applies authoritative damage and impact. Interruption/target loss cleans the flight and cast loop. Its recovered release gesture begins at launch.
- Sorcerer adapters implement a shared per-victim contact interface. Lightning Strike cleave and Thundering Dominion victims now receive contact effects after positive damage, including lethal hits. Shared release/audio remains separate from per-victim visuals.
- Force Speed, Polarity Shift and Thundering Blast's cast bed use actual looping streams. Buff effects/audio follow gameplay expiry/reset/death, with the existing normal-completion tails retained.
- Force Cloak starts the recovered 36978213 loop in a dedicated voice, follows the actor and stops on reveal, expiry, abort or death.
- Lacerate's Razor Wake bonus damage dispatches its contact through the actual D3 adapter after positive applied damage. The focused test verifies three base contacts plus two fresh-spread contacts.
- Assassin's presentation test profile now supplies a valid Deception allocation for Chain Shock. Discharge expiry assertions use the effective class/talent duration.

## April asset recovery

`pipeline/scripts/prepare_electrocute.py` reuses the exact-hash TOR reader. It extracts the three Electrocution EPP resources, the two Electrocute FXSpecs and selected dependency closure; it converts the original icon/textures and recovers the associated Imperial melee/general-bank Wwise media. Original archives remain read-only, checked against the existing baseline metadata.

The recovered instant EPP requests `fp_1h_elect_instant_right_1`, already available as B5/Shock. The adapter uses that clip rather than guessing the visually similar D3 electrical clip. Cast audio resolves through the separate general-abilities bank. All selected Play events have resolved playable media; 35 media records retain exact source-bank offsets and decoded hashes.

Provenance: `pipeline/manifests/electrocute-source.json`, `electrocute-conversion.json`, `electrocute-audio.json`, and `electrocute-general-audio.json`. Parsed EPP/FXSpec/particle evidence is retained in `pipeline/godot/Assets/Electrocute/`. These are bounded Godot adapters; they do not recreate every native particle/action/Wwise graph condition.

## Validation

- New revision suite: 25 Sorcerer checks and 24 Assassin checks pass, including rendered runs. Covers grounded/moving start admission, stationary priority, moving persistence, Assassin exemption, projectile timing/abort, per-victim Strike contacts, looping audio, owned-Discharge targeting, three-victim Electrocute, exposure/DoT preservation, boss stun immunity, Razor Wake contacts and a full quickbar.
- Assassin presentation: 434 checks pass.
- Sorcerer refinement: 154 checks pass.
- Sorcerer/Assassin class smoke: 30 / 11 checks pass.
- Assassin current run-power suite: 898 checks pass.
- Sorcerer relic suite: 54 checks pass, including overlapping storms and Thundering Dominion. Its fixture now settles the player on the ground before freezing movement.
- Assassin relic suite: 142 checks pass.
- Inspected native captures `pipeline/manifests/inquisitor-electrocute.png` and `inquisitor-blast-flight.png`. Playback/loop lifecycle is verified by runtime assertions; no listening-quality claim is made.
- The older Assassin refinement suite stops at its pre-existing expectation of one bounded global damage track, which the current run-power system replaced. It was not treated as a passing suite. The current 898-check run-power suite covers that progression system.
- Build succeeds with existing nullable/obsolete warnings and unavailable online NuGet vulnerability metadata (NU1900).

Run the new checks with `pipeline/Run-Inquisitor-Revision-Smoke.ps1 -SkipImport`; add `-Capture` for hidden native rendering. Omit `-SkipImport` after first recovering assets.
