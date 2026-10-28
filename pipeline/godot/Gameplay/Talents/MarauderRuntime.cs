using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    private float Mr(string key)=>Math.Max(Hook(key),_world.Build.Talent(key));
    public int MarauderTalentRank(string key)=>(int)Hook(key);
    private bool IsMarauder=>_world.RunConfiguration.AdvancedClassId=="marauder";
    private double _mrBerserk,_mrPredation,_mrCamo,_mrObfuscate,_mrPain,_mrDeadly,_mrGore,_mrFrenzy,_mrPayoff;
    private int _mrCharges,_mrCrush;
    private float _mrSmashBonus;
    private int _mrAssault;
    private int MrPoints(string tree)=>TalentCatalog.Current.ForClass("marauder").Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
    public bool MarauderAuraActive(string id)=>IsMarauder && (id switch {"predation"=>_mrPredation>0,"berserk"=>_mrBerserk>0,"frenzy"=>_mrFrenzy>0,"forcecamouflage"=>_mrCamo>0,"obfuscate"=>_mrObfuscate>0,"cloakofpain"=>_mrPain>0,"deadlysaber"=>_mrDeadly>0 && _mrCharges>0,"gore"=>_mrGore>0,"batteringassault"=>_mrAssault>0,_=>false});
    public string[] MarauderEarlyAbilities=>MrPoints("carnage")>MrPoints("annihilation") && MrPoints("carnage")>MrPoints("marauder_rage")
        ?new[]{"batteringassault","massacre","gore","ravage","sweepingslash"}
        :MrPoints("marauder_rage")>MrPoints("annihilation")?new[]{"forcecrush","smash","forcescream","saberthrow","intimidatingroar"}
        :new[]{"rupture","sweepingslash","ravage","annihilate","deadlysaber","forcecrush","saberthrow"};
    private bool MrWeapon(AutomaticAbility a)=>a.Definition.PresentationKind=="saber" && a.Definition.Effect==AbilityEffect.Damage;
    public float MarauderSpeed=>!IsMarauder?1:_mrPredation>0?1.45f:_mrCamo>0?1.35f:1;
    private float MarauderIncoming=>!IsMarauder?1:_mrCamo>0?.35f:_mrObfuscate>0?.7f:_mrPain>0?.8f:_mrPredation>0?.85f:1;
    public bool CanBeginMarauder(AutomaticAbility a)=>!IsMarauder || (a.Definition.Id switch {
        "frenzy"=>_world.Abilities.Find("berserk") is {Unlocked:true,CooldownRemaining:>5},
        "deadlysaber"=>_mrDeadly<=0 || _mrCharges<=0,
        "forcecamouflage"=>_world.PlayerActor.Health.Current<_world.PlayerActor.Health.Maximum*.5f,
        "obfuscate" or "cloakofpain"=>_world.PlayerActor.Health.Current<_world.PlayerActor.Health.Maximum*.8f,
        "forcecharge" or "obliterate"=>false,
        "rupture" or "forcecrush"=>!_world.Periodic.SpreadSources(a.Caster,a.Targeting.InArea(a.Caster.GlobalPosition,a.Range,a.Range)).Contains(a) || MarauderDotTarget(a)!=null && _world.Abilities.Find("sweepingslash")?.Unlocked!=true && _world.Abilities.Find("smash")?.Unlocked!=true,
        _=>true});
    // Cooldown reduction creates opportunities, not permission to overwrite healthy DoTs.
    public Combatant? MarauderDotTarget(AutomaticAbility a)=>a.Targeting.InArea(a.Caster.GlobalPosition,a.Range,a.Range)
        .Where(t=>_world.Periodic.Remaining(a,t)<=.65)
        .OrderBy(t=>_world.Periodic.Has(a,t)?1:0)
        .ThenBy(t=>t.GlobalPosition.DistanceSquaredTo(a.Caster.GlobalPosition))
        .ThenBy(t=>t.SelectionKey,StringComparer.Ordinal).FirstOrDefault();
    public int MarauderPriority(AutomaticAbility a)
    {
        if(!IsMarauder)return 0;
        // Spread a seeded effect before spending another global cooldown applying it by hand.
        if(a.Definition.Id=="sweepingslash" && a.Definition.SpreadsPeriodicEffects) {
            var primary=a.Targeting.Acquire(a.Range,a.Definition.TargetRule,a.Radius);
            if(primary!=null){
                var forward=primary.GlobalPosition-a.Caster.GlobalPosition;forward.Y=0;
                var targets=a.Targeting.InArea(a.Caster.GlobalPosition,a.Radius,a.Radius).Where(t=>{
                    var d=t.GlobalPosition-a.Caster.GlobalPosition;d.Y=0;
                    return d.LengthSquared()<.00001f || forward.Normalized().Dot(d.Normalized())>=.7071f;
                }).ToArray();
                if(targets.Length>1 && _world.Periodic.SpreadSources(a.Caster,targets).Where(source=>MarauderMaySpread(a,source)).Any(source=>targets.Any(t=>!_world.Periodic.Has(source,t))))return -4;
            }
        }
        if(a.Definition.Id is "rupture" or "forcecrush")return MarauderDotTarget(a)!=null?-2:0;
        if(a.Definition.Id=="viciousslash")return 1; // Starter fills gaps after acquired attacks.
        return 0;
    }
    private void MarauderStats(AutomaticAbility a)
    {
        if(!IsMarauder)return;
        float haste=1+(_mrBerserk>0?.3f:0)+Mr("mr_alacrity");
        a.TickSeconds/=haste;a.CooldownSeconds/=haste;
        string id=a.Definition.Id;
        if(id is "rupture" or "forcecrush")a.CooldownSeconds=Math.Max(8,a.CooldownSeconds);
        if(id is "batteringassault" or "massacre" or "gore" or "ravage")a.DamagePerTick*=1+.07f*MrPoints("carnage");
        if(id is "smash" or "forcescream" or "saberthrow" or "intimidatingroar")a.DamagePerTick*=1+.06f*MrPoints("marauder_rage");
        int cleave=(int)Mr("mr_blademaster");
        if(cleave>0 && MrWeapon(a) && id is not ("saberthrow" or "deadlythrow")){
            a.Definition.Area=AbilityArea.FrontalCone;a.Radius=Math.Max(a.Radius,.5f);
            a.Definition.MaximumTargets=2+Math.Min(6,cleave);a.Definition.SplashMultiplier=Math.Min(1,.4f+.1f*cleave);
        }
        if(id=="forcescream" && Mr("mr_dominate")>0){a.Definition.Area=AbilityArea.TargetSplash;a.Radius=.45f;a.Definition.SplashMultiplier=1;}
        if(id=="sweepingslash") {a.Definition.SpreadsPeriodicEffects=true;a.Radius*=1+Mr("mr_reach");}
        if(id=="ravage" && Mr("mr_cleave")>0){a.Definition.Area=AbilityArea.FrontalCone;a.Radius=.5f;a.Definition.MaximumTargets=Math.Max(4,cleave>0?2+Math.Min(6,cleave):4);a.Definition.SplashMultiplier=Math.Max(.6f,cleave>0?Math.Min(1,.4f+.1f*cleave):.6f);}
        if(id=="smash"){a.CooldownSeconds=Math.Max(10,a.CooldownSeconds);a.Radius*=1+Mr("mr_shockwave");a.Definition.SpreadsPeriodicEffects=true;}
        if(id=="deadlythrow" && Mr("mr_trauma")>0){a.Definition.SlowFactor=.5f;a.Definition.SlowSeconds=3;}
        if(id=="saberthrow" && Mr("mr_ricochet")>0){a.Definition.MaximumTargets=8;a.Definition.SplashMultiplier=1;}
    }
    private float MarauderBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsMarauder || a.Definition.Id=="viciousslash")return 0;
        bool bleed=_world.Periodic.On(t).Any(x=>x.Caster==a.Caster && x.Definition.Id is "rupture" or "deadlysaber");
        return (_mrPayoff>0 && MrWeapon(a)?.35f:0)+(bleed && a.Definition.Id=="annihilate"?.35f:0)
            +(a.Definition.Id=="deadlythrow" && t.Health.Current<t.Health.Maximum*.3f?.6f:0)
            +(_mrGore>0 && MrWeapon(a)?.6f:0)+(a.Definition.Id=="smash"?_mrSmashBonus+(Mr("mr_fissure")>0?.3f:0):0)
            +(_mrAssault>0 && a.Definition.Id is "ravage" or "massacre"?.4f:0)
            +(MrWeapon(a) && Mr("mr_precision")>0 && a.Casts%4==0?.5f:0);
    }
    private void MarauderActivated(AutomaticAbility a)
    {
        if(!IsMarauder)return;
        switch(a.Definition.Id){
            case "batteringassault":_mrAssault=1;break;
            case "gore":_mrGore=4+2*Mr("mr_gore");break;
            case "massacre":case "ravage":break;
            case "smash":if(_mrCrush==3 && Mr("mr_shockwave")>0)_world.Abilities.Find("predation")?.ReduceCooldown(4);_mrSmashBonus=_mrCrush*.18f;_mrCrush=0;break;
            case "frenzy":_mrFrenzy=6;_world.Abilities.Find("berserk")?.ReduceCooldown(30);_world.Abilities.Find("deadlysaber")?.ReduceCooldown(12);break;
            case "deadlysaber":_mrDeadly=6;_mrCharges=3+(int)Mr("mr_charges");break;
            case "berserk":_mrBerserk=6+Mr("mr_frenzy");break;
            case "predation":_mrPredation=6;break;
            case "forcecamouflage":_mrCamo=2;break;
            case "obfuscate":_mrObfuscate=3;break;
            case "cloakofpain":_mrPain=4;break;
            case "annihilate":
                if(Mr("mr_reset")>0)_world.Abilities.Find("rupture")?.ReduceCooldown(3);

                break;
            case "forcecharge":
                if(a.CastTarget is {} t){var d=t.GlobalPosition-_world.PlayerActor.GlobalPosition;d.Y=0;_world.Player.BeginAbilityDash(d,Math.Min(1.1f,Math.Max(0,d.Length()-.28f)));}
                if(Mr("mr_dominate")>0)_mrGore=3;
                break;
        }
        _world.Progression?.Recalculate();
    }
    private void MarauderHit(AutomaticAbility a,Combatant t)
    {
        if(!IsMarauder)return;
        if(!MrWeapon(a) || a.Definition.Id=="viciousslash")return;
        if(t.Alive && _mrDeadly>0 && _mrCharges>0 && _world.Abilities.Find("deadlysaber") is {Unlocked:true} ds && _world.Periodic.Remaining(ds,t)<=.65){_world.Periodic.Apply(ds,t);_mrCharges--;}
        if(Mr("mr_rhythm")>0 && Ready("mr_rhythm",.4))_world.Abilities.Find("deadlysaber")?.ReduceCooldown(.5f);
        if(Mr("mr_relentless")>0 && Ready("mr_relentless",.5))_world.Abilities.Find("berserk")?.ReduceCooldown(.5f);
        if(Mr("mr_bloodletting")>0 && Every("mr_openwounds",3) && t.Alive)MarauderProc(a,t,a.DamagePerTick*.5f,DamageType.Energy);
        if(Mr("mr_flurry")>0 && Every("mr_flurry",5) && t.Alive)MarauderProc(a,t,a.DamagePerTick*.6f,DamageType.Energy);
        if(a.Definition.Id is "massacre" or "ravage")_mrAssault=0;
        if(Mr("mr_siphon")>0 && _world.Periodic.Has(t) && Ready("mr_siphon",2))_world.PlayerActor.Health.ApplyHealing(4);
        if(Mr("mr_extend")>0 && _world.Abilities.Find("rupture") is {} r)_world.Periodic.Extend(r,t,.5f,8);
        if(Mr("mr_execution")>0 && !t.Alive)_world.Abilities.Find("deadlythrow")?.ReduceCooldown(2);
        if(Mr("mr_rageblade")>0 && _mrCrush>0 && Ready("mr_rageblade",1) && t.Alive)MarauderProc(a,t,6,DamageType.Energy);
        if(Mr("mr_crushreset")>0 && Every("mr_crushreset",6))_world.Abilities.Find("forcecrush")?.ReduceCooldown(1);
    }
    private void MarauderPeriodic(AutomaticAbility a)
    {
        if(!IsMarauder)return;
        if(a.Definition.Id=="forcecrush"){
            _mrCrush=Math.Min(3,_mrCrush+1);
            if(Mr("mr_forceflow")>0 && Ready("mr_forceflow",1))_world.Abilities.Find("saberthrow")?.ReduceCooldown(.6f);
        } else {
            if(Mr("mr_heal")>0 && Ready("mr_heal",1))_world.PlayerActor.Health.ApplyHealing(2);
            if(Mr("mr_hemorrhage")>0 && Ready("mr_hemorrhage",1))_world.Abilities.Find("annihilate")?.ReduceCooldown(.4f);
        }
    }
    public bool MarauderMaySpread(AutomaticAbility spreading,AutomaticAbility source)=>!IsMarauder || spreading.Definition.Id!="sweepingslash" || source.Definition.Id=="rupture" || Mr("mr_spread")>0;
    public void MarauderSpread(AutomaticAbility a,Combatant[] newTargets)
    {
        if(!IsMarauder || newTargets.Length==0 || !Ready("mr_spread_reward",1))return;
        _mrPayoff=3;
        _world.PlayerActor.Health.ApplyHealing(Math.Min(8,2*newTargets.Length));
        if(Mr("mr_heal")>0)_world.PlayerActor.Health.ApplyShield(8,3);
        if(Mr("mr_spread")>0)foreach(var t in newTargets.Where(t=>t.Alive))MarauderProc(a,t,12,DamageType.Internal);
        if(_mrBerserk>0 && Mr("mr_frenzy")>0)_world.Abilities.Find("frenzy")?.ReduceCooldown(2);
    }
    private void MarauderProc(AutomaticAbility source,Combatant target,float damage,DamageType type)
    {
        if(target.Health.ApplyDamage(damage,type)<=0)return;
        if(source.Presentation is D3AbilityPresentation visual)visual.ProcImpact(target);
    }
    private void MarauderDamaged(float amount){if(IsMarauder && amount>0 && _mrPain>0 && Ready("mr_reactive",1))foreach(var t in _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,.4f,.4f).Take(3).ToArray())MarauderProc(_world.Abilities.Find("cloakofpain")!,t,4,DamageType.Internal);}
    private void AdvanceMarauder(double d){_mrFrenzy=Math.Max(0,_mrFrenzy-d);_mrPayoff=Math.Max(0,_mrPayoff-d);bool active=_mrBerserk>0;_mrBerserk=Math.Max(0,_mrBerserk-d);_mrPredation=Math.Max(0,_mrPredation-d);_mrCamo=Math.Max(0,_mrCamo-d);_mrObfuscate=Math.Max(0,_mrObfuscate-d);_mrPain=Math.Max(0,_mrPain-d);_mrDeadly=Math.Max(0,_mrDeadly-d);_mrGore=Math.Max(0,_mrGore-d);if(active && _mrBerserk==0)_world.Progression?.Recalculate();}
    private void ResetMarauder(){_mrBerserk=_mrPredation=_mrCamo=_mrObfuscate=_mrPain=_mrDeadly=_mrGore=0;_mrFrenzy=_mrPayoff=0;_mrCharges=_mrCrush=_mrAssault=0;_mrSmashBonus=0;}
}
