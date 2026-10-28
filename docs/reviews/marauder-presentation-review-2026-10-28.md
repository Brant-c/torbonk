# Marauder presentation review — 2026-10-28

Scope: the current working tree, including existing uncommitted work. Reviewed the 21 equipped/learnable Marauder abilities (starter plus 20 kit entries), compared Smash with Juggernaut, traced presentation dispatch, target impacts, status lifetimes, icons, and existing checks. No gameplay implementation was changed. This is a source review and local image inspection, not an interactive visual acceptance pass. Native animation graph fidelity remains unverified.

## Findings

### 1. High: Marauder Smash explicitly selects the wrong presentation

`pipeline/godot/Gameplay/Abilities/marauder_smash.tres:14–23` selects `Marauder/cb_2saber_sp1_attack_right_3`, zero windup, and a 0.95-second gesture. It does not enable `FullBodyGesture`.

Juggernaut's `Abilities/smash.tres:17–32` selects `D3/SmashLeap`, a 1.7-second windup, a 2.4-second gesture, and full-body playback. CharacterPresentation constructs SmashLeap by joining recovered launch and descent/slam clips (`CharacterPresentation.cs:329`). These are separate configurations, so the different motion is deterministic, not an intermittent asset-loading failure. Impact audio also differs; Marauder has no configured cast audio.

`manifests/marauder-assets.json` explicitly marks the substituted Smash clip as `direct: false`. The local Warrior catalog identifies Smash as a shared base-class ability for Juggernaut and Marauder, with shared Smash appearance dependencies. That supports sharing presentation, although it does not independently prove every native animation graph transition.

Repair direction: share the leap/slam presentation contract, including full-body selection, impact timing, and audio, while preserving deliberate class balance values. Merely changing StartClip would leave damage and the shockwave occurring before the landing. Also check the Marauder effect dispatch's dependency on the `Marauder/` clip prefix before reusing a `D3/` clip.

### 2. High: Smash's reward still uses the blue icon

`Upgrades/marauder_unlock_smash.tres:7` uses `mr_smash`, which is visibly blue. The ability and the relevant talent use orange `juggernautsmash`.

`UI/UpgradeCard.cs:71` takes its icon from the upgrade definition; `UI/CombatHud.cs:86` takes it from the ability. This explains why Smash can appear blue in the unlock reward and orange in the action bar. Both files exist, so fallback loading is not required to explain the symptom. The older generic `unlock_smash.tres` also uses blue `smash`, but is not in the current Marauder talent pool.

Repair direction: use the same Sith icon identity across the ability, unlock reward, generated mastery offers, and talents. Include the reward path in verification.

### 3. High: Marauder's recorded effect identities are not executed by its renderer

All current Marauder abilities use `D3AbilityPresentation`. Its Marauder adapter does not interpret `Definition.SourceFx`. The long `epp...` strings in the resources therefore document intended/source appearances without making them render.

`MarauderAbilityEffects.cs:40–47` handles every Marauder damage ability with `PresentationKind == "saber"` by playing its impact sound, spawning the same small lightsaber-clash flash, and returning. This bypasses each resource's configured `ImpactTexture`. This affects Vicious Slash, Rupture, Sweeping Slash, Ravage, Annihilate, Battering Assault, Gore, Massacre, Deadly Throw, and Dual Saber Throw.

Smash falls through to the generic expanding ring. Its recorded anticipation, target lightning/rings, and other recovered Smash dependencies are not instantiated by that path. Force Scream has a dedicated simplified traveling wave; it is not completely absent. Force Crush uses a generic burst plus a periodic status visual.

Repair direction: explicit per-ability cast, contact, target, and sustained effect handling using recovered dependencies. Treat source metadata and implemented rendering as separate coverage columns.

### 4. High: area attacks do not present contact effects on each enemy hit

`AutomaticAbility.cs:198` calls `Presentation.Impact(...)` once before iterating damaged targets. The per-target loop only invokes `TargetImpact` for `SurvivalAbilityPresentation` (`:219`); Marauder uses D3AbilityPresentation instead.

Sweeping Slash, cleaving Ravage/Massacre and other talent-enabled cleaves can damage several enemies while producing only one clash flash. For frontal cones, that flash is at the primary target. Radial Smash produces its central ring but no per-victim Smash impact layer. Generic enemy hit reactions still run, and supported periodic effects may attach afterward; the missing piece is ability-specific contact feedback on each victim.

Repair direction: a per-target presentation hook after successful damage, while keeping shared area effects separate so the ground shockwave is emitted once.

### 5. Medium: Battering Assault, Gore and Massacre are Annihilate copies

Their resources all select `cb_2saber_sp2_attack_right_15` and copy Annihilate's `SourceFx` identities. Their basic impact texture and audio are copied as well. The local Warrior catalog has distinct Battering Assault, Gore and Massacre appearance identities, so these configurations do not represent their individually recovered appearances.

Repair direction: inspect each ability's own recovered appearance and animation candidates before assigning motion. Distinct source identities do not by themselves prove which native graph-selected clip is correct. Ravage and Vicious Slash also have explicitly provisional clip assignments in `marauder-assets.json`.

### 6. Medium: several gameplay windows have no matching sustained visual

`Talents/MarauderRuntime.cs:100–102` starts Force Camouflage for 2 seconds, Obfuscate for 3 seconds and Cloak of Pain for 4 seconds. Their presentation is a buff gesture plus generic one-shot glow/smoke. No persistent camouflage material/visibility effect, dedicated Obfuscate target effect, or sustained Cloak of Pain visual is connected to those timers. Obfuscate currently targets the caster through the generic buff path.

