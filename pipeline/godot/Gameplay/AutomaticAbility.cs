using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;
// Retained B2 enum for inspection fixtures; all abilities share this lifecycle.
public enum LightningState { Idle, Starting, Channeling, Ending, Cooldown, Disabled }
public partial class AutomaticAbility : Node
{
    public AbilityDefinition Definition { get; set; } = new();
    public Combatant Caster { get; set; } = null!;
    public TargetingService Targeting { get; set; } = null!;
    public IAbilityPresentation Presentation { get; set; } = null!;
    public float DamagePerTick { get; set; } = 8;
    public float ChannelSeconds { get; set; } = 3;
    public float TickSeconds { get; set; } = 0.5f;
    public float CooldownSeconds { get; set; } = 1;
    public float Range { get; set; } = 0.85f;
    public float Radius { get; set; } = 0.3f;
    public float Healing { get; set; }
    public float ShieldCapacity { get; set; }
    public float ShieldDuration { get; set; }
    public float HealthThreshold { get; set; }
    public int ExtraBounces { get; set; }
    public RunBuildEffects? Build { get; set; }
    public PeriodicEffects? Periodic {get;set;}
    public int LastDistinctHits => _damaged.Count;
    public float LastHealing { get; private set; }
    private readonly HashSet<ulong> _damaged = new();
    public bool CombatEnabled { get; set; } = true;
    public bool Unlocked { get; private set; } = true;
    public LightningState State { get; private set; }
    public bool Casting => State is LightningState.Starting or LightningState.Channeling or LightningState.Ending;
    public Combatant? CastTarget { get; private set; }
    public Vector3 GroundCenter { get; private set; }
    public double CooldownRemaining { get; private set; }
    public double CooldownDuration { get; private set; }
    public int Casts { get; private set; }
    public int DamageEvents { get; private set; }
    public string LastEvent { get; private set; } = "Waiting for a hostile target";
    public event Action<Combatant, float>? DamageDealt;
    public void ReduceCooldown(float seconds) { CooldownRemaining=Math.Max(0,CooldownRemaining-Math.Max(0,seconds)); }
    private readonly ChannelClock _clock = new();
    private double _phaseRemaining;
    private bool _presented;
    private double _gestureRemaining;
    private void Present(Action action)
    {
        try { action(); }
        catch(Exception ex) { GD.PushWarning(Definition.Title+" presentation: "+ex.Message); }
    }
    public void Configure(AbilityDefinition definition)
    {
        Definition=(AbilityDefinition)definition.Duplicate();
        DamagePerTick=definition.Damage;ChannelSeconds=definition.ChannelSeconds;TickSeconds=definition.TickSeconds;
        CooldownSeconds=definition.CooldownSeconds;Range=definition.Range;Radius=definition.Radius;
        Healing=definition.Healing;ShieldCapacity=definition.ShieldCapacity;ShieldDuration=definition.ShieldDuration;HealthThreshold=definition.HealthThreshold;
        SetUnlocked(definition.StartsUnlocked);
    }
    public void Advance(double delta)
    {
        if (!Unlocked || !CombatEnabled || !Caster.Alive) { if(State!=LightningState.Disabled) Abort();return; }
        if(State==LightningState.Disabled) State=LightningState.Idle;
        CooldownRemaining=Math.Max(0,CooldownRemaining-delta);
        if(State==LightningState.Cooldown && CooldownRemaining<=0) State=LightningState.Idle;
        if(!Casting) return;
        _gestureRemaining-=delta;
        if(Definition.MaintainChannelGesture && State is LightningState.Starting or LightningState.Channeling)
        {
            _gestureRemaining=Math.Max(0.2,_gestureRemaining);
            Present(()=> {
                if(Presentation.Actor.GestureOwner!=this)
                    Presentation.Actor.TryBeginGesture(this,State==LightningState.Starting?Definition.StartClip:Definition.LoopClip,Definition.GesturePriority,Definition.FullBodyGesture);
            });
        }
        if(_gestureRemaining<=0) Present(()=>Presentation.Actor.ReleaseGesture(this));
        if(State!=LightningState.Ending)
        {
            bool valid=Definition.Area==AbilityArea.CasterRadial || Definition.Effect!=AbilityEffect.Damage || (Definition.Area==AbilityArea.GroundSphere
                ? Definition.PersistentArea && State==LightningState.Channeling || Caster.GlobalPosition.DistanceSquaredTo(GroundCenter)<=Range*Range
                : Targeting.IsValidTarget(CastTarget,Range));
            if(!valid) { Finish(true,"Target lost");return; }
            if(Presentation.Actor.GestureOwner==this && Definition.Area!=AbilityArea.CasterRadial)
                Presentation.Actor.FacingTarget=Definition.Area==AbilityArea.GroundSphere?GroundCenter:CastTarget!.GlobalPosition;
        }
        if(State==LightningState.Starting)
        {
            _phaseRemaining-=delta;if(_phaseRemaining>0) return;
            delta=-_phaseRemaining;State=LightningState.Channeling;
            Present(()=>Presentation.Actor.SetGestureClip(this,Definition.LoopClip));
            if(_presented) Present(Presentation.Channel);
            if(ChannelSeconds<=0) { Tick();if(State==LightningState.Channeling) Finish(false,"Burst complete");return; }
        }
        if(State==LightningState.Channeling)
        {
            _clock.Advance(delta,Tick);
            if(State==LightningState.Channeling && _clock.Complete) Finish(false,"Channel complete");
            return;
        }
        if(State==LightningState.Ending)
        {
            _phaseRemaining-=delta;if(_phaseRemaining>0) return;
            if(_presented) Present(Presentation.Complete);
            Present(()=>Presentation.Actor.ReleaseGesture(this));
            _presented=false;CastTarget=null;State=CooldownRemaining>0?LightningState.Cooldown:LightningState.Idle;
        }
    }
    public bool TryBegin()
    {
        if(!Unlocked || !CombatEnabled || !Caster.Alive || State!=LightningState.Idle || CooldownRemaining>0) return false;
        if(Definition.Effect==AbilityEffect.Heal && (Caster.Health.Current>=Caster.Health.Maximum || Caster.Health.Current>=Caster.Health.Maximum*HealthThreshold)) return false;
        if(Definition.Effect==AbilityEffect.Shield && (Caster.Health.Shielded || Targeting.Acquire(Range,AbilityTargetRule.NearestHostile,0)==null)) return false;
        if(Definition.Effect==AbilityEffect.Buff && (Build?.Buffed==true || Targeting.Acquire(Range,AbilityTargetRule.NearestHostile,0)==null)) return false;
        if(Definition.Area==AbilityArea.CasterRadial && Targeting.InArea(Caster.GlobalPosition,Radius,Radius).Count()<Math.Max(1,Definition.NearbyTriggerCount)) return false;
        var target=Definition.Area==AbilityArea.CasterRadial || Definition.Effect!=AbilityEffect.Damage?Caster:Targeting.Acquire(Range,Definition.TargetRule,Radius);
        if(target==null) return false;
        if(!float.IsFinite(DamagePerTick) || (Definition.Effect==AbilityEffect.Damage && DamagePerTick<=0) || !float.IsFinite(TickSeconds) || TickSeconds<=0 ||
            !float.IsFinite(ChannelSeconds) || ChannelSeconds<0 || !float.IsFinite(Range) || Range<=0 ||
            !float.IsFinite(Radius) || Radius<0 || !float.IsFinite(Definition.WindupSeconds) || Definition.WindupSeconds<0 ||
            !float.IsFinite(CooldownSeconds) || CooldownSeconds<0 || !float.IsFinite(Definition.RecoverySeconds) || Definition.RecoverySeconds<0 ||
            !float.IsFinite(Definition.GestureSeconds) || Definition.GestureSeconds<0 ||
            !float.IsFinite(Healing) || Healing<0 || !float.IsFinite(Definition.HealingCap) || Definition.HealingCap<0 ||
            !float.IsFinite(HealthThreshold) || HealthThreshold<=0 || HealthThreshold>1 ||
            !float.IsFinite(ShieldCapacity) || ShieldCapacity<=0 || !float.IsFinite(ShieldDuration) || ShieldDuration<=0)
        { CombatEnabled=false;LastEvent="Invalid ability tuning";Abort();return false; }
        CastTarget=target;GroundCenter=target.GlobalPosition;
        _damaged.Clear();LastHealing=0;
        if(ChannelSeconds>0) _clock.Reset(ChannelSeconds,TickSeconds);
        State=LightningState.Starting;_phaseRemaining=Definition.WindupSeconds;
        CooldownRemaining=CooldownDuration=CooldownSeconds;
        _gestureRemaining=Definition.GestureSeconds;
        if(_gestureRemaining>0) Present(()=>Presentation.Actor.TryBeginGesture(this,Definition.StartClip,Definition.GesturePriority,Definition.FullBodyGesture));
        // Presentation failures are reported and cannot suppress damage.
        try { _presented=Presentation.Begin(target,GroundCenter,Radius); }
        catch(Exception ex) { _presented=false;GD.PushWarning(Definition.Title+": "+ex.Message); }
        if(!_presented) GD.PushWarning(Definition.Title+": original presentation unavailable; see B5 provenance.");
        Casts++;LastEvent="Cast started: "+target.SelectionKey;
        Build?.Activated(this);
        if(Definition.Effect!=AbilityEffect.Damage)
        { State=LightningState.Channeling;Tick();Finish(false,"Self effect applied"); }
        return true;
    }
    private bool Tick()
    {
        if(State!=LightningState.Channeling || !Caster.Alive || !CombatEnabled) return false;
        if(Definition.Effect!=AbilityEffect.Damage)
        {
            if(Definition.Effect==AbilityEffect.Shield) Caster.Health.ApplyShield(ShieldCapacity,ShieldDuration);
            else if(Definition.Effect==AbilityEffect.Buff) Build?.ApplyBuff(Definition);
            else LastHealing=Caster.Health.ApplyHealing(Build?.HealingAmount(Healing)??Healing);
            if(_presented) Present(()=>Presentation.Impact(Caster.AimPosition));
            return true;
        }
        if(Definition.Area==AbilityArea.Chain) return ChainTick();
        var targets=Definition.Area==AbilityArea.TargetSplash && Targeting.IsValidTarget(CastTarget,Range)
            ? Targeting.InArea(CastTarget!.GlobalPosition,Radius,Range,true).Append(CastTarget!).Distinct().ToArray()
            : Definition.Area==AbilityArea.CasterRadial
            ? Targeting.InArea(Caster.GlobalPosition,Radius,Radius).ToArray()
            : Definition.Area==AbilityArea.GroundSphere
            ? Targeting.InArea(GroundCenter,Radius,Range,Definition.PersistentArea).ToArray()
            : Targeting.IsValidTarget(CastTarget,Range)?new[]{CastTarget!}:Array.Empty<Combatant>();
        if(_presented) Present(()=>Presentation.Impact(Definition.Area==AbilityArea.GroundSphere?GroundCenter:CastTarget?.AimPosition??GroundCenter));
        foreach(var target in targets)
        {
            if(State!=LightningState.Channeling || !Caster.Alive || !CombatEnabled) return false;
            if(!GodotObject.IsInstanceValid(target) || !target.Alive) continue;
            bool secondary=Definition.Area==AbilityArea.TargetSplash && target!=CastTarget;
            float applied=DealHit(target,DamagePerTick*(secondary?Definition.SplashMultiplier:1));
            if(target.Alive && Definition.PeriodicDuration>0)Periodic?.Apply(this,target,secondary?Definition.SecondaryPeriodicScale:1);
            if(target.Alive && Definition.VulnerabilitySeconds>0 && !secondary)target.Health.VulnerabilitySeconds=Math.Max(target.Health.VulnerabilitySeconds,Definition.VulnerabilitySeconds);
            if(target.Alive && Definition.SlowSeconds>0 && target is EnemyAgent slowed)slowed.Slow(Definition.SlowFactor,Definition.SlowSeconds);
            Build?.AbilityHit(this,target);
            if(Definition.Area==AbilityArea.CasterRadial && target is EnemyAgent enemy && enemy.Alive) enemy.DisplaceFrom(Caster.GlobalPosition,Definition.KnockbackDistance,Definition.StaggerSeconds);
            if(applied<=0) continue;
            _damaged.Add(target.GetInstanceId());
            if(_presented && Presentation is SurvivalAbilityPresentation original) Present(()=>original.TargetImpact(target));
            DamageEvents++;LastEvent=$"{Definition.Title}: {target.SelectionKey} −{applied:0.#}";
            DamageDealt?.Invoke(target,applied);
            Build?.Hit(this,target,targets);
        }
        if(Healing>0 && ChannelSeconds<=0)
        {
            LastHealing=Caster.Health.ApplyHealing(Math.Min(Definition.HealingCap,_damaged.Count*Healing));
            if(_presented && Presentation is SurvivalAbilityPresentation original) Present(()=>original.SustainHealing(LastHealing));
        }
        if(State!=LightningState.Channeling) return false;
        if(Definition.Area==AbilityArea.SingleTarget && !Targeting.IsValidTarget(CastTarget,Range))
        { Finish(!(Definition.ImpactTailsOnTargetDeath && ChannelSeconds<=0),"Target died / invalidated");return false; }
        return true;
    }
    private float DealHit(Combatant target,float amount) => target.Health.ApplyDamage(amount*(Build?.DamageMultiplier(this,target)??1)*(Definition.AfflictedBonus>0 && Periodic?.Has(target)==true?1+Definition.AfflictedBonus:1),Definition.DamageType);
    public void ApplyPeriodicHit(Combatant target,float amount)
    {
        if(!Caster.Alive || !target.Alive || !Caster.IsHostileTo(target))return;
        float applied=DealHit(target,amount);
        if(applied<=0)return;
        DamageEvents++;DamageDealt?.Invoke(target,applied);Build?.Hit(this,target,new[]{target});
    }
    private bool ChainTick()
    {
        if(!Targeting.IsValidTarget(CastTarget,Range))return false;
        if(_presented)Present(()=>Presentation.Impact(CastTarget!.AimPosition));
        var current=CastTarget!;var from=Presentation is D2AbilityPresentation source?source.Hand:Caster.AimPosition;float damage=DamagePerTick;
        var visited=new HashSet<ulong>();
        for(int i=0;i<=Math.Clamp(Definition.BounceCount+ExtraBounces,0,24);i++)
        {
            if(!GodotObject.IsInstanceValid(current) || !current.Alive || !visited.Add(current.GetInstanceId()))break;
            var point=current.AimPosition;
            // One gameplay hit per link. Rendering an arc has no combat authority.
            float applied=DealHit(current,damage);
            if(_presented && Presentation is D2AbilityPresentation visual)Present(()=>visual.Arc(from,point));
            if(applied>0) { _damaged.Add(current.GetInstanceId());DamageEvents++;DamageDealt?.Invoke(current,applied);Build?.Hit(this,current,new[]{current}); }
            from=point;
            var next=Targeting.ChainTarget(point,Definition.BounceRange,visited);
            if(next==null)break;
            current=next;damage*=Math.Clamp(Definition.BounceFalloff,0,1);
        }
        LastEvent=$"Chain Lightning: {_damaged.Count} distinct hits";
        return true;
    }
    private void Finish(bool interrupted,string reason)
    {
        if(_presented) Present(()=>Presentation.End(interrupted));
        Present(()=>Presentation.Actor.SetGestureClip(this,Definition.EndClip));
        State=LightningState.Ending;_phaseRemaining=Definition.RecoverySeconds;LastEvent=reason;
    }
    public void Abort()
    {
        if(Definition.Effect==AbilityEffect.Shield && Caster!=null && GodotObject.IsInstanceValid(Caster.Health)) Caster.Health.ClearShield();
        // Completed gestures can leave finite tails or a persistent shield presentation.
        if(Presentation!=null) Present(Presentation.Release);
        if(Presentation!=null && GodotObject.IsInstanceValid(Presentation.Actor)) Presentation.Actor.ReleaseGesture(this);
        _gestureRemaining=0;_presented=false;CastTarget=null;CooldownRemaining=CooldownDuration=0;State=LightningState.Disabled;
    }
    public void SetUnlocked(bool unlocked)
    {
        if(Unlocked==unlocked) return;
        Abort();Unlocked=unlocked;State=unlocked?LightningState.Idle:LightningState.Disabled;
        LastEvent=unlocked?"Ability unlocked":"Unlock through a level-up choice";
    }
    public void ResetCombat() { Abort();DamageEvents=0;Casts=0;_damaged.Clear();LastHealing=0;State=Unlocked?LightningState.Idle:LightningState.Disabled;LastEvent="Combat reset"; }
    public override void _ExitTree() { if(Presentation!=null && GodotObject.IsInstanceValid(Presentation.Actor)) Abort(); }
}
