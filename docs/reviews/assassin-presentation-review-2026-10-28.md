# Assassin presentation review — 2026-10-28

Reviewed the current working tree's 16 Assassin abilities, presentation dispatch, movement blending, target effects, defensive lifetimes, source clip mappings, and current refinement checks. Gameplay implementation was not changed in this review. Existing unrelated work was retained. Local source files/manifests are the evidence; this is not a fresh verification of every original native animation graph condition.

## Confirmed findings

### 1. High: Assassin does not have directional locomotion while attacking

`pipeline/godot/Gameplay/GameWorld.cs:359` enables the new directional locomotion only for Marauder and Juggernaut. Assassin still selects `D3/PoleRun`, a forward run.

Assassin already benefits from the shared change that leaves ordinary attack legs under locomotion/jump control. However, the character can face an enemy while moving sideways or backwards and still play forward-running legs. The remaining mismatch needs pole-specific forward/back cycles plus in-place lateral adaptation, retaining target facing. The Warrior lesson applies: do not loop a source lateral transition that contains travel or turning.

### 2. High: Lacerate has no per-enemy contact effect or pole-saber trail adapter

Lacerate uses `D3AbilityPresentation` with `PresentationKind = "saber"`, an empty impact texture and empty impact audio. `D3AbilityPresentation.cs:76` explicitly emits no saber contact effect. Although the shared hit loop calls D3's target hook, `WarriorAbilityEffects.cs:130` rejects Assassin, so it does not supply a fallback.

The real weapon and generic enemy hit reactions remain visible, and the swing/cast sounds exist. The missing layer is ability contact feedback on each enemy struck. `MarauderBladeTrails` is installed only for Marauder (`GameWorld.cs:360`); the Assassin renderer also does not invoke its recovered dynamic pole-saber trail recipes. Lacerate therefore lacks the equivalent of the reconstructed blade trails and per-victim contacts recently added for Warriors.

Lacerate's existing clip maps to `cb_pole_blade_dance_end` in `manifests/d3-conversion.json`. That is an ending segment. It warrants checking the complete appearance sequence before substituting another clip; the filename alone does not prove the native assignment is wrong.

### 3. High: talented Shock chains and Voltaic Fury use an incompatible arc hook

Assassin Shock uses `OriginalAbilityPresentation`. Its main shock tail draws the caster-to-primary-target beam. In contrast, `AutomaticAbility.cs:335` only draws a chain-link arc when the renderer is `D2AbilityPresentation`.

Consequently, Chain Shock can damage secondary targets without displaying the links between them. `AssassinRelics.cs:75` repeats the same D2-only type check for Voltaic Fury's additional Shock targets. This is a concrete rendering-dispatch mismatch, not evidence that secondary damage is absent.

The Original renderer also only adds its direct impact flash when the ability area is `SingleTarget`, so changing Shock to `Chain` removes that flash without supplying per-link replacement flashes.

### 4. High: Discharge lacks target-specific burst and periodic presentation

Discharge uses the generic D3 impact path. Its radial impact produces one burst at the caster, not an effect on each affected enemy. Its target hook is rejected by the Warrior-only condition described above.

`PeriodicEffects.cs:52–57` attaches visuals for Warrior effects, Affliction, Crushing Darkness, Creeping Terror and Incendiary Missile, but has no Discharge attachment. The actual four-second Discharge effects can persist and spread through Lacerate without their own sustained target visual. Enemy hit reactions still occur.

### 5. Medium: Deflection's visual lifetime ignores its duration talent

`AssassinAbilityPresentation.cs:64` starts a hardcoded four-second emitter loop. Gameplay starts `_deflection = 4 + Hook("deflection_time")` in `Talents/AssassinRuntime.cs:44`, and already exposes `AssassinDeflectionDuration`.

The visual can therefore stop while the extended defensive window remains active. It should follow the actual active state, including reset/expiry, rather than a separate fixed timer. Force Cloak already checks `AssassinCloaked` during its loop, so its approach is closer to the required behavior.

### 6. Medium: Force Shroud can release its gesture early

`Abilities/forceshroud.tres:23–24` specifies 0.4-second recovery and a 0.9-second gesture, without `FinishGesture`. `AutomaticAbility.Finish` uses recovery alone in that case, then releases the gesture when ending completes.

Thus the configured 0.9-second gesture is released after roughly 0.4 seconds. The shield and its aura have a separate lifetime and are not the missing part. Preserve the gesture lifetime while still allowing moving/jumping legs.

### 7. Medium: Wither's alpha wisps use a shader that discards their growth scale

