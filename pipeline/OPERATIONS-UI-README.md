# Operations destination selection

The existing destination screen now separates Worlds and Operations using the same SWTOR theme, blue values, button textures and destination presentation. Planets retain their landscape previews. Eternity Vault expands to a selectable Annihilation Droid XRR-3 encounter, showing the imported model in an isolated viewport. Drag to rotate; double-click to reset. Hidden previews stop rendering.

The catalog declares operation identity, encounter name and model path. Existing `eternity_vault` scene routing and regular loading artwork remain intact. Normal / Hard / Nightmare is saved in `SelectionConfiguration.OperationDifficulty`, passed through the validated gameplay handoff, and retained during restart and returning to selection. Hard is the default and preserves the original tuning. Invalid operation modes are rejected at launch. World maps ignore operation difficulty.

## Encounter difficulty

`godot/Gameplay/Planets/EternityVault/difficulties.json` defines multipliers relative to the existing Hard run. These are initial balance values, not original SWTOR difficulty rules.

| Setting | Normal | Hard | Nightmare |
|---|---|---|---|
| Enemy health | 75% | 100% | 135% |
| Enemy damage, including boss specials | 70% | 100% | 130% |
| Initial / maximum population | 6 / 18 | 8 / 24 | 10 / 30 |
| Spawn interval | 120% | 100% | 85% |
| Attack interval | 120% | 100% | 85% |
| Boss warning duration | 125% | 100% | 85% |
| Recovery time | 125% | 100% | 85% |

Enemy level scaling, movement, XP rewards, level-50 cap, milestones, boss phase thresholds and attack geometry are unchanged. Base health and damage multipliers apply to wildlife, guards and all milestone bosses. XRR-3's warning, pursuit and recovery timing follows the selected mode; intermediate `TombAttack` mechanics receive the warning and cooldown adjustments. Scaling is applied to freshly loaded run data once and retained on restart. The HUD shows the chosen mode.

Run `./Run-Operations-UI-Smoke.ps1` from this folder to build and verify category filtering, nested boss expansion, selection persistence, preview lifecycle, visible keyboard navigation and the real loading transition into Eternity Vault. Rendered captures go to `work/operations-ui-worlds.png`, `work/operations-ui-boss.png` and `work/operations-ui-loading.png`; logs go to `logs/operations-ui-smoke.log`.

Run `./Run-Operation-Difficulty-Smoke.ps1` to verify all three modes through menu selection and real loading, health/damage/cadence at levels 1/25/50, XRR-3 health and actual special damage, warning timing, dodge geometry, phase thresholds and restart without compounded scaling. Logs go to `logs/operation-difficulty-<mode>.log`. Development launches with `--advanced-class=<class> --map=eternity_vault` also accept `--operation-difficulty=Normal|Hard|Nightmare`.
