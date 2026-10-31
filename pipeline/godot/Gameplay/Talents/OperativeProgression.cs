using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    // Single-axis scoped cards. Debilitate, Sever Tendon and Flashbang are stuns: no recovery card.
    private static readonly (string id,string title,float amount)[] OperativeDamageCards={
        ("backstab","Razor Edge",.9f),("hiddenstrike","Silent Execution",.9f),("laceration","Serrated Blades",.9f),("shiv","Quick Cuts",.9f),
        ("debilitate","Crippling Strike",.8f),("severtendon","Hamstring Cut",.9f),("dart","Potent Toxin",1f),("corrosivegrenade","Corrosive Cloud",1f),
        ("agentcull","Lethal Barrage",1f),("weakeningblast","Exploited Weakness",1f),("overloadshot","Close Quarters Payload",.9f),
        ("fragmentationgrenade","Fragmentation Cluster",.9f),("carbineburst","Burst Discipline",.9f)};
    private static readonly (string id,string title,float amount)[] OperativeRadiusCards={
        ("backstab","Wider Edge",.12f),("hiddenstrike","Wider Execution",.12f),("laceration","Wider Blades",.12f),("severtendon","Wider Cut",.12f),
        ("corrosivegrenade","Wider Cloud",.15f),("agentcull","Wider Barrage",.12f),("fragmentationgrenade","Wider Fragments",.12f)};
    private static readonly (string id,string title,float amount)[] OperativeCooldownCards={
        ("backstab","Quickened Edge",.18f),("hiddenstrike","Quickened Execution",.18f),("laceration","Quickened Blades",.18f),("shiv","Quickened Cuts",.18f),
        ("agentcull","Quickened Barrage",.18f),("weakeningblast","Quickened Weakness",.18f),("corrosivegrenade","Quickened Cloud",.18f),
        ("fragmentationgrenade","Quickened Fragments",.18f),("overloadshot","Quickened Payload",.18f)};
    // Laceration has no periodic component in this build, so only the poisons have a tick card.
    private static readonly (string id,string title)[] OperativeTickCards={("dart","Toxin Pulse"),("corrosivegrenade","Cloud Pulse")};

    public IEnumerable<UpgradeDefinition> OperativeRunPowers()
    {
        foreach(var u in AgentAxisCards("operative",OperativeDamageCards,OperativeRadiusCards,OperativeCooldownCards,OperativeTickCards))yield return u;
        UpgradeDefinition M(string id,string title,string icon,string ability,string spec,string key,float amount,string text)=>AgentMech("operative",id,title,icon,ability,spec,key,amount,text);
        // Concealment
        yield return M("flank","Flanking Rhythm","backstab","backstab","concealment","flank",1.5f,"Backstab recovers 1.5 seconds of Hidden Strike cooldown per rank, at most once per 0.5 seconds.");
        yield return M("bleed_echo","Venom Carrier","laceration","laceration","agent_lethality","venom",.25f,"Laceration echoes 25% of a tick per rank of your Dart and Corrosive Grenade on its victim, without consuming or refreshing them.");
        yield return M("cull_reset","Opportunist","agentcull","agentcull","agent_lethality","opportunist",1,"Cull hits recover 1 second per rank of Backstab and Laceration cooldown, once per second.");
        yield return M("weak_reset","Exposed Nerves","weakeningblast","weakeningblast","agent_lethality","exposed",1.5f,"Weakening Blast hits recover 1.5 seconds per rank of Backstab and Sever Tendon cooldown, once per second.");
        yield return M("kill_reset","Predator's Momentum","backstab","backstab","concealment","momentum",1,"Defeating an enemy recovers 1 second per rank of Backstab and Hidden Strike cooldown, at most once per second.");
        yield return M("gambit","Opening Gambit","hiddenstrike","hiddenstrike","concealment","gambit",.2f,"Leaving stealth with Hidden Strike opens a 4 second window in which all Concealment attacks deal 20% more per rank.");
        yield return M("hamstring","Hamstring Mark","severtendon","severtendon","concealment","hamstring",.15f,"Sever Tendon marks its victims for 6 seconds. Backstab and Laceration deal 15% more per rank to marked enemies; the mark spreads when a marked enemy dies.");
        yield return M("twin","Twin Slash","laceration","laceration","concealment","twin",0,"Laceration repeats once more per rank 0.25 seconds apart, each repeat at 50% damage.");
        yield return M("counter","Counter Surge","blowforblow","blowforblow","concealment","counter",.15f,"Blow for Blow reflects 15% more per rank and lasts 0.5 seconds longer per rank.");
        yield return M("exploit","Exploit the Stun","debilitate","","concealment","exploit",.15f,"Stunned or Debilitated enemies take 15% more per rank from Backstab, Hidden Strike, Laceration, Shiv and Sever Tendon.");
        yield return M("veil","Shadow Veil","agentstealth","","concealment","veil",.01f,"While your stealth opening is available, damage taken is divided by 1 plus 1% per rank.");
        yield return M("recovery","Close Quarters Recovery","backstab","","concealment","recovery",2,"Defeating an enemy heals 2 health per rank, at most once per second.");
        yield return M("venomous_echo","Venomous Echo","laceration","laceration","agent_lethality","venomous",.25f,"With Venomous Blades, each Laceration hit has a 25% chance per rank (100% guarantees one) to add another Dart instance to its victim, filling Dart Legion slots.");
        foreach(var u in AgentLethalityCards("operative"))yield return u;
        if(_world.Abilities.Find("shieldprobe") is {} shield)
            yield return new UpgradeDefinition {Id="operative_shield_shieldprobe",Title="Reinforced Shield",Icon=shield.Definition.Icon,AbilityId="shieldprobe",Spec="concealment",Effect=UpgradeEffect.AbilityPower,ShieldPerRank=.1f};
    }
    public bool OperativeUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true || id=="dart" && AgentVenomousBlades && _world.Abilities.Find("laceration")?.Unlocked==true;
        bool Poison()=>Has("dart") || Has("corrosivegrenade");
        bool Melee()=>Has("backstab") || Has("laceration") || Has("hiddenstrike");
        if(u.Mechanic.StartsWith("opm_dup_",StringComparison.Ordinal) || u.Mechanic.StartsWith("opm_multi_",StringComparison.Ordinal))return Has(u.AbilityId);
        return u.Mechanic switch {
            "opm_flank"=>Has("backstab") && Has("hiddenstrike"),
            "opm_venom"=>Has("laceration") && Poison(),
            "opm_opportunist"=>Has("agentcull") && (Has("backstab") || Has("laceration")),
            "opm_exposed"=>Has("weakeningblast") && (Has("backstab") || Has("severtendon")),
            "opm_momentum"=>Has("backstab") || Has("hiddenstrike"),
            "opm_gambit"=>Has("hiddenstrike") && (Has("backstab") || Has("laceration")),
            "opm_hamstring"=>Has("severtendon") && (Has("backstab") || Has("laceration")),
            "opm_twin"=>Has("laceration"),
            "opm_counter"=>AgentCounterstrikeReady,
            "opm_exploit"=>(Has("debilitate") || Has("severtendon") || Has("flashbang")) && (Melee() || Has("shiv")),
            "opm_veil"=>Has("hiddenstrike"),
            "opm_recovery"=>Melee() || Has("shiv"),
            "opm_venomous"=>AgentVenomousBlades && Has("laceration"),
            "opm_pandemic" or "opm_mending"=>Poison(),
            "opm_septic"=>Has("agentcull") && Poison(),
            "opm_tempo"=>Poison() && (Has("agentcull") || Has("weakeningblast")),
            _=>true
        };
    }
    private void AgentMechanicHit(AutomaticAbility a,Combatant target)
    {
        string id=a.Definition.Id;
        void Cut(float seconds,params string[] abilities){if(seconds>0)foreach(var next in abilities)_world.Abilities.Find(next)?.ReduceCooldown(seconds);}
        if(IsOperative) {
            if(id=="backstab" && AgP("flank") is var flank && flank>0 && Ready("opm_flank",.5))Cut(1.5f*flank,"hiddenstrike");
            if(id=="laceration" && target.Alive && AgP("venom") is var venom && venom>0)
            {
                float echo=_world.Periodic.On(target).Where(x=>x.Caster==a.Caster && x.Definition.Id is "dart" or "corrosivegrenade")
                    .Sum(x=>x.Definition.PeriodicDamage*x.DamagePerTick/Math.Max(.001f,x.Definition.Damage))*.25f*venom;
                if(echo>0)target.Health.ApplyDamage(echo,DamageType.Internal,abilityId:id);
            }
            if(id=="agentcull" && AgP("opportunist") is var opp && opp>0 && Ready("opm_opportunist",1))Cut(opp,"backstab","laceration");
            if(id=="weakeningblast" && AgP("exposed") is var exposed && exposed>0 && Ready("opm_exposed",1))Cut(1.5f*exposed,"backstab","severtendon");
        }
        if(id=="fragmentationgrenade" && IsSniper && AgP("frag_probe") is var frag && frag>0 && Ready("snm_frag_probe",.5))Cut(1.5f*frag,"explosiveprobe");
        AgentCardsHit(a,target);
    }
    public void Defeated(Combatant dead)
    {
        SorcererKilled(dead);
        if(IsOperative && AgP("momentum") is var momentum && momentum>0 && Ready("opm_momentum",1)){_world.Abilities.Find("backstab")?.ReduceCooldown(momentum);_world.Abilities.Find("hiddenstrike")?.ReduceCooldown(momentum);}
        if(IsSniper && AgP("kill_reset") is var kill && kill>0 && Ready("snm_kill_reset",1)){_world.Abilities.Find("ambush")?.ReduceCooldown(kill);_world.Abilities.Find("followthrough")?.ReduceCooldown(kill);}
        AgentCardsDefeated(dead);
    }
}
