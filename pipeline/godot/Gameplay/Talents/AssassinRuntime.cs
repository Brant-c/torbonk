using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    private bool IsAssassin=>_world.RunConfiguration.AdvancedClassId=="assassin";
    private double _opening,_cloak,_deflection,_shroudedEdge;
    private int _voltaic;
    private bool _shockSpent;
    private readonly Dictionary<ulong,double> _control=new();
    private readonly Dictionary<ulong,double> _electrified=new();
    public bool AssassinDeception=>IsAssassin && Ranks.Any(p=>p.Value>0 && p.Key.StartsWith("deception.",StringComparison.Ordinal));
    public bool AssassinElectrified(Combatant t)=>AssassinDeception && _electrified.GetValueOrDefault(t.GetInstanceId())>0;
    public bool CanBeginAssassin(AutomaticAbility a)=>a.Definition.Id!="electrocute" || AssassinDeception;
    public Combatant? ElectrocuteTarget(AutomaticAbility a)=>_world.Abilities.Find("discharge") is {} discharge?
        a.Targeting.InArea(a.Caster.GlobalPosition,a.Range,a.Range).Where(t=>_world.Periodic.Has(discharge,t) && !AssassinElectrified(t)).OrderBy(t=>t.GlobalPosition.DistanceSquaredTo(a.Caster.GlobalPosition)).FirstOrDefault():null;
    public float AssassinDeflectionDuration=>4+Hook("deflection_time");
    public bool AssassinDeflecting=>IsAssassin && _deflection>0;
    public bool AssassinCloaked=>IsAssassin && _cloak>0;
    public bool StealthOpening=>IsAssassin && _opening>0;
    public int VoltaicCharges=>IsAssassin?_voltaic:0;
    public bool AssassinSpread=>IsAssassin && (Hook("spread")>0 || As("as_spread"));
    private float AssassinIncoming=>!IsAssassin?1:(1-Math.Min(.15f,Hook("mitigation")))*(_cloak>0?.35f:1)*(_deflection>0?.5f:1);
    private float AssassinBonus(AutomaticAbility a,Combatant t)=>!IsAssassin?0:
        (a.Definition.Id=="shock"?.2f*_voltaic:0)+(a.Definition.Id=="maul" && _opening>0?.5f+Hook("ambush"):0)
        +(a.Definition.Id=="discharge"?.1f*_voltaic:0)
        +(a.Definition.Id=="maul" && _opening>0 && As("as_ambush")?.5f:0)
        +(_shroudedEdge>0 && As("as_guard") && a.Definition.Id is "lacerate" or "wither"?.6f:0)
        +(a.Definition.Id=="deathfield" && As("as_harvest")?.3f*Math.Min(3,_world.Periodic.On(t).Count(x=>x.Caster==a.Caster)):0)
        +(a.Definition.Id is "lacerate" or "voltaicslash" or "maul" && t is EnemyAgent e && e.OilRemaining>0?.1f:0)
        +(AssassinElectrified(t) && a.Definition.Id is "maul" or "voltaicslash"?.35f+Hook("electrocute_burst"):0);
    public string[] EarlyAbilityOrder {
        get {
            int madness=TalentCatalog.Current.ForClass("assassin").Where(t=>t.Tree=="assassin_madness").Sum(t=>Ranks.GetValueOrDefault(t.Id));
            int deception=TalentCatalog.Current.ForClass("assassin").Where(t=>t.Tree=="deception").Sum(t=>Ranks.GetValueOrDefault(t.Id));
            return madness>0 && madness>=deception?new[]{"creepingterror","deathfield","discharge","spike","shock","voltaicslash"}:
                deception>0?new[]{"discharge","voltaicslash","maul","spike","shock","forcecloak"}:new[]{"creepingterror","discharge","deathfield","spike","voltaicslash","shock"};
        }
    }
    private void AssassinStats(AutomaticAbility a)
    {
        if(!IsAssassin)return;
        string id=a.Definition.Id;
        if(id=="discharge") {
            a.DamagePerTick*=1+Hook("surging");a.Definition.PeriodicDamage=AssassinDeception?6:3;
            if(AssassinDeception)a.CooldownSeconds*=.6f;
        }
        if(id=="electrocute"){
            bool area=Hook("electrocute_area")>0;
            a.Definition.Area=area?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.MaximumTargets=area?3:1;a.Definition.SplashMultiplier=1;a.Radius=area?.28f:0;
        }
        if(id=="shock") {
            bool chain=Hook("chain_shock")>0, fork=As("as_chain");
            a.Definition.Area=chain || fork?AbilityArea.Chain:AbilityArea.TargetSplash;
            a.Definition.BounceCount=(chain?2:0)+(fork?2:0);
            a.Definition.BounceRange=.25f;a.Definition.BounceFalloff=fork?.8f:.65f;
        }
        if(id is "creepingterror" or "affliction" or "crushingdarkness" && As("as_decay"))a.TickSeconds/=1.35f;
        if(id=="maul" && Hook("maul_cleave")>0){a.Definition.Area=AbilityArea.TargetSplash;a.Radius=.22f;}

    }
    private void AssassinActivated(AutomaticAbility a)
    {
        if(!IsAssassin)return;
        if(a.Definition.Id is "spike" or "forcecloak" && As("as_ambush"))_world.Abilities.Find("maul")?.ReduceCooldown(4);
        if(a.Definition.Id is "forceshroud" or "deflection" && As("as_guard"))_shroudedEdge=5;
        if(a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack){_cloak=0;UpdateStealthVisual();}
        switch(a.Definition.Id){case "spike":_opening=4;_cloak=a.Definition.WindupSeconds;UpdateStealthVisual();break;case "shock":_shockSpent=true;break;case "forcecloak":_opening=4;_cloak=2;UpdateStealthVisual();break;case "deflection":_deflection=4+Hook("deflection_time");break;}
    }
    private void AssassinHit(AutomaticAbility a,Combatant target)
    {
        if(!IsAssassin)return;
        string id=a.Definition.Id;
        if(id=="electrocute" && target.Alive){
            if(target is EnemyAgent stunned)stunned.Stun(3,false);
            _electrified[target.GetInstanceId()]=6;
            if(Ready("electrocute_opening",.5)){
                _opening=4;
                if(Hook("electrocute_opening")>0)_world.Abilities.Find("maul")?.ReduceCooldown(4);
            }
        }
        if(id=="voltaicslash" && Ready("voltaic_hit",.5))_voltaic=Math.Min(2,_voltaic+1);
        if(id=="voltaicslash" && As("as_conduit") && Ready("as_conduit",.5))
            foreach(string follow in new[]{"shock","discharge"})if(_world.Abilities.Find(follow) is {Unlocked:true} next)next.ReduceCooldown(1);
        if(id=="wither" && As("as_wither") && Ready("as_wither",.5))
            foreach(string follow in new[]{"maul","voltaicslash"})if(_world.Abilities.Find(follow) is {Unlocked:true} next)next.ReduceCooldown(2);
        if(id=="shock")
        {
            float bonus=_world.Periodic.On(target).Where(x=>x.Caster==a.Caster).Sum(x=>x.Definition.PeriodicDamage*x.DamagePerTick/Math.Max(.001f,x.Definition.Damage))*(.25f+Hook("dot_shock")+(As("as_echo")?.75f:0));
            if(target.Alive && bonus>0)target.Health.ApplyDamage(bonus,DamageType.Internal,abilityId:"shock");
        }
        if(id is "spike" or "lowslash" && target is EnemyAgent enemy && !_control.ContainsKey(target.GetInstanceId()))
        {enemy.Stun(id=="spike"?1:2,false);_control[target.GetInstanceId()]=6;}
        if(id=="wither" && target is EnemyAgent weakened)weakened.OilRemaining=Math.Max(weakened.OilRemaining,4);
    }
    private void AdvanceAssassin(double delta)
    {
        _opening=Math.Max(0,_opening-delta);_cloak=Math.Max(0,_cloak-delta);_deflection=Math.Max(0,_deflection-delta);_shroudedEdge=Math.Max(0,_shroudedEdge-delta);
        UpdateStealthVisual();
        // Keep the empowerment through every link of a single Shock activation.
        if(_shockSpent && _world.Abilities.Find("shock") is {} shock && !shock.Casting){_voltaic=0;_shockSpent=false;}
        foreach(var id in _control.Keys.ToArray()){_control[id]-=delta;if(_control[id]<=0)_control.Remove(id);}
        foreach(var id in _electrified.Keys.ToArray()){_electrified[id]-=delta;if(_electrified[id]<=0)_electrified.Remove(id);}
    }
    private bool? _visibleBeforeStealth;
    public void RevealSpike(){if(IsAssassin){_cloak=0;UpdateStealthVisual();}}
    private void UpdateStealthVisual()
    {
        if(!IsAssassin || !GodotObject.IsInstanceValid(_world.Player.Presentation))return;
        var actor=_world.Player.Presentation;
        if(_cloak>0) {_visibleBeforeStealth??=actor.Visible;actor.Visible=false;}
        else if(_visibleBeforeStealth is bool visible){actor.Visible=visible;_visibleBeforeStealth=null;}
    }
    public void AssassinSpreadHit(AutomaticAbility a,IEnumerable<Combatant> fresh)
    {
        if(a.Definition.Id!="lacerate" || !As("as_spread"))return;
        foreach(var t in fresh.Distinct().Where(t=>t.Alive)) {
            float applied=t.Health.ApplyDamage(a.DamagePerTick*.75f*_world.Build.DamageMultiplier(a,t),DamageType.Internal,abilityId:"lacerate");
            if(applied>0 && a.Presentation is D3AbilityPresentation visual)visual.TargetImpact(t);
        }
    }
    private void ResetAssassin(){_opening=_cloak=_deflection=_shroudedEdge=0;_voltaic=0;_shockSpent=false;_control.Clear();_electrified.Clear();UpdateStealthVisual();}
}
