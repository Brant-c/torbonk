using Godot;
using System;

namespace SwtorSurvivors;

public enum EnemyState { Idle, Chase, Attack, Dead }

/// <summary>Combat rules on a movable body; optional presentation has no authority over movement/damage.</summary>
public partial class EnemyAgent : Combatant
{
    [Export] public float PreferredRange {get;set;}
    [Export] public float DetectionRange { get; set; } = 2.5f;
    [Export] public float MoveSpeed { get; set; } = 0.22f;
    [Export] public float MeleeRange { get; set; } = 0.10f;
    [Export] public float AttackInterval { get; set; } = 1.2f;
    [Export] public float AttackDamage { get; set; } = 5;
    [Export] public float Gravity { get; set; } = 3;
    [Export] public string DisplayName { get; set; } = "HOSTILE";
    [Export] public int ExperienceReward { get; set; } = 4;
    [Export] public float CollisionRadius { get; set; } = 0.03f;
    [Export] public float CollisionHeight { get; set; } = 0.185f;
    [Export] public int RewardTier { get; set; } // 0 ordinary, 1 elite, 2 major boss
    [Export] public float KnockbackResistance { get; set; }
    [Export] public bool GuaranteedReward { get; set; }
    [Export] public bool ScaleWithWaves { get; set; } = true;
    public int SpawnLevel { get; set; } = 1;
    public bool IsEncounterMember {get;set;}
    public int ReferenceLevel { get; set; } = 1;
    public float LevelDamageMultiplier { get; set; } = 1;
    public double StunRemaining {get;private set;}
    public double OilRemaining {get;set;}
    public float DealAttack(float amount,DamageType type=DamageType.Energy)
    {
        var build=(Target.GetParent()?.GetParent() as GameWorld)?.Build;
        build?.Attacked(this,Target.Health.Shielded);
        float received=Target.Health.ApplyDamage(amount*LevelDamageMultiplier*(OilRemaining>0?.75f:1)*(build?.IncomingMultiplier??1),type);
        build?.Pregame.AgentRetaliate(this,received);
        return received;
    }
    private bool _carbonized;
    public bool Stun(float seconds,bool carbonized=true)
    {
        if(!Alive || RewardTier>=2)return false;
        _carbonized=carbonized;
        if(carbonized)Visual?.CarbonizePose();
        StunRemaining=Math.Max(StunRemaining,seconds*(RewardTier==1?.35:1));
        if(Visual!=null)Visual.ProcessMode=ProcessModeEnum.Disabled;if(Presentation!=null)Presentation.ProcessMode=ProcessModeEnum.Disabled;
        if(carbonized)PowertechCarbonite.Attach(this);
        return true;
    }
    public double RootRemaining {get;private set;}
    public void Root(float seconds)=>RootRemaining=Math.Max(RootRemaining,seconds);
    public float SorcererDotMovementFactor {get;set;}=1;
    public double SlowRemaining { get; private set; }
    private float _slowFactor=1;
    public void Slow(float factor,float seconds) { _slowFactor=Math.Min(_slowFactor,Math.Clamp(factor,0.1f,1));SlowRemaining=Math.Max(SlowRemaining,seconds); }
    public float DisplaceFrom(Vector3 origin,float distance,float stagger=0.45f)
    {
        if(!Alive || Navigation==null)return 0;
        float length=Math.Max(0,distance)*(1-Math.Clamp(KnockbackResistance,0,1));
        var direction=GlobalPosition-origin;direction.Y=0;direction=direction.Normalized();
        var start=Body.GlobalPosition;
        // Bounded controlled steps, using this enemy's own full-body collision profile.
        int steps=Math.Clamp((int)Math.Ceiling(length/0.015f),0,64);
        for(int i=0;i<steps;i++)
        {
            var before=Body.GlobalPosition;var candidate=before+direction*Math.Min(0.015f,length-i*0.015f);
            if(!Navigation.CanWalk(before,candidate) || !Navigation.TryStandingPoint(candidate,out var feet))break;
            if(Body.MoveAndCollide(feet-before)!=null)break;
        }
        Body.Velocity=Vector3.Zero;_steerIn=0;
        _attackIn=Math.Max(_attackIn,stagger); // Resistant enemies still take damage and delay their next melee swing.
        float slowed=stagger>0.45f?stagger*(1-Math.Clamp(KnockbackResistance,0,1)):0;
        if(slowed>0)Slow(0.4f,slowed);
        return start.DistanceTo(Body.GlobalPosition);
    }
    public EnemyPresentation? Visual { get; private set; }
    public EnemySpawner? Crowd { get; set; }
    public Combatant Target { get; set; } = null!;
    public ArenaNavigation Navigation { get; set; } = null!;
    public CharacterBody3D Body { get; private set; } = null!;
    public EnemyState State { get; private set; } = EnemyState.Idle;
    public bool Running { get; private set; } = true;
    public int Attacks { get; private set; }
    public int SteeringSide { get; set; } = 1;
    private double _attackIn;
    private double _steerIn;
    private Vector3 _direction;
    private bool _engaged;
    private Label3D? _nameplate;

