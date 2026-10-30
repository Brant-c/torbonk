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
    private int JuggernautAreaRank(string key,int cap)=>Math.Clamp(JuggernautTalentRank(key)+_world.Build.Talent(key),0,cap);
    private bool IsJuggernaut=>_world.RunConfiguration.AdvancedClassId=="juggernaut";
    private double _jgReaction,_jgGuard,_jgDefense,_jgWard,_jgUnstoppable;
    private AutomaticAbility? _jgEmpoweredAttack;
    private int _jgEmpoweredCast;
    private bool JgSaber(AutomaticAbility a)=>a.Definition.PresentationKind=="saber" && a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack;
    public bool JuggernautAuraActive(string id)=>IsJuggernaut && (id switch {"enrageddefense"=>_jgDefense>0,"endurepain"=>_world.PlayerActor.Health.Shielded,"saberward"=>_world.PlayerActor.Health.Shielded || _jgWard>0,_=>false});
    private int _jgCharges;
    private bool _jgSavagery,_jgDominate,_jgHatred;
    private float _jgSmashBonus,_jgScreamBonus,_jgHeavyBonus;
    private int _jgSustainCast=-1,_jgSustainHits;
    public float JuggernautIncoming=>!IsJuggernaut?1:.88f*(1-Jg("jg_mitigation"))*(_jgGuard>0?.85f:1)*(_jgUnstoppable>0?.9f:1)*(_jgWard>0 && Jg("jg_invincible")>0?.8f:1);
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
        if(id=="forcescream") {
            int rank=JuggernautAreaRank("jg_resonance",3);
            a.Definition.Area=rank>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank*2;a.Definition.SplashMultiplier=.65f;
            if(rank>0)a.Radius=Math.Max(a.Radius,.28f+.04f*rank);
        }
        if(id=="forcechoke") {
            int rank=JuggernautAreaRank("jg_grip",3);
            a.Definition.Area=rank>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank;a.Definition.SplashMultiplier=1;
            if(rank>0)a.Radius=Math.Max(a.Radius,.28f+.04f*rank);
        }
        if(id=="ravage") {
            int rank=JuggernautAreaRank("jg_sweep",3);
            a.Definition.Area=rank>0?AbilityArea.FrontalCone:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank;a.Definition.SplashMultiplier=.6f;
            if(rank>0)a.Radius=Math.Max(a.Radius,a.Range);
        }
        if(id=="sunderingassault") {
            int rank=JuggernautAreaRank("jg_sundering",3);
            a.Definition.Area=rank>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=1+rank;a.Definition.SplashMultiplier=.6f;
            a.Definition.SplashAppliesVulnerability=rank>0;
            if(rank>0)a.Radius=Math.Max(a.Radius,.28f);
        }
        if(id=="smash") {
            bool wound=JuggernautAreaRank("jg_fissure",1)>0;
            a.Definition.PeriodicDamage=wound?3:0;a.Definition.PeriodicDuration=wound?4:0;a.Definition.PeriodicInterval=1;
        }
        if(id is "impale" or "forcescream") {
            bool bleed=Jg(id=="impale"?"jg_impale_bleed":"jg_scream_bleed")>0;
            a.Definition.PeriodicDamage=bleed?3:0;a.Definition.PeriodicDuration=bleed?6:0;a.Definition.PeriodicInterval=2;
        }
        if(id=="crushingblow")a.Definition.SpreadsPeriodicEffects=Jg("jg_shatter")>0;
        if(id=="smash")a.Definition.SpreadsPeriodicEffects=Jg("jg_crushingwave")>0;
        if(id=="crushingblow" && Jg("jg_shatter")>0){a.Definition.Area=AbilityArea.TargetSplash;a.Radius=Math.Max(a.Radius,.3f);a.Definition.SpreadsPeriodicEffects=true;}
        if(id=="smash" && Jg("jg_crushingwave")>0)a.Definition.SpreadsPeriodicEffects=true;
    }
    private float JuggernautBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsJuggernaut)return 0;
        float generic=(JgSaber(a) && Jg("jg_bloodmaster")>0?.08f:0)+(a.Definition.PresentationKind!="saber" && a.Definition.Effect==AbilityEffect.Damage && Jg("jg_obliteration")>0?.08f:0);
        if(a==_jgEmpoweredAttack && a.Casts==_jgEmpoweredCast)generic+=_jgHeavyBonus;
        return generic+(a.Definition.Id switch {"smash"=>_jgSmashBonus,"forcescream"=>_jgScreamBonus,_=>0});
    }
    private void JuggernautActivated(AutomaticAbility a)
    {
        if(!IsJuggernaut)return;
        string id=a.Definition.Id;
        if(id=="retaliation"){_jgReaction=0;if(Jg("jg_barricade")>0)_jgGuard=4;}
        if(id=="enrageddefense"){_jgDefense=6;_world.Build.JuggernautRelics.DefenseStarted();}
        if(id=="saberward" && Jg("jg_invincible")>0)_jgWard=6;
        if(id=="forcescream"){_jgScreamBonus=_jgSavagery?.3f:0;_jgSavagery=false;if(Jg("jg_dominate")>0)_jgDominate=true;}
        if(id=="smash"){_jgSmashBonus=_jgCharges*.12f+(_jgDominate?.25f:0);_jgCharges=0;_jgDominate=false;_jgSustainCast=-1;}
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
        _jgReaction=4;
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
        if(a.Definition.Id=="forcechoke" && _world.Build.Talent("jg_convergence")>0 && t.Alive && _world.Abilities.Find("forcecrush") is {Unlocked:true} crush && !_world.Periodic.Has(crush,t))_world.Periodic.Apply(crush,t);
        if(a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack && Jg("jg_darkblood")>0 && Ready("jg_darkblood",1))_world.PlayerActor.Health.ApplyHealing(a.Definition.Id=="sunderingassault"?3:2);
        if(a.Definition.Effect==AbilityEffect.Damage && a.Definition.Id!="smash" && Jg("jg_rage_heal")>0 && Ready("jg_general_heal",1))_world.PlayerActor.Health.ApplyHealing(1);
        if(a.Definition.Id=="forcescream" && Jg("jg_sonic")>0 && Ready("jg_sonic",1) && !_world.PlayerActor.Health.Shielded)_world.PlayerActor.Health.ApplyShield(10,3);
        if(a.Definition.Id=="smash" && Jg("jg_rage_heal")>0){if(_jgSustainCast!=a.Casts){_jgSustainCast=a.Casts;_jgSustainHits=0;}if(_jgSustainHits++<3)_world.PlayerActor.Health.ApplyHealing(2);}
    }
    private void JuggernautPeriodic(AutomaticAbility a)
    {
        if(!IsJuggernaut || a.Definition.Id!="forcecrush")return;
        if(Jg("jg_shockwave")>0)_jgCharges=Math.Min(3,_jgCharges+1);

    }
    private void AdvanceJuggernaut(double d){_jgUnstoppable=Math.Max(0,_jgUnstoppable-d);_jgReaction=Math.Max(0,_jgReaction-d);_jgGuard=Math.Max(0,_jgGuard-d);_jgDefense=Math.Max(0,_jgDefense-d);_jgWard=Math.Max(0,_jgWard-d);}
    private void ResetJuggernaut(){_jgReaction=_jgGuard=_jgDefense=_jgWard=_jgUnstoppable=0;_jgEmpoweredAttack=null;_jgEmpoweredCast=0;_jgCharges=0;_jgSavagery=_jgDominate=_jgHatred=false;_jgSmashBonus=_jgScreamBonus=_jgHeavyBonus=0;_jgSustainCast=-1;}
}
