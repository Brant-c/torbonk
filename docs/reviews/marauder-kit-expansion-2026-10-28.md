# Marauder kit expansion and presentation alignment

## Playable additions

The Marauder profile now exposes Ataru Form, Bloodthirst, Undying Rage, Saber Ward, Crippling Slash, Force Choke, Retaliation, Vicious Throw, and the previously disabled Force Charge. Each has a learnable upgrade and recovered icon. New damaging abilities have mastery upgrades. Ataru stance and the conditional counter/execute do not consume ordinary ability slots.

- Ataru weapon hits have a deterministic seeded 20% follow-up chance, with a 1.5-second shared internal cooldown. Massacre guarantees a follow-up when that cooldown is ready, then raises the chance to 50% for six seconds. Follow-up damage is 30% of the triggering attack's configured damage; it has a distinct combat-meter identity and contact effect without acquiring a casting gesture. Calculated Ferocity additionally makes the next Force Scream critical after an Ataru strike; the bonus is snapshotted and consumed on activation.
- Bloodthirst adds 15% direct damage for ten seconds. Undying Rage activates below 30% health, spends half current health without consuming shields or triggering incoming-hit reactions, and grants 99% damage reduction for five seconds. Saber Ward grants six seconds of 50% incoming damage reduction below 80% health.
- Crippling Slash applies a six-second 50% slow. Force Choke uses the existing three-hit channel/control implementation, including boss resistance. Retaliation requires recent incoming damage and consumes its opportunity. Vicious Throw selects a target at or below 30% health and uses the thrown-weapon lifecycle. Force Charge requires a target outside melee reach and uses the existing collision/terrain-checked dash. Damage waits 0.45 seconds and requires melee contact; a blocked dash cannot deal damage from its original acquisition range.

These numbers are Survivors tuning, not a claim of original server balance. Existing Gore's damage window, the adapted talent tree, and the game's automatic combat model remain in place. Full original talent-tree parity and noncombat/shared utility coverage (including Unleash, which needs a player control-state system) remain separate work.

## Presentation

Berserk uses the recovered cast flare plus smoke/glow on shoulders, chest, neck and elbows. Its cast, force-bomb and sustained loop audio have separate playback paths. The loop follows the character and stops on gameplay expiry or release. Predation has a cast shockwave, attached electrical aura, and world-space movement tails emitted from the source's elbow, shoulder, hip and knee sockets. Its cast audio plays; its speed/static loop events remain unresolved in the saved audio graph and are not fabricated.

Gore, Deadly Saber and Battering Assault now use distinct sustained weapon artwork. Non-damage Warrior utilities can play their configured activation sound. Retaliation uses the recovered dual-saber clip listed in its appearance parameters. Crippling Slash retains an explicit dual-saber substitute because its recovered clip assignment is a different weapon-style motion. Ataru's unavailable native `cb_warrior_assault` is represented by a separate impact rather than replacing the player's current attack animation.

Haste fits ordinary Marauder attack gestures to their effective duration. Ravage keeps its existing tick-based playback and maintained channel gesture; Smash retains its deliberate airborne full-body exception. Ordinary attacks retain running/jumping legs and target-facing upper-body motion. Buffs remain low-priority, optional gestures and can activate during attacks without replacing them.

This is a bounded adapter using recovered appearance/FX/audio ingredients. Full native FXSpec scheduling, particle simulation, projections and Wwise graphs are not implemented. Rendered source-derived effects should not be described as pixel-identical original effects.

## Validation

The expanded Marauder gameplay suite covers activation conditions, execute and reaction consumption, Ataru/Massacre, Force Scream proc consumption, utility audio, Berserk loop lifecycle, movement tails, buff activation during Ravage, protection expiry and reset. The Warrior presentation suite captures every learnable Marauder ability and checks contact dispatch, cleanup, timing and overlapping buffs. The Warrior movement suite checks all new gestures in five movement directions along with baseline attacks, target facing, stationary poses and jumping. Shared Juggernaut presentation/movement regressions are retained.

The Marauder gameplay suite passes 372 checks. Movement suites pass 918 Marauder and 466 Juggernaut checks. The presentation suites pass 242 Marauder and 117 Juggernaut checks; captures are inspected separately from these behavioral assertions. Existing environment warnings include unavailable online NuGet vulnerability metadata, nullable/obsolete compiler warnings, and stale imported scene UIDs that resolve through their text paths.

Results are recorded in `pipeline/manifests/marauder-smoke.json`, `warrior-presentation-marauder.json`, `warrior-presentation-juggernaut.json`, `warrior-movement-marauder.json` and `warrior-movement-juggernaut.json`. Recovered source evidence is in `Assets/WarriorLibrary/{catalog,appearance-parameters,fx,audio}.json` and `pipeline/manifests/marauder-presentation-evidence.json`.
