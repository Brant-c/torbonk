using Godot;
using System;
using System.Collections.Generic;
namespace SwtorSurvivors;

/// <summary>One refreshable instance per caster/ability/target; no mesh or range dependency after application.</summary>
public partial class PeriodicEffects : Node
{
    private sealed class Effect
    {
        public AutomaticAbility Source=null!;public Combatant Target=null!;
        public double Remaining,Next,Interval;public float Damage;
    }
    private readonly Dictionary<(ulong,string,ulong),Effect> _effects=new();
    public int Count=>_effects.Count;
    public int Ticks {get;private set;}
    public void Apply(AutomaticAbility source,Combatant target)
    {
        var d=source.Definition;
        if(d.PeriodicDuration<=0 || d.PeriodicInterval<=0 || !target.Alive)return;
        _effects[(source.Caster.GetInstanceId(),d.Id,target.GetInstanceId())]=new Effect {
            Source=source,Target=target,Remaining=d.PeriodicDuration,Next=d.PeriodicInterval,Interval=d.PeriodicInterval,
            Damage=d.PeriodicDamage*(source.DamagePerTick/Math.Max(.001f,d.Damage)) };
    }
    public void Advance(double delta)
    {
        foreach(var pair in new List<KeyValuePair<(ulong,string,ulong),Effect>>(_effects))
        {
            var e=pair.Value;
            if(!GodotObject.IsInstanceValid(e.Target) || !e.Target.Alive || !GodotObject.IsInstanceValid(e.Source.Caster) || !e.Source.Caster.Alive)
            { _effects.Remove(pair.Key);continue; }
            double active=Math.Min(delta,e.Remaining);e.Next-=active;e.Remaining-=delta;
            while(e.Next<=.000001 && e.Target.Alive)
            { e.Next+=e.Interval;e.Source.ApplyPeriodicHit(e.Target,e.Damage);Ticks++; }
            if(e.Remaining<=0 || !e.Target.Alive)_effects.Remove(pair.Key);
        }
    }
    public override void _PhysicsProcess(double delta)=>Advance(delta);
    public void Clear(){_effects.Clear();Ticks=0;}
}
