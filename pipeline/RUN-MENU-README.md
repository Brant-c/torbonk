# In-run menu

The compact toolbar at the top center has Return to Main Menu, Restart Run and Collections icons. Their tooltips name each action. Escape opens a pause panel with Resume Run and the same three actions. R retains its restart shortcut.

Collections opens on the current run's class. Combat and run time pause while it is open. Closing it applies the saved cosmetic profile to the live character, including removed overrides, without resetting combat progress. Opening it over a pending level-up freezes that presentation and restores its pause when closed. Restart clears the run and any pending upgrade through the existing reset flow. Return uses the existing scene teardown.

Artwork reuses recovered April button subimages and window chrome; exact icon symbol chains are in `manifests/run-menu-art.json`, with underlying atlas receipts in `ui-conversion.json`. Godot controls supply the toolbar behavior.

`Run-Unlocks-Smoke.ps1 -Capture` runs the isolated-save integration checks and writes `work/run-menu-gameplay.png` and `work/run-menu-collections.png`. It checks actual toolbar pointer clicks, pause/resume, live outfit application, pending upgrades, restart, Escape and return-to-menu cleanup, alongside existing unlock, compatibility and icon checks.
