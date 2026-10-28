using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;

// Secondary hits never enter ability/proc hooks. State belongs to this run only.
public sealed class MarauderRelics(RunBuildEffects build)
{
    private GameWorld W=>build.World;
    private double _time,_exsang,_frenzy,_goreRecovery;
    private float _stored;
    private int _shock,_castShock;
    private bool _engaged,_castEngaged;
    private int _throwCast=-1;
    private readonly List<(AutomaticAbility Source,Vector3 Center,float Radius,float Damage,double At)> _echoes=new();
    public int Rank(string id) {
        if(W.RunConfiguration.AdvancedClassId!="marauder")return -1;
        var inv=UI.EquipmentWindows.Instance?.Inventory;
        return inv!=null && PersistentInventory.Slots.Where(s=>s.StartsWith("relic")).Any(s=>inv.Equipped("marauder",s)==id)?inv.State.Owned.GetValueOrDefault(id,-1):-1;
    }
    public float Acceleration=>Rank("predatory_carnage") is int r && r>=0 && build.Pregame.MarauderAuraActive("predation")?1.2f+.1f*r:1;
    public float AtaruInterval=>Rank("ataru_frenzy") is int r && r>=0 && _frenzy>_time?.9f-.15f*r:1.5f;
    public int AtaruTargets=>Rank("ataru_frenzy") is int r && r>=0 && _frenzy>_time?3+r:3;
    public float AtaruBonus=>Rank("ataru_frenzy") is int r && r>=0 && _frenzy>_time?1.25f+.15f*r:1;
    public bool TargetSmash=>_castEngaged && Rank("obliteration")>=0;
    public float Radius(AutomaticAbility a) => (W.RunConfiguration.AdvancedClassId!="marauder"?build.AssassinRelics?.Radius(a)??a.Radius:a.Radius)*(a.Definition.Id=="smash"?1+(_castShock>0 && Rank("shockwave") is int r && r>=0?_castShock*(.08f+.04f*r):0)+(TargetSmash?.2f+.1f*Rank("obliteration"):0):1);
    public float Bonus(AutomaticAbility a,Combatant t)=>a.Definition.Id=="smash"?(_castShock>0 && Rank("shockwave") is int r && r>=0?_castShock*(.1f+.05f*r):0)+(TargetSmash?.3f+.15f*Rank("obliteration"):0):0;
    public void Activated(AutomaticAbility a) {
        if(a.Caster!=W.PlayerActor)return;
        if(a.Definition.Id=="massacre") {
            _frenzy=_time+6;
            if(Rank("predatory_carnage") is int r && r>=0 && build.Pregame.MarauderAuraActive("predation") && _time>=_goreRecovery){_goreRecovery=_time+.5;W.Abilities.Find("gore")?.ReduceCooldown(1+.5f*r);}
        }
        if(a.Definition.Id is "obliterate" or "forcecharge")_engaged=true;
        if(a.Definition.Id=="smash"){_castShock=_shock;_shock=0;_castEngaged=_engaged;_engaged=false;}
    }
    private void Hit(AutomaticAbility a,Combatant t,float damage,DamageType type) {
        if(!t.Alive || !a.Caster.Alive || damage<=0)return;
        t.Health.ApplyDamage(damage,type,false,a.Definition.Id);
        if(a.Presentation is D3AbilityPresentation v)v.TargetImpact(t);
    }
    public void Resolved(AutomaticAbility a,Combatant[] targets) {
        if(a.Caster!=W.PlayerActor)return;
        int r;
        if(a.Definition.Id=="annihilate") {
            if((r=Rank("accelerated_agony"))>=0)W.Periodic.AccelerateMarauder(a.Caster,1.5f+r/6f,3+2f*r/3);
            var t=a.CastTarget;
            if(t!=null && (r=Rank("exsanguination"))>=0 && _time>=_exsang){_exsang=_time+2;Hit(a,t,W.Periodic.RemainingDamage(a.Caster,t,true)*(.2f+.1f*r),DamageType.Internal);}
        }
        if(a.Definition.Id is "annihilate" or "viciousthrow" && Rank("crimson_executioner")>=0 && a.CastTarget is {} release && release.Alive){float damage=_stored;_stored=0;Hit(a,release,damage,DamageType.Internal);}
        if(a.Definition.Id is "massacre" or "ravage" && build.Pregame.MarauderAuraActive("gore") && (r=Rank("gore_master"))>=0)
            foreach(var t in targets)Hit(a,t,a.DamagePerTick*(.25f+.15f*r)*build.DamageMultiplier(a,t),DamageType.Energy);
        if(a.Definition.Id=="saberthrow" && (r=Rank("twin_blades"))>=0) {
            var ds=W.Abilities.Find("deadlysaber");
            bool extra=_throwCast!=a.Casts;_throwCast=a.Casts;
            foreach(var t in targets.Take(2+r)) {
                if(ds!=null && !W.Periodic.Has(ds,t))W.Periodic.Apply(ds,t,relicApplied:true);
                if(r>=1 && extra)Hit(a,t,a.DamagePerTick*(.25f*r),DamageType.Energy);
            }
        }
        if(a.Definition.Id=="smash") {
            if((r=Rank("crushing_fury"))>=0)foreach(var t in targets)Hit(a,t,W.Periodic.RemainingDamage(a.Caster,t,false)*(.2f+.1f*r),DamageType.Internal);
            if(Rank("shockwave")==3 && _castShock>0)_echoes.Add((a,TargetSmash?a.CastTarget?.GlobalPosition??a.Caster.GlobalPosition:a.Caster.GlobalPosition,Radius(a),a.DamagePerTick*.6f*(1+Bonus(a,a.Caster)),_time+.45));
        }
    }
    public void Periodic(AutomaticAbility a){if(a.Caster==W.PlayerActor && a.Definition.Id=="forcecrush" && Rank("shockwave")>=0)_shock=Math.Min(3,_shock+1);}
    public void Killed(Combatant dead) {
        int r=Rank("crimson_executioner");
        if(r>=0)_stored=Math.Min(150+150*r,_stored+W.Periodic.RemainingDamage(W.PlayerActor,dead,true)*(.25f+.15f*r));
        r=Rank("endless_berserk");if(r>=0)build.Pregame.ExtendMarauderBerserk(.5f+.5f*r,10+4*r);
    }
    public void Advance(double delta) {
        _time+=delta;
        foreach(var e in _echoes.Where(e=>e.At<=_time).ToArray()) {
            _echoes.Remove(e);if(Rank("shockwave")!=3 || !e.Source.Caster.Alive || !e.Source.CombatEnabled)continue;
            e.Source.Presentation.Impact(e.Center);
            foreach(var t in e.Source.Targeting.InArea(e.Center,e.Radius,e.Source.Range,true))Hit(e.Source,t,e.Damage,DamageType.Kinetic);
        }
    }
    public void Reset(){_time=_exsang=_frenzy=_goreRecovery=0;_stored=0;_shock=_castShock=0;_engaged=_castEngaged=false;_echoes.Clear();_throwCast=-1;}
}
