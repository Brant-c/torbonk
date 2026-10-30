using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class AgentSpecIdentitySmoke : Node
{
 public async void Run(GameWorld w){var checks=new List<string>();string? failure=null;string cls=w.RunConfiguration.AdvancedClassId;
  void Check(string s,bool ok){if(!ok)throw new Exception(s);checks.Add(s);}
  var original=w.Build.Pregame;var prop=typeof(RunBuildEffects).GetProperty(nameof(RunBuildEffects.Pregame))!;
  try{
   w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Player.TestInput=Vector2.Zero;
   for(int i=0;i<8;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);w.Player.SetPhysicsProcess(false);
   foreach(var enemy in w.Spawner.Enemies){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
   var p=w.Progression!;var cat=TalentCatalog.Current;
   TalentRuntime Profile(Dictionary<string,int> ranks){var error=TalentProfiles.Save(cls,ranks);Check("profile validates: "+(error??"saved"),error==null);var rt=new TalentRuntime(w);prop.SetValue(w.Build,rt);p.ResetRun();rt.Advance(.61);return rt;}
   var generate=typeof(RunProgression).GetMethod("GenerateOffers",BindingFlags.Instance|BindingFlags.NonPublic)!;
   List<UpgradeDefinition> Offers(int level)=>(List<UpgradeDefinition>)generate.Invoke(p,new object[]{level,Array.Empty<UpgradeDefinition>(),3})!;
   if(cls=="sniper"){
    var builds=new[]{("marksmanship",new[]{"ambush","seriesofshots","followthrough","suppressivefire","entrench"}),
     ("engineering",new[]{"interrogationprobe","explosiveprobe","plasmaprobe","empdischarge","shieldprobe"}),
     ("agent_lethality",new[]{"dart","corrosivegrenade","agentcull","weakeningblast","shieldprobe"})};
    foreach(var (tree,kit) in builds){
     var talents=cat.ForClass(cls).Where(t=>t.Tree==tree).ToArray();Check(tree+" has nine talents",talents.Length==9);
     var rt=Profile(talents.ToDictionary(t=>t.Id,t=>1));Check(tree+" all talents survive validation",rt.Ranks.Count==9);
     foreach(string id in kit)Check(tree+" core fits: "+id,w.Abilities.Unlock(id));p.Recalculate();
     Check(tree+" stays inside four/two budgets",w.Abilities.Equipped(AbilityCategory.Offensive)==4 && w.Abilities.Equipped(AbilityCategory.Utility)<=2);
     Check(tree+" opening follows its own core",rt.AgentEarlyAbilities.Take(2).SequenceEqual(tree=="marksmanship"?new[]{"suppressivefire","ambush"}:kit.Take(2)));
     var mechanics=p.Upgrades.Where(u=>u.Spec==tree && u.Effect==UpgradeEffect.Mechanic && p.IsEligible(u)).ToArray();
     Check(tree+" has at least three working build drops",mechanics.Length>=3);
     Check(tree+" drop affinity follows talent investment",mechanics.All(u=>rt.OfferWeight(u)>1));
     for(ulong seed=1;seed<=12;seed++){p.SeedOffers(seed);var offers=Offers(12);Check(tree+" offers support owned kit "+seed,offers.Count==3 && offers.All(p.IsEligible) && offers.Any(u=>u.Spec==tree && u.Effect==UpgradeEffect.Mechanic) && offers.Any(u=>u.Effect==UpgradeEffect.AbilityPower && kit.Contains(u.AbilityId)));}
     if(tree=="marksmanship")Check("precision talents add ricochets and Ambush damage",w.Abilities.Find("ambush")!.ExtraBounces>=2 && w.Abilities.Find("seriesofshots")!.ExtraBounces>=2 && w.Abilities.Find("ambush")!.DamagePerTick>w.Abilities.Find("ambush")!.Definition.Damage);
     if(tree=="engineering")Check("engineering talents improve probes and EMP recovery",w.Abilities.Find("empdischarge")!.CooldownSeconds<w.Abilities.Find("empdischarge")!.Definition.CooldownSeconds && w.Abilities.Find("explosiveprobe")!.Radius>w.Abilities.Find("explosiveprobe")!.Definition.Radius);
     if(tree=="agent_lethality")Check("lethality has independent poison/finisher mechanics",rt.PeriodicDuration(w.Abilities.Find("dart")!)>w.Abilities.Find("dart")!.Definition.PeriodicDuration && w.Abilities.Find("weakeningblast")!.Definition.SpreadsPeriodicEffects);
    }
   }else{
    Profile(new());Check("non-Concealment retains Carbine option",p.IsEligible(p.Upgrades.Single(u=>u.Id=="unlock_carbineburst")));
    var rt=Profile(new(){{"concealment.collateral_strike",1},{"concealment.culling",1}});
    Check("Concealment removes Carbine from core",rt.AgentConcealment && !rt.AgentEarlyAbilities.Contains("carbineburst"));
    Check("Concealment rejects Carbine acquisition and upgrades",p.Upgrades.Where(u=>u.AbilityId=="carbineburst").All(u=>!p.IsEligible(u)));
    var carbine=w.Abilities.Find("carbineburst")!;carbine.SetUnlocked(true);Check("Concealment cannot fire a force-enabled Carbine",!rt.CanBeginAgent(carbine));p.Recalculate();Check("recalculation removes legacy Carbine ownership",!carbine.Unlocked);
    var lac=w.Abilities.Find("laceration")!;Check("Collateral Strike now supports both Laceration swipes",rt.Sum("damage","laceration")>=.7f && rt.Sum("radius","laceration")>=.5f && cat.ForClass(cls).Where(t=>t.Tree=="concealment").SelectMany(t=>t.Effects).All(e=>e.Ability!="carbineburst"));
    for(ulong seed=1;seed<=24;seed++){p.SeedOffers(seed);Check("Concealment offers omit Carbine "+seed,Offers(2+(int)(seed%12)).All(u=>u.AbilityId!="carbineburst"));}
   }
  }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}finally{prop.SetValue(w.Build,original);TalentProfiles.Save(cls,original.Ranks);}
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/agent-spec-identity-{cls}.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
