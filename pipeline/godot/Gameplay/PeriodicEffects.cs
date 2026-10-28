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
        public bool RelicApplied;public double AcceleratedUntil;public float TickRate=1;
        public double Remaining,Next,Interval;public float Damage;public int Ticks,Dose;public Node3D? Visual;
    }
    private readonly Dictionary<(ulong,string,ulong),Effect> _effects=new();
    private double _elapsed;
    private readonly Dictionary<(ulong,string),double> _nextHeal=new();
    public void RemoveSource(AutomaticAbility source)
    {
        foreach(var pair in _effects.Where(p=>p.Value.Source==source).ToArray())
        {if(GodotObject.IsInstanceValid(pair.Value.Visual))pair.Value.Visual!.QueueFree();_effects.Remove(pair.Key);RecalculateSlow(pair.Value.Target);}
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
    public void Apply(AutomaticAbility source,Combatant target,float scale=1,bool relicApplied=false)
    {
        var d=source.Definition;
        if(d.PeriodicDuration<=0 || d.PeriodicInterval<=0 || !target.Alive)return;
        var key=(source.Caster.GetInstanceId(),d.Id,target.GetInstanceId());
        // Lethal Dose: a refresh deepens the poison (+10% per stack, 3 stacks) instead of only restarting it.
        int dose=_effects.TryGetValue(key,out var old)&&(source.Build?.Talent("lethal_dose")??0)>0?Math.Min(3,old.Dose+1):0;
        if(old?.Visual!=null && GodotObject.IsInstanceValid(old.Visual))old.Visual.QueueFree();
        var effect=new Effect {
            Source=source,Target=target,RelicApplied=relicApplied,AcceleratedUntil=old?.AcceleratedUntil??0,TickRate=old?.TickRate??1,Remaining=source.Build?.Pregame.PeriodicDuration(source)??d.PeriodicDuration,
            Next=source.Build?.Pregame.PeriodicInterval(source)??d.PeriodicInterval,Interval=source.Build?.Pregame.PeriodicInterval(source)??d.PeriodicInterval,Dose=dose,
            Damage=d.PeriodicDamage*scale*(1+.1f*dose)*(source.DamagePerTick/Math.Max(.001f,d.Damage)) };
        _effects[key]=effect;
        RecalculateSlow(target);
        if(source.Build?.World.RunConfiguration.AdvancedClassId is "marauder" or "juggernaut")
            effect.Visual=PresentationStatusVisual.AttachWarrior(target,d.Id=="forcecrush",()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="discharge")effect.Visual=PresentationStatusVisual.AttachDischarge(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="affliction")effect.Visual=PresentationStatusVisual.AttachAffliction(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="crushingdarkness")effect.Visual=PresentationStatusVisual.AttachDarkness(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="creepingterror")effect.Visual=PresentationStatusVisual.AttachTerror(target,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
        if(d.Id=="incendiarymissile")effect.Visual=PowertechStatusVisual.Attach(target,"4x4_flame_lick_02_bw",16,()=>_effects.TryGetValue(key,out var active) && active==effect && effect.Remaining>0 && GodotObject.IsInstanceValid(source.Caster) && source.Caster.Alive);
    }
    public double Remaining(AutomaticAbility source,Combatant target)=>_effects.GetValueOrDefault((source.Caster.GetInstanceId(),source.Definition.Id,target.GetInstanceId()))?.Remaining??0;
    public void TransferOwned(Combatant caster,Combatant dead)
    {
        foreach(var e in _effects.Values.Where(e=>e.Target==dead && e.Source.Caster==caster && e.Remaining>0 && (e.Source.Unlocked || e.RelicApplied)).ToArray())
        {
            var skip=_effects.Values.Where(x=>x.Source.Caster==caster && x.Source.Definition.Id==e.Source.Definition.Id).Select(x=>x.Target.GetInstanceId()).ToHashSet();
            skip.Add(dead.GetInstanceId());
            var next=e.Source.Targeting.ChainTarget(dead.AimPosition,e.Source.Build?.Talent("last_gasp")>0?.45f:.3f,skip);
            if(next==null)continue;
            Apply(e.Source,next);
            var transferred=_effects[(caster.GetInstanceId(),e.Source.Definition.Id,next.GetInstanceId())];
            transferred.Remaining=e.Remaining;transferred.Next=e.Next;transferred.Interval=e.Interval;
            transferred.Damage=e.Damage;transferred.Dose=e.Dose;transferred.Ticks=e.Ticks;transferred.RelicApplied=e.RelicApplied;transferred.AcceleratedUntil=e.AcceleratedUntil;transferred.TickRate=e.TickRate;
        }
    }
    public void Advance(double delta)
    {
        foreach(var enemy in _slowed.ToArray())RecalculateSlow(enemy);
        double start=_elapsed;_elapsed+=delta;
        foreach(var pair in new List<KeyValuePair<(ulong,string,ulong),Effect>>(_effects))
        {
            var e=pair.Value;
            if(!GodotObject.IsInstanceValid(e.Target) || !e.Target.Alive || !GodotObject.IsInstanceValid(e.Source.Caster) || !e.Source.Caster.Alive || (!e.Source.Unlocked && !e.RelicApplied))
            { _effects.Remove(pair.Key);continue; }
            double interval=e.Source.Build?.Pregame.PeriodicInterval(e.Source)??e.Source.Definition.PeriodicInterval;
            if(Math.Abs(interval-e.Interval)>.000001){e.Next*=interval/e.Interval;e.Interval=interval;}
            double active=Math.Min(delta,e.Remaining);
            double boosted=Math.Clamp(e.AcceleratedUntil-start,0,active);
            if(SorcererRelics.Rank(e.Source,"accelerated_torment")<0 && (e.Source.Build?.MarauderRelics?.Rank("accelerated_agony")??-1)<0)boosted=0;
            e.Next-=active+boosted*(e.TickRate-1);e.Remaining-=delta;
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
            RecalculateSlow(e.Target);
        }
    }
    private readonly HashSet<EnemyAgent> _slowed=new();
    private void RecalculateSlow(Combatant target)
    {
        if(target is not EnemyAgent enemy || !GodotObject.IsInstanceValid(enemy))return;
        var owned=_effects.Values.Where(e=>e.Target==target && e.Remaining>0 && SorcererRelics.Rank(e.Source,"grip_of_madness")>=0).ToArray();
        float slow=owned.GroupBy(e=>e.Source.Caster).Select(g=>Math.Min(.42f,g.Select(e=>e.Source.Definition.Id).Distinct().Count()*(.08f+.02f*SorcererRelics.Rank(g.First().Source,"grip_of_madness")))).DefaultIfEmpty(0).Max();
        enemy.SorcererDotMovementFactor=1-slow;
        if(slow>0)_slowed.Add(enemy);else _slowed.Remove(enemy);
    }
    public void Accelerate(AutomaticAbility shock,Combatant target)
    {
        int rank=SorcererRelics.Rank(shock,"accelerated_torment");if(rank<0)return;
        foreach(var e in _effects.Values.Where(e=>e.Target==target && e.Source.Caster==shock.Caster)) {
            e.AcceleratedUntil=_elapsed+new[]{4,4,4.5,5}[rank];e.TickRate=new[]{1.5f,1.65f,1.8f,2f}[rank];
        }
    }
    public void AccelerateMarauder(Combatant caster,float rate,float seconds)
    {
        foreach(var e in _effects.Values.Where(e=>e.Source.Caster==caster && e.Source.Definition.Id is "rupture" or "deadlysaber")) {e.TickRate=rate;e.AcceleratedUntil=_elapsed+seconds;}
    }
    // Snapshot actual scheduled ticks, including acceleration, without consuming the effect.
    public float RemainingDamage(Combatant caster,Combatant target,bool bleed) => (float)_effects.Values
        .Where(e=>e.Source.Caster==caster && e.Target==target && (bleed?e.Source.Definition.Id is "rupture" or "deadlysaber":e.Source.Definition.Id=="forcecrush"))
        .Sum(e=>Math.Max(0,Math.Floor((e.Remaining+((e.Source.Build?.MarauderRelics?.Rank("accelerated_agony")??-1)>=0?Math.Clamp(e.AcceleratedUntil-_elapsed,0,e.Remaining)*(e.TickRate-1):0)-e.Next)/e.Interval+1.000001))*e.Damage*(e.Source.Build?.DamageMultiplier(e.Source,target)??1)*(e.Source.Build?.Pregame.PeriodicMultiplier(target)??1));
    private bool _detonating;
    public void Detonate(Combatant dead)
    {
        if(_detonating)return;
        var effects=_effects.Values.Where(e=>e.Target==dead && e.Remaining>0 && SorcererRelics.Rank(e.Source,"lingering_agony")>=0).ToArray();
        _detonating=true;
        try {
            foreach(var group in effects.GroupBy(e=>e.Source.Caster)) {
                var source=group.First().Source;int rank=SorcererRelics.Rank(source,"lingering_agony");
                float damage=(float)group.Sum(e=>Math.Max(0,Math.Floor((e.Remaining+(SorcererRelics.Rank(e.Source,"accelerated_torment")>=0?Math.Clamp(e.AcceleratedUntil-_elapsed,0,e.Remaining)*(e.TickRate-1):0)-e.Next)/e.Interval+1.000001))*e.Damage*(e.Source.Build?.DamageMultiplier(e.Source,dead)??1)*(e.Source.Build?.Pregame.PeriodicMultiplier(dead)??1));
                damage*=new[]{.5f,.65f,.8f,1f}[rank];
                foreach(var target in source.Targeting.InArea(dead.GlobalPosition,.3f+.1f*rank,source.Range,true).ToArray())
                    target.Health.ApplyDamage(damage,DamageType.Internal,false,"lingering_agony");
            }
        } finally {_detonating=false;}
    }
    public override void _PhysicsProcess(double delta)=>Advance(delta);
    public void Clear(){foreach(var enemy in _slowed)if(GodotObject.IsInstanceValid(enemy))enemy.SorcererDotMovementFactor=1;_slowed.Clear();foreach(var e in _effects.Values)if(e.Visual!=null && GodotObject.IsInstanceValid(e.Visual))e.Visual.QueueFree();_effects.Clear();_nextHeal.Clear();_elapsed=0;Ticks=0;}
}
