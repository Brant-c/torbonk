# Warrior movement and baseline attacks

Attack facing still turns the character toward its target. Ordinary abilities now blend their upper-body motion over running/jumping legs instead of forcing their standing foot poses while the player moves. Stopping restores the original full-body standing gesture. Smash retains its deliberate airborne body motion, then returns the legs to running after landing, before its remaining recovery animation ends.

Directional locomotion uses recovered single/dual-saber forward and backward cycles, with derived lateral cycles that rotate only the pelvis/legs. The recovered `mv_run_left/right` candidates contain travel and turning, so they are explicitly excluded from runtime looping locomotion. The derived strafe leaves Root/RootSpine and attack facing untouched. The directional blend supports forward, backward, both sides, and diagonals.

Run cycles have matching lengths for blending. Playback rate follows actual world velocity, using ground-stride speeds measured from the source foot tracks at the character's scale. Measuring tracks directly avoids advancing an active cast or depending on deferred animation-tree caches. Source GLB/JBA files are unchanged by runtime blending and adaptation.

Marauder starts with Assault in quickbar slot 1, using no offensive slot and no shared cooldown. Existing Vicious Slash remains the free, replaceable class starter behind it, with a 4.5-second base cooldown. Class abilities get first refusal; Assault fills remaining gaps, including when every available target already has the learned DoTs. A fresh target restores DoT priority. Assault does not receive Marauder weapon talent procs or spend Deadly Saber charges.

Juggernaut starts with Vicious Slash in slot 1 instead of Assault. It is a free, non-replaceable basic fallback with a 2-second base cooldown and no shared cooldown. Juggernaut continues to start with its existing free, replaceable Smash. Baseline attacks remain outside ability unlock offers and ordinary replacement candidates. Quickbar ordering keeps the basic attack first after learning abilities, replacement, or restart.

The existing controller already attempts every eligible class ability before its basic attack and permits a newly available class ability to preempt a basic gesture. The implementation reuses that behavior; display order does not determine combat priority.

## Validation

`pipeline/Run-Warrior-Movement-Smoke.ps1` checks both classes with isolated saved profiles. Add `-Capture` for rendered strafe frames. Source extraction/conversion scripts are `prepare_warrior_locomotion.py` and `convert_warrior_locomotion.py`; exact archive/resource hashes are retained in `pipeline/manifests/warrior-locomotion-source.json`.

Movement tests sample every non-Smash ability in five directions, checking both feet keep cycling, standing lower-body weight is released, facing stays on the target, the visible rig stays centered on its body, and locomotion does not introduce a root turn. They also cover cadence changes, standing poses, jump priority, Smash landing recovery, starting loadouts, quickbar order, slot budgets, basic fallback admission, covered DoTs, fresh-target priority, and restart.

The Marauder movement/fallback suite passes 648 checks and Juggernaut passes 466. Rendered strafe frames for both classes were inspected after excluding the translating/turning lateral source clips. Both class regression suites, both basic-attack suites, and the existing Warrior presentation suites pass separately. No claim of exact native lateral animation graph selection is made: the in-place lateral leg motion is an explicit adaptation of the recovered weapon run cycle.
