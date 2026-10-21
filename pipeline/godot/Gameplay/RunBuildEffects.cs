using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;

/// <summary>Run-only mechanical hooks. Equipment modifiers are derived from current ownership, never accumulated.</summary>
public partial class RunBuildEffects : Node
{
    public GameWorld World {get;set;}=null!;
    private double _haste,_conduitLock,_buff;
    private float _damageBuff,_speedBuff;
    private bool _guardianShield;
    public void EquipmentRemoved(RunArtifactDefinition item)
    {
        if(item.Effect=="guardian" && _guardianShield)World.PlayerActor.Health.ClearShield();
        if(item.Effect=="vulnerability")
            foreach(var actor in GetTree().GetNodesInGroup(Combatant.GroupName).OfType<Combatant>())
                if(World.PlayerActor.IsHostileTo(actor))actor.Health.VulnerabilitySeconds=0;
    }
    public int Talent(string key) => World.Progression?.Upgrades.Where(u=>u.Mechanic==key).Sum(u=>World.Progression.Rank(u))??0;
    public float Item(string key) => key=="health_cost"?World.Artifacts?.Equipped.Sum(a=>a.HealthCost)??0:World.Artifacts?.Equipped.Where(a=>a.Effect==key).Sum(a=>a.Amount)??0;
    public float DamageMultiplier(AutomaticAbility ability,Combatant target)
    {
        float bonus=Item("sacrifice");
        if(World.PlayerActor.Health.Current<World.PlayerActor.Health.Maximum*0.35f)bonus+=0.35f*Talent("bloodlust");
        if(_buff>0)bonus+=_damageBuff;
        return (1+bonus)*(target.Health.VulnerabilitySeconds>0?1.2f:1);
    }
    public float HealingAmount(float amount) => amount*(World.PlayerActor.Health.Current<World.PlayerActor.Health.Maximum*0.3f?1+Item("restoration"):1);
    public void Activated(AutomaticAbility ability)
    {
        if(ability.Definition.Category==AbilityCategory.Utility && Talent("haste")>0)
        { _haste=3;World.Progression?.Recalculate(); }
    }
    public float SpeedMultiplier => (_haste>0?1.3f:1)*(_buff>0?1+_speedBuff:1);
    public bool Buffed => _buff>0;
    public void ApplyBuff(AbilityDefinition d)
    { _buff=d.BuffSeconds;_damageBuff=d.DamageBuff;_speedBuff=d.SpeedBuff;World.Progression?.Recalculate(); }
    /// <summary>Per-hit class mechanics that need the ability, not just the target.</summary>
    public void AbilityHit(AutomaticAbility ability,Combatant target)
    {
        if(Talent("static_blades")>0 && ability.Definition.PeriodicDuration<=0 && World.Periodic.Has(target))World.Periodic.RefreshAll(target);
    }
    /// <summary>DoT mechanics: Contagion jumps every third tick; Blood Rhythm shortens Rupture's cooldown.</summary>
    private void PeriodicTicked(AutomaticAbility source,Combatant target,int tick)
    {
        if(Talent("contagion")>0 && tick%3==0)Spread(source,target);
        if(Talent("blood_rhythm")>0 && tick%4==0)source.ReduceCooldown(0.5f);
    }
    /// <summary>Last Gasp / Wildfire: an afflicted enemy passes its DoTs on when it dies.</summary>
    public void PassOn(Combatant dead)
    {
        if(Talent("last_gasp")>0)
            foreach(var source in World.Periodic.On(dead))Spread(source,dead);
    }
    private void Spread(AutomaticAbility source,Combatant from)
    {
        var skip=GetTree().GetNodesInGroup(Combatant.GroupName).OfType<Combatant>().Where(c=>World.Periodic.Has(source,c)).Select(c=>c.GetInstanceId()).ToHashSet();
        skip.Add(from.GetInstanceId());
        var next=World.Targeting.ChainTarget(from.AimPosition,0.3f,skip);
        if(next!=null)World.Periodic.Apply(source,next);
    }
    public void Hit(AutomaticAbility ability,Combatant target,IEnumerable<Combatant> primary)
    {
        if(ability.Definition.Id=="storm" && Talent("storm_slow")>0 && target is EnemyAgent enemy)enemy.Slow(0.65f,1.4f);
        if(ability.Definition.Id=="deathfield" && Item("vulnerability")>0 && target.Alive)target.Health.VulnerabilitySeconds=4;
        if(ability.Definition.Id=="lightning" && Talent("fork")>0)
        {
            var excluded=primary.Select(c=>c.GetInstanceId()).ToHashSet();
            var next=World.Targeting.ChainTarget(target.AimPosition,0.3f,excluded);
            if(next!=null)next.Health.ApplyDamage(ability.DamagePerTick*0.45f*DamageMultiplier(ability,next),ability.Definition.DamageType);
        }
        if(ability.Definition.Id is "lightning" or "chain" && Item("conduit")>0 && _conduitLock<=0)
        {
            _conduitLock=1;
            // Every fifth successful lightning hit is deterministic, independently of visual randomness.
            if(++_lightningHits%5==0)
            {
                var other=World.Abilities.Abilities.Where(a=>a!=ability && a.Unlocked && a.CooldownRemaining>0)
                    .OrderByDescending(a=>a.CooldownRemaining).FirstOrDefault();
                other?.ReduceCooldown(Item("conduit"));
            }
        }
    }
    private int _lightningHits;
    public override void _Ready() { World.PlayerActor.Health.ShieldEnded+=ShieldEnded;World.Spawner!.EnemyKilled+=EnemyKilled;World.Periodic.Ticked+=PeriodicTicked; }
    private void ShieldEnded(bool depleted)
    {
        _guardianShield=false;
        if(!depleted || Talent("backlash")==0 || !World.PlayerActor.Alive)return;
        foreach(var enemy in World.Targeting.InArea(World.PlayerActor.GlobalPosition,0.35f,0.35f).ToArray())
            enemy.Health.ApplyDamage(16,DamageType.Energy);
    }
    private void EnemyKilled(EnemyAgent enemy)
    {
        PassOn(enemy);
        if(enemy.RewardTier>0 && Item("guardian")>0 && World.PlayerActor.Health.ApplyShield(Item("guardian"),8))_guardianShield=true;
    }
    public override void _PhysicsProcess(double delta)
    {
        _conduitLock=Math.Max(0,_conduitLock-delta);
        if(_buff>0) { _buff=Math.Max(0,_buff-delta);if(_buff==0)World.Progression?.Recalculate(); }
        if(_haste>0) { _haste=Math.Max(0,_haste-delta);if(_haste==0)World.Progression?.Recalculate(); }
    }
    public void Reset() { _haste=_conduitLock=_buff=0;_lightningHits=0;_guardianShield=false; }
    public override void _ExitTree()
    {
        if(GodotObject.IsInstanceValid(World.PlayerActor))World.PlayerActor.Health.ShieldEnded-=ShieldEnded;
        if(GodotObject.IsInstanceValid(World.Spawner))World.Spawner!.EnemyKilled-=EnemyKilled;
        if(GodotObject.IsInstanceValid(World.Periodic))World.Periodic.Ticked-=PeriodicTicked;
    }
}
