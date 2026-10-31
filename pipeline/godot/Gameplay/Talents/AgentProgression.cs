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

    // Single-axis mechanic cards (snm_ / opm_) read their rarity-weighted rank power straight from the upgrade list.
    private string AgentMechPrefix=>IsSniper?"snm_":"opm_";
    private bool AgentUsable(UpgradeDefinition u)=>IsSniper?SniperUpgradeUsable(u):OperativeUpgradeUsable(u);
    private IEnumerable<UpgradeDefinition> AgCards(string key)
    {
        var progression=_world.Progression;
        if(!IsAgent || progression==null)yield break;
        string mechanic=AgentMechPrefix+key;
        foreach(var u in progression.Upgrades)
            if(u.Mechanic==mechanic && progression.Rank(u)>0 && AgentUsable(u))yield return u;
    }
    private float AgP(string key)=>AgCards(key).Sum(u=>_world.Progression!.RankPower(u));
    private int AgR(string key)=>AgCards(key).Sum(u=>_world.Progression!.Rank(u));
    public float AgentDuplicateChance(AutomaticAbility a)=>IsAgent && a.Definition.Id is "dart" or "corrosivegrenade" or "interrogationprobe"?AgP("dup_"+a.Definition.Id)*.2f:0;
    public int AgentDotSlots(AutomaticAbility a)=>IsAgent && a.Definition.Id is "dart" or "corrosivegrenade" or "interrogationprobe"?1+AgR("multi_"+a.Definition.Id):1;
    public bool AgentAxisCard(UpgradeDefinition u)=>u.Effect==UpgradeEffect.TickRate || u.Effect==UpgradeEffect.AbilityPower && (u.RadiusPerRank>0 || u.CooldownPerRank>0) || u.Mechanic.StartsWith(AgentMechPrefix+"target_",StringComparison.Ordinal);
    public bool AgentDefenceCard(UpgradeDefinition u)=>u.ShieldPerRank>0 || u.Effect==UpgradeEffect.Mechanic && u.Mechanic is "snm_dampers" or "snm_focus" or "snm_probeshield" or "snm_mending" or "opm_veil" or "opm_recovery" or "opm_mending";
    private string AgentSpec(string id)=>AgentAbilityTrees(id).FirstOrDefault()??"";
    private bool OwnsOrVenomous(string id)=>Owns(id) || id=="dart" && AgentVenomousBlades && Owns("laceration");

    private IEnumerable<UpgradeDefinition> AgentAxisCards(string cls,(string id,string title,float amount)[] damage,(string id,string title,float amount)[] radius,
        (string id,string title,float amount)[] cooldown,(string id,string title)[] tick)
    {
        foreach(var (id,title,amount) in damage) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id=cls+"_power_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Spec=AgentSpec(id),Effect=UpgradeEffect.AbilityPower,DamagePerRank=amount};
        }
        foreach(var (id,title,amount) in radius) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id=cls+"_radius_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Spec=AgentSpec(id),Effect=UpgradeEffect.AbilityPower,RadiusPerRank=amount};
        }
        foreach(var (id,title,amount) in cooldown) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id=cls+"_cooldown_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Spec=AgentSpec(id),Effect=UpgradeEffect.AbilityPower,CooldownPerRank=amount};
        }
        foreach(var (id,title) in tick) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id=cls+"_tick_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Spec=AgentSpec(id),Effect=UpgradeEffect.TickRate,AmountPerRank=.15f};
        }
    }

    private UpgradeDefinition AgentMech(string cls,string id,string title,string iconAbility,string ability,string spec,string key,float amount,string text)
    {
        string prefix=cls=="sniper"?"snm_":"opm_";
        return new UpgradeDefinition {Id=cls+"_"+id,Title=title,Icon=_world.Abilities.Find(iconAbility)?.Definition.Icon??_world.Abilities.Find("dart")?.Definition.Icon??"dart",
            AbilityId=ability,Spec=spec,Effect=UpgradeEffect.Mechanic,Mechanic=prefix+key,AmountPerRank=amount,MaximumRank=0,
            MechanicalDescription=amount>0?text+" Scales with rarity.":text};
    }

    // Lethality is shared by both classes; each class gets its own prefixed copy so Scalable() and the usable checks stay per class.
    private IEnumerable<UpgradeDefinition> AgentLethalityCards(string cls)
    {
        foreach(var (id,title) in new[]{("dart","Dart"),("corrosivegrenade","Grenade"),("interrogationprobe","Probe")}) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return AgentMech(cls,"echo_"+id,title+" Echo",id,id,"" ,"dup_"+id,.2f,$"{a.Definition.Title}: each tick has a +20% chance per rank to strike again (100% guarantees an extra tick).").WithSpec(AgentSpec(id));
            yield return AgentMech(cls,"legion_"+id,title+" Legion",id,id,"","multi_"+id,0,$"{a.Definition.Title}: +1 simultaneous instance per rank on each enemy. Extra casts add a new instance instead of refreshing, until slots are full.").WithSpec(AgentSpec(id));
        }
        yield return AgentMech(cls,"pandemic","Pandemic","dart","","agent_lethality","pandemic",0,"When your Dart or Corrosive Grenade expires or its host dies, it jumps to one more nearby unpoisoned enemy per rank. At most once per 2 seconds per poison.");
        yield return AgentMech(cls,"septic","Septic Detonation","agentcull","agentcull","agent_lethality","septic",.35f,"Cull pays out 35% per rank of up to 6 seconds of remaining Dart and Corrosive Grenade damage on its target, then leaves 2 seconds of each.");
        yield return AgentMech(cls,"tempo","Toxic Tempo","agentcull","","agent_lethality","tempo",.15f,"Each Dart or Corrosive Grenade tick recovers 0.15 seconds per rank of Cull and Weakening Blast cooldown, at most once every 0.5 seconds.");
        yield return AgentMech(cls,"mending","Toxic Mending","dart","","agent_lethality","mending",1,"Dart and Corrosive Grenade ticks heal 1 health per rank, at most once per second.");
    }
}
internal static class UpgradeSpecExtensions
{
    public static UpgradeDefinition WithSpec(this UpgradeDefinition u,string spec){u.Spec=spec;return u;}
}
