using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    private bool IsAssassin=>_world.RunConfiguration.AdvancedClassId=="assassin";
    private double _opening,_cloak,_deflection;
    private int _voltaic;
    private bool _shockSpent;
    private readonly Dictionary<ulong,double> _control=new();
    public float AssassinDeflectionDuration=>4+Hook("deflection_time");
    public bool AssassinDeflecting=>IsAssassin && _deflection>0;
    public bool AssassinCloaked=>IsAssassin && _cloak>0;
    public bool StealthOpening=>IsAssassin && _opening>0;
    public int VoltaicCharges=>IsAssassin?_voltaic:0;
    public bool AssassinSpread=>IsAssassin && Hook("spread")>0;
    private float AssassinIncoming=>!IsAssassin?1:(1-Math.Min(.15f,Hook("mitigation")))*(_cloak>0?.35f:1)*(_deflection>0?.5f:1);
    private float AssassinBonus(AutomaticAbility a,Combatant t)=>!IsAssassin?0:
        (a.Definition.Id=="shock"?.2f*_voltaic:0)+(a.Definition.Id=="maul" && _opening>0?.5f+Hook("ambush"):0)
        +(a.Definition.Id=="discharge"?.1f*_voltaic:0)
        +(a.Definition.Id is "lacerate" or "voltaicslash" or "maul" && t is EnemyAgent e && e.OilRemaining>0?.1f:0);
    public string[] EarlyAbilityOrder {
        get {
            int madness=TalentCatalog.Current.ForClass("assassin").Where(t=>t.Tree=="assassin_madness").Sum(t=>Ranks.GetValueOrDefault(t.Id));
            int deception=TalentCatalog.Current.ForClass("assassin").Where(t=>t.Tree=="deception").Sum(t=>Ranks.GetValueOrDefault(t.Id));
            return madness>0 && madness>=deception?new[]{"creepingterror","deathfield","discharge","spike","shock","voltaicslash"}:
                deception>0?new[]{"discharge","spike","voltaicslash","shock","creepingterror","deathfield"}:new[]{"creepingterror","discharge","deathfield","spike","voltaicslash","shock"};
        }
    }
    private void AssassinStats(AutomaticAbility a)
    {
        if(!IsAssassin)return;
        string id=a.Definition.Id;
        if(id=="discharge") {a.DamagePerTick*=1+Hook("surging");}
        if(id=="shock" && Hook("chain_shock")>0){a.Definition.Area=AbilityArea.Chain;a.Definition.BounceCount=2;a.Definition.BounceRange=.25f;a.Definition.BounceFalloff=.65f;}
        if(id=="maul" && Hook("maul_cleave")>0){a.Definition.Area=AbilityArea.TargetSplash;a.Radius=.22f;}

    }
    private void AssassinActivated(AutomaticAbility a)
    {
        if(!IsAssassin)return;
        if(a.Definition.Effect==AbilityEffect.Damage && !a.Definition.IsBasicAttack){_cloak=0;UpdateStealthVisual();}
        switch(a.Definition.Id){case "spike":_opening=4;_cloak=a.Definition.WindupSeconds;UpdateStealthVisual();break;case "shock":_shockSpent=true;break;case "forcecloak":_opening=4;_cloak=2;UpdateStealthVisual();break;case "deflection":_deflection=4+Hook("deflection_time");break;}
    }
    private void AssassinHit(AutomaticAbility a,Combatant target)
    {
        if(!IsAssassin)return;
        string id=a.Definition.Id;
        if(id=="voltaicslash" && Ready("voltaic_hit",.5))_voltaic=Math.Min(2,_voltaic+1);
        if(id=="shock")
        {
            float bonus=_world.Periodic.On(target).Where(x=>x.Caster==a.Caster).Sum(x=>x.Definition.PeriodicDamage*x.DamagePerTick/Math.Max(.001f,x.Definition.Damage))*(.25f+Hook("dot_shock"));
            if(target.Alive && bonus>0)target.Health.ApplyDamage(bonus,DamageType.Internal);
        }
        if(id is "spike" or "lowslash" && target is EnemyAgent enemy && !_control.ContainsKey(target.GetInstanceId()))
        {enemy.Stun(id=="spike"?1:2,false);_control[target.GetInstanceId()]=6;}
        if(id=="wither" && target is EnemyAgent weakened)weakened.OilRemaining=Math.Max(weakened.OilRemaining,4);
    }
    private void AdvanceAssassin(double delta)
    {
        _opening=Math.Max(0,_opening-delta);_cloak=Math.Max(0,_cloak-delta);_deflection=Math.Max(0,_deflection-delta);
        UpdateStealthVisual();
        // Keep the empowerment through every link of a single Shock activation.
        if(_shockSpent && _world.Abilities.Find("shock") is {} shock && !shock.Casting){_voltaic=0;_shockSpent=false;}
        foreach(var id in _control.Keys.ToArray()){_control[id]-=delta;if(_control[id]<=0)_control.Remove(id);}
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
    private void ResetAssassin(){_opening=_cloak=_deflection=0;_voltaic=0;_shockSpent=false;_control.Clear();UpdateStealthVisual();}
}