Only Berserk, Predation and Frenzy are exposed through `MarauderAuraActive`. Deadly Saber gets an initial flash and subsequent generic bleed status, but no charge/window-driven weapon coating. Gore's active damage window likewise has no dedicated sustained indicator. Cloak retaliation and talent follow-up damage call `Health.ApplyDamage` directly, without a distinct ability effect cue (`MarauderRuntime.cs:121–122,151`).

Repair direction: expose the relevant actual windows/charges to presentation and bind start, follow, proc, expiry, interruption, and reset behavior to them.

### 7. Medium: Berserk suppresses active Predation and Frenzy aura emitters

`MarauderAbilityEffects.cs:14–15` chooses Berserk whenever it is active, then returns from every presentation instance whose ability is not Berserk. Consequently, active Predation/Frenzy emitters stop spawning their own sustained particles during Berserk. Existing short particles can finish. Their gameplay timers continue independently.

Repair direction: evaluate each aura's own timer independently, with any intentional combined appearance specified explicitly. Berserk and Frenzy also currently use the same fire atlas and tint treatment.

### 8. Medium: throw damage and visible travel use separate clocks

`MarauderThrownSaber.cs:22–25` hardcodes a 0.9-second round trip and reaches the target at 0.45 seconds. Deadly Throw has zero windup and a 0.4-second gesture, so damage/contact feedback can occur immediately while the visible saber is still outbound, and the held saber remains hidden after its gesture ends.

Dual Saber Throw has 0.25-second windup and a 0.3-second channel with 0.15-second ticks. Its hit schedule is not derived from the flight. The visual follows one target while gameplay can hit several enemies within a cone; actual paths through each victim are not established by the flight renderer.

Repair direction: use shared presentation timing markers for release/contact/return while retaining gameplay authority in AutomaticAbility. Check deaths, target movement and overlapping weapon leases.

### 9. Medium: moving attacks lose the source clip's lower-body motion

Marauder attack resources do not request full-body gestures. `CharacterPresentation.cs:179` fades the full standing cast out as locomotion/jump weight rises; the remaining gesture layers cover the spine/chest/upper body. Thus source animation legs/root are replaced while moving. Library loading also compresses most clips to at most 0.95 seconds (`:302`), independently of contact markers. Ravage is assigned 1.5 seconds; attack-speed adjustments change tick intervals without retiming its clip.

This is confirmed configuration behavior, but its severity for each non-Smash ability needs a moving/stationary visual comparison. It should not be mistaken for missing animation files.

## Ability coverage

| Ability | Current presentation and main gap |
| --- | --- |
| Vicious Slash | Provisional dual-saber clip; shared clash flash; immediate contact timing. |
| Rupture | Source-linked clip; shared clash; simplified bleed follows target. |
| Sweeping Slash | Source-linked clip; shared clash on primary target despite area hits. |
| Ravage | Provisional clip compressed to 1.5 s; shared clash; cleave lacks per-enemy contacts. |
| Annihilate | Source-linked clip; shared clash replaces configured impact texture. |
| Battering Assault | Copies Annihilate motion/effect metadata/audio. |
| Gore | Copies Annihilate; active window lacks its own sustained cue. |
| Massacre | Copies Annihilate; additional cleave victims lack contact effects. |
| Deadly Saber | Initial weapon flash; generic bleed; no sustained charge/window coating. |
| Deadly Throw | Actual equipped weapon flight; immediate hit precedes visible arrival. |
| Dual Saber Throw | Two equipped weapon flights; hit/channel and flight timings differ. |
| Force Crush | Generic burst plus simplified purple target status; no full source lifecycle. |
| Smash | Different clip, body mask, timing and audio from Juggernaut; blue unlock icon; generic ring. |
| Force Scream | Buff gesture and dedicated simplified wave; splash lacks per-victim effect hook. |
| Intimidating Roar | Buff gesture, generic ring, displacement/slow; no dedicated target fear appearance. |
| Berserk | Sustained chest/wrist fire exists; suppresses overlapping Predation/Frenzy emitters. |
| Predation | Sustained chest/wrist electricity exists; suppressed during Berserk. |
| Frenzy | Sustained chest/wrist fire exists; suppressed during Berserk; visually shares Berserk treatment. |
| Obfuscate | Provisional feint clip and caster smoke; no sustained dedicated target effect. |
| Force Camouflage | Feint clip and one-shot smoke; no sustained camouflage treatment. |
| Cloak of Pain | Buff clip and one-shot smoke; no sustained defensive or distinct retaliation effect. |

Force Charge exists as a resource but is not in the current Marauder kit and its activation is explicitly disabled. It was excluded from the 21 active/learnable abilities.

## Checks and limits

- Traced all 21 ability resources through the selected presentation adapters and gameplay lifecycle.
- Checked their literal `res://` resource references: no missing files found. This does not validate imported GLB animation names or native correctness.
- Compared current ability icons with their configured Marauder unlock icons: Smash was the only naming mismatch. Visually inspected blue `mr_smash`, blue `smash`, and orange `juggernautsmash`.
- Inspected current smoke-test source. The Smash check validates the ability icon and cooldown only, not its reward icon, leap motion, landing synchronization or enemy effects. Wrist/trail motion checks cover five saber attacks and exclude Smash. Aura checks count objects without validating concurrent auras or their visual identity.
- No fresh runtime test or interactive capture was performed. Existing passing reports are not treated as proof that presentation is correct.
- The source catalog/manifests are local recovered evidence, not a fresh verification of every original April 2012 asset binding. The report distinguishes implemented substitutions from unresolved native clip selection.

Recommended implementation order: shared Smash presentation and icon paths; per-enemy impact dispatch and ability-specific FX; copied Carnage abilities; sustained utility/proc feedback and aura overlap; synchronized throws and movement/haste timing. Follow with stationary, moving, single-target, multi-target, lethal-hit and overlapping-buff visual checks.
