using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;

/// <summary>One refreshable instance per caster/ability/target; no mesh or range dependency after application.</summary>
public partial class PeriodicEffects : Node
{
    private sealed class Effect
    {
        public AutomaticAbility Source=null!;public Combatant Target=null!;
        public double Remaining,Next,Interval;public float Damage;public int Ticks,Dose;
    }
    private readonly Dictionary<(ulong,string,ulong),Effect> _effects=new();
    public int Count=>_effects.Count;
    public int Ticks {get;private set;}
    /// <summary>Raised after each applied periodic hit with that effect's own tick number (1-based).</summary>
    public event Action<AutomaticAbility,Combatant,int>? Ticked;
    public bool Has(Combatant target)=>_effects.Keys.Any(k=>k.Item3==target.GetInstanceId());
    public bool Has(AutomaticAbility source,Combatant target)=>_effects.ContainsKey((source.Caster.GetInstanceId(),source.Definition.Id,target.GetInstanceId()));
    public IReadOnlyList<AutomaticAbility> On(Combatant target)=>_effects.Where(p=>p.Key.Item3==target.GetInstanceId()).Select(p=>p.Value.Source).ToArray();
    /// <summary>Re-applies every owned DoT on the target at full duration (Static Blades / Searing Sweep).</summary>
    public void RefreshAll(Combatant target)
    { foreach(var e in _effects.Values.Where(e=>e.Target==target)){e.Remaining=e.Source.Definition.PeriodicDuration;} }
    public void Apply(AutomaticAbility source,Combatant target,float scale=1)
    {
        var d=source.Definition;
        if(d.PeriodicDuration<=0 || d.PeriodicInterval<=0 || !target.Alive)return;
        var key=(source.Caster.GetInstanceId(),d.Id,target.GetInstanceId());
        // Lethal Dose: a refresh deepens the poison (+10% per stack, 3 stacks) instead of only restarting it.
        int dose=_effects.TryGetValue(key,out var old)&&(source.Build?.Talent("lethal_dose")??0)>0?Math.Min(3,old.Dose+1):0;
        _effects[key]=new Effect {
            Source=source,Target=target,Remaining=d.PeriodicDuration,Next=d.PeriodicInterval,Interval=d.PeriodicInterval,Dose=dose,
            Damage=d.PeriodicDamage*scale*(1+.1f*dose)*(source.DamagePerTick/Math.Max(.001f,d.Damage)) };
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
            { e.Next+=e.Interval;e.Source.ApplyPeriodicHit(e.Target,e.Damage);Ticks++;e.Ticks++;Ticked?.Invoke(e.Source,e.Target,e.Ticks); }
            if(e.Remaining<=0 || !e.Target.Alive)_effects.Remove(pair.Key);
        }
    }
    public override void _PhysicsProcess(double delta)=>Advance(delta);
    public void Clear(){_effects.Clear();Ticks=0;}
}
