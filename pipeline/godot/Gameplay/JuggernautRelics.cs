using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;

// Echoes dispatch damage once, without re-entering cast, talent or relic hooks.
public sealed class JuggernautRelics(RunBuildEffects build)
{
    private GameWorld W=>build.World;
    private double _time;
    private float _healed;
    private readonly List<(AutomaticAbility Source,Combatant? Target,Vector3 Center,float Radius,float Damage,double At,string Relic)> _echoes=new();
    public int Rank(string id) {
        if(W.RunConfiguration.AdvancedClassId!="juggernaut")return -1;
        var inv=UI.EquipmentWindows.Instance?.Inventory;
        return inv!=null && PersistentInventory.Slots.Where(s=>s.StartsWith("relic")).Any(s=>inv.Equipped("juggernaut",s)==id)?inv.State.Owned.GetValueOrDefault(id,-1):-1;
    }
    public float Bonus=>Rank("undying_wrath") is int r && r>=0 && build.Pregame.JuggernautAuraActive("enrageddefense")?.2f+.1f*r:0;
    public double HealInterval=>Rank("undying_wrath") is int r && r>=0?1d/(2+r):1;
    public void DefenseStarted()=>_healed=0;
    public float CappedHealing(float amount) {
        int r=Rank("undying_wrath");if(r<0)return amount;
        float heal=Math.Min(amount,Math.Max(0,32+8*r-_healed));_healed+=heal;return heal;
    }
    public float Range(AutomaticAbility a)=>a.Range*(a.Definition.Id=="retaliation" && Rank("vengeful_retribution") is int r && r>=0?1.25f+.25f*r:1);
    public bool OverridesTargets(AutomaticAbility a)=>a.Definition.Id=="forcecrush" && Rank("endless_fury")>=0 || a.Definition.Id=="ravage" && Rank("ravagers_wrath")>=0;
    public Combatant[] Targets(AutomaticAbility a,Combatant[] normal) {
        if(a.Caster!=W.PlayerActor || !OverridesTargets(a) || a.CastTarget is not {} primary || !GodotObject.IsInstanceValid(primary))return normal;
        bool crush=a.Definition.Id=="forcecrush";int r=Rank(crush?"endless_fury":"ravagers_wrath");
        // Primary first; no overlap with the normal dispatch, even with cleave talents.
        return a.Targeting.InArea(primary.GlobalPosition,crush?.4f:a.Range,a.Range,true).Append(primary).Distinct()
            .Where(t=>t.Alive).OrderBy(t=>t==primary?0:1).ThenBy(t=>t.GlobalPosition.DistanceSquaredTo(primary.GlobalPosition))
            .ThenBy(t=>t.SelectionKey,StringComparer.Ordinal).Take(crush?2+r:Math.Max(3+r,a.Definition.MaximumTargets)).ToArray();
    }
    public void Resolved(AutomaticAbility a,Combatant[] targets) {
        if(a.Caster!=W.PlayerActor)return;
        string id=a.Definition.Id=="smash"?"worldbreaker":a.Definition.Id=="impale"?"impaling_fury":"";
        int r=Rank(id);if(r<0)return;
        int count=new[]{2,3,3,4}[r];
        for(int i=1;i<count;i++) {
            if(id=="worldbreaker")_echoes.Add((a,null,a.Caster.GlobalPosition,a.Radius,a.DamagePerTick,_time+(.35-.05*r)*i,id));
            else foreach(var t in targets)_echoes.Add((a,t,t.GlobalPosition,0,a.DamagePerTick*build.DamageMultiplier(a,t),_time+(.25-.025*r)*i,id));
        }
    }
    public void Advance(double delta) {
        _time+=delta;
        var retaliation=W.Abilities.Find("retaliation");int r=Rank("vengeful_retribution");
        if(r>=0 && retaliation is {Unlocked:true,CombatEnabled:true} && W.PlayerActor.Alive) {
            retaliation.ReduceCooldown((float)delta*(.25f+.15f*r));
            // The ordinary activation gate consumes the damage reaction and starts its cooldown.
            retaliation.TryBegin();
        }
        foreach(var e in _echoes.Where(e=>e.At<=_time).ToArray()) {
            _echoes.Remove(e);
            if(Rank(e.Relic)<0 || !e.Source.Caster.Alive || !e.Source.CombatEnabled || !e.Source.Unlocked)continue;
            if(e.Target==null)e.Source.Presentation.Impact(e.Center);
            var victims=e.Target==null?e.Source.Targeting.InArea(e.Center,e.Radius,e.Radius):new[]{e.Target};
            foreach(var t in victims) {
                if(!GodotObject.IsInstanceValid(t) || !t.Alive)continue;
                t.Health.ApplyDamage(e.Damage*(e.Target==null?build.DamageMultiplier(e.Source,t):1),e.Target==null?DamageType.Kinetic:DamageType.Energy,false,e.Source.Definition.Id);
                if(e.Source.Presentation is D3AbilityPresentation visual)visual.TargetImpact(t);
            }
        }
    }
    public void Reset(){_time=0;_healed=0;_echoes.Clear();}
}
