using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    private bool IsSniper=>_world.RunConfiguration.AdvancedClassId=="sniper";
    private double _snVolley;
    private bool _snWeakeningEmpowered,_snPoisonCover;
    public IEnumerable<UpgradeDefinition> SniperRunPowers()
    {
        foreach(var (id,title,damage,radius,recovery) in new (string,string,float,float,float)[] {
            ("ambush","High Caliber",.8f,0,.12f),
            ("seriesofshots","Sustained Barrage",.8f,0,.1f),
            ("followthrough","Finishing Salvo",.8f,0,.12f),
            ("suppressivefire","Saturation Fire",.8f,.15f,0),
            ("orbitalstrike","Heavy Ordnance",.8f,.12f,.15f),
            ("plasmaprobe","Plasma Saturation",.8f,.15f,0),
            ("explosiveprobe","Overcharged Payload",.9f,.12f,.1f),
            ("interrogationprobe","Interrogation Matrix",.8f,.12f,.1f),
            ("dart","Concentrated Toxin",1f,0,.1f),
            ("corrosivegrenade","Corrosive Cloud",1f,.15f,0),
            ("agentcull","Lethal Barrage",1f,.12f,.1f),
            ("weakeningblast","Exploited Weakness",1f,0,.15f),
            ("fragmentationgrenade","Fragmentation Cluster",.8f,.15f,.1f),
            ("overloadshot","Close Quarters Payload",1f,0,.15f) }) {
            var a=_world.Abilities.Find(id)!;
            string text=$"{a.Definition.Title}: +{damage:P0} base damage"+(a.Definition.PeriodicDamage>0?" (including periodic damage)":"");
            if(radius>0)text+=$", +{radius:P0} base radius";
            if(recovery>0)text+=$", +{recovery:P0} cooldown recovery";
            yield return new UpgradeDefinition {Id="sniper_power_"+id,Title=title,Icon=a.Definition.Icon,
                AbilityId=id,Spec=AgentAbilityTrees(id).FirstOrDefault()??"",Effect=UpgradeEffect.AbilityPower,
                DamagePerRank=damage,RadiusPerRank=radius,CooldownPerRank=recovery,
                MechanicalDescription=text+" per rank. Bonuses add from base; no rank cap."};
        }
        foreach(var (key,title,id,spec,text) in new (string,string,string,string,string)[] {
            ("sn_crossfire","Crossfire Discipline","ambush","marksmanship","Ambush recovers Followthrough by 2 seconds. Followthrough recovers Ambush by 3 seconds. Each refund happens once per activation."),
            ("sn_ricochet","Penetrating Rounds","","marksmanship","Ambush, Series of Shots and Followthrough gain two ricochets and retain at least 90% damage per jump. Stacks with Focused Barrage; each enemy is hit once per shot."),
            ("sn_suppression","Targeted Suppression","suppressivefire","engineering","Suppressive Fire and Plasma Probe deal 60% additional damage to enemies carrying your Interrogation Probe."),
            ("sn_bombardment","Coordinated Bombardment","orbitalstrike","engineering","Activating Suppressive Fire recovers Orbital Strike by 4 seconds. Orbital Strike gains 30% base radius."),
            ("sn_overload","Probe Overload","explosiveprobe","engineering","Explosive Probe detonates each victim's own Interrogation Probe for its remaining scheduled damage, then reapplies it to survivors. Unmarked survivors receive a probe. Other casters' probes are untouched."),
            ("sn_emp","Overclocked EMP","empdischarge","engineering","EMP Discharge also recovers Explosive Probe and Orbital Strike by 6 seconds. Adds to existing EMP and talent recovery; once per activation."),
            ("sn_targeting","Probe Targeting","interrogationprobe","engineering","Series of Shots and Explosive Probe deal 50% additional damage to enemies carrying your Interrogation Probe."),
            ("sn_cull","Concentrated Volley","agentcull","agent_lethality","Cull gains 50% base splash radius and full secondary damage. Activating Cull empowers the next Weakening Blast within 6 seconds for 100% additional damage, consumed once per activation."),
            ("sn_decay","Zeroing In","","agent_lethality","While in cover, your Dart and Corrosive Grenade tick 35% faster without shortening duration. Includes running effects and spread copies; returns to normal when you leave cover."),
            ("sn_weakening","Virulent Weakness","weakeningblast","agent_lethality","Weakening Blast echoes one tick of each of your Dart and Corrosive Grenade effects on its victim without consuming or refreshing them."),
            ("sn_contamination","Contaminated Rounds","ambush","agent_lethality","Ambush applies your learned Dart to unpoisoned victims. Against victims already carrying your Dart, it echoes half a poison tick without consuming or refreshing it. Each ricochet can affect its own victim."),
            ("sn_entrench","Entrenched Salvo","entrench","marksmanship","During Entrench, Ambush, Series of Shots, Followthrough and Suppressive Fire gain 40% additional damage. Activating Entrench recovers Series of Shots and Suppressive Fire by 3 seconds.") }) {
            string iconId=id!=""?id:key=="sn_decay"?"dart":"seriesofshots";
            yield return new UpgradeDefinition {Id="sniper_"+key[3..],Title=title,AbilityId=key=="sn_suppression"?"":id,Spec=spec,
                Icon=_world.Abilities.Find(iconId)!.Definition.Icon,Effect=UpgradeEffect.Mechanic,
                Mechanic=key,MaximumRank=1,MechanicalDescription=text};
        }
    }
    public bool SniperUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Poison()=>Has("dart") || Has("corrosivegrenade");
        bool Precision()=>new[]{"ambush","seriesofshots","followthrough"}.Any(Has);
        return u.Mechanic switch {
            "sn_crossfire"=>Has("ambush") && Has("followthrough"),
            "sn_ricochet"=>Precision(),
            "sn_suppression"=>Has("interrogationprobe") && (Has("suppressivefire") || Has("plasmaprobe")),
            "sn_bombardment"=>Has("suppressivefire") && Has("orbitalstrike"),
            "sn_overload"=>Has("explosiveprobe") && Has("interrogationprobe"),
            "sn_emp"=>Has("empdischarge") && (Has("explosiveprobe") || Has("orbitalstrike")),
            "sn_targeting"=>Has("interrogationprobe") && (Has("seriesofshots") || Has("explosiveprobe")),
            "sn_cull"=>Has("agentcull") && Has("weakeningblast") && Poison(),
            "sn_decay"=>Poison(),
            "sn_weakening"=>Has("weakeningblast") && Poison(),
            "sn_contamination"=>Has("ambush") && Has("dart"),
            "sn_entrench"=>Has("entrench") && (Precision() || Has("suppressivefire")),
            _=>true
        };
    }
    private bool Sn(string key)=>IsSniper && (_world.Progression?.Upgrades.Any(u=>u.Mechanic==key && _world.Progression.Rank(u)>0 && SniperUpgradeUsable(u))??false);
    private void SniperStats(AutomaticAbility a)
    {
        if(!IsSniper)return;
        string id=a.Definition.Id;
        if(id is "ambush" or "seriesofshots" or "followthrough") {
            a.ExtraBounces+=Sn("sn_ricochet")?2:0;
            a.Definition.BounceFalloff=Sn("sn_ricochet")?Math.Max(.9f,SniperBase(id).BounceFalloff):SniperBase(id).BounceFalloff;
        }
        if(id=="orbitalstrike" && Sn("sn_bombardment"))a.Radius*=1.3f;
        if(id=="agentcull") {
            a.Definition.SplashMultiplier=Sn("sn_cull")?1:SniperBase(id).SplashMultiplier;
            if(Sn("sn_cull"))a.Radius*=1.5f;
        }
        if(id is "dart" or "corrosivegrenade" && AgentPositioned && Sn("sn_decay"))a.TickSeconds/=1.35f;
        if(id=="weakeningblast") {
            bool spread=Hook("sn_lethal_spread")>0;
            a.Definition.Area=spread?AbilityArea.TargetSplash:SniperBase(id).Area;
            a.Definition.SpreadsPeriodicEffects=spread;
            a.Radius=spread?.3f:a.Radius;
            a.Definition.SplashMultiplier=spread?1:SniperBase(id).SplashMultiplier;
        }
    }
    private static AbilityDefinition SniperBase(string id)=>GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{id}.tres");
    private void SniperActivated(AutomaticAbility a)
    {
        if(!IsSniper)return;
        void Recover(string id,float seconds){if(_world.Abilities.Find(id) is {Unlocked:true} next)next.ReduceCooldown(seconds);}
        if(Sn("sn_crossfire")) {
            if(a.Definition.Id=="ambush")Recover("followthrough",2);
            if(a.Definition.Id=="followthrough")Recover("ambush",3);
        }
        if(a.Definition.Id=="suppressivefire" && Sn("sn_bombardment"))Recover("orbitalstrike",4);
        if(a.Definition.Id=="empdischarge" && Sn("sn_emp")){Recover("explosiveprobe",6);Recover("orbitalstrike",6);}
        if(a.Definition.Id=="entrench" && Sn("sn_entrench")){Recover("seriesofshots",3);Recover("suppressivefire",3);}
        if(a.Definition.Id=="agentcull" && Sn("sn_cull"))_snVolley=6;
        if(a.Definition.Id=="weakeningblast"){_snWeakeningEmpowered=_snVolley>0 && Sn("sn_cull");_snVolley=0;}
    }
    private float SniperBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsSniper)return 0;
        string id=a.Definition.Id;
        bool probe=_world.Periodic.On(t).Any(p=>p.Caster==a.Caster && p.Definition.Id=="interrogationprobe");
        return (probe && Sn("sn_suppression") && id is "suppressivefire" or "plasmaprobe"?.6f:0)
            +(probe && Sn("sn_targeting") && id is "seriesofshots" or "explosiveprobe"?.5f:0)
            +(AgentPositioned && Hook("sn_lethal_cover")>0 && id is "dart" or "corrosivegrenade"?.2f:0)
            +(id=="weakeningblast" && _snWeakeningEmpowered && Sn("sn_cull")?1:0)
            +(_agentGuard>0 && Sn("sn_entrench") && id is "ambush" or "seriesofshots" or "followthrough" or "suppressivefire"?.4f:0);
    }
    private void SniperHit(AutomaticAbility a,Combatant t)
    {
        if(!IsSniper || !t.Alive)return;
        string id=a.Definition.Id;
        if(id=="explosiveprobe" && Sn("sn_overload") && _world.Abilities.Find("interrogationprobe") is {} probe) {
            if(_world.Periodic.DetonateOwned(probe,t,"probe_overload",out _) && a.Presentation is D3AbilityPresentation v)v.TargetImpact(t);
            if(t.Alive)_world.Periodic.Apply(probe,t);
        }
        if(id=="weakeningblast" && Hook("sn_lethal_extend")>0)
            foreach(var poison in _world.Periodic.On(t).Where(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade"))
                _world.Periodic.Extend(poison,t,2,PeriodicDuration(poison)+4);
        float echo=id=="weakeningblast" && Sn("sn_weakening")?1:id=="agentcull" && Hook("sn_lethal_echo")>0?.25f:0;
        if(echo>0) {
            float damage=_world.Periodic.On(t).Where(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade")
                .Sum(p=>p.Definition.PeriodicDamage*p.DamagePerTick/Math.Max(.001f,p.Definition.Damage)*_world.Build.DamageMultiplier(p,t)*PeriodicMultiplier(t));
            if(damage>0)t.Health.ApplyDamage(damage*echo,DamageType.Internal,abilityId:id);
        }
        if(id=="agentcull" && AgentPositioned && Hook("sn_lethal_focus")>0 && Ready("sn_lethal_shield",6))_world.PlayerActor.Health.ApplyShield(8,6);
        if(id=="weakeningblast" && Hook("sn_lethal_focus")>0 && Ready("sn_lethal_recovery",2))_world.Abilities.Find("shieldprobe")?.ReduceCooldown(1);
        if(id=="ambush" && Sn("sn_contamination") && _world.Abilities.Find("dart") is {} dart) {
            if(!_world.Periodic.Has(dart,t))_world.Periodic.Apply(dart,t);
            else t.Health.ApplyDamage(dart.Definition.PeriodicDamage*dart.DamagePerTick/Math.Max(.001f,dart.Definition.Damage)*.5f*_world.Build.DamageMultiplier(dart,t)*PeriodicMultiplier(t),DamageType.Internal,abilityId:"ambush");
        }
    }
    private void SniperPoisonTick()
    {
        if(!IsSniper || !AgentPositioned || Hook("sn_lethal_alignment")==0 || !Ready("sn_lethal_alignment",2))return;
        _world.Abilities.Find("agentcull")?.ReduceCooldown(.75f);_world.Abilities.Find("weakeningblast")?.ReduceCooldown(.5f);
    }
    private void AdvanceSniper(double delta)
    {
        if(!IsSniper)return;
        _snVolley=Math.Max(0,_snVolley-delta);
        bool covered=AgentPositioned;
        if(covered!=_snPoisonCover){_snPoisonCover=covered;if(Sn("sn_decay"))_world.Progression?.Recalculate();}
    }
    private void ResetSniper(){_snVolley=0;_snWeakeningEmpowered=_snPoisonCover=false;}
}
