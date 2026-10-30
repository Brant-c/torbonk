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
    private readonly HashSet<Combatant> _sweepHits=new();
    private bool _spreadSeed;
    private readonly HashSet<string> _sweepSeeds=new();
    private Vector3 _coneForward;
    private readonly HashSet<ulong> _damaged = new();
    public bool CombatEnabled { get; set; } = true;
    public bool Unlocked { get; private set; } = true;
    // Trading Affliction carries its free position to the replacement for this run.
    public bool InheritsFreeSlot { get; set; }
    public LightningState State { get; private set; }
    public bool BlocksCasting => Casting && !(Definition.DetachedArea && State==LightningState.Channeling && _gestureRemaining<=0);
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
    private ChannelClock _clock = new();
    private readonly List<AutomaticAbility> _storms=new();
    private bool _stormInstance;
    private int _stormTicks;
    private int _chainRepeats;
    private int _lacerationStrikes;
    public IEnumerable<AutomaticAbility> ActiveStorms => _storms.Where(a=>a.State==LightningState.Channeling).Concat(Definition.Id=="storm" && State==LightningState.Channeling?new[]{this}:Array.Empty<AutomaticAbility>());
    private double _phaseRemaining;
    private bool _presented;
    private bool _projectileReleased;
    private double _gestureRemaining;
    /// <summary>Playtest tuning: long casts no longer hold the rotation. Storm and Death Field keep their authored timing.</summary>
    public const double MaxGestureSeconds=1.3, MaxRecoverySeconds=.6;
    private bool KeepsAuthoredTiming => Definition.Id is "storm" or "deathfield" or "deathfromabove";
    public double RecoveryDuration => KeepsAuthoredTiming?Definition.RecoverySeconds:Math.Min(Definition.RecoverySeconds,MaxRecoverySeconds);
    public double GestureDuration => (KeepsAuthoredTiming?Definition.GestureSeconds:Math.Min(Definition.GestureSeconds,MaxGestureSeconds)) / (Definition.Effect==AbilityEffect.Damage && Definition.Id!="smash" ? Build?.Pregame.MarauderAnimationHaste??1 : 1);
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
        foreach(var storm in _storms.ToArray()) {
            if(SorcererRelics.Rank(this,"eye_of_the_tempest")<0 || !Unlocked || !CombatEnabled || !Caster.Alive)storm.Abort();
            else storm.Advance(delta);
            if(!storm.Casting){storm.Abort();((Node)storm.Presentation).QueueFree();_storms.Remove(storm);storm.Free();}
        }
        if (!_stormInstance && Definition.Id=="storm" && State==LightningState.Channeling && _gestureRemaining<=0 && SorcererRelics.Rank(this,"eye_of_the_tempest")>=0) {
            var detached=new AutomaticAbility {Definition=Definition,Caster=Caster,Targeting=Targeting,Presentation=Presentation,Build=Build,Periodic=Periodic,
                DamagePerTick=DamagePerTick,Radius=Radius,Range=Range,TickSeconds=TickSeconds,ChannelSeconds=ChannelSeconds,
                State=State,GroundCenter=GroundCenter,CastTarget=CastTarget,_clock=_clock,_presented=_presented,_stormInstance=true,_stormTicks=_stormTicks};
            _storms.Add(detached);_clock=new();_presented=false;
            Present(()=>Presentation.Actor.ReleaseGesture(this));
            var visual=new OriginalAbilityPresentation {Actor=Presentation.Actor,Definition=Definition};GetParent().AddChild(visual);Presentation=visual;
            State=CooldownRemaining>0?LightningState.Cooldown:LightningState.Idle;CastTarget=null;
        }
        if (!Unlocked || !CombatEnabled || !Caster.Alive) { if(State!=LightningState.Disabled) Abort();return; }
        if(State==LightningState.Disabled) State=LightningState.Idle;
        CooldownRemaining=Math.Max(0,CooldownRemaining-delta*(Build?.AssassinRelics?.Haste??1)*(Build?.MarauderRelics?.Acceleration??1));
        if(State==LightningState.Cooldown && CooldownRemaining<=0) State=LightningState.Idle;
        if(!Casting) return;
        _gestureRemaining-=delta;
        if(Definition.MaintainChannelGesture && State is LightningState.Starting or LightningState.Channeling)
        {
            _gestureRemaining=Math.Max(0.2,_gestureRemaining);
            Present(()=> {
                if(Presentation.Actor.GestureOwner!=this)
                    Presentation.Actor.TryBeginGesture(this,State==LightningState.Starting?Definition.StartClip:Definition.LoopClip,Definition.GesturePriority,Definition.FullBodyGesture,Definition.KeepFullBodyWhileMoving);
            });
        }
        if(_gestureRemaining<=0) Present(()=>Presentation.Actor.ReleaseGesture(this));
        if(State!=LightningState.Ending)
        {
            if(Definition.Area==AbilityArea.FrontalCone)Presentation.Actor.FacingTarget=Caster.GlobalPosition+_coneForward;
            bool valid=Definition.Area is AbilityArea.CasterRadial or AbilityArea.FrontalCone || Definition.Effect!=AbilityEffect.Damage || (Definition.Area==AbilityArea.GroundSphere
                ? Definition.PersistentArea && State==LightningState.Channeling || Caster.GlobalPosition.DistanceSquaredTo(GroundCenter)<=Range*Range
                : Targeting.IsValidTarget(CastTarget,Range));
            if(!valid) { Finish(true,"Target lost");return; }
            if(Presentation.Actor.GestureOwner==this && Definition.Area is not (AbilityArea.CasterRadial or AbilityArea.FrontalCone))
                Presentation.Actor.FacingTarget=Definition.Area==AbilityArea.GroundSphere?GroundCenter:CastTarget!.GlobalPosition;
        }
        if(State==LightningState.Starting)
        {
            _phaseRemaining-=delta;if(_phaseRemaining>0) return;
            if(Definition.ProjectileSeconds>0 && !_projectileReleased){
                _projectileReleased=true;
                if(_presented && Presentation is SorcererAbilityPresentation blast)Present(()=>blast.Launch(CastTarget!));
                Present(()=>Presentation.Actor.SetGestureClip(this,Definition.EndClip));
                _phaseRemaining+=Definition.ProjectileSeconds;
                if(_phaseRemaining>0)return;
            }
            delta=-_phaseRemaining;State=LightningState.Channeling;
            if(Definition.Id=="spike" && Build?.World.RunConfiguration.AdvancedClassId=="assassin") {Build.Pregame.RevealSpike();_gestureRemaining=Definition.GestureSeconds;Present(()=>Presentation.Actor.TryBeginGesture(this,Definition.LoopClip,Definition.GesturePriority,true));}
            if(!Definition.DetachedArea)Present(()=>Presentation.Actor.SetGestureClip(this,Definition.LoopClip));
            if(Definition.Id=="smash")Present(()=>Presentation.Actor.ReleaseMovingBody(this));
            if(_presented) Present(Presentation.Channel);
            // Two knife contacts: one on entering the sequence, one at its final tick.
            if(Definition.Id=="laceration" && ChannelSeconds>0)Tick();
            if(ChannelSeconds<=0) { Tick();
                int fork=Definition.Id=="chain"?SorcererRelics.Rank(this,"forked_lightning"):-1;
                if(fork>=0 && !Targeting.IsValidTarget(CastTarget,Range))CastTarget=Targeting.Acquire(Range,Definition.TargetRule,Radius);
                if(fork>=0 && _chainRepeats<(fork==3?2:1) && Targeting.IsValidTarget(CastTarget,Range)) {
                    _chainRepeats++;Present(()=>Presentation.End(false));
                    _gestureRemaining=GestureDuration;
                    Present(()=>Presentation.Actor.ReleaseGesture(this));
                    Present(()=>Presentation.Actor.TryBeginGesture(this,Definition.StartClip,Definition.GesturePriority,Definition.FullBodyGesture,Definition.KeepFullBodyWhileMoving));
                    _presented=Presentation.Begin(CastTarget!,GroundCenter,Radius);State=LightningState.Starting;_phaseRemaining=Math.Max(Definition.WindupSeconds,GestureDuration);return;
                }
                if(State==LightningState.Channeling) Finish(false,"Burst complete");return; }
        }
        if(State==LightningState.Channeling)
        {
            _clock.SetInterval(Definition.Id=="laceration"?Definition.ChannelSeconds:TickSeconds);
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
    public BallisticShieldField? BallisticField {get;private set;}
    public bool TryBegin()
    {
        float relicRange=Build?.JuggernautRelics?.Range(this)??Range;
        if(Definition.Id=="storm" && SorcererRelics.Rank(this,"eye_of_the_tempest") is int stormRank && stormRank>=0 && _storms.Count>=stormRank+2)return false;
        if(!Unlocked || !CombatEnabled || !Caster.Alive || State!=LightningState.Idle || CooldownRemaining>0) return false;
        if(Build?.World.RunConfiguration.AdvancedClassId=="sorcerer" && Definition.Id is "deathfield" or "storm" && !Build.World.Player.StationaryForAbility)return false;
        if(Definition.Effect==AbilityEffect.BallisticField && (!Presentation.Actor.SniperCoverActive || GodotObject.IsInstanceValid(BallisticField) && BallisticField!.Active))return false;
        if(Build?.Pregame.CanBeginAgent(this)==false)return false;
        if(Build?.Pregame.CanBeginAssassin(this)==false)return false;
        if(Build?.Pregame.CanBeginJuggernaut(this)==false || Build?.Pregame.CanBeginMarauder(this)==false)return false;
        if(Build?.Pregame.CanBeginBountyHunter(this)==false)return false;
        bool spikeOpening=Definition.Id=="spike" && Build?.World.RunConfiguration.AdvancedClassId=="assassin";
        if(Definition.Effect==AbilityEffect.Heal && (Caster.Health.Current>=Caster.Health.Maximum || Caster.Health.Current>=Caster.Health.Maximum*HealthThreshold)) return false;
        if(Definition.Effect==AbilityEffect.Shield && (Caster.Health.Shielded || Targeting.Acquire(relicRange,AbilityTargetRule.NearestHostile,0)==null)) return false;
        if(Definition.Effect==AbilityEffect.Buff && ((Definition.Id=="polarityshift"?Build?.Pregame.PolarityActive==true:Build?.World.RunConfiguration.AdvancedClassId!="marauder" && Build?.Buffed==true) || Targeting.Acquire(relicRange,AbilityTargetRule.NearestHostile,0)==null)) return false;
        if(Definition.Area==AbilityArea.CasterRadial && Targeting.InArea(Caster.GlobalPosition,AssassinRelicRadius,AssassinRelicRadius).Count()<Math.Max(1,Definition.NearbyTriggerCount)) return false;
        var target=Definition.Area==AbilityArea.CasterRadial || Definition.Effect!=AbilityEffect.Damage?Caster:Targeting.Acquire(relicRange,Definition.TargetRule,Radius);
        if(Definition.PeriodicDuration>0 && Caster==Build?.World.PlayerActor && Build.World.RunConfiguration.AdvancedClassId is "sorcerer" or "assassin")
        {
            target=Targeting.InArea(Caster.GlobalPosition,relicRange,relicRange).Where(t=>Periodic?.Has(this,t)!=true)
                .OrderBy(t=>Caster.GlobalPosition.DistanceSquaredTo(t.GlobalPosition)).ThenBy(t=>t.SelectionKey,StringComparer.Ordinal).FirstOrDefault();
        }
        if(Build?.World.RunConfiguration.AdvancedClassId=="marauder" && Definition.Id is "rupture" or "forcecrush")
            target=Build.Pregame.MarauderDotTarget(this);
        if(Definition.Id=="polarityshift" && Build?.Pregame.PolarityReady!=true)return false;
        if((Build?.World.RunConfiguration.AdvancedClassId is "juggernaut" or "marauder") && Definition.Id=="viciousthrow")target=Targeting.InArea(Caster.GlobalPosition,relicRange,relicRange).Where(t=>t.Health.Current<=t.Health.Maximum*.3f).OrderBy(t=>t.Health.Current).FirstOrDefault();
        if(Definition.Id=="electrocute" && Build!=null)target=Build.Pregame.ElectrocuteTarget(this);
        if(target==null) return false;
        if(Definition.Id=="dart" && Build?.World.RunConfiguration.AdvancedClassId=="operative"){
            target=Targeting.InArea(Caster.GlobalPosition,relicRange,relicRange).Where(t=>Periodic?.Remaining(this,t)<=2).OrderBy(t=>t.GlobalPosition.DistanceSquaredTo(Caster.GlobalPosition)).FirstOrDefault();
            if(target==null)return false;
        }
        if((Build?.World.RunConfiguration.AdvancedClassId is "juggernaut" or "marauder") && Definition.Id=="viciousthrow" && target.Health.Current>target.Health.Maximum*.3f)return false;
        if(!float.IsFinite(DamagePerTick) || (Definition.Effect==AbilityEffect.Damage && DamagePerTick<=0) || !float.IsFinite(TickSeconds) || TickSeconds<=0 ||
            !float.IsFinite(ChannelSeconds) || ChannelSeconds<0 || !float.IsFinite(relicRange) || relicRange<=0 ||
            !float.IsFinite(Radius) || Radius<0 || !float.IsFinite(Definition.WindupSeconds) || Definition.WindupSeconds<0 ||
            !float.IsFinite(CooldownSeconds) || CooldownSeconds<0 || !float.IsFinite(Definition.RecoverySeconds) || Definition.RecoverySeconds<0 ||
            !float.IsFinite(Definition.GestureSeconds) || Definition.GestureSeconds<0 ||
            !float.IsFinite(Healing) || Healing<0 || !float.IsFinite(Definition.HealingCap) || Definition.HealingCap<0 ||
            !float.IsFinite(HealthThreshold) || HealthThreshold<=0 || HealthThreshold>1 ||
            !float.IsFinite(ShieldCapacity) || ShieldCapacity<=0 || !float.IsFinite(ShieldDuration) || ShieldDuration<=0)
        { CombatEnabled=false;LastEvent="Invalid ability tuning";Abort();return false; }
        // A full-body ability only fires with its animation, and cannot come off cooldown before that animation has finished.
        if(Definition.Id=="carbonize" && Presentation.Actor.GestureOwner!=null)return false;
        if(Definition.FullBodyGesture && Definition.GestureSeconds>0 && Presentation!=null && !Presentation.Actor.CanBeginGesture(Definition.StartClip,Definition.GesturePriority)) return false;
        // Defer activation until its gesture can start. Missing clips keep the failure-reporting path.
        if(!spikeOpening && Definition.RequireGesture && Presentation.Actor.Animator.HasAnimation(Definition.StartClip) && !Presentation.Actor.TryBeginGesture(this,Definition.StartClip,Definition.GesturePriority,Definition.FullBodyGesture,Definition.KeepFullBodyWhileMoving))return false;
        CastTarget=target;GroundCenter=target.GlobalPosition;_coneForward=target.GlobalPosition-Caster.GlobalPosition;_projectileReleased=false;
        if(Definition.Area==AbilityArea.FrontalCone)Presentation.Actor.FacingTarget=GroundCenter;
        _damaged.Clear();_sweepHits.Clear();_spreadSeed=false;_sweepSeeds.Clear();LastHealing=0;_stormTicks=0;_chainRepeats=0;_lacerationStrikes=0;
        if(ChannelSeconds>0) _clock.Reset(Definition.Id=="laceration"?Definition.ChannelSeconds:ChannelSeconds,Definition.Id=="laceration"?Definition.ChannelSeconds:TickSeconds);
        State=LightningState.Starting;_phaseRemaining=Definition.WindupSeconds;
        CooldownRemaining=CooldownDuration=Math.Max(Definition.Id=="shock" && SorcererRelics.Rank(this,"accelerated_torment")>=0?20:Definition.Id=="storm" && SorcererRelics.Rank(this,"eye_of_the_tempest")>=0?0:CooldownSeconds,Definition.FullBodyGesture?GestureDuration/Math.Max(1,Build?.World.Abilities.CooldownRate??1):0);
        _gestureRemaining=GestureDuration;
        if(spikeOpening)_gestureRemaining=0;
        if(_gestureRemaining>0) Present(()=>Presentation.Actor.TryBeginGesture(this,Definition.StartClip,Definition.GesturePriority,Definition.FullBodyGesture,Definition.KeepFullBodyWhileMoving));
        // Presentation failures are reported and cannot suppress damage.
        try { _presented=Presentation.Begin(target,GroundCenter,AssassinRelicRadius); }
        catch(Exception ex) { _presented=false;GD.PushWarning(Definition.Title+": "+ex.Message); }
        if(!_presented) GD.PushWarning(Definition.Title+": original presentation unavailable; see B5 provenance.");
        Casts++;LastEvent="Cast started: "+target.SelectionKey;
        Build?.Activated(this);
        if(Definition.Effect!=AbilityEffect.Damage)
        { State=LightningState.Channeling;Tick();Finish(false,"Self effect applied"); }
        return true;
    }
    private float AssassinRelicRadius => Build?.MarauderRelics?.Radius(this) ?? Build?.AssassinRelics?.Radius(this) ?? Radius;
    private bool Tick()
    {
        float relicRange=Build?.JuggernautRelics?.Range(this)??Range;
        if(State!=LightningState.Channeling || !Caster.Alive || !CombatEnabled) return false;
        if(Definition.Effect!=AbilityEffect.Damage)
        {
            if(Definition.Effect==AbilityEffect.BallisticField) {
                BallisticField=new BallisticShieldField {Source=this,Remaining=ShieldDuration,Radius=Radius};
                GetTree().CurrentScene.AddChild(BallisticField);
                BallisticField.GlobalPosition=GroundCenter;
            }
            else if(Definition.Effect==AbilityEffect.Shield) Caster.Health.ApplyShield(ShieldCapacity,ShieldDuration);
            else if(Definition.Effect==AbilityEffect.Buff && Definition.Id!="polarityshift") Build?.ApplyBuff(Definition);
            else LastHealing=Caster.Health.ApplyHealing(Build?.HealingAmount(Healing)??Healing);
            if(_presented) Present(()=>Presentation.Impact(Caster.AimPosition));
            return true;
        }
        if(Definition.Area==AbilityArea.Chain) return ChainTick();
        _stormTicks++;
        var targets=Definition.Id=="smash" && Build?.MarauderRelics?.TargetSmash==true && CastTarget!=null
            ? Targeting.InArea(CastTarget.GlobalPosition,AssassinRelicRadius,relicRange,true).Append(CastTarget).Distinct().ToArray()
            : Definition.Area==AbilityArea.TargetSplash && Targeting.IsValidTarget(CastTarget,relicRange)
            ? Targeting.InArea(CastTarget!.GlobalPosition,Radius,relicRange,true).Append(CastTarget!).Distinct().ToArray()
            : Definition.Area==AbilityArea.CasterRadial
            ? Targeting.InArea(Caster.GlobalPosition,AssassinRelicRadius,AssassinRelicRadius).ToArray()
            : Definition.Area==AbilityArea.GroundSphere
            ? Targeting.InArea(GroundCenter,Radius,relicRange,Definition.PersistentArea).ToArray()
            : Definition.Area==AbilityArea.FrontalCone ? Targeting.InArea(Caster.GlobalPosition,AssassinRelicRadius,AssassinRelicRadius).Where(t=>InCone(t)).ToArray()
            : Targeting.IsValidTarget(CastTarget,relicRange)?new[]{CastTarget!}:Array.Empty<Combatant>();
        if(Definition.Id=="forcecharge" && Build?.World.RunConfiguration.AdvancedClassId=="marauder")
            targets=targets.Where(t=>t.GlobalPosition.DistanceSquaredTo(Caster.GlobalPosition)<=.45f*.45f).ToArray();
        targets=Build?.JuggernautRelics?.Targets(this,targets)??targets;
        if(Definition.MaximumTargets>0 && Build?.JuggernautRelics?.OverridesTargets(this)!=true)targets=targets.OrderBy(t=>t==CastTarget?0:1)
            .ThenBy(t=>t.GlobalPosition.DistanceSquaredTo(CastTarget?.GlobalPosition??Caster.GlobalPosition))
            .ThenBy(t=>t.SelectionKey,StringComparer.Ordinal).Take(Definition.MaximumTargets).ToArray();
        var dominion=SorcererRelics.Rank(this,"thundering_dominion");
        if(Definition.Id=="thunderingblast" && dominion>=0) {
            var storm=Build?.World.Abilities.Find("storm");
            if(storm!=null)targets=targets.Concat(storm.ActiveStorms.SelectMany(a=>Targeting.InArea(a.GroundCenter,a.Radius,a.Range,true))).Distinct().ToArray();
        }
        // Snapshot before damage: a lethal field hit must still spread the victim's owned DoTs.
        var spread=(Definition.SpreadsPeriodicEffects || Definition.Id=="lacerate" && Build?.World.RunConfiguration.AdvancedClassId=="assassin")?Periodic?.SpreadSources(Caster,targets):null;
        if(Definition.Id=="smash" && Build?.World.RunConfiguration.AdvancedClassId=="juggernaut")spread=spread?.Where(a=>a.Definition.Id=="forcecrush").ToArray();
        if(Definition.Id=="weakeningblast" && Build?.World.RunConfiguration.AdvancedClassId=="sniper")spread=spread?.Where(a=>a.Definition.Id is "dart" or "corrosivegrenade").ToArray();
        if(Definition.Id=="lacerate" && Build?.World.RunConfiguration.AdvancedClassId=="assassin" && Build.Pregame.AssassinSpread!=true)spread=spread?.Where(a=>a.Definition.Id=="discharge").ToArray();
        if(Definition.Id=="laceration" && Presentation is D3AbilityPresentation knife)knife.PrepareAgentKnifeStrike(++_lacerationStrikes);
        if(_presented) Present(()=>Presentation.Impact(Definition.Area==AbilityArea.GroundSphere?GroundCenter:CastTarget?.AimPosition??GroundCenter));
        if(Definition.Id=="flamesweep")
        {
            // Seeds are only this caster's burns found on targets of this activation.
            foreach(string id in new[]{"incendiarymissile","immolate"})
                if(Build?.World.Abilities.Find(id) is {} burn && targets.Any(t=>Periodic?.Has(burn,t)==true)){_spreadSeed=true;_sweepSeeds.Add(id);}
            foreach(var t in targets)_sweepHits.Add(t);
        }
        foreach(var target in targets)
        {
            if(State!=LightningState.Channeling || !Caster.Alive || !CombatEnabled) return false;
            if(!GodotObject.IsInstanceValid(target) || !target.Alive) continue;
            bool secondary=(Definition.Area==AbilityArea.TargetSplash || Definition.Area==AbilityArea.FrontalCone && Definition.MaximumTargets>0) && target!=CastTarget;
            float applied=DealHit(target,DamagePerTick*(Definition.Id=="thunderingblast" && target!=CastTarget && dominion>=0?new[]{.4f,.6f,.8f,1f}[dominion]:secondary && !(Definition.Id=="ravage" && Build?.JuggernautRelics?.OverridesTargets(this)==true)?Definition.SplashMultiplier:1));
            if(Definition.Id=="shock")Periodic?.Accelerate(this,target);
            int corruption=SorcererRelics.Rank(this,"corrupting_storm");
            if(Definition.Id=="storm" && corruption>=0 && target.Alive) {
                var ids=new[]{"affliction","crushingdarkness","creepingterror"};
                foreach(var id in ids.Take(corruption==3?3:Math.Min(corruption+1,_stormTicks))) {
                    var dot=Build?.World.Abilities.Find(id);
                    if(dot!=null && Periodic?.Has(dot,target)!=true)Periodic?.Apply(dot,target,relicApplied:true);
                }
            }
            if(target.Alive && Definition.PeriodicDuration>0)Periodic?.Apply(this,target,secondary?Definition.SecondaryPeriodicScale:1);
            if(target.Alive && Definition.VulnerabilitySeconds>0 && (!secondary || Definition.SplashAppliesVulnerability))target.Health.VulnerabilitySeconds=Math.Max(target.Health.VulnerabilitySeconds,Definition.VulnerabilitySeconds);
            if(target.Alive && Definition.SlowSeconds>0 && target is EnemyAgent slowed)slowed.Slow(Definition.SlowFactor,Definition.SlowSeconds);
            if((Build?.World.RunConfiguration.AdvancedClassId is "juggernaut" or "marauder") && Definition.Id is "forcechoke" or "backhand" && target is EnemyAgent controlled)controlled.Stun(3,false);
            Build?.AbilityHit(this,target);
            if(Definition.Area==AbilityArea.CasterRadial && target is EnemyAgent enemy && enemy.Alive) enemy.DisplaceFrom(Caster.GlobalPosition,Definition.KnockbackDistance,Definition.StaggerSeconds);
            if(applied<=0) continue;
            _damaged.Add(target.GetInstanceId());
            if(_presented && Presentation is ITargetImpactPresentation contact)Present(()=>contact.TargetImpact(target));
            if(_presented && Presentation is AssassinAbilityPresentation assassin)Present(()=>assassin.TargetImpact(target));
            if(_presented && Presentation is D3AbilityPresentation warrior) Present(()=>warrior.TargetImpact(target)); else if(_presented && Presentation is SurvivalAbilityPresentation original) Present(()=>original.TargetImpact(target));
            DamageEvents++;LastEvent=$"{Definition.Title}: {target.SelectionKey} Ã¢Ë†â€™{applied:0.#}";
            DamageDealt?.Invoke(target,applied);
            Build?.Hit(this,target,targets);
        }
        Build?.JuggernautRelics?.Resolved(this,targets);Build?.AssassinRelics?.Resolved(this,targets);Build?.MarauderRelics?.Resolved(this,targets);
        if(spread!=null){
            bool marauder=Build?.World.RunConfiguration.AdvancedClassId=="marauder";
            var newlyWounded=new HashSet<Combatant>();
            foreach(var target in targets.Where(t=>GodotObject.IsInstanceValid(t) && t.Alive))
                foreach(var source in spread){
                    if(marauder && !Build!.Pregame.MarauderMaySpread(this,source))continue;
                    bool fresh=!Periodic!.Has(source,target);
                    if(marauder && !fresh)continue;
                    int plague=Build?.AssassinRelics?.Rank("devouring_plague")??-1;
                    if(Definition.Id=="lacerate" && plague>=0 && fresh && !newlyWounded.Contains(target) && newlyWounded.Count>=2+plague)continue;
                    if(Definition.Id!="lacerate" || fresh)Periodic.Apply(source,target,marauder && fresh?1.35f:1);
                    if(fresh)newlyWounded.Add(target);
                }
            Build?.AssassinRelics?.Spread(this,newlyWounded);
            Build?.Pregame.AssassinSpreadHit(this,newlyWounded);
            if(marauder)Build!.Pregame.MarauderSpread(this,newlyWounded.ToArray());
        }
        if(Healing>0 && ChannelSeconds<=0)
        {
            LastHealing=Caster.Health.ApplyHealing(Math.Min(Definition.HealingCap,_damaged.Count*Healing));
            if(_presented && Presentation is SurvivalAbilityPresentation original) Present(()=>original.SustainHealing(LastHealing));
        }
        if(State!=LightningState.Channeling) return false;
        if(Definition.Area==AbilityArea.SingleTarget && !Targeting.IsValidTarget(CastTarget,relicRange))
        { Finish(!(Definition.ImpactTailsOnTargetDeath || ChannelSeconds<=0 && _damaged.Count>0),"Target died / invalidated");return false; }
        return true;
    }
    private bool InCone(Combatant target)
    {
        var forward=_coneForward;forward.Y=0;
        var direction=target.GlobalPosition-Caster.GlobalPosition;direction.Y=0;
        return direction.LengthSquared()<.00001f || forward.Normalized().Dot(direction.Normalized())>=.7071f;
    }
    private float DealHit(Combatant target,float amount)
    {
        bool critical=Definition.Id=="forcescream" && Build?.Pregame.MarauderScreamCritical==true || Definition.CriticalAgainstPeriodic!="" && Periodic?.On(target).Any(a=>a.Caster==Caster && a.Definition.Id==Definition.CriticalAgainstPeriodic)==true;
        float applied=target.Health.ApplyDamage(amount*(Build?.DamageMultiplier(this,target)??1)
            *(Definition.AfflictedBonus>0 && Periodic?.Has(target)==true?1+Definition.AfflictedBonus:1)
            *(critical?Definition.CriticalMultiplier:1),Definition.DamageType,critical,Definition.Id);
        if(applied>0){Build?.AssassinRelics?.Damaged(this,target);if(!target.Alive)Build?.AssassinRelics?.WitherKilled(this,target);}
        return applied;
    }
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
            int prison=SorcererRelics.Rank(this,"lightning_prison");
            if(prison>=0 && current is EnemyAgent rooted)rooted.Root(1.5f+.5f*prison);
            if(Build?.World.RunConfiguration.AdvancedClassId is "assassin" or "sniper")Build.AbilityHit(this,current);
            if(_presented && Presentation is IArcPresentation visual && (i>0 || Presentation is not OriginalAbilityPresentation))Present(()=>visual.Arc(from,point));
            if(applied>0) { _damaged.Add(current.GetInstanceId());DamageEvents++;DamageDealt?.Invoke(current,applied);Build?.Hit(this,current,new[]{current}); }
            from=point;
            var next=Targeting.ChainTarget(point,Definition.BounceRange,visited);
            if(next==null)break;
            current=next;damage*=Math.Clamp(Definition.BounceFalloff,0,1);
        }
        Build?.AssassinRelics?.Resolved(this,_damaged.Select(id=>Targeting.InArea(Caster.GlobalPosition,Range,Range,true).FirstOrDefault(t=>t.GetInstanceId()==id)).Where(t=>t!=null).Cast<Combatant>().Append(CastTarget!).Distinct().ToArray());
        LastEvent=$"Chain Lightning: {_damaged.Count} distinct hits";
        return true;
    }
    private void Finish(bool interrupted,string reason)
    {
        if(!interrupted && Definition.Id=="flamesweep" && _spreadSeed && (Build?.Talent("combustible_spread")>0 || Build?.Pregame.PowertechSweepSpreads==true))
        {
            foreach(string id in _sweepSeeds)
                if(Build!.World.Abilities.Find(id) is {Unlocked:true} burn)
                    foreach(var t in _sweepHits.Where(t=>GodotObject.IsInstanceValid(t) && t.Alive).OrderBy(t=>t.GetInstanceId()).Take(Build.SpreadLimit+Build.Pregame.PowertechSpreadLimit))Periodic?.Apply(burn,t,Build.SpreadEffectiveness);
        }
        if(_presented) Present(()=>Presentation.End(interrupted));
        if(!Definition.DetachedArea)Present(()=>Presentation.Actor.SetGestureClip(this,Definition.EndClip));
        State=LightningState.Ending;_phaseRemaining=Definition.FinishGesture?Math.Max(RecoveryDuration/(Build?.MarauderRelics?.Acceleration??1),_gestureRemaining):RecoveryDuration/(Build?.MarauderRelics?.Acceleration??1);LastEvent=reason;
    }
    public void Interrupt()
    {
        double remaining=CooldownRemaining,duration=CooldownDuration;
        Abort();CooldownRemaining=remaining;CooldownDuration=duration;
        State=Unlocked?(remaining>0?LightningState.Cooldown:LightningState.Idle):LightningState.Disabled;
    }
    public void Abort()
    {
        if(GodotObject.IsInstanceValid(BallisticField))BallisticField!.QueueFree();BallisticField=null;
        foreach(var storm in _storms.ToArray()){storm.Abort();((Node)storm.Presentation).QueueFree();storm.Free();}_storms.Clear();
        if(Definition.Effect==AbilityEffect.Shield && Caster!=null && GodotObject.IsInstanceValid(Caster.Health)) Caster.Health.ClearShield();
        // Completed gestures can leave finite tails or a persistent shield presentation.
        if(Presentation!=null) Present(Presentation.Release);
        if(Definition.Id=="spike" && Build?.World.RunConfiguration.AdvancedClassId=="assassin")Build.Pregame?.RevealSpike();
        if(Presentation!=null && GodotObject.IsInstanceValid(Presentation.Actor)) Presentation.Actor.ReleaseGesture(this);
        _gestureRemaining=0;_presented=false;CastTarget=null;CooldownRemaining=CooldownDuration=0;State=LightningState.Disabled;
    }
    public void SetUnlocked(bool unlocked)
    {
        if(Unlocked==unlocked) return;
        Abort();Unlocked=unlocked;State=unlocked?LightningState.Idle:LightningState.Disabled;
        LastEvent=unlocked?"Ability unlocked":"Unlock through a level-up choice";
    }
    public void ResetCombat() { Abort();DamageEvents=0;Casts=0;_damaged.Clear();_sweepHits.Clear();_spreadSeed=false;_sweepSeeds.Clear();LastHealing=0;State=Unlocked?LightningState.Idle:LightningState.Disabled;LastEvent="Combat reset"; }
    public override void _ExitTree() { if(Presentation!=null && GodotObject.IsInstanceValid(Presentation.Actor)) Abort(); }
}
