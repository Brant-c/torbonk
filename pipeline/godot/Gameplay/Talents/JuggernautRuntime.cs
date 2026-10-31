using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    public string[] JuggernautEarlyAbilities {
        get {
            int Points(string tree)=>TalentCatalog.Current.ForClass("juggernaut").Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
            // Smash starts free. Every opening can add Scream and multi-target control.
            return Points("vengeance")>Points("juggernaut_rage") && Points("vengeance")>=Points("immortal")
                ?new[]{"forcescream","forcechoke","ravage","impale","forcecrush","saberward"}
                :Points("immortal")>Points("juggernaut_rage")
                ?new[]{"forcescream","forcechoke","sunderingassault","saberward","retaliation","impale"}
                :new[]{"forcescream","forcechoke","forcecrush","intimidatingroar","saberward","impale"};
        }
    }
    public int JuggernautTalentRank(string key)=>!IsJuggernaut?0:key is "jg_grip" or "jg_sweep" or "jg_resonance"?(int)Hook(key):Hook(key)>0?1:0;
    private float Jg(string key)=>Math.Max(Hook(key),_world.Build.Talent(key)*(key=="jg_mitigation"?.08f:1));
    private int JuggernautAreaRank(string key)=>Math.Max(0,JuggernautTalentRank(key)+_world.Build.Talent(key));
    private bool IsJuggernaut=>_world.RunConfiguration.AdvancedClassId=="juggernaut";
    private double _jgReaction,_jgGuard,_jgDefense,_jgWard,_jgUnstoppable;
    private AutomaticAbility? _jgEmpoweredAttack;
    private int _jgEmpoweredCast;
    private const float JuggernautSmashFloor=3f; // the base cooldown is 4.5s, so the plan's 5s floor would have slowed Smash; the leap itself lasts about 2.4s
    private bool JgSaber(AutomaticAbility a)=>a.Definition.PresentationKind=="saber" && a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack;
    public bool JuggernautAuraActive(string id)=>IsJuggernaut && (id switch {"enrageddefense"=>_jgDefense>0,"endurepain"=>_world.PlayerActor.Health.Shielded,"saberward"=>_world.PlayerActor.Health.Shielded || _jgWard>0,_=>false});
    private int _jgCharges;
    private bool _jgSavagery,_jgDominate,_jgHatred;
    private float _jgSmashBonus,_jgScreamBonus,_jgHeavyBonus;
    private int _jgSustainCast=-1,_jgSustainHits,_jgQuakeCast=-1,_jgQuakeHits,_jgFury;
    private double _jgFuryT,_jgStoredT;
    private float _jgStored;
    private readonly Dictionary<ulong,double> _jgMark=new(),_jgDread=new();
    // Soresu cards divide damage taken, so stacking them can approach but never reach full immunity.
    public float JuggernautIncoming=>!IsJuggernaut?1:.88f*(1-Jg("jg_mitigation"))/(1+CardPower("jgm_soresu")*.01f)*(_jgGuard>0?.85f:1)*(_jgUnstoppable>0?.9f:1)*(_jgWard>0 && Jg("jg_invincible")>0?.8f:1);
    public bool CanBeginJuggernaut(AutomaticAbility a)
    {
        if(!IsJuggernaut)return true;
        var h=_world.PlayerActor.Health;
        return a.Definition.Id switch {
            "retaliation"=>_jgReaction>0,"saberward"=>h.Current<h.Maximum*.8f,
            "endurepain"=>h.Current<h.Maximum*.35f,"enrageddefense"=>h.Current<h.Maximum*.55f && _jgDefense<=0,
            "forcecharge" or "obliterate"=>false,_=>true};
    }
    private void JuggernautStats(AutomaticAbility a)
    {
        if(!IsJuggernaut)return;
        string id=a.Definition.Id;
        if(a.Definition.PresentationKind!="saber" && a.Definition.Effect==AbilityEffect.Damage)a.CooldownSeconds/=1+.1f*Jg("jg_alacrity");
        if(id=="smash")a.Radius*=1+.25f*Jg("jg_decimate");
        // Aftershock, Fury Bank and recovery cards compound on Smash, so its recovery has an internal floor.
        if(id=="smash")a.CooldownSeconds=Math.Max(a.CooldownSeconds,JuggernautSmashFloor);
        if(id=="forcescream") {
            int rank=JuggernautAreaRank("jg_resonance");
            a.Definition.Area=rank>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank*2;a.Definition.SplashMultiplier=.65f;
            if(rank>0)a.Radius=Math.Max(a.Radius,(.28f+.04f*Math.Min(rank,10))*(1+RadiusCardBoost("juggernaut_","forcescream")));
        }
        if(id=="forcechoke") {
            int rank=JuggernautAreaRank("jg_grip");
            a.Definition.Area=rank>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank;a.Definition.SplashMultiplier=1;
            if(rank>0)a.Radius=Math.Max(a.Radius,.28f+.04f*Math.Min(rank,10));
        }
        if(id=="ravage") {
            int rank=JuggernautAreaRank("jg_sweep");
            a.Definition.Area=rank>0?AbilityArea.FrontalCone:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank;a.Definition.SplashMultiplier=.6f;
            if(rank>0)a.Radius=Math.Max(a.Radius,a.Range*(1+RadiusCardBoost("juggernaut_","ravage")));
        }
        if(id=="sunderingassault") {
            int rank=JuggernautAreaRank("jg_sundering");
            a.Definition.Area=rank>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank;a.Definition.SplashMultiplier=.6f;
            a.Definition.SplashAppliesVulnerability=rank>0;
            if(rank>0)a.Radius=Math.Max(a.Radius,.28f);
        }
        if(id=="smash") {
            bool wound=JuggernautAreaRank("jg_fissure")>0;
            a.Definition.PeriodicDamage=wound?3:0;a.Definition.PeriodicDuration=wound?4:0;a.Definition.PeriodicInterval=1;
        }
        if(id is "impale" or "forcescream") {
            bool bleed=Jg(id=="impale"?"jg_impale_bleed":"jg_scream_bleed")>0;
            a.Definition.PeriodicDamage=bleed?3:0;a.Definition.PeriodicDuration=bleed?6:0;a.Definition.PeriodicInterval=2;
        }
        if(id=="crushingblow")a.Definition.SpreadsPeriodicEffects=Jg("jg_shatter")>0;
        if(id=="smash")a.Definition.SpreadsPeriodicEffects=Jg("jg_crushingwave")>0;
        if(id=="crushingblow" && Jg("jg_shatter")>0){a.Definition.Area=AbilityArea.TargetSplash;a.Radius=Math.Max(a.Radius,.3f*(1+RadiusCardBoost("juggernaut_","crushingblow")));a.Definition.SpreadsPeriodicEffects=true;}
        if(id=="smash" && Jg("jg_crushingwave")>0)a.Definition.SpreadsPeriodicEffects=true;
    }
    private float JuggernautBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsJuggernaut)return 0;
        float generic=(JgSaber(a) && Jg("jg_bloodmaster")>0?.08f:0)+(a.Definition.PresentationKind!="saber" && a.Definition.Effect==AbilityEffect.Damage && Jg("jg_obliteration")>0?.08f:0);
        if(a==_jgEmpoweredAttack && a.Casts==_jgEmpoweredCast)generic+=_jgHeavyBonus;
        string id=a.Definition.Id;ulong key=t.GetInstanceId();
        float cards=(id is "impale" or "ravage" or "crushingblow" && _jgMark.GetValueOrDefault(key)>0?.15f*CardPower("jgm_rend"):0)
            +(id=="crushingblow" && t.Health.Current<t.Health.Maximum*.3f?.3f*CardPower("jgm_finisher"):0)
            +(id is "smash" or "forcescream" && _jgDread.GetValueOrDefault(key)>0?.1f*CardPower("jgm_dread"):0);
        return generic+cards+(id switch {"smash"=>_jgSmashBonus,"forcescream"=>_jgScreamBonus,_=>0});
    }
    private void JuggernautActivated(AutomaticAbility a)
    {
        if(!IsJuggernaut)return;
        string id=a.Definition.Id;
        if(id=="retaliation"){_jgReaction=0;if(Jg("jg_barricade")>0)_jgGuard=4;JuggernautReprisal();}
        if(id=="enrageddefense"){_jgDefense=6;_world.Build.JuggernautRelics.DefenseStarted();}
        if(id=="saberward" && Jg("jg_invincible")>0)_jgWard=6;
        if(id=="saberward")JuggernautReprisal();
        if(id=="forcescream"){_jgScreamBonus=_jgSavagery?.3f:0;_jgSavagery=false;if(Jg("jg_dominate")>0)_jgDominate=true;}
        if(id=="smash"){
            _jgSmashBonus=_jgCharges*.12f+(_jgDominate?.25f:0)+_jgFury*.04f*CardPower("jgm_fury");_jgCharges=0;_jgFury=0;_jgDominate=false;_jgSustainCast=-1;
            QueueSmashEchoes(CardRanks("jgm_aftershock"));
        }
        if(JgSaber(a)){_jgHeavyBonus=_jgHatred?.2f:0;_jgHatred=false;_jgEmpoweredAttack=a;_jgEmpoweredCast=a.Casts;
            if(Jg("jg_unstoppable")>0 && Ready("jg_unstoppable",6))_jgUnstoppable=2;}
        if(id=="impale"){if(Jg("jg_savagery")>0)_jgSavagery=true;if(Jg("jg_rampage")>0)_world.Abilities.Find("ravage")?.ReduceCooldown(3);}
        if(id is "forcecharge" or "obliterate")
        {
            if(Jg("jg_dominate")>0)_jgDominate=true;
            if(Jg("jg_unstoppable")>0)_jgGuard=4;
            // Navigation validates and resolves the short dash through ordinary player movement.
            if(a.CastTarget is {} target){var direction=target.GlobalPosition-_world.PlayerActor.GlobalPosition;direction.Y=0;_world.Player.BeginAbilityDash(direction,Math.Min(1.1f,Math.Max(0,direction.Length()-.28f)));}
        }
        if(id is "intimidatingroar" or "forcechoke" && Jg("jg_unleashed")>0)_world.Abilities.Find("saberward")?.ReduceCooldown(3);
    }
    private void JuggernautDamaged(float amount)
    {
        if(!IsJuggernaut || !_world.PlayerActor.Alive || amount<=0)return;
        _jgReaction=4;_jgStored+=amount;_jgStoredT=4;
        if(Jg("jg_hatred")>0)_jgHatred=true;
        if(Jg("jg_revenge")>0 && Ready("jg_revenge",1)){_world.Abilities.Find("retaliation")?.ReduceCooldown(1);_world.Abilities.Find("saberward")?.ReduceCooldown(.5f);}
        if(_jgDefense>0 && _world.Abilities.Find("enrageddefense") is {Unlocked:true} && Ready("jg_heal",_world.Build.JuggernautRelics.HealInterval))
        {var healed=_world.PlayerActor.Health.ApplyHealing(_world.Build.JuggernautRelics.CappedHealing(4));
         if(healed>0 && _world.Abilities.Find("enrageddefense")?.Presentation is D3AbilityPresentation defense)defense.DefensiveHealProc();
         if(Jg("jg_undying")>0)_world.Abilities.Find("endurepain")?.ReduceCooldown(1);}
    }
    private void JuggernautHit(AutomaticAbility a,Combatant t)
    {
        if(!IsJuggernaut)return;
        JuggernautCardHit(a,t);
        if(a.Definition.Id=="forcechoke" && _world.Build.Talent("jg_convergence")>0 && t.Alive && _world.Abilities.Find("forcecrush") is {Unlocked:true} crush && !_world.Periodic.Has(crush,t))_world.Periodic.Apply(crush,t);
        if(a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack && Jg("jg_darkblood")>0 && Ready("jg_darkblood",1))_world.PlayerActor.Health.ApplyHealing((a.Definition.Id=="sunderingassault"?3:2)*(1+.15f*CardPower("jgm_darkblood")));
        if(a.Definition.Effect==AbilityEffect.Damage && a.Definition.Id!="smash" && Jg("jg_rage_heal")>0 && Ready("jg_general_heal",1))_world.PlayerActor.Health.ApplyHealing(1*(1+.15f*CardPower("jgm_rageheal")));
        if(a.Definition.Id=="forcescream" && Jg("jg_sonic")>0 && Ready("jg_sonic",1) && !_world.PlayerActor.Health.Shielded)_world.PlayerActor.Health.ApplyShield(10,3);
        if(a.Definition.Id=="smash" && Jg("jg_rage_heal")>0){if(_jgSustainCast!=a.Casts){_jgSustainCast=a.Casts;_jgSustainHits=0;}if(_jgSustainHits++<3)_world.PlayerActor.Health.ApplyHealing(2*(1+.15f*CardPower("jgm_rageheal")));}
    }
    private void JuggernautPeriodic(AutomaticAbility a)
    {
        if(!IsJuggernaut)return;
        if(a.Definition.Id=="impale" && CardPower("jgm_cadence")>0 && Ready("jgm_cadence",.5))_world.Abilities.Find("ravage")?.ReduceCooldown(.3f*CardPower("jgm_cadence"));
        if(a.Definition.Id!="forcecrush")return;
        if(Jg("jg_shockwave")>0)_jgCharges=Math.Min(3,_jgCharges+1);

    }
    // Fury Bank, Reprisal, Rend Mark, Dread Presence and Aftershock all run on short timers.
    private void JuggernautReprisal()
    {
        float power=CardPower("jgm_reprisal");
        if(power<=0 || _jgStored<=0 || !Ready("jgm_reprisal",1))return;
        float damage=_jgStored*.25f*power;_jgStored=0;
        foreach(var enemy in _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,.35f,.35f).ToArray())enemy.Health.ApplyDamage(damage,DamageType.Energy,false,"retaliation");
    }
    private void JuggernautCardHit(AutomaticAbility a,Combatant t)
    {
        string id=a.Definition.Id;ulong key=t.GetInstanceId();
        if(CardPower("jgm_fury")>0 && id!="smash" && a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack && Ready("jgm_fury_bank",.25)){_jgFury++;_jgFuryT=6;}
        if(id=="forcescream" && CardPower("jgm_rend")>0)_jgMark[key]=6;
        if(id=="crushingblow" && CardPower("jgm_rend")>0 && _jgMark.GetValueOrDefault(key)>0 && Jg("jg_shatter")>0)
            foreach(var near in _world.Targeting.InArea(t.GlobalPosition,.3f,.3f,true))_jgMark[near.GetInstanceId()]=6;
        if(id is "intimidatingroar" or "forcechoke" && CardPower("jgm_dread")>0)_jgDread[key]=5;
        if(id=="crushingblow" && !t.Alive && CardPower("jgm_finisher")>0 && Ready("jgm_finisher_refund",.5))a.ReduceCooldown(2);
        if(id=="impale" && !t.Alive && CardPower("jgm_cadence")>0 && Ready("jgm_cadence",.5))_world.Abilities.Find("ravage")?.ReduceCooldown(.3f*CardPower("jgm_cadence"));
        if(id=="smash" && CardPower("jgm_quake")>0)
        {
            if(_jgQuakeCast!=a.Casts){_jgQuakeCast=a.Casts;_jgQuakeHits=0;}
            if(++_jgQuakeHits==3 && _world.Abilities.Find("forcecrush") is {Unlocked:true} crush)
                crush.ReduceCooldown(1.5f*CardPower("jgm_quake"));
            if(_jgQuakeHits>=3 && t.Alive && _world.Abilities.Find("forcecrush") is {Unlocked:true} wound && _world.Periodic.Has(wound,t))_world.Periodic.Apply(wound,t);
        }
    }
    private void AdvanceJuggernaut(double d){
        if(IsJuggernaut){
            _jgFuryT=Math.Max(0,_jgFuryT-d);if(_jgFuryT==0)_jgFury=0;
            _jgStoredT=Math.Max(0,_jgStoredT-d);if(_jgStoredT==0)_jgStored=0;
            foreach(var map in new[]{_jgMark,_jgDread})foreach(var k in map.Keys.ToArray()){map[k]-=d;if(map[k]<=0)map.Remove(k);}
            AdvanceSmashEchoes(d);
        }
_jgUnstoppable=Math.Max(0,_jgUnstoppable-d);_jgReaction=Math.Max(0,_jgReaction-d);_jgGuard=Math.Max(0,_jgGuard-d);_jgDefense=Math.Max(0,_jgDefense-d);_jgWard=Math.Max(0,_jgWard-d);}
    private void ResetJuggernaut(){_jgReaction=_jgGuard=_jgDefense=_jgWard=_jgUnstoppable=0;_jgEmpoweredAttack=null;_jgEmpoweredCast=0;_jgCharges=0;_jgSavagery=_jgDominate=_jgHatred=false;_jgSmashBonus=_jgScreamBonus=_jgHeavyBonus=0;_jgSustainCast=-1;_jgQuakeCast=-1;_jgQuakeHits=_jgFury=0;_jgFuryT=_jgStoredT=0;_jgStored=0;_jgMark.Clear();_jgDread.Clear();_smashEchoes.Clear();}
}
