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
        public double Remaining,Next,Interval;public float Damage;public int Ticks,Dose;public Node3D? Visual;
    }
    private readonly Dictionary<(ulong,string,ulong),Effect> _effects=new();
    private double _elapsed;
    private readonly Dictionary<(ulong,string),double> _nextHeal=new();
    public void RemoveSource(AutomaticAbility source)
    {
        foreach(var pair in _effects.Where(p=>p.Value.Source==source).ToArray())
        {if(GodotObject.IsInstanceValid(pair.Value.Visual))pair.Value.Visual!.QueueFree();_effects.Remove(pair.Key);}
    }
    public int Count=>_effects.Count;
    public int Ticks {get;private set;}
    /// <summary>Raised after each applied periodic hit with that effect's own tick number (1-based).</summary>
    public event Action<AutomaticAbility,Combatant,int>? Ticked;
    public bool Has(Combatant target)=>_effects.Keys.Any(k=>k.Item3==target.GetInstanceId());
    public bool Has(AutomaticAbility source,Combatant target)=>_effects.ContainsKey((source.Caster.GetInstanceId(),source.Definition.Id,target.GetInstanceId()));
    public IReadOnlyList<AutomaticAbility> On(Combatant target)=>_effects.Where(p=>p.Key.Item3==target.GetInstanceId()).Select(p=>p.Value.Source).ToArray();
    public AutomaticAbility[] SpreadSources(Combatant caster,IEnumerable<Combatant> targets) => targets.SelectMany(On)
        .Where(a=>a.Caster==caster && a.Unlocked).DistinctBy(a=>a.Definition.Id).ToArray();
    /// <summary>Re-applies every owned DoT on the target at full duration (Static Blades / Searing Sweep).</summary>
    public void RefreshAll(Combatant target)
    { foreach(var e in _effects.Values.Where(e=>e.Target==target)){e.Remaining=e.Source.Definition.PeriodicDuration;} }
    public void Extend(AutomaticAbility source,Combatant target,float seconds,float cap)
    {if(_effects.TryGetValue((source.Caster.GetInstanceId(),source.Definition.Id,target.GetInstanceId()),out var e))e.Remaining=Math.Min(cap,e.Remaining+seconds);}
    public void Apply(AutomaticAbility source,Combatant target,float scale=1)
    {
        var d=source.Definition;
        if(d.PeriodicDuration<=0 || d.PeriodicInterval<=0 || !target.Alive)return;
        var key=(source.Caster.GetInstanceId(),d.Id,target.GetInstanceId());
        // Lethal Dose: a refresh deepens the poison (+10% per stack, 3 stacks) instead of only restarting it.
        int dose=_effects.TryGetValue(key,out var old)&&(source.Build?.Talent("lethal_dose")??0)>0?Math.Min(3,old.Dose+1):0;
        if(old?.Visual!=null && GodotObject.IsInstanceValid(old.Visual))old.Visual.QueueFree();
        var effect=new Effect {
            Source=source,Target=target,Remaining=source.Build?.Pregame.PeriodicDuration(source)??d.PeriodicDuration,
            Next=source.Build?.Pregame.PeriodicInterval(source)??d.PeriodicInterval,Interval=source.Build?.Pregame.PeriodicInterval(source)??d.PeriodicInterval,Dose=dose,
            Damage=d.PeriodicDamage*scale*(1+.1f*dose)*(source.DamagePerTick/Math.Max(.001f,d.Damage)) };
        _effects[key]=effect;
        if(d.Id=="affliction")effect.Visual=PresentationStatusVisual.AttachAffliction(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="crushingdarkness")effect.Visual=PresentationStatusVisual.AttachDarkness(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="creepingterror")effect.Visual=PresentationStatusVisual.AttachTerror(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="incendiarymissile")effect.Visual=PowertechStatusVisual.Attach(target,"4x4_flame_lick_02_bw",16,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
    }
    public double Remaining(AutomaticAbility source,Combatant target)=>_effects.GetValueOrDefault((source.Caster.GetInstanceId(),source.Definition.Id,target.GetInstanceId()))?.Remaining??0;
    public void TransferOwned(Combatant caster,Combatant dead)
    {
        foreach(var e in _effects.Values.Where(e=>e.Target==dead && e.Source.Caster==caster && e.Remaining>0 && e.Source.Unlocked).ToArray())
        {
            var skip=_effects.Values.Where(x=>x.Source.Caster==caster && x.Source.Definition.Id==e.Source.Definition.Id).Select(x=>x.Target.GetInstanceId()).ToHashSet();
            skip.Add(dead.GetInstanceId());
            var next=e.Source.Targeting.ChainTarget(dead.AimPosition,e.Source.Build?.Talent("last_gasp")>0?.45f:.3f,skip);
            if(next==null)continue;
            Apply(e.Source,next);
            var transferred=_effects[(caster.GetInstanceId(),e.Source.Definition.Id,next.GetInstanceId())];
            transferred.Remaining=e.Remaining;transferred.Next=e.Next;transferred.Interval=e.Interval;
            transferred.Damage=e.Damage;transferred.Dose=e.Dose;transferred.Ticks=e.Ticks;
        }
    }
    public void Advance(double delta)
    {
        double start=_elapsed;_elapsed+=delta;
        foreach(var pair in new List<KeyValuePair<(ulong,string,ulong),Effect>>(_effects))
        {
            var e=pair.Value;
            if(!GodotObject.IsInstanceValid(e.Target) || !e.Target.Alive || !GodotObject.IsInstanceValid(e.Source.Caster) || !e.Source.Caster.Alive || !e.Source.Unlocked)
            { _effects.Remove(pair.Key);continue; }
            double interval=e.Source.Build?.Pregame.PeriodicInterval(e.Source)??e.Source.Definition.PeriodicInterval;
            if(Math.Abs(interval-e.Interval)>.000001){e.Next*=interval/e.Interval;e.Interval=interval;}
            double active=Math.Min(delta,e.Remaining);e.Next-=active;e.Remaining-=delta;
            while(e.Next<=.000001 && e.Target.Alive)
            {
                double tickAt=start+active+e.Next;e.Next+=e.Interval;int before=e.Source.DamageEvents;
                e.Source.ApplyPeriodicHit(e.Target,e.Damage*(e.Source.Build?.Pregame.PeriodicMultiplier(e.Target)??1));
                var healKey=(e.Source.Caster.GetInstanceId(),e.Source.Definition.Id);
                if(e.Source.DamageEvents>before && e.Source.Definition.PeriodicHealing>0 && tickAt+0.00001>=_nextHeal.GetValueOrDefault(healKey))
                {e.Source.Caster.Health.ApplyHealing(e.Source.Definition.PeriodicHealing);_nextHeal[healKey]=tickAt+1;}
                Ticks++;e.Ticks++;Ticked?.Invoke(e.Source,e.Target,e.Ticks);
            }
            if(e.Remaining<=0 || !e.Target.Alive)_effects.Remove(pair.Key);
        }
    }
    public override void _PhysicsProcess(double delta)=>Advance(delta);
    public void Clear(){foreach(var e in _effects.Values)if(e.Visual!=null && GodotObject.IsInstanceValid(e.Visual))e.Visual.QueueFree();_effects.Clear();_nextHeal.Clear();_elapsed=0;Ticks=0;}
}
