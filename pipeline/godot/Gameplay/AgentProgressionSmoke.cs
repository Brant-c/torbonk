using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;

public partial class AgentExpansionSmoke
{
    private void CheckProgressionOffers(GameWorld w,Action<string,bool> check)
    {
        string cls=w.RunConfiguration.AdvancedClassId;var original=w.Build.Pregame;
        var property=typeof(RunBuildEffects).GetProperty(nameof(RunBuildEffects.Pregame))!;
        var generate=typeof(RunProgression).GetMethod("GenerateOffers",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var progression=w.Progression!;
        List<UpgradeDefinition> Offers(int level,IEnumerable<UpgradeDefinition>? excluded=null)=>
            (List<UpgradeDefinition>)generate.Invoke(progression,new object[]{level,excluded??Array.Empty<UpgradeDefinition>(),3})!;
        TalentRuntime Profile(params string[] trees){
            var ranks=trees.SelectMany(tree=>TalentCatalog.Current.ForClass(cls).Where(t=>t.Tree==tree).Take(3)).ToDictionary(t=>t.Id,t=>1);
            check("offer fixture profile saves",TalentProfiles.Save(cls,ranks)==null);
            var runtime=new TalentRuntime(w);property.SetValue(w.Build,runtime);return runtime;
        }
        try{
            foreach(string tree in cls=="sniper"?new[]{"marksmanship","engineering","agent_lethality"}:new[]{"concealment","agent_lethality"}){
                var runtime=Profile(tree);
                string[] core=runtime.AgentEarlyAbilities;
                var preferred=new UpgradeDefinition {AbilityId=core[0],Effect=UpgradeEffect.UnlockAbility};
                var offTree=new UpgradeDefinition {AbilityId=tree=="marksmanship"?"plasmaprobe":cls=="sniper"?"ambush":"koltoprobe",Effect=UpgradeEffect.UnlockAbility};
                check(tree+" talent points weight the preferred kit without excluding hybrids",runtime.OfferWeight(preferred)>runtime.OfferWeight(offTree) && runtime.OfferWeight(offTree)>0);
                for(ulong seed=1;seed<=12;seed++){
                    w.Abilities.ResetUnlocks();progression.ResetRun();progression.SeedOffers(seed);
                    var expected=core.Where(id=>progression.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==id && progression.IsEligible(u))).Take(2).ToArray();
                    var offers=Offers(2);
                    check(tree+" opening supplies its two core unlocks seed "+seed,offers.Count==3 && expected.Length==2 && expected.All(id=>offers.Any(u=>u.AbilityId==id && u.Effect==UpgradeEffect.UnlockAbility)) && offers.All(progression.IsEligible));
                    var rerolled=Offers(2,offers);
                    check(tree+" reroll respects excluded unlocks seed "+seed,rerolled.Count==3 && rerolled.All(u=>!offers.Any(old=>old.Id==u.Id || old.Effect==UpgradeEffect.UnlockAbility && u.Effect==UpgradeEffect.UnlockAbility && old.AbilityId==u.AbilityId)));
                    w.Abilities.Unlock(expected[0]);
                    var later=Offers(5);
                    // A Sniper with only one core attack gets another unlock rather than Snipe filler.
                    int usefulPowers=progression.Upgrades.Count(u=>u.Effect is not (UpgradeEffect.UnlockAbility or UpgradeEffect.Health or UpgradeEffect.Speed) && progression.IsEligible(u));
                    int expectedUnlocks=cls=="sniper"?Math.Max(1,3-usefulPowers):1;
                    check(tree+" later offers retain a kit option and leave upgrade space seed "+seed,later.Count==3 && later.Count(u=>u.Effect==UpgradeEffect.UnlockAbility)==expectedUnlocks && later.Any(u=>u.Effect!=UpgradeEffect.UnlockAbility) && later.Any(u=>core.Contains(u.AbilityId)));
                }
            }
            if(cls=="sniper"){
                var hybrid=Profile("marksmanship","engineering");
                check("tied Sniper hybrid interleaves both kits",hybrid.AgentEarlyAbilities.Take(2).SequenceEqual(new[]{"suppressivefire","interrogationprobe"}));
                w.Abilities.ResetUnlocks();progression.ResetRun();var offers=Offers(2);
                check("hybrid opening offers both invested trees",offers.Any(u=>u.AbilityId=="suppressivefire") && offers.Any(u=>u.AbilityId=="interrogationprobe"));
            }
            check("blank talent profile saves",TalentProfiles.Save(cls,new Dictionary<string,int>())==null);
            property.SetValue(w.Build,new TalentRuntime(w));w.Abilities.ResetUnlocks();progression.ResetRun();
            check("blank profile gets offensive opening choices without starter filler",Offers(2).Count(u=>u.Effect==UpgradeEffect.UnlockAbility)==(cls=="sniper"?3:2));
        }finally{
            property.SetValue(w.Build,original);TalentProfiles.Save(cls,original.Ranks);
        }
    }
}
