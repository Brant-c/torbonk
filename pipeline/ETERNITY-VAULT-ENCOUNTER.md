# Eternity Vault: surface encounter

Choose Eternity Vault in the destination menu. Every implemented class begins a
fresh run at level 1 and stops gaining levels at 50. The existing class kits,
specialization offers, upgrades, artifact rewards, HUD and restart controls are
used. This is a finite Survivors adaptation, not original server encounter code.

| Levels | Pressure |
|---|---|
| 1–3 | Ice cats: `B` in `enc_snow_wilderness` |
| 4–10 | Tauntauns (`A`) join the cats; Whitefang Matriarch at 10 |
| 11–15 | Prison guard patrols: `A` in `enc_trash_prison_guards` |
| 16–25 | Melee guards plus suppression lanes; Guard Captain at 25 |
| 26–40 | Mixed lockdown waves; Siege Warden at 40 |
| 41–49 | Mixed security pressure, preparing for annihilation protocol |
| 50 | Annihilation Droid XRR-3; victory on defeat |

Data: `godot/Gameplay/Planets/EternityVault/run.json`. XP starts at 16 and grows
by 1.075 per level, matching Korriban. Every second kill drops XP, with a raid-only
2× reward multiplier to shorten the level 1–50 climb. New spawns
use Korriban's 8% health and 4.5% damage growth, with each enemy's first unlock
level as its baseline. Movement and attack-frequency coefficients also match
Korriban. The smaller arena caps population at 24 (Korriban: 36), reserves a
boss slot, and uses five-second recovery breaks rather than travel to new zones.

XRR-3 clears ordinary enemies on activation without awarding despawn rewards.
During the fight, regular adds are limited to 12% of the normal population cap
and arrive at three times the normal interval. The boss starts at 18,000 HP.
Its original Rakata warbot model, 4.5 appearance scale and compatible rig clips
are retained. The attacks below are new solo-arena rules:

- Missile salvo locks two ground circles before impact; three below 65% HP.
- Cannon burst locks a firing lane; three separated lanes below 65% HP.
- Overwhelming swipe warns a frontal cone, leaving the rear safe. A distant
  player triggers missiles instead, avoiding a melee-only kiting exploit.
- Below 35% HP, recovery is shorter and warnings shrink by 0.2 seconds, retaining
  at least 1.3 seconds for the fastest attack. Overlapping marks hit once per
  attack. Walls block damage; warnings clear on death, failure or restart.

The three earlier bosses have doubled visual and collision sizes: Matriarch
scale 1.6→3.2, Captain 1.3→2.6, Warden 1.45→2.9. Their base movement speeds are
0.30, 0.28 and 0.26 source units/s respectively; XRR-3 uses 0.18 and retains
scale 4.5. Nameplates, aim height and the enlarged bosses' melee reach follow
their new dimensions. Bosses use original `Walk` clips rather than full-speed
run cycles. Playback follows actual horizontal displacement divided by the
scaled clip stride speed, so slowing and obstruction also slow the footsteps.
Idle, hit and attack playback return to normal speed. Other enemies/maps retain
their existing presentation behavior.

Raid guards use dedicated quieter warning/impact cues on the Combat bus, with
global cooldowns of 1.5/1.0 seconds to limit overlapping repetitions. The shared
Korriban guardian-droid cue is reduced from -10 to -22 dB with a two-second
cooldown. The raid's audio tuning is `Planets/EternityVault/audio.json`.

`measure_eternity_stride.py` measures horizontal paw/ankle travel during the
lowest 35% of contact height in the original walk clips. The unit-scale fits are
approximately 0.1501 (cat), 0.1395 (guard), and 0.1538 (XRR-3). These are estimates
from clip kinematics, not original server speed values; samples and the method
are recorded in `manifests/eternity-stride-calibration.json`.

Appearance evidence follows April NPC → NPP → creature registry model/material:
ice cat `1000473/1001432`, tauntaun `1000154/1001461`, prison guard
`1005669/1177013`, XRR-3 (`boss_silentior`) `1492589/1492590`. The prison guard's
electrostaff is an original weapon model. Pantran ear bones require the original
catboss skeleton, which includes all cat animation bones; conversion asserts no
unmatched animation or weighted mesh bones. Source hashes are recorded in
`manifests/eternity-encounter-source.json`, and output hashes and clip checks in
`manifests/eternity-enemies-conversion.json`. Elite variants and suppression
roles reuse these original appearances with explicitly authored gameplay stats.

Rebuild enemy assets: run `scripts/prepare_eternity_encounter.py` with the bundled
image Python, then `scripts/convert_eternity_enemies.py` in Blender. Run
`scripts/measure_eternity_stride.py` in Blender, then
`scripts/make_eternity_encounter.py` to regenerate encounter scenes/data, then
import Godot assets and rebuild C#. Map geometry has its separate rebuild path.

Validation: `Run-Eternity-Vault-Smoke.ps1` checks all eight implemented classes
through all 49 level gains, the hard cap, four ordered milestones, boss phase
thresholds, locked-target dodge behavior, doubled XP rewards, boss sizes,
locomotion playback, victory and restart. Use `-Rendered`
with `-Class sorcerer` for a captured rendered pass. Receipts are
`manifests/eternity-encounter-<class>-smoke.json`. These checks verify function;
full-length manual playtests are still needed to tune class-by-class difficulty.

The earlier `--eternity-vault-smoke` remains a map-only inspection option.

Difficulty mechanics refinement:
- Hard uses 1.18x mechanic radius and 0.95x authored warning time; Nightmare
  uses 1.35x radius and 0.75x warning time. Normal retains its existing profile.
- Ground lane width now follows the rendered lane boundary and radius.
- Hard/Nightmare XRR-3 missiles leave at most three burning circles for 7/9
  seconds, ticking once per 0.65 seconds across overlapping circles. A new
  salvo replaces the previous patches. Cannon lanes strike sequentially;
  Nightmare adds a return to the first lane, with a new warning between shots.
- Repeated XRR-3 mechanic hits within 9/14 seconds deal 35% extra damage.
- Advanced cleave, crossfire and trial patterns add one newly locked follow-up;
  Nightmare bolt mobs also fire a follow-up. Repeated hits from the same actor
  within 8/12 seconds deal 25% extra damage. These effects do not stack.
- Death, restart and scene disposal clear XRR-3 fire. Warnings remain locked
  during each windup and boss recovery still follows the final cannon shot.
- These are authored solo-arena mechanics. Manual class-by-class playtesting
  is still required to judge escape routes and final balance in the small arena.

Boss reinforcements now scale with operation difficulty. Hard permits regular
adds up to 75% of normal population during earlier bosses and 50% during XRR-3;
Nightmare uses 95% and 75%. At level 50 XRR-3 permits 12 Hard or 23 Nightmare
adds, versus 3 on Normal. Reinforcement interval multipliers are 1.5/1.25 for
Hard earlier/final bosses and 1/1 for Nightmare. Normal retains authored values.
The global population ceiling, safe spawn placement, introduction pause and
post-encounter recovery still apply. These are ceilings, not instant spawns.