    public override void _Ready()
    {
        base._Ready();
        Body = GetParent<CharacterBody3D>();
        // World collision is retained. Soft separation replaces solid mob-to-mob queues.
        Body.CollisionLayer = 8; Body.CollisionMask = 3;
        Body.FloorMaxAngle = Mathf.DegToRad(48); Body.FloorSnapLength = 0.045f;
        Body.FloorStopOnSlope = true; Body.FloorConstantSpeed = true; Body.SafeMargin = 0.0005f;
        Presentation = GetNodeOrNull<CharacterPresentation>("Presentation");
        Visual = GetNodeOrNull<EnemyPresentation>("Visual");
        _nameplate = GetNodeOrNull<Label3D>("Nameplate");
        if(_nameplate!=null && Crowd?.ReuseAnimations==true)_nameplate.Font=GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf");
        Health.Damaged += _ => { _engaged=true; if (Health.IsAlive) { Presentation?.PlayReaction("Hit"); Visual?.Reaction(false); } UpdateNameplate(); };
        Health.Died += Die;
        UpdateNameplate();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Alive || !Running) return;
        RootRemaining=Math.Max(0,RootRemaining-delta);
        OilRemaining=Math.Max(0,OilRemaining-delta);
        if(StunRemaining>0){if(_carbonized)Visual?.CarbonizePose();
        StunRemaining=Math.Max(0,StunRemaining-delta);Body.Velocity=Vector3.Zero;
            if(StunRemaining==0){if(Visual!=null)Visual.ProcessMode=ProcessModeEnum.Inherit;if(Presentation!=null)Presentation.ProcessMode=ProcessModeEnum.Inherit;_attackIn=Math.Max(_attackIn,.35);}
            return;
        }
        SlowRemaining=Math.Max(0,SlowRemaining-delta);if(SlowRemaining<=0)_slowFactor=1;
        if (!GodotObject.IsInstanceValid(Target) || !Target.Alive) { Stop(); return; }
        if (GetNodeOrNull<Xrr3Attack>("Xrr3Attack") is { } raid && raid.Tick(delta))
        { Body.Velocity=Vector3.Zero;Visual?.Present(Vector3.Zero,Target.GlobalPosition,delta);return; }
        if (GetNodeOrNull<SoaAttack>("SoaAttack") is { } soa && soa.Tick(delta))
        { Body.Velocity=Vector3.Zero;Visual?.Present(Vector3.Zero,Target.GlobalPosition,delta);return; }
        if (GetNodeOrNull<TombAttack>("TombAttack") is { } trial && trial.Tick(delta))
        { Body.Velocity=Vector3.Zero;Visual?.Present(Vector3.Zero,Target.GlobalPosition,delta);return; }
        if (GetNodeOrNull<BossEncounter>("BossEncounter") is { } boss && boss.Tick(delta))
        { Body.Velocity = Vector3.Zero; Visual?.Present(Vector3.Zero, Target.GlobalPosition, delta); return; }
        if(RewardTier<2 && (Target.GetParent()?.GetParent() as GameWorld)?.Build?.Pregame.AssassinCloaked==true) {Body.Velocity=Vector3.Zero;Visual?.Present(Vector3.Zero,Target.GlobalPosition,delta);return;}
        float distance = GlobalPosition.DistanceTo(Target.GlobalPosition);
        // Detection starts pursuit; distance alone never ends an existing fight.
        _engaged |= distance <= DetectionRange;
        _attackIn = Math.Max(0, _attackIn - delta);
        bool inRange = distance <= MeleeRange;
        bool canHit = inRange && Navigation.HasLineOfSight(AimPosition, Target.AimPosition);
        var next = !_engaged ? EnemyState.Idle : canHit ? EnemyState.Attack : EnemyState.Chase;
        if (next != State)
        {
            // Every approach has a readable windup, including the very first swing.
            if (next == EnemyState.Attack) _attackIn = Math.Max(_attackIn, 0.35);
            State = next; _steerIn = 0;
        }
        if (Presentation != null) Presentation.FacingTarget = Target.GlobalPosition;
        if (State == EnemyState.Attack && _attackIn <= 0)
        {
            _attackIn = Math.Max(0.1f, AttackInterval);
            // Only physics time and current range/LOS determine damage. Hit animation is cosmetic.
            if (Alive && Target.Alive && DealAttack(AttackDamage) > 0) Attacks++;
            Visual?.Attack();
            // The player's synchronous death callback may have stopped this actor.
            if (!Running) return;
        }
        var before = Body.GlobalPosition;
        var horizontal = Vector3.Zero;
        if (State == EnemyState.Chase && (PreferredRange<=0 || distance>PreferredRange || !Navigation.HasLineOfSight(AimPosition,Target.AimPosition)))
        {
            _steerIn -= delta;
            if (_steerIn <= 0)
            {
                _direction = Navigation.Steer(before, Target.GlobalPosition, Math.Max(0.075f, MoveSpeed * 0.4f), SteeringSide);
                _steerIn = 0.18;
            }
            horizontal = _direction * MoveSpeed;
            if (Crowd != null) horizontal = (horizontal + Crowd.Separation(this) * MoveSpeed * 0.7f).LimitLength(MoveSpeed);
            horizontal *= RootRemaining>0?0:_slowFactor*SorcererDotMovementFactor;
            // MoveAndSlide handles walls. Validate the actual slide path below, rather than
            // probing an intended path and then probing almost the same path a second time.
        }
        Body.Velocity = new Vector3(horizontal.X, Body.IsOnFloor() ? 0 : Mathf.Max(-3, Body.Velocity.Y - Gravity * (float)delta), horizontal.Z);
        Body.MoveAndSlide();
        // Slide motion can differ from the steering sweep: reject unsafe sideways slips and falls.
        var flatAfter = new Vector3(Body.GlobalPosition.X, before.Y, Body.GlobalPosition.Z);
        bool movedHorizontally = flatAfter != before;
        if ((movedHorizontally && !Navigation.Arena.CanTraverse(before, flatAfter, Navigation.Radius)) || Body.GlobalPosition.Y < before.Y - 0.065f)
        {
            Body.GlobalPosition = before; Body.Velocity = Vector3.Zero; _direction = Vector3.Zero;
            // A blocked crowd must not retry all nine directions every physics tick.
            _steerIn = Math.Max(_steerIn, 0.06);
        }
        if (Body.IsOnWall() && !_direction.IsZeroApprox())
        { _direction = Vector3.Zero; _steerIn = 0.06; }
        Presentation?.Present((Body.GlobalPosition - before) / (float)delta, delta);
        Visual?.Present((Body.GlobalPosition - before) / (float)delta, Target.GlobalPosition, delta);
    }

    private void UpdateNameplate()
    {
        string classification=Crowd?.ReuseAnimations==true?(RewardTier>=2?"CHAMPION · ":RewardTier==1?"ELITE · ":""):"";
        if (_nameplate != null) _nameplate.Text = Health.IsAlive ? $"{classification}{DisplayName} · {Health.Current:0}/{Health.Maximum:0}" : "DEFEATED";
    }

    private void Die()
    {
        if(Visual!=null)Visual.ProcessMode=ProcessModeEnum.Inherit;if(Presentation!=null)Presentation.ProcessMode=ProcessModeEnum.Inherit;StunRemaining=0;
        State = EnemyState.Dead; CombatEnabled = false; Stop();
        // Retain Death pose while freeing the crowd collision immediately.
        Body.CollisionLayer = 0; Body.CollisionMask = 0;
        Body.GetNode<CollisionShape3D>("CollisionShape3D").SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
        Presentation?.PlayReaction("Death"); Visual?.Reaction(true); UpdateNameplate();
    }

    public void Stop()
    {
        Running = false; Body.Velocity = Vector3.Zero;
        if (State != EnemyState.Dead) { State = EnemyState.Idle; Presentation?.Present(Vector3.Zero, 0.1); }
        GetNodeOrNull<BossEncounter>("BossEncounter")?.End();
        GetNodeOrNull<TombAttack>("TombAttack")?.End();
        GetNodeOrNull<Xrr3Attack>("Xrr3Attack")?.End();
        GetNodeOrNull<SoaAttack>("SoaAttack")?.End();
        if (Visual != null && Target != null) Visual.Present(Vector3.Zero, Target.GlobalPosition, 0.1);
        SetPhysicsProcess(false);
    }
}
