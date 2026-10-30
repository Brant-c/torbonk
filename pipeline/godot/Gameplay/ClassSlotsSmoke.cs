using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class ClassSlotsSmoke : Node
{
 public void Run(GameWorld w){var checks=new List<string>();string? failure=null;string cls=w.RunConfiguration.AdvancedClassId;
  void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);}
  try{
   w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
   var c=w.Abilities;var p=w.Progression!;p.ResetRun();
   Check("common four offensive and two utility slots",c.OffensiveLimit==4 && c.UtilityLimit==2);
   Check("startup is inside both caps",c.Equipped(AbilityCategory.Offensive)<=4 && c.Equipped(AbilityCategory.Utility)<=2);
   var free=c.Abilities.Where(a=>!a.Definition.IsBasicAttack && a.Definition.Slotless).Select(a=>a.Definition.Id).OrderBy(s=>s).ToArray();
   var expected=cls=="sorcerer"?new[]{"affliction","polarityshift"}:new[]{w.RunConfiguration.Profile.StartingAbility};
   Check("only benchmark exemptions and the free starter survive",free.SequenceEqual(expected.OrderBy(s=>s)));
   Check("every ordinary utility counts",c.Abilities.Where(a=>a.Definition.Category==AbilityCategory.Utility && a.Definition.Id!="polarityshift").All(a=>!a.Definition.Slotless));
   Check("fallback weapons remain free",c.Abilities.Where(a=>a.Definition.IsBasicAttack).All(a=>a.Definition.Slotless));
   foreach(var category in new[]{AbilityCategory.Offensive,AbilityCategory.Utility}){
    foreach(var a in c.Abilities.Where(a=>a.Definition.Category==category && !a.Definition.Slotless && !a.Unlocked).ToArray()){
     bool room=c.Equipped(category)<c.Limit(category) && !c.Conflicts(a);int before=c.Equipped(category);
     bool unlocked=c.Unlock(a.Definition.Id);Check(a.Definition.Id+" respects admission",unlocked==room && c.Equipped(category)<=c.Limit(category) && (unlocked || c.Equipped(category)==before));
    }
   }
   void Offer(UpgradeDefinition upgrade){
    var offers=(List<UpgradeDefinition>)typeof(RunProgression).GetField("_offers",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(p)!;
    offers.Clear();offers.Add(upgrade);typeof(RunProgression).GetProperty("PresentationPhase")!.SetValue(p,ProgressionPresentationPhase.Choosing);
   }
   foreach(var category in new[]{AbilityCategory.Utility,AbilityCategory.Offensive}){
    var upgrade=p.Upgrades.FirstOrDefault(u=>u.Effect==UpgradeEffect.UnlockAbility && c.Find(u.AbilityId) is {} a && !a.Unlocked && a.Definition.Category==category && p.IsEligible(u));
    if(c.Equipped(category)!=c.Limit(category) || upgrade==null)continue;
    Offer(upgrade);Check(category+" offers replacement at the cap",p.Choose(0) && p.Replacing);
    Check(category+" replacements stay within their category",p.ReplacementOptions.All(a=>a.Definition.Category==category && !a.Definition.IsBasicAttack));
    int index=category==AbilityCategory.Offensive?p.ReplacementOptions.ToList().FindIndex(a=>a.Definition.ReplaceableStarter):p.ReplacementOptions.ToList().FindIndex(a=>a.Definition.Id is "electrocute" or "blowforblow");
    if(index<0)index=0;
    var old=p.ReplacementOptions[index];bool oldFree=old.Definition.Slotless || old.InheritsFreeSlot;
    Check(category+" replacement succeeds",p.Replace(index));var incoming=c.Find(upgrade.AbilityId)!;
    p.Recalculate();Check(category+" replaced ability stays removed after recalculation",!old.Unlocked && incoming.Unlocked && c.Equipped(category)<=c.Limit(category));
    if(old.Definition.Id is "electrocute" or "blowforblow")Check("replaced talent ability remains a normal acquisition choice",p.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==old.Definition.Id && p.IsEligible(u)));
    if(oldFree){Check("free starter credit transfers exactly once",incoming.InheritsFreeSlot && !old.Definition.Slotless && !old.InheritsFreeSlot);Check("reacquiring starter cannot add a second free position",!c.CanUnlock(old));}
   }
   if(w.Build.Pregame.AssassinDeception)Check("talent Electrocute consumes a utility slot",c.Find("electrocute") is {Definition.Slotless:false});
   if(w.Build.Pregame.AgentCounterstrikeReady)Check("talent countermeasure consumes a utility slot",c.Find("blowforblow") is {Definition.Slotless:false});
   p.ResetRun();Check("reset restores the starter and clears inherited credits",c.Find(w.RunConfiguration.Profile.StartingAbility)!.Unlocked && c.Abilities.All(a=>!a.InheritsFreeSlot));
   Check("reset respects caps and original exemptions",c.Equipped(AbilityCategory.Offensive)<=4 && c.Equipped(AbilityCategory.Utility)<=2 && c.Abilities.Where(a=>!a.Definition.IsBasicAttack && a.Definition.Slotless).Select(a=>a.Definition.Id).OrderBy(s=>s).SequenceEqual(expected.OrderBy(s=>s)));
  }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
  string variant=OS.GetCmdlineUserArgs().FirstOrDefault(s=>s.StartsWith("--slots-variant="))?.Split('=')[1]??"base";
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/class-slots-{cls}-{variant}.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
