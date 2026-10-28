# Marauder and shared Warrior presentation fixes

Implemented against the existing working tree following the [initial review](marauder-presentation-review-2026-10-28.md). Existing unrelated changes were retained.

## Changes

- Marauder and Juggernaut Smash now use the same recovered launch/descent composition, full-body gesture, 1.7-second landing impact, cast audio, impact audio, anticipation glow, ground wave, smoke, and lightning layers. Each damaged enemy receives its own impact. Class damage, cooldown and radius tuning remain separate.
- Smash's Marauder unlock card and the older generic unlock use the orange Sith icon. Massacre now uses its recovered Overpower artwork consistently on the ability and unlock card.
- Added explicit Warrior cast/contact/status adapters instead of treating `SourceFx` metadata as rendered coverage. Saber attacks have different contact treatments; Force Crush, Force Choke, Scream, Roar and Smash have target feedback. Shared area waves play once while contact effects follow every successfully damaged victim, including secondary and lethal hits.
- Battering Assault, Gore and Massacre use their own recovered clips and appearance identities. Gore and Massacre also use appropriate distinct recovered audio samples and contact artwork.
- Recovered an additive library containing `cb_saber_sp3_attack_right_6` for single-saber Ravage and `cb_2saber_sp1_attack_right_1` for dual-saber Vicious Slash. Their appearance schedules request `SaberSpecial3Attack6` and `SaberSpecial1Attack1`, respectively. Juggernaut no longer uses its basic swing for Ravage.
- Melee attacks preserve their complete full-body motion while moving. The actor can fit recovered clip duration to the ability gesture and keeps facing its target during an owned gesture. Marauder's immediate melee contact timings now have a short 0.18-second lead-in. Ravage adapts playback rate to its actual tick interval and repeats its sequence when accelerated channels have further hits, without changing hit counts or damage rules. Its swing/contact audio plays at strike times.
- Berserk, Predation and Frenzy emit independently while overlapping, with distinct fire/electricity/ring treatments. Deadly Saber, Gore and Battering Assault have weapon-window feedback; Cloak of Pain has sustained character and retaliation effects; Obfuscate follows its presentation target.
- Force Camouflage fades per-instance clothing/equipment materials and restores their original overrides and transparency on expiry, abort or reset. The material treatment is needed for visible fading in the Compatibility renderer.
- Deadly Throw damage now waits for a 0.3-second outbound flight. Both throws use ability/tick-derived contact/return times. Abort restores held weapons immediately; overlapping weapon leases remain supported. Hidden held blades no longer generate stationary ghost trails.
- Juggernaut receives the shared per-victim effects, periodic status visuals, a sustained Enraged Defense cue, and Warrior Saber Ward artwork in place of the generic bounty-hunter hexagon shell. Choke maintains its casting gesture through its channel.
- Fixed the shared particle shader's billboard transform so it preserves model scale, allowing expanding wave/glow effects to scale visibly.

## Validation

`pipeline/Run-Warrior-Presentation-Smoke.ps1` builds and checks both classes. Use `-Capture` for a hidden rendering run that saves per-ability PNGs in `pipeline/manifests/`. Test profiles use isolated application-data directories.

The dedicated checks exercise the actual runtime adapters with three enemy models: all learnable Marauder abilities and the Juggernaut kit, moving Smash leap/root motion and landing timing, one contact effect per successful damage event, lethal tails, simultaneous auras, utility expiry, camouflage cleanup, accelerated Ravage motion/audio, thrown weapon timing, immediate abort restoration and Cloak retaliation feedback. Reports are `warrior-presentation-marauder.json` and `warrior-presentation-juggernaut.json`.

The existing Marauder and Juggernaut class regressions also pass. The Marauder fixture now isolates direct DoT refresh checks from its separately enabled spread abilities, and waits for the new melee windup in its spread/audio checks. The identity fixture begins the presentation before inspecting its emission.

Final results: Marauder presentation 182 checks; Juggernaut presentation 117 checks; Marauder class regression 335 checks; Juggernaut class regression 151 checks; melee/audio regression 273 checks. Marauder identity regression also passed. Runtime logs for these checks contain no `ERROR:` or `SCRIPT ERROR:` entries.

Rendered per-ability captures were reviewed for both classes, including Smash, distinct melee poses, Saber Ward, camouflage and thrown weapons. Camouflage was visibly checked after adding per-instance material fading. The build succeeds with existing nullable/obsolete warnings and unavailable online NuGet vulnerability metadata.

## Fidelity boundary

These are bounded Godot adapters built from recovered clips, textures and audio. They do not implement the original general-purpose FXSpec/particle engine or Wwise graph evaluator, nor establish every original native animation graph condition. Obfuscate retains the existing Survivors defensive gameplay while gaining a target presentation cue. No claim of exact original-server balance or pixel-identical native effects is made. Gameplay and damage remain authoritative outside presentation.

Recovered animation conversion evidence is in `pipeline/manifests/warrior-presentation-animation-conversion.json`; the additive conversion recipe is `pipeline/scripts/convert_warrior_presentation_animations.py`.
