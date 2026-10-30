using System;
using System.Collections.Generic;
using System.Linq;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors.UI;
/// <summary>Both offer cards and the quickbar describe the actual class-local runtime ability.</summary>
public static class AbilityTooltips
{
 public static bool Warrior(GameWorld w)=>w.RunConfiguration.AdvancedClassId is "marauder" or "juggernaut";
 public static string Summary(GameWorld w,AutomaticAbility a)
 {
  string text=Rules(w,a).Split(new[]{". "},StringSplitOptions.None)[0].TrimEnd('.');
  if(text.Length>110){int end=text.LastIndexOf(' ',107);text=text[..Math.Max(1,end)]+"...";}else text+=".";
  if(w.RunConfiguration.AdvancedClassId=="sniper" && TalentRuntime.AgentCoverAbility(a.Definition.Id) && !text.StartsWith("Requires cover"))text="Requires cover. "+text;
  return text+$"\n{a.CooldownSeconds:0.##}s cooldown";
 }
 public static string Choice(GameWorld w,UpgradeDefinition u,AutomaticAbility? a)
 {
  string detail=u.ChoiceDescription!=""?u.ChoiceDescription:u.Description;
  if(a==null)return u.Title+"\n"+detail;
  return (u.Effect==UpgradeEffect.UnlockAbility?"Learn this ability\n":u.Title+"\n"+detail+"\n\n")+Describe(w,a,false);
 }
 public static string Describe(GameWorld w,AutomaticAbility a,bool equipped=true)
 {
  var d=a.Definition;var lines=new List<string>{d.Title,Rules(w,a)};
  if(w.RunConfiguration.AdvancedClassId=="sorcerer" && d.Id is "deathfield" or "storm")lines.Add("Stand still on the ground to begin. Prioritized while stationary; movement is available after activation.");
  if(w.Build.Pregame.AssassinDeception && d.Id=="discharge")lines.Add("Deception's main DoT: stronger ticks and faster cooldown recovery. Electrocute seeks enemies carrying your Discharge.");
  if(w.RunConfiguration.AdvancedClassId=="sniper" && TalentRuntime.AgentCoverAbility(d.Id))lines.Add("Requires cover to begin: stand still on the ground for 0.6s. Active attacks can finish after leaving cover.");
  if(d.Effect==AbilityEffect.Damage){
   int hits=d.Id=="laceration"?2:a.ChannelSeconds>0?Math.Max(1,(int)Math.Floor(a.ChannelSeconds/a.TickSeconds+.00001)):1;
   lines.Add(hits>1?$"{a.DamagePerTick:0.#} {d.DamageType.ToString().ToLowerInvariant()} damage per hit; {hits} hits over {a.ChannelSeconds:0.##}s.":$"{a.DamagePerTick:0.#} {d.DamageType.ToString().ToLowerInvariant()} damage per target.");
   if(d.Area is AbilityArea.CasterRadial or AbilityArea.FrontalCone or AbilityArea.TargetSplash or AbilityArea.GroundSphere){
    string shape=d.Area==AbilityArea.FrontalCone?"Frontal cleave":d.Area==AbilityArea.CasterRadial?"Around you":d.Area==AbilityArea.TargetSplash?"Around the target":"Ground area";
    lines.Add(shape+(d.MaximumTargets>0?$"; up to {d.MaximumTargets} enemies.":"; hits enemies in the area.")+(d.MaximumTargets>1 && d.SplashMultiplier<1 && d.Area is AbilityArea.FrontalCone or AbilityArea.TargetSplash?$" Secondary hits deal {d.SplashMultiplier:P0} damage.":""));
   }
  }
  if(d.PeriodicDamage>0 && d.PeriodicDuration>0){
   float tick=d.PeriodicDamage*a.DamagePerTick/Math.Max(.001f,d.Damage);
   float interval=w.Build.Pregame.PeriodicInterval(a),duration=w.Build.Pregame.PeriodicDuration(a);
   lines.Add($"{(d.Id=="deadlysaber"?"Applied bleed":"Periodic effect")}: {tick:0.#} damage every {interval:0.##}s for {duration:0.#}s. Same effect refreshes; does not stack.");
  }
  if(d.Effect==AbilityEffect.Shield)lines.Add($"Absorbs {a.ShieldCapacity:0.#} damage for up to {a.ShieldDuration:0.#}s. Does not stack.");
  if(d.Effect==AbilityEffect.Heal)lines.Add($"Heals {a.Healing:0.#} health; activates below {a.HealthThreshold:P0} maximum health.");
  if(d.SlowSeconds>0)lines.Add($"Slows movement by {1-d.SlowFactor:P0} for {d.SlowSeconds:0.#}s.");
  lines.Add($"Cooldown {a.CooldownSeconds:0.##}s."+(d.UsesGlobalCooldown?" Uses the shared attack cooldown.":" Can activate outside the shared attack cooldown."));
  if(d.IsBasicAttack)lines.Add("Automatic fallback in quickbar slot 1. Only used when other attacks cannot activate; uses no offensive slot.");
  else if(d.ReplaceableStarter)lines.Add("Free starting ability. Can be replaced; its replacement keeps the free position.");
  else if(d.Slotless || a.InheritsFreeSlot)lines.Add("Uses no ordinary ability slot.");
  else lines.Add($"Uses one {(d.Category==AbilityCategory.Utility?"utility":"offensive")} slot."+(equipped?"":" Learning it may require replacing an equipped ability."));
  lines.Add("Activates automatically. Movement remains available.");
  var learned=TalentCatalog.Current.ForClass(w.RunConfiguration.AdvancedClassId).Where(t=>w.Build.Pregame.Ranks.GetValueOrDefault(t.Id)>0 && t.Effects.Any(e=>e.Ability==d.Id || Related(d.Id,e.Key))).Select(t=>t.Name).ToArray();
  if(learned.Length>0)lines.Add("Learned talent interactions: "+string.Join(", ",learned.Take(6))+(learned.Length>6?" and others.":"."));
  var trained=w.Progression?.Upgrades.Where(u=>u.Effect==UpgradeEffect.Mechanic && w.Progression.Rank(u)>0 && (u.AbilityId==d.Id || Related(d.Id,u.Mechanic))).Select(u=>u.Title).Distinct().ToArray();
  if(trained?.Length>0)lines.Add("Run passives: "+string.Join(", ",trained)+".");
  return string.Join("\n",lines);
 }
 private static bool Related(string id,string key)=>key switch {
  "sn_crossfire"=>id is "ambush" or "followthrough",
  "sn_ricochet"=>id is "ambush" or "seriesofshots" or "followthrough",
  "sn_suppression"=>id is "suppressivefire" or "plasmaprobe" or "interrogationprobe",
  "sn_bombardment"=>id is "suppressivefire" or "orbitalstrike",
  "sn_overload"=>id is "explosiveprobe" or "interrogationprobe",
  "sn_emp"=>id is "empdischarge" or "explosiveprobe" or "orbitalstrike",
  "sn_targeting"=>id is "interrogationprobe" or "seriesofshots" or "explosiveprobe",
  "sn_cull" or "sn_decay" or "sn_weakening"=>id is "dart" or "corrosivegrenade" or "agentcull" or "weakeningblast",
  "sn_contamination"=>id is "ambush" or "dart",
  "sn_lethal_cover" or "sn_lethal_alignment" or "sn_lethal_echo" or "sn_lethal_extend" or "sn_lethal_spread" or "sn_lethal_focus"=>id is "dart" or "corrosivegrenade" or "agentcull" or "weakeningblast" or "shieldprobe",
  "sn_entrench"=>id is "entrench" or "ambush" or "seriesofshots" or "followthrough" or "suppressivefire",
  "as_conduit"=>id is "voltaicslash" or "shock" or "discharge",
  "as_ambush"=>id is "maul" or "spike" or "forcecloak",
  "as_chain" or "as_echo"=>id=="shock",
  "as_spread"=>id is "lacerate" or "creepingterror" or "affliction" or "crushingdarkness",
  "as_harvest"=>id=="deathfield",
  "as_wither"=>id is "wither" or "maul" or "voltaicslash",
  "as_guard"=>id is "lacerate" or "wither" or "forceshroud" or "deflection",
  "as_decay"=>id is "creepingterror" or "affliction" or "crushingdarkness",
  "agent_mass_debilitation"=>id=="debilitate",
  "agent_counterstrike"=>id=="blowforblow",
  "agent_controlled_burst"=>id=="laceration",
  "mr_spread" or "mr_heal"=>id is "sweepingslash" or "smash" or "rupture" or "deadlysaber",
  "mr_cleave" or "mr_blademaster"=>id is "massacre" or "ravage",
  "mr_alacrity" or "mr_flurry"=>id is "ataruform" or "massacre" or "ravage",
  "mr_bloodletting" or "mr_gore" or "mr_overwhelm"=>id is "massacre" or "ravage" or "gore",
  "mr_precision"=>id is "massacre" or "ravage" or "gore" or "forcescream", "mr_ricochet"=>id=="saberthrow",
  "mr_charges" or "mr_rhythm"=>id=="deadlysaber",
  "mr_reset" or "mr_hemorrhage"=>id is "annihilate" or "rupture",
  "mr_shockwave" or "mr_fissure"=>id=="smash",
  "mr_forceflow"=>id is "forcecrush" or "saberthrow",
  "mr_frenzy" or "mr_relentless"=>id=="berserk",
  "jg_mitigation" or "jg_darkblood" or "jg_revenge"=>true,
  "jg_hatred" or "jg_unstoppable" or "jg_bloodmaster"=>id is "impale" or "ravage" or "crushingblow" or "sunderingassault" or "retaliation" or "viciousthrow",
  "jg_alacrity" or "jg_obliteration"=>id is "smash" or "forcescream" or "forcecrush" or "forcechoke" or "intimidatingroar",
  "jg_dominate"=>id is "forcescream" or "smash",
  "jg_rampage"=>id is "impale" or "ravage",
  "jg_savagery"=>id is "impale" or "forcescream",
  "jg_grip" or "jg_convergence"=>id=="forcechoke", "jg_resonance"=>id=="forcescream", "jg_sweep"=>id=="ravage",
  "jg_fissure" or "jg_decimate" or "jg_shockwave"=>id=="smash",
  "jg_sundering"=>id=="sunderingassault",
  _=>false};
 private static string Rules(GameWorld w,AutomaticAbility a)
 {
  var d=a.Definition;bool mara=w.RunConfiguration.AdvancedClassId=="marauder";
  if(w.Build.Pregame.AssassinDeception && d.Id=="discharge")return "Deception's main damage-over-time attack. Spread dark energy around you; maintain it to set up Electrocute and the following Maul/Voltaic Slash burst.";
  if(mara)return d.Id switch {
   "viciousslash"=>"Quick dual-saber starter. Learned attacks take priority; replace it as your build develops.",
   "rupture"=>"Wound one enemy. With a spread attack equipped, waits for the seeded bleed to expire before applying it by hand again. Position Sweeping Slash to spread it. Cooldown stays at least 8s.",
   "batteringassault"=>"Heavy dual-saber strike. Empowers the next Massacre cleave or full Ravage sequence by 40%.",
   "gore"=>"Heavy direct strike that opens four seconds of 60% additional weapon damage.",
   "massacre"=>"Fast dual-saber cleave for three enemies at full damage, five with Sweeping Blades. Guarantees an Ataru follow-up when ready and empowers its proc chance for 6s. Slaughter recovers Gore cooldown.",
   "sweepingslash"=>"Slash enemies in front of you and spread Rupture. Newly spread effects deal 35% more periodic damage; successful spreads heal up to 8 health and grant +35% weapon damage for 3s. Blood Wake also spreads Deadly Saber and Force Crush, with bonus impact damage.",
   "ravage"=>"Each existing strike cleaves four enemies at full damage, six with Sweeping Blades. Gore empowers the full sequence; Blademaster's Reach adds targets and reach.",
   "annihilate"=>"Heavy focused strike. Deals 35% additional damage to enemies carrying your Rupture or Deadly Saber bleed.",
   "deadlysaber"=>"For 6s, the next three learned weapon hits apply a separate bleed. Vicious Slash does not consume these charges. Talents can add an application.",
   "deadlythrow"=>"Ranged saber strike. Deals 60% additional damage below 30% enemy health.",
   "saberthrow"=>"Throw both sabers through a frontal group and return them. Hits twice. Forked Blades increases the target cap and secondary damage.",
   "forcecrush"=>"Periodic Force damage and a slow. Seeds one target, then waits for spreading when a spread attack is equipped. Ticks build up to three charges for empowered Smash. Cooldown stays at least 8s.",
   "smash"=>"Force burst around you that spreads existing periodic effects. Consumes Crush charges for up to 54% additional damage. Cooldown stays at least 10s.",
   "berserk"=>"For 6s, attacks, periodic ticks and cooldown recovery are 30% faster. Lasting Frenzy extends this window by 2s.",
   "predation"=>"For 6s, movement speed increases 45% and incoming damage is reduced 15%.",
   "frenzy"=>"Readies Berserk and Deadly Saber. Activates when learned Berserk has more than 5s remaining on its cooldown.",
   "forcecamouflage"=>"Activates below 50% health: 2s of 65% damage avoidance and 35% movement speed.",
   "obfuscate"=>"Activates below 80% health: 3s of 30% damage avoidance.",
   "cloakofpain"=>"Activates below 80% health: 4s of 20% damage reduction. Taking damage retaliates against up to three nearby enemies, once per second.",
   "forcecharge"=>"Dash toward a hostile more than 0.5 units away. Rage talents can open an empowered weapon window.",
   _=>d.Description};
  return d.Id switch {
   "retaliation"=>"Counterattack available for four seconds after taking damage. Talents can turn it into a brief defensive window.",
   "viciousthrow"=>"Ranged execution. Only activates against enemies at or below 30% health.",
   "enrageddefense"=>"Activates below 55% health. For 6s, taking damage restores 4 health, at most once per second.",
   "endurepain"=>"Emergency recovery below 35% health. "+d.Description,
   "saberward"=>"Defensive shield, used below 80% health. Talents may add temporary mitigation.",
   "forcecharge" or "obliterate"=>"Close the gap with a short dash. Talent interactions can empower attacks or grant temporary protection.",
   _=>d.Description};
 }
}
