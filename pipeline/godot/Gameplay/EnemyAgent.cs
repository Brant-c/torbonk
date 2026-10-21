using Godot;
using System;

namespace SwtorSurvivors;

public enum EnemyState { Idle, Chase, Attack, Dead }

/// <summary>Combat rules on a movable body; optional presentation has no authority over movement/damage.</summary>
public partial class EnemyAgent : Combatant
{
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
        Health.Damaged += _ => { if (Health.IsAlive) { Presentation?.PlayReaction("Hit"); Visual?.Reaction(false); } UpdateNameplate(); };
        Health.Died += Die;
        UpdateNameplate();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Alive || !Running) return;
        if (!GodotObject.IsInstanceValid(Target) || !Target.Alive) { Stop(); return; }
        if (GetNodeOrNull<BossEncounter>("BossEncounter") is { } boss && boss.Tick(delta))
        { Body.Velocity = Vector3.Zero; Visual?.Present(Vector3.Zero, Target.GlobalPosition, delta); return; }
        float distance = GlobalPosition.DistanceTo(Target.GlobalPosition);
        _attackIn = Math.Max(0, _attackIn - delta);
        bool inRange = distance <= MeleeRange;
        bool canHit = inRange && Navigation.HasLineOfSight(AimPosition, Target.AimPosition);
        var next = distance > DetectionRange ? EnemyState.Idle : canHit ? EnemyState.Attack : EnemyState.Chase;
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
            if (Alive && Target.Alive && Target.Health.ApplyDamage(AttackDamage) > 0) Attacks++;
            Visual?.Attack();
            // The player's synchronous death callback may have stopped this actor.
            if (!Running) return;
        }
        var before = Body.GlobalPosition;
        var horizontal = Vector3.Zero;
        if (State == EnemyState.Chase)
        {
            _steerIn -= delta;
            if (_steerIn <= 0)
            {
                _direction = Navigation.Steer(before, Target.GlobalPosition, Math.Max(0.075f, MoveSpeed * 0.4f), SteeringSide);
                _steerIn = 0.18;
            }
            horizontal = _direction * MoveSpeed;
            if (Crowd != null) horizontal = (horizontal + Crowd.Separation(this) * MoveSpeed * 0.7f).LimitLength(MoveSpeed);
            if (!Navigation.CanWalk(before, before + horizontal * (float)delta))
            { horizontal = Vector3.Zero; _steerIn = 0; }
        }
        Body.Velocity = new Vector3(horizontal.X, Body.IsOnFloor() ? 0 : Mathf.Max(-3, Body.Velocity.Y - Gravity * (float)delta), horizontal.Z);
        Body.MoveAndSlide();
        // Slide motion can differ from the steering sweep: reject unsafe sideways slips and falls.
        var flatAfter = new Vector3(Body.GlobalPosition.X, before.Y, Body.GlobalPosition.Z);
        if (!Navigation.Arena.CanTraverse(before, flatAfter, Navigation.Radius) || Body.GlobalPosition.Y < before.Y - 0.065f)
        { Body.GlobalPosition = before; Body.Velocity = Vector3.Zero; _steerIn = 0; }
        if (Body.IsOnWall()) _steerIn = 0;
        Presentation?.Present((Body.GlobalPosition - before) / (float)delta, delta);
        Visual?.Present((Body.GlobalPosition - before) / (float)delta, Target.GlobalPosition, delta);
    }

    private void UpdateNameplate()
    {
        if (_nameplate != null) _nameplate.Text = Health.IsAlive ? $"{DisplayName} · {Health.Current:0}/{Health.Maximum:0}" : "DEFEATED";
    }

    private void Die()
    {
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
        if (Visual != null && Target != null) Visual.Present(Vector3.Zero, Target.GlobalPosition, 0.1);
        SetPhysicsProcess(false);
    }
}
