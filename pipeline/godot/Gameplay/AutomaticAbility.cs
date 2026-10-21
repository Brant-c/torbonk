using Godot;
using System;
using System.Linq;
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
    private readonly ChannelClock _clock = new();
    private double _phaseRemaining;
    private bool _presented;
    private double _gestureRemaining;
    public void Configure(AbilityDefinition definition)
    {
        Definition=(AbilityDefinition)definition.Duplicate();
        DamagePerTick=definition.Damage;ChannelSeconds=definition.ChannelSeconds;TickSeconds=definition.TickSeconds;
        CooldownSeconds=definition.CooldownSeconds;Range=definition.Range;Radius=definition.Radius;
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
        if(_gestureRemaining<=0) Presentation.Actor.ReleaseGesture(this);
        if(State!=LightningState.Ending)
        {
            bool valid=Definition.Area==AbilityArea.GroundSphere
                ? Definition.PersistentArea && State==LightningState.Channeling || Caster.GlobalPosition.DistanceSquaredTo(GroundCenter)<=Range*Range
                : Targeting.IsValidTarget(CastTarget,Range);
            if(!valid) { Finish(true,"Target lost");return; }
            if(Presentation.Actor.GestureOwner==this)
                Presentation.Actor.FacingTarget=Definition.Area==AbilityArea.GroundSphere?GroundCenter:CastTarget!.GlobalPosition;
        }
        if(State==LightningState.Starting)
        {
            _phaseRemaining-=delta;if(_phaseRemaining>0) return;
            delta=-_phaseRemaining;State=LightningState.Channeling;
            Presentation.Actor.SetGestureClip(this,Definition.LoopClip);
            if(_presented) Presentation.Channel();
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
            if(_presented) Presentation.Release();
            Presentation.Actor.ReleaseGesture(this);
            _presented=false;CastTarget=null;State=CooldownRemaining>0?LightningState.Cooldown:LightningState.Idle;
        }
    }
    public bool TryBegin()
    {
        if(!Unlocked || !CombatEnabled || !Caster.Alive || State!=LightningState.Idle || CooldownRemaining>0) return false;
        var target=Targeting.Acquire(Range,Definition.TargetRule,Radius);if(target==null) return false;
        if(!float.IsFinite(DamagePerTick) || DamagePerTick<=0 || !float.IsFinite(TickSeconds) || TickSeconds<=0 ||
            !float.IsFinite(ChannelSeconds) || ChannelSeconds<0 || !float.IsFinite(Range) || Range<=0 ||
            !float.IsFinite(Radius) || Radius<0 || !float.IsFinite(Definition.WindupSeconds) || Definition.WindupSeconds<0 ||
            !float.IsFinite(CooldownSeconds) || CooldownSeconds<0 || !float.IsFinite(Definition.RecoverySeconds) || Definition.RecoverySeconds<0 ||
            !float.IsFinite(Definition.GestureSeconds) || Definition.GestureSeconds<0)
        { CombatEnabled=false;LastEvent="Invalid ability tuning";Abort();return false; }
        CastTarget=target;GroundCenter=target.GlobalPosition;
        if(ChannelSeconds>0) _clock.Reset(ChannelSeconds,TickSeconds);
        State=LightningState.Starting;_phaseRemaining=Definition.WindupSeconds;
        CooldownRemaining=CooldownDuration=CooldownSeconds;
        _gestureRemaining=Definition.GestureSeconds;
        if(_gestureRemaining>0) Presentation.Actor.TryBeginGesture(this,Definition.StartClip);
        // Presentation failures are reported and cannot suppress damage.
        _presented=Presentation.Begin(target,GroundCenter,Radius);
        if(!_presented) GD.PushWarning(Definition.Title+": original presentation unavailable; see B5 provenance.");
        Casts++;LastEvent="Cast started: "+target.SelectionKey;return true;
    }
    private bool Tick()
    {
        if(State!=LightningState.Channeling || !Caster.Alive || !CombatEnabled) return false;
        var targets=Definition.Area==AbilityArea.GroundSphere
            ? Targeting.InArea(GroundCenter,Radius,Range,Definition.PersistentArea).ToArray()
            : Targeting.IsValidTarget(CastTarget,Range)?new[]{CastTarget!}:Array.Empty<Combatant>();
        if(_presented) Presentation.Impact(Definition.Area==AbilityArea.GroundSphere?GroundCenter:CastTarget?.AimPosition??GroundCenter);
        foreach(var target in targets)
        {
            if(State!=LightningState.Channeling || !Caster.Alive || !CombatEnabled) return false;
            if(!GodotObject.IsInstanceValid(target) || !target.Alive) continue;
            float applied=target.Health.ApplyDamage(DamagePerTick,Definition.DamageType);
            if(applied<=0) continue;
            DamageEvents++;LastEvent=$"{Definition.Title}: {target.SelectionKey} −{applied:0.#}";
            DamageDealt?.Invoke(target,applied);
        }
        if(State!=LightningState.Channeling) return false;
        if(Definition.Area==AbilityArea.SingleTarget && !Targeting.IsValidTarget(CastTarget,Range))
        { Finish(true,"Target died / invalidated");return false; }
        return true;
    }
    private void Finish(bool interrupted,string reason)
    {
        if(_presented) Presentation.End(interrupted);
        Presentation.Actor.SetGestureClip(this,Definition.EndClip);
        State=LightningState.Ending;_phaseRemaining=Definition.RecoverySeconds;LastEvent=reason;
    }
    public void Abort()
    {
        if(_presented) Presentation.Release();
        if(Presentation!=null && GodotObject.IsInstanceValid(Presentation.Actor)) Presentation.Actor.ReleaseGesture(this);
        _gestureRemaining=0;_presented=false;CastTarget=null;CooldownRemaining=CooldownDuration=0;State=LightningState.Disabled;
    }
    public void SetUnlocked(bool unlocked)
    {
        if(Unlocked==unlocked) return;
        Abort();Unlocked=unlocked;State=unlocked?LightningState.Idle:LightningState.Disabled;
        LastEvent=unlocked?"Ability unlocked":"Unlock through a level-up choice";
    }
    public void ResetCombat() { Abort();DamageEvents=0;Casts=0;State=Unlocked?LightningState.Idle:LightningState.Disabled;LastEvent="Combat reset"; }
    public override void _ExitTree() { if(_presented && GodotObject.IsInstanceValid(Presentation.Actor)) Abort(); }
}
