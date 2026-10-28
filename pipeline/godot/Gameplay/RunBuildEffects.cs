using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;

/// <summary>Run-only mechanical hooks. Equipment modifiers are derived from current ownership, never accumulated.</summary>
public partial class RunBuildEffects : Node
{
    public Talents.TalentRuntime Pregame {get;private set;}=null!;
    public int SpreadLimit {get;set;}=4;
    public float SpreadEffectiveness {get;set;}=1;
    private bool _energyShield;
    private double _pyroLock,_reinforced;
    private float _reinforcedReduction;
    public float IncomingMultiplier=>(_reinforced>0?1-_reinforcedReduction:1)*(Pregame?.IncomingMultiplier??1);
    public void Attacked(EnemyAgent enemy,bool shielded)
    {
        if(enemy.GlobalPosition.DistanceSquaredTo(World.PlayerActor.GlobalPosition)>.36f*.36f || !_energyShield || !shielded || _pyroLock>0 || Talent("pyro_shield")==0 || World.Abilities.Find("ionshield")?.Unlocked!=true)return;
        _pyroLock=1;enemy.Health.ApplyDamage(8,DamageType.Internal);
        PowertechStatusVisual.Flash(enemy,"4x4_flame_lick_02_bw",16);
    }
    public GameWorld World {get;set;}=null!;
    private double _haste,_conduitLock,_buff;
    private double _leechLock;
    private float _damageBuff,_speedBuff;
    // Class mechanics: Rage stacks, Target Lock chain, Tracer marks.
    private int _rage,_lockStacks;private double _rageTime;private ulong _lockTarget;
    private readonly Dictionary<ulong,double> _marks=new();
    public int RageStacks=>_rage;
    public bool Marked(Combatant target)=>_marks.GetValueOrDefault(target.GetInstanceId())>0;
    private bool _guardianShield;
    public void EquipmentRemoved(RunArtifactDefinition item)
    {
        if(item.Effect=="guardian" && _guardianShield)World.PlayerActor.Health.ClearShield();
        if(item.Effect=="vulnerability")
            foreach(var actor in GetTree().GetNodesInGroup(Combatant.GroupName).OfType<Combatant>())
                if(World.PlayerActor.IsHostileTo(actor))actor.Health.VulnerabilitySeconds=0;
    }
    public int Talent(string key) => (Pregame?.Mechanic(key)??0)+(World.Progression?.Upgrades.Where(u=>u.Mechanic==key && (!u.Id.StartsWith("powertech_") || ((u.AbilityId=="" || World.Abilities.Find(u.AbilityId)?.Unlocked==true) && (u.RequiresUpgrade=="" || World.Progression.Ranks.GetValueOrDefault(u.RequiresUpgrade)>0)))).Sum(u=>World.Progression.Rank(u))??0);
    public float Item(string key) => key=="health_cost"?World.Artifacts?.Equipped.Sum(a=>a.HealthCost)??0:World.Artifacts?.Equipped.Where(a=>a.Effect==key).Sum(a=>a.Amount)??0;
    public float DamageMultiplier(AutomaticAbility ability,Combatant target)
    {
        float bonus=Item("sacrifice")+(Pregame?.DirectBonus(ability,target)??0);
        var burn=World.Abilities.Find("incendiarymissile");
        if(burn!=null && World.Periodic.Has(burn,target))
        {
            if(ability.Definition.Id=="flamethrower" && Talent("engulfing_flames")>0)bonus+=.35f;
            if(ability.Definition.Id=="rocketpunch" && Talent("prototype_weaponry")>0)bonus+=.4f;
        }
        if(target.Health.Current<target.Health.Maximum*0.35f)bonus+=Item("execution");
        if(World.PlayerActor.Health.Current<World.PlayerActor.Health.Maximum*0.35f)bonus+=0.35f*Talent("bloodlust");
        if(_buff>0)bonus+=_damageBuff;
        if(_rage>0 && Talent("rage")>0)bonus+=0.05f*_rage;
        if(_lockStacks>0 && target.GetInstanceId()==_lockTarget && Talent("target_lock")>0)bonus+=0.1f*_lockStacks;
        if(ability.Definition.Id!="tracer" && Talent("tracer_lock")>0 && Marked(target))bonus+=0.25f;
        return (1+bonus)*(target.Health.VulnerabilitySeconds>0?1.2f:1);
    }
    public float HealingAmount(float amount) => amount*(World.PlayerActor.Health.Current<World.PlayerActor.Health.Maximum*0.3f?1+Item("restoration"):1);
    public void Activated(AutomaticAbility ability)
    {
        Pregame?.Activated(ability);
        if(ability.Definition.Id=="ionshield")_energyShield=true;
        if(ability.Definition.Id=="oilslick"){
            var slick=new PowertechOilSlick {Source=ability};World.AddChild(slick);slick.GlobalPosition=ability.Caster.GlobalPosition;
        }
        if(ability.Definition.Category==AbilityCategory.Utility && Talent("haste")>0)
        { _haste=3;World.Progression?.Recalculate(); }
    }
    public float SpeedMultiplier => (Pregame?.MarauderSpeed??1)* (_haste>0?1.3f:1)*(_buff>0?1+_speedBuff:1)*(1+Item("stride"));
    public bool Buffed => _buff>0;
    public void ApplyBuff(AbilityDefinition d)
    { if(World.RunConfiguration.AdvancedClassId=="assassin" && d.Id is "forcecloak" or "deflection")return;
        _buff=d.BuffSeconds;_damageBuff=d.DamageBuff;_speedBuff=d.SpeedBuff;World.Progression?.Recalculate(); }
    /// <summary>Per-hit class mechanics that need the ability, not just the target.</summary>
    public void AbilityHit(AutomaticAbility ability,Combatant target)
    {
        Pregame?.DirectHit(ability,target);
        if(ability.Presentation is AssassinAbilityPresentation original)original.TargetImpact(target);
        if(ability.Definition.IsBasicAttack)return;
        if(ability.Definition.Id=="flameburst" && Talent("accelerant")>0 && World.Abilities.Find("incendiarymissile") is {} burn)World.Periodic.Extend(burn,target,1,8);
        if(ability.Definition.Id=="carbonize" && target is EnemyAgent frozen && frozen.Stun(3) && Talent("reinforced_shielding")>0){_reinforced=4;_reinforcedReduction=Math.Min(.25f,_reinforcedReduction+.05f);}
        if(Item("leech")>0 && _leechLock<=0) {_leechLock=1;World.PlayerActor.Health.ApplyHealing(Item("leech"));}
        if(Talent("static_blades")>0 && ability.Definition.PeriodicDuration<=0 && World.Periodic.Has(target))World.Periodic.RefreshAll(target);
        if(ability.Definition.Effect!=AbilityEffect.Damage)return;
        if(Talent("rage")>0){_rage=Math.Min(5,_rage+1);_rageTime=8;}
        if(Talent("shockwave")>0 && ability.Definition.Id=="smash" && target is EnemyAgent shocked)shocked.Slow(0.5f,2);
        if(Talent("target_lock")>0)
        { _lockStacks=target.GetInstanceId()==_lockTarget?Math.Min(4,_lockStacks+1):1;_lockTarget=target.GetInstanceId(); }
        if(Talent("followthrough")>0 && ability.Definition.Id=="ambush" && !target.Alive)World.Abilities.Find("snipershot")?.ReduceCooldown(99);
        if(ability.Definition.Id=="tracer" && Talent("tracer_lock")>0 && target.Alive)_marks[target.GetInstanceId()]=6;
    }
    /// <summary>DoT mechanics: Contagion jumps every third tick; Blood Rhythm shortens Rupture's cooldown.</summary>
    private void PeriodicTicked(AutomaticAbility source,Combatant target,int tick)
    {
        Pregame?.PeriodicTick(source);
        if(Talent("contagion")>0 && tick%3==0)Spread(source,target);
        if(Talent("blood_rhythm")>0 && tick%4==0)source.ReduceCooldown(0.5f);
    }
    /// <summary>Last Gasp / Wildfire: an afflicted enemy passes its DoTs on when it dies.</summary>
    public void PassOn(Combatant dead)
    {
        if(Talent("chain_reaction")>0 && Marked(dead))
        {
            _marks.Remove(dead.GetInstanceId());
            foreach(var near in World.Targeting.InArea(dead.GlobalPosition,0.2f,0.2f,true).Where(c=>c!=dead).ToArray())near.Health.ApplyDamage(20,DamageType.Energy);
        }
        if(World.RunConfiguration.AdvancedClassId=="sorcerer" || World.Build.Pregame.Mechanic("assassin_transfer")>0)World.Periodic.TransferOwned(World.PlayerActor,dead);
        if(World.RunConfiguration.AdvancedClassId!="sorcerer" && Talent("last_gasp")>0)
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
        Pregame?.Hit(ability);
        if(ability.Definition.Id=="storm" && Talent("storm_slow")>0 && target is EnemyAgent enemy)enemy.Slow(0.65f,1.4f);
        if(ability.Definition.Id=="deathfield" && Item("vulnerability")>0 && target.Alive)target.Health.VulnerabilitySeconds=4;
        if(ability.Definition.Id is "lightning" or "lightningstrike" && Talent("fork")>0)
        {
            var excluded=primary.Select(c=>c.GetInstanceId()).ToHashSet();
            var next=World.Targeting.ChainTarget(target.AimPosition,0.3f,excluded);
            if(next!=null)
            {
                next.Health.ApplyDamage(ability.DamagePerTick*0.45f*DamageMultiplier(ability,next),ability.Definition.DamageType,abilityId:ability.Definition.Id);
                if(ability.Presentation is SorcererRefinementPresentation original)original.Arc(target.AimPosition,next.AimPosition);
            }
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
    public override void _Ready() { Pregame=new Talents.TalentRuntime(World);World.PlayerActor.Health.Damaged+=Pregame.Damaged;World.PlayerActor.Health.ShieldEnded+=ShieldEnded;World.Spawner!.EnemyKilled+=EnemyKilled;World.Periodic.Ticked+=PeriodicTicked; }
    private void ShieldEnded(bool depleted)
    {
        Pregame?.ShieldEnded(depleted);
        _energyShield=false;_guardianShield=false;
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
        Pregame?.Advance(delta);
        _pyroLock=Math.Max(0,_pyroLock-delta);_reinforced=Math.Max(0,_reinforced-delta);if(_reinforced==0)_reinforcedReduction=0;
        _leechLock=Math.Max(0,_leechLock-delta);
        _conduitLock=Math.Max(0,_conduitLock-delta);
        if(_rageTime>0){_rageTime-=delta;if(_rageTime<=0)_rage=0;}
        foreach(var key in _marks.Keys.ToArray()){_marks[key]-=delta;if(_marks[key]<=0)_marks.Remove(key);}
        if(_buff>0) { _buff=Math.Max(0,_buff-delta);if(_buff==0)World.Progression?.Recalculate(); }
        if(_haste>0) { _haste=Math.Max(0,_haste-delta);if(_haste==0)World.Progression?.Recalculate(); }
    }
    public void Reset() { foreach(var oil in World.GetChildren().OfType<PowertechOilSlick>())oil.QueueFree(); _energyShield=false; _pyroLock=_reinforced=0;_reinforcedReduction=0; _haste=_conduitLock=_buff=_leechLock=0;_rage=_lockStacks=0;_rageTime=0;_lockTarget=0;_marks.Clear();_lightningHits=0;_guardianShield=false; }
    public override void _ExitTree()
    {
        if(GodotObject.IsInstanceValid(World.PlayerActor))World.PlayerActor.Health.Damaged-=Pregame.Damaged;
        if(GodotObject.IsInstanceValid(World.PlayerActor))World.PlayerActor.Health.ShieldEnded-=ShieldEnded;
        if(GodotObject.IsInstanceValid(World.Spawner))World.Spawner!.EnemyKilled-=EnemyKilled;
        if(GodotObject.IsInstanceValid(World.Periodic))World.Periodic.Ticked-=PeriodicTicked;
    }
}

