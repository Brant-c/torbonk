using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;

/// <summary>Run-only mechanical hooks. Equipment modifiers are derived from current ownership, never accumulated.</summary>
public partial class RunBuildEffects : Node
{
    public GameWorld World {get;set;}=null!;
    private double _haste,_conduitLock;
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
        return (1+bonus)*(target.Health.VulnerabilitySeconds>0?1.2f:1);
    }
    public float HealingAmount(float amount) => amount*(World.PlayerActor.Health.Current<World.PlayerActor.Health.Maximum*0.3f?1+Item("restoration"):1);
    public void Activated(AutomaticAbility ability)
    {
        if(ability.Definition.Category==AbilityCategory.Utility && Talent("haste")>0)
        { _haste=3;World.Progression?.Recalculate(); }
    }
    public float SpeedMultiplier => _haste>0?1.3f:1;
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
    public override void _Ready() { World.PlayerActor.Health.ShieldEnded+=ShieldEnded;World.Spawner!.EnemyKilled+=EnemyKilled; }
    private void ShieldEnded(bool depleted)
    {
        _guardianShield=false;
        if(!depleted || Talent("backlash")==0 || !World.PlayerActor.Alive)return;
        foreach(var enemy in World.Targeting.InArea(World.PlayerActor.GlobalPosition,0.35f,0.35f).ToArray())
            enemy.Health.ApplyDamage(16,DamageType.Energy);
    }
    private void EnemyKilled(EnemyAgent enemy)
    {
        if(enemy.RewardTier>0 && Item("guardian")>0 && World.PlayerActor.Health.ApplyShield(Item("guardian"),8))_guardianShield=true;
    }
    public override void _PhysicsProcess(double delta)
    {
        _conduitLock=Math.Max(0,_conduitLock-delta);
        if(_haste>0) { _haste=Math.Max(0,_haste-delta);if(_haste==0)World.Progression?.Recalculate(); }
    }
    public void Reset() { _haste=_conduitLock=0;_lightningHits=0;_guardianShield=false; }
    public override void _ExitTree()
    {
        if(GodotObject.IsInstanceValid(World.PlayerActor))World.PlayerActor.Health.ShieldEnded-=ShieldEnded;
        if(GodotObject.IsInstanceValid(World.Spawner))World.Spawner!.EnemyKilled-=EnemyKilled;
    }
}
