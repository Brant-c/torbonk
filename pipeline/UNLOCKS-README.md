# Cosmetic unlocks through gameplay

The five SS A04 alternate pieces in **Collections → Appearances** now unlock by playing a Sith Inquisitor (Sorcerer or Assassin) run on Korriban, and are wearable wherever Collections marks them compatible. `--wardrobe-dev` still unlocks everything at once for visual review.

| Milestone | Unlocks |
|---|---|
| Reach wave 2 | SS A04 Feet |
| Reach wave 3 | SS A04 Hands |
| Reach wave 4 | SS A04 Waist |
| Reach wave 5 (boss wave) | SS A04 Legs |
| Defeat the Korriban boss | SS A04 Chest |

Each milestone is recorded once per save, the moment it is met, even if the run is later lost. A short **WARDROBE UNLOCKED** notice appears at the top of the screen. Locked pieces in Collections show their milestone under the button and in the tooltip. Unlocks are cosmetic only: the live outfit, items, stats and abilities never change.

## Data and save

* `godot/Gameplay/Equipment/cosmetic-unlocks.json` holds the rules: `Kind` is `Wave` (reach wave `Value`) or `Boss`, `BaseClasses` limits the class (empty means any), and `Appearances` lists the catalog appearance IDs granted. Retune or add milestones here.
* `CosmeticUnlocks.cs` loads the rules and hosts `CosmeticUnlockTracker`, which the survival run adds next to the HUD. It only records progress on normal launches (no user arguments, or just `--wardrobe-dev`), so the existing validators keep their unchanged Collections bytes.
* The save stays `SchemaVersion` 1 with one additive field, `Milestones`. Older saves load with no milestones and keep every piece already unlocked and equipped. A completed milestone re-grants its pieces on load, so rewards added to an existing milestone reach older saves.

## Testing locally

From `D:/SWTORClassic/torbonk`:

```powershell
.\pipeline\Run-Unlocks-Smoke.ps1
```

It uses an isolated `APPDATA`, so your real wardrobe save is untouched. It checks legacy save loading, the rules, persistence and re-grant, then starts a Sorcerer run and drives the live tracker. The receipt is `manifests/unlock-smoke.json`.

For a hands-on check, launch `.\pipeline\Open-Survivors.ps1`, play a Sith Inquisitor run past wave 2, then open Collections to see the Feet unlocked. To start over, delete `%APPDATA%/Godot/app_userdata/SWTOR Survivors - Static Asset Inspection/cosmetic-collections.json`.
