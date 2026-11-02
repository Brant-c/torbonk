using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class AgentExpansionSmoke
{
    private void CheckCoreRefinements(GameWorld w,Combatant first,Combatant second,Action<string,bool> check)
    {
        var original=w.Build.Pregame;var prop=typeof(RunBuildEffects).GetProperty(nameof(RunBuildEffects.Pregame))!;
        var enabled=w.Abilities.Abilities.ToDictionary(a=>a,a=>a.CombatEnabled);
        try{
            if(w.RunConfiguration.AdvancedClassId=="operative"){
                TalentProfiles.Save("operative",new Dictionary<string,int>{{"concealment.revitalizers",1},{"concealment.meticulously_kept_blades",1},{"concealment.tactical_opportunity",1}});
                var venom=new TalentRuntime(w);prop.SetValue(w.Build,venom);w.Abilities.ResetUnlocks();w.Progression!.ResetRun();
                var dart=w.Abilities.Find("dart")!;var lac=w.Abilities.Find("laceration")!;var replacedShiv=w.Abilities.Find("shiv")!;
                check("Lacerating Blades replaces Shiv with Laceration at the same slot cost",lac.Unlocked && !replacedShiv.Unlocked && dart.Unlocked && w.Abilities.Equipped(AbilityCategory.Offensive)==1);
                check("replaced Shiv is excluded from learning choices",!w.Progression.IsEligible(w.Progression.Upgrades.First(u=>u.AbilityId=="shiv" && u.Effect==UpgradeEffect.UnlockAbility)));
                w.Periodic.RemoveSource(dart);venom.DirectHit(lac,first);venom.DirectHit(lac,second);
                check("Laceration no longer applies Corrosive Dart",!w.Periodic.Has(dart,first) && !w.Periodic.Has(dart,second));
                var backstabDef=w.Abilities.Find("backstab")!.Definition;
                check("Backstab out-damages and out-cleaves Laceration",backstabDef.Damage>lac.Definition.Damage*2 && backstabDef.Radius>lac.Definition.Radius && backstabDef.Range>lac.Definition.Range);
                CheckAcidBlade(w,first,second,check);
                var shiv=new AutomaticAbility();shiv.Configure(replacedShiv.Definition);float baseDamage=shiv.DamagePerTick;venom.ApplyStats(shiv);check("Concealment core training no longer buffs Shiv",shiv.DamagePerTick==baseDamage);
                TalentProfiles.Save("operative",new Dictionary<string,int>());var baseline=new TalentRuntime(w);prop.SetValue(w.Build,baseline);
                string[] order={"debilitate","backstab","laceration","shiv","dart"};
                foreach(var a in w.Abilities.Abilities){a.CombatEnabled=false;a.SetUnlocked(order.Contains(a.Definition.Id));}
                foreach(string id in order){
                    w.Abilities.Abort();w.Player.Presentation.ResetPresentation();
                    foreach(var a in w.Abilities.Abilities)a.ResetCombat();
                    foreach(string pending in order.SkipWhile(x=>x!=id))w.Abilities.Find(pending)!.CombatEnabled=true;
                    w.Periodic.RemoveSource(dart);w.Targeting.Update(0,true);int casts=w.Abilities.Find(id)!.Casts;
                    w.Abilities._PhysicsProcess(.2);
                    check("automatic Operative priority selects "+id,w.Abilities.Find(id)!.Casts==casts+1);
                    w.Abilities.Find(id)!.CombatEnabled=false;
                }
                w.Abilities.Abort();w.Player.Presentation.ResetPresentation();dart.CombatEnabled=true;dart.SetUnlocked(true);dart.ResetCombat();
                foreach(var nearby in w.Targeting.InArea(w.PlayerActor.GlobalPosition,dart.Range,dart.Range))w.Periodic.Apply(dart,nearby);
                check("standalone Dart waits while nearby poisons are fresh",!dart.TryBegin());
            }else{
                var ranks=TalentCatalog.Current.ForClass("sniper").Where(t=>t.Tree=="marksmanship").ToDictionary(t=>t.Id,t=>1);TalentProfiles.Save("sniper",ranks);var marksman=new TalentRuntime(w);
                var snipe=new AutomaticAbility();snipe.Configure(w.Abilities.Find("snipershot")!.Definition);float damage=snipe.DamagePerTick;marksman.ApplyStats(snipe);
                check("Marksmanship no longer buys Snipe damage or extra bounces",snipe.DamagePerTick==damage && snipe.ExtraBounces==0);
                check("Marksmanship talent descriptions target core attacks",TalentCatalog.Current.ForClass("sniper").Where(t=>t.Tree=="marksmanship").All(t=>!t.Description.Contains("Snipe ",StringComparison.Ordinal)));
            }
        }finally{
            prop.SetValue(w.Build,original);TalentProfiles.Save(w.RunConfiguration.AdvancedClassId,original.Ranks);
            foreach(var p in enabled){p.Key.CombatEnabled=p.Value;p.Key.ResetCombat();}
        }
    }
}
