# Level-up text and cosmetic rarity

Replaced the oversized bold gold header with a smaller cyan `CHOOSE A POWER`
title and a separate level/pause subtitle. Card names use regular Eurostile;
descriptions show short benefits with consistent alignment. The offered rank
is shown directly instead of `RANK 0 → 1`, and the card action is `TRAIN [1]`.
Trimmed the footer and added interior spacing so text clears the original frame.
Keyboard focus now uses a thin cyan outline instead of a stretched gold window.

Rarity is presentation metadata only, as requested:

| Visual tier | Colour | Powers |
|---|---|---|
| Premium | Green | Crackling Power, Mark of Power, Force Speed |
| Prototype | Blue | Forked Lightning, Lightning Reach |
| Artifact | Purple | Recklessness |

These are authored cosmetic labels, not rarity rolls or claims about native
SWTOR ability rarity. Each power keeps its tier across offers and ranks.
Rarity does not affect offer selection, bonus amounts, ranks, XP, cooldowns or
any combat/progression calculation. Existing effect descriptions remain in
tooltips, along with a brief explanation that rarity is cosmetic.

The tier terminology/colours follow SWTOR's item quality convention described
in the [original manual](https://eaassets-a.akamaihd.net/eahelp/manuals/star-wars-the-old-republic-manual_PC.pdf).
Icon frames reuse original green/blue/purple outlines from `bwaui_slot.gfx`
subimages 63/72/49 in the preserved atlas 1. Reusable AtlasTexture views and
regions are in `Assets/UI/hud/rarity_*.tres` and `manifests/ui-player-hud.json`;
the additive `recover_player_hud.py` helper regenerates them. No source pixels
or existing conversion pipelines were changed.

Verification: C# build succeeded with zero errors (the existing NU1900 network
warning remains). One brief headless gameplay startup exited 0 without reported
runtime errors; log: `logs/level-up-ui-startup.log`. The startup check does not
exercise an actual level-up offer or establish rendered layout quality. No
regression suite or rendered comparison cycles were run. Manual review pending.

Launch from the project root with `.\pipeline\Open-Gameplay.ps1` and gain a level
to review the cards. Click a card or press 1/2/3 to train as before.
