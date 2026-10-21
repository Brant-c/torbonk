# C1 limitations

- Godot has never been launched for C1. Compilation and resource checks do not establish correct import or rendering. Manual visual and interaction review is required.
- No gameplay integration, scene switching to gameplay, new abilities, new environments, launcher change or main-scene change exists.
- Only Sith Sorcerer has a compatible staged character. Every other advanced class displays its own original codex artwork, labelled as artwork with no 3D preview. Base-class selection also uses base-class artwork, avoiding an implicit Sorcerer substitution.
- The Sorcerer preview uses the existing original modular model and Idle clip. Camera, lights, normalization, zoom, drag response and fades are Godot adaptations. The background uses original character-selection star artwork; the original class-specific 3D preview environment was not converted. The menu's globe uses the original character-selection Tython surface projected with a small canvas shader.
- Appearance remains the existing Human / male / body type 2 (`bmn`) / head A01 / hair A01 with V01 material / original skin and default complexion / SS A04 C01 S01 outfit. Every editing row is disabled. No alternative species, gender/body presentation, body geometry, facial morph, hair mesh, palette, complexion or outfit has a supported conversion/transformation pipeline in C1.
- An unsupported class retains the configuration's Sorcerer preset as inactive future state; its Ready summary explicitly reports appearance unavailable. That preset is never rendered as the selected unrelated class.
- Galaxy destinations are schematic. The background and panel/planet artwork are original; planet positions, navigation rules, spacing, shader projection and highlight/fade behavior are authored. The April native 3D galaxy, region graph, camera travel and full planet FX are not executed.
- Korriban is the only map with existing gameplay content. Tython, Dromund Kaas, Coruscant, Hutta and Ord Mantell are viewable future selections only. None imports gameplay environments.
- Only the existing Sorcerer/Korriban combination with the frozen appearance is content-playable. The Ready Play button is always disabled while integration is pending.
- Audio uses one traced embedded variation per event, not the native Wwise random container, mixing, spatialization or runtime. No original character-creation music or class narration was recovered for use. No replacement music was invented.
- Fonts reuse the existing original Eurostile outline conversion; original kerning, hinting, Scaleform text/layout and timeline behavior are not reproduced.
- New Run deliberately resets to the supported preset. Character Selection and ordinary navigation retain the in-memory session. No disk save system, character-name service or account creation exists.
- Settings is a placeholder plus a session-local interface-sound mute. No game settings are changed. Exit quits the temporary review harness.
- Historical resource filenames are hypotheses until matched to a unique April archive entry. Missing candidates are recorded and were not replaced with newer client assets.

Stop at C1. Await manual review and explicit authorization before integrating or beginning another milestone.