`AssassinWitherVisual.cs:59` changes each wisp's `Sprite3D.Scale`. Its alpha sprites use `D3ParticleAlpha.gdshader`, whose billboard matrix at line 8 replaces the model axes with unscaled camera axes.

The analogous additive shader was corrected in the Warrior work, but this alpha variant still discards model scale. Wisp drift, alpha fade and target following work; their requested scale growth does not. The horizontal ground ring uses a different path, so this does not imply that the ground-wave expansion is missing.

## Timing and attachment work needing a closer visual pass

- Lacerate, Maul, Voltaic Slash, Low Slash, Wither and Discharge use zero windup. Damage and initial target effects occur on the first ability update, while their configured gestures last 1.2–1.6 seconds. This is confirmed timing configuration; the correct contact marker must be sampled per clip rather than assigning one universal delay.
- Wither's `si_hand_cast_particle_trail` is emitted at the actor's position plus a fixed height, with no hand-bone attachment or follow flag (`AssassinAbilityPresentation.cs:67`). While moving, that cast effect remains at its initial world position for its lifetime.
- Generic sustained Assassin particles follow actor center, not their distinct intended bones. The follow update also discards the per-layer vertical offsets supplied at creation.
- The renderer takes the first five layers of every recipe. Maul has eight layers; its final glow/fire/smoke layers are never selected. A fixed budget is reasonable, but this is partial source coverage and should use deliberate layer selection rather than treating the complete recipe as implemented.
- Maul, Voltaic Slash, Wither and Deflection have explicit recovered clip references in the existing evidence. Spike and Low Slash retain documented semantic/substitute mappings. Do not declare a clip wrong solely because its name says single saber or throw; inspect the original appearance and actual pole-equipped motion.

## Ability coverage

| Ability | Current state and review result |
| --- | --- |
| Lacerate | Forward-only moving legs; ending-segment clip needs sequence review; no dedicated contacts/trails. |
| Discharge | Caster burst exists; per-enemy impact and sustained/spread target effects are missing. |
| Maul | Dedicated recovered impact exists; zero-windup contact timing, five-of-eight recipe limit, movement and weapon trails need work. |
| Voltaic Slash | Dedicated impact exists; immediate damage timing and movement need work; charged Shock's secondary arc dispatch is broken. |
| Spike | One-second vanish, reveal, attached weapon hiding and interrupt restoration are implemented; emergence clip/attachments need moving inspection. |
| Low Slash | Dedicated impact exists; immediate timing and substitute clip require contact/pole-motion review. |
| Shock | Primary beam exists; chained/relic secondary arcs and per-link flashes are missing. |
| Wither | Ground wave and per-target four-second wisps exist; hand attachment and alpha-wisp scaling need fixes. |
| Creeping Terror | Target periodic visual exists; directional movement remains unextended. |
| Affliction | Dedicated source presentation and target periodic visual exist; directional movement remains unextended. |
| Crushing Darkness | Cast/impact and target periodic visual exist; short recovery/gesture alignment needs sampling. |
| Death Field | Dedicated ground effect and per-enemy impacts exist; avoid replacing this with a generic shared burst. |
| Force Shroud | Shield aura follows actual shield health; gesture can end early. |
| Force Cloak | Actual character/equipment visibility is saved/restored; offensive break and expiry are implemented. Center-following smoke remains a simplified adaptation. |
| Force Speed | Dedicated trail/audio path exists; empty gesture is intentional movement utility behavior. |
| Deflection | Dedicated loop exists; visual timer does not cover talent-extended gameplay duration. |

## Verification and limits

- Re-ran `Run-Assassin-Refinement-Smoke.ps1 -SkipImport`: baseline 73 checks, Madness 74, Deception 74; all passed.
- Ran the baseline renderer review with an isolated profile and inspected current Maul, Voltaic Slash and Wither captures. No runtime `ERROR:`/`SCRIPT ERROR:` entries appeared in the rendered review.
- Checked literal resource paths in all 16 ability resources; no missing files were found. This does not establish complete effect playback or exact native clip selection.
- Current refinement tests cover stealth visibility, Wither emitter existence/expiry, effect/audio presence and combat interactions. They do not verify directional leg motion, chain-link rendering, every victim's contact effect, extended Deflection visuals, or wisp scaling.
- Existing screenshots are isolated fixture snapshots, with some UI overlays and targets lacking visible rigs. They are not a complete moving, per-frame visual acceptance pass.

Recommended repair order: pole-specific directional movement; Lacerate/Discharge contacts and statuses; Shock and Voltaic Fury chain dispatch; defensive lifetime/gesture cleanup; per-clip contact timing, bone attachments and deliberate source-layer selection. Add moving, multi-target, talent-duration and lethal-hit presentation checks alongside the existing combat checks.
