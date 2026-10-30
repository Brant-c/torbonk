using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    private string[] AgentTrees=>IsOperative?new[]{"concealment","agent_lethality","medicine"}:new[]{"marksmanship","engineering","agent_lethality"};
    private int AgentPoints(string tree)=>TalentCatalog.Current.ForClass(_world.RunConfiguration.AdvancedClassId).Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
    private string[] AgentCore(string tree)=>tree switch {
        "marksmanship"=>new[]{"suppressivefire","ambush","seriesofshots","followthrough"},
        "engineering"=>new[]{"interrogationprobe","explosiveprobe","plasmaprobe","empdischarge","orbitalstrike","fragmentationgrenade"},
        "agent_lethality"=>new[]{"dart","corrosivegrenade","agentcull","weakeningblast"},
        "concealment"=>new[]{"debilitate","backstab","laceration","severtendon","hiddenstrike"},
        _=>new[]{"backstab","corrosivegrenade","laceration"}
    };
    public string[] AgentEarlyAbilities {
        get {
            var trees=AgentTrees;int most=trees.Max(AgentPoints);
            if(most==0)return Array.Empty<string>();
            // Tied hybrids interleave both cores instead of silently choosing Marksmanship.
            var cores=trees.Where(t=>AgentPoints(t)==most).Select(AgentCore).ToArray();
            return Enumerable.Range(0,cores.Max(c=>c.Length)).SelectMany(i=>cores.Where(c=>i<c.Length).Select(c=>c[i])).Distinct().ToArray();
        }
    }
    private string[] AgentAbilityTrees(string id)=>id switch {
        "snipershot" or "basicrifle" or "ambush" or "suppressivefire" or "followthrough"=>new[]{"marksmanship"},
        "seriesofshots"=>new[]{"marksmanship","engineering"},
        "interrogationprobe" or "explosiveprobe" or "plasmaprobe" or "empdischarge" or "orbitalstrike" or "fragmentationgrenade"=>new[]{"engineering"},
        "dart" or "corrosivegrenade" or "agentcull" or "weakeningblast"=>new[]{"agent_lethality"},
        "shiv" or "backstab" or "hiddenstrike" or "laceration" or "debilitate" or "severtendon" or "blowforblow" or "agentstealth" or "cloakingscreen"=>new[]{"concealment"},
        "koltoprobe" or "koltoinjection" or "surgicalprobe" or "stimboost"=>new[]{"medicine"},
        _=>Array.Empty<string>()
    };
    private float AgentOfferWeight(UpgradeDefinition u)
    {
        var trees=AgentTrees.Contains(u.Spec)?new[]{u.Spec}:AgentAbilityTrees(u.AbilityId);
        int investment=trees.Select(AgentPoints).DefaultIfEmpty(0).Max();
        float affinity=_effects.Where(p=>p.effect.Ability!="" && p.effect.Ability==u.AbilityId).Sum(p=>p.rank);
        // Same talent-point preference model as Marauder. Off-tree choices retain weight 1.
        return 1+.4f*investment+.1f*affinity;
    }
}
