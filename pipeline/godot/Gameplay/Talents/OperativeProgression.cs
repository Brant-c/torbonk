using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    // Assassin-scale repeatable cards: damage may climb, recovery and area stay modest.
    public IEnumerable<UpgradeDefinition> OperativeRunPowers()
    {
        foreach(var (id,title,damage,radius,recovery) in new (string,string,float,float,float)[] {
            ("backstab","Razor Edge",.8f,.1f,.1f),
            ("hiddenstrike","Silent Execution",.9f,0,.12f),
            ("laceration","Serrated Blades",.8f,.12f,.1f),
            ("debilitate","Crippling Strike",.7f,.1f,.1f),
            ("severtendon","Hamstring Cut",.8f,0,.1f),
            ("dart","Potent Toxin",1f,0,.1f),
            ("corrosivegrenade","Corrosive Cloud",1f,.15f,0),
            ("agentcull","Lethal Barrage",1f,.12f,.1f),
            ("weakeningblast","Exploited Weakness",1f,0,.15f),
            ("overloadshot","Close Quarters Payload",1f,0,.15f),
            ("fragmentationgrenade","Fragmentation Cluster",.8f,.15f,.1f),
            ("carbineburst","Burst Discipline",.8f,0,.1f) }) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            string text=$"{a.Definition.Title}: +{damage:P0} base damage"+(a.Definition.PeriodicDamage>0?" (including periodic damage)":"");
            if(radius>0)text+=$", +{radius:P0} base radius";
            if(recovery>0)text+=$", +{recovery:P0} cooldown recovery";
            yield return new UpgradeDefinition {Id="operative_power_"+id,Title=title,Icon=a.Definition.Icon,
                AbilityId=id,Spec=AgentAbilityTrees(id).FirstOrDefault()??"",Effect=UpgradeEffect.AbilityPower,
                DamagePerRank=damage,RadiusPerRank=radius,CooldownPerRank=recovery,
                MechanicalDescription=text+" per rank. Bonuses add from base and scale with the card's rolled rarity; no rank cap."};
        }
        foreach(var (key,title,id,text) in new (string,string,string,string)[] {
            ("op_flank","Flanking Rhythm","backstab","Backstab recovers 2 seconds of Hidden Strike cooldown, at most once per 0.5 seconds."),
            ("op_bleed_echo","Venom Carrier","laceration","Laceration echoes half a tick of your Dart and Corrosive Grenade on its victim, without consuming or refreshing them."),
            ("op_cull_reset","Opportunist","agentcull","Cull hits recover 1 second of Backstab and Laceration cooldown, once per second."),
            ("op_weak_reset","Exposed Nerves","weakeningblast","Weakening Blast hits recover 2 seconds of Debilitate and Sever Tendon cooldown, once per second."),
            ("op_kill_reset","Predator's Momentum","backstab","Defeating an enemy recovers 1 second of Backstab and Hidden Strike cooldown, at most once per second.") })
            yield return new UpgradeDefinition {Id="operative_"+key[3..],Title=title,AbilityId=id,Icon=_world.Abilities.Find(id)?.Definition.Icon??"",
                Effect=UpgradeEffect.Mechanic,Mechanic=key,MaximumRank=1,MechanicalDescription=text};
    }
    public IEnumerable<UpgradeDefinition> SniperExtraMechanics()
    {
        foreach(var (key,title,id,text) in new (string,string,string,string)[] {
            ("sn_kill_reset","Clean Kill","ambush","Defeating an enemy recovers 1 second of Ambush and Followthrough cooldown, at most once per second."),
            ("sn_frag_probe","Shrapnel Feed","fragmentationgrenade","Fragmentation Grenade hits recover 1.5 seconds of Explosive Probe cooldown, at most once per 0.5 seconds.") })
            yield return new UpgradeDefinition {Id="sniper_"+key[3..],Title=title,AbilityId=id,Icon=_world.Abilities.Find(id)?.Definition.Icon??"",
                Effect=UpgradeEffect.Mechanic,Mechanic=key,MaximumRank=1,MechanicalDescription=text};
    }
    private bool Ag(string key)=>IsAgent && (_world.Progression?.Upgrades.Any(u=>u.Mechanic==key && _world.Progression.Rank(u)>0)??false);
    private void AgentMechanicHit(AutomaticAbility a,Combatant target)
    {
        string id=a.Definition.Id;
        void Cut(float seconds,params string[] abilities){foreach(var next in abilities)_world.Abilities.Find(next)?.ReduceCooldown(seconds);}
        if(id=="backstab" && Ag("op_flank") && Ready("op_flank",.5))Cut(2,"hiddenstrike");
        if(id=="laceration" && Ag("op_bleed_echo") && target.Alive)
        {
            float echo=_world.Periodic.On(target).Where(x=>x.Caster==a.Caster && x.Definition.Id is "dart" or "corrosivegrenade")
                .Sum(x=>x.Definition.PeriodicDamage*x.DamagePerTick/Math.Max(.001f,x.Definition.Damage))*.5f;
            if(echo>0)target.Health.ApplyDamage(echo,DamageType.Internal,abilityId:id);
        }
        if(id=="agentcull" && Ag("op_cull_reset") && Ready("op_cull_reset",1))Cut(1,"backstab","laceration");
        if(id=="weakeningblast" && Ag("op_weak_reset") && Ready("op_weak_reset",1))Cut(2,"debilitate","severtendon");
        if(id=="fragmentationgrenade" && Ag("sn_frag_probe") && Ready("sn_frag_probe",.5))Cut(1.5f,"explosiveprobe");
    }
    public void Defeated(Combatant dead)
    {
        SorcererKilled(dead);
        if(Ag("op_kill_reset") && Ready("op_kill_reset",1)){_world.Abilities.Find("backstab")?.ReduceCooldown(1);_world.Abilities.Find("hiddenstrike")?.ReduceCooldown(1);}
        if(Ag("sn_kill_reset") && Ready("sn_kill_reset",1)){_world.Abilities.Find("ambush")?.ReduceCooldown(1);_world.Abilities.Find("followthrough")?.ReduceCooldown(1);}
    }
}
