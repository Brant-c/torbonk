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
    public int JuggernautTalentRank(string key)=>IsJuggernaut?(int)Hook(key):0;
    private int JuggernautAreaRank(string key,int cap)=>Math.Clamp(JuggernautTalentRank(key)+_world.Build.Talent(key),0,cap);
    private bool IsJuggernaut=>_world.RunConfiguration.AdvancedClassId=="juggernaut";
    private double _jgReaction,_jgGuard,_jgDefense,_jgWard;
    public bool JuggernautAuraActive(string id)=>IsJuggernaut && (id switch {"enrageddefense"=>_jgDefense>0,"saberward"=>_world.PlayerActor.Health.Shielded || _jgWard>0,_=>false});
    private int _jgCharges;
    private bool _jgSavagery,_jgDominate,_jgHatred;
    private float _jgSmashBonus,_jgScreamBonus,_jgHeavyBonus;
    private int _jgSustainCast=-1,_jgSustainHits;
    public float JuggernautIncoming=>!IsJuggernaut?1:.88f*(1-Hook("jg_mitigation"))*(_jgGuard>0?.85f:1)*(_jgWard>0 && Hook("jg_invincible")>0?.8f:1);
    public bool CanBeginJuggernaut(AutomaticAbility a)
    {
        if(!IsJuggernaut)return true;
        var h=_world.PlayerActor.Health;
        return a.Definition.Id switch {
            "retaliation"=>_jgReaction>0,
            "saberward"=>h.Current<h.Maximum*.8f,
            "endurepain"=>h.Current<h.Maximum*.35f,
            "enrageddefense"=>h.Current<h.Maximum*.55f && _jgDefense<=0,
            "forcecharge" or "obliterate"=>false,
            _=>true};
    }
    private void JuggernautStats(AutomaticAbility a)
    {
        if(!IsJuggernaut)return;
        string id=a.Definition.Id;
        if(id=="smash")a.Radius*=1+.25f*Hook("jg_decimate");
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
        if(id=="impale" && Hook("jg_impale_bleed")>0 || id=="forcescream" && Hook("jg_scream_bleed")>0)
        {a.Definition.PeriodicDamage=3;a.Definition.PeriodicDuration=6;a.Definition.PeriodicInterval=2;}
        if(id=="crushingblow" && Hook("jg_shatter")>0){a.Definition.Area=AbilityArea.TargetSplash;a.Radius=Math.Max(a.Radius,.3f);a.Definition.SpreadsPeriodicEffects=true;}
        if(id=="smash" && Hook("jg_crushingwave")>0)a.Definition.SpreadsPeriodicEffects=true;
    }
    private float JuggernautBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsJuggernaut)return 0;
        return a.Definition.Id switch {
            "smash"=>_jgSmashBonus,
            "forcescream"=>_jgScreamBonus,
            "impale" or "crushingblow"=>_jgHeavyBonus,
            "ravage"=>_jgHeavyBonus+(_world.Periodic.On(t).Any(x=>x.Caster==a.Caster) && Hook("jg_bloodmaster")>0?.2f:0),_=>0};
    }
    private void JuggernautActivated(AutomaticAbility a)
    {
        if(!IsJuggernaut)return;
        string id=a.Definition.Id;
        if(id=="retaliation"){_jgReaction=0;if(Hook("jg_barricade")>0)_jgGuard=4;}
        if(id=="enrageddefense")_jgDefense=6;
        if(id=="saberward" && Hook("jg_invincible")>0)_jgWard=6;
        if(id=="forcescream"){_jgScreamBonus=_jgSavagery?.3f:0;_jgSavagery=false;if(Hook("jg_alacrity")>0)_world.Abilities.Find("forcecrush")?.ReduceCooldown(2);}
        if(id=="smash"){_jgSmashBonus=_jgCharges*.12f+(_jgDominate?.25f:0);_jgCharges=0;_jgDominate=false;_jgSustainCast=-1;}
        if(id is "impale" or "crushingblow" or "ravage"){_jgHeavyBonus=_jgHatred?.2f:0;_jgHatred=false;}
        if(id=="impale"){if(Hook("jg_savagery")>0)_jgSavagery=true;if(Hook("jg_rampage")>0)_world.Abilities.Find("ravage")?.ReduceCooldown(3);}
        if(id is "forcecharge" or "obliterate")
        {
            if(Hook("jg_dominate")>0)_jgDominate=true;
            if(Hook("jg_unstoppable")>0)_jgGuard=4;
            // Navigation validates and resolves the short dash through ordinary player movement.
            if(a.CastTarget is {} target){var direction=target.GlobalPosition-_world.PlayerActor.GlobalPosition;direction.Y=0;_world.Player.BeginAbilityDash(direction,Math.Min(1.1f,Math.Max(0,direction.Length()-.28f)));}
        }
        if(id is "intimidatingroar" or "forcechoke" && Hook("jg_unleashed")>0)_world.Abilities.Find("saberward")?.ReduceCooldown(3);
    }
    private void JuggernautDamaged(float amount)
    {
        if(!IsJuggernaut || !_world.PlayerActor.Alive || amount<=0)return;
        _jgReaction=4;
        if(Hook("jg_hatred")>0)_jgHatred=true;
        if(Hook("jg_revenge")>0 && Ready("jg_revenge",1))_world.Abilities.Find("retaliation")?.ReduceCooldown(1);
        if(_jgDefense>0 && _world.Abilities.Find("enrageddefense") is {Unlocked:true} && Ready("jg_heal",1))
        {_world.PlayerActor.Health.ApplyHealing(4);if(Hook("jg_undying")>0)_world.Abilities.Find("endurepain")?.ReduceCooldown(1);}
    }
    private void JuggernautHit(AutomaticAbility a,Combatant t)
    {
        if(!IsJuggernaut)return;
        if(a.Definition.Id=="forcechoke" && _world.Build.Talent("jg_convergence")>0 && t.Alive && _world.Abilities.Find("forcecrush") is {Unlocked:true} crush && !_world.Periodic.Has(crush,t))_world.Periodic.Apply(crush,t);
        if(a.Definition.Id=="sunderingassault" && Hook("jg_darkblood")>0 && Ready("jg_darkblood",1))_world.PlayerActor.Health.ApplyHealing(3);
        if(a.Definition.Id=="forcescream" && Hook("jg_sonic")>0 && Ready("jg_sonic",1) && !_world.PlayerActor.Health.Shielded)_world.PlayerActor.Health.ApplyShield(10,3);
        if(a.Definition.Id=="smash" && Hook("jg_rage_heal")>0){if(_jgSustainCast!=a.Casts){_jgSustainCast=a.Casts;_jgSustainHits=0;}if(_jgSustainHits++<3)_world.PlayerActor.Health.ApplyHealing(2);}
    }
    private void JuggernautPeriodic(AutomaticAbility a)
    {
        if(!IsJuggernaut || a.Definition.Id!="forcecrush")return;
        if(Hook("jg_shockwave")>0)_jgCharges=Math.Min(3,_jgCharges+1);
        if(Hook("jg_obliteration")>0 && Ready("jg_obliteration",1))_world.Abilities.Find("obliterate")?.ReduceCooldown(1);
    }
    private void AdvanceJuggernaut(double d){_jgReaction=Math.Max(0,_jgReaction-d);_jgGuard=Math.Max(0,_jgGuard-d);_jgDefense=Math.Max(0,_jgDefense-d);_jgWard=Math.Max(0,_jgWard-d);}
    private void ResetJuggernaut(){_jgReaction=_jgGuard=_jgDefense=_jgWard=0;_jgCharges=0;_jgSavagery=_jgDominate=_jgHatred=false;_jgSmashBonus=_jgScreamBonus=_jgHeavyBonus=0;_jgSustainCast=-1;}
}
