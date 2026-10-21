using Godot;
using System;

namespace SwtorSurvivors;

public enum BossPhase { Pursuit, Telegraph, Recovery, Ended }
/// <summary>One authored encounter: lock a ground target, warn, strike once, then recover.</summary>
public partial class BossEncounter : Node
{
    [Export] public float Radius { get; set; } = 0.30f;
    [Export] public float WindupSeconds { get; set; } = 1.35f;
    [Export] public float CooldownSeconds { get; set; } = 6;
    [Export] public float Damage { get; set; } = 18;
    public BossPhase Phase { get; private set; } = BossPhase.Pursuit;
    public int Telegraphs { get; private set; }
    public int Strikes { get; private set; }
    public Vector3 LockedCenter { get; private set; }
    private EnemyAgent _actor = null!;
    private double _remaining = 3;
    private MeshInstance3D _ring = null!;
    private ShaderMaterial _telegraphMaterial = null!;
    private double _impactRemaining;
    public override void _Ready()
    {
        _actor = GetParent<EnemyAgent>();
        // Authored amber telegraph; outer boundary is exactly the unchanged damage radius.
        _telegraphMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://Gameplay/BossTelegraph.gdshader") };
        _ring = new MeshInstance3D { Visible = false, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Mesh = new PlaneMesh { Size = Vector2.One * Radius * 2 }, MaterialOverride = _telegraphMaterial };
        _actor.GetTree().CurrentScene.AddChild(_ring);
        // Child _Ready runs before Combatant initializes its convenience Health property.
        _actor.GetNode<Health>("Health").Died += End;
    }
    public override void _Process(double delta)
    {
        if (Phase == BossPhase.Telegraph)
            _telegraphMaterial.SetShaderParameter("progress", 1 - (float)(_remaining / WindupSeconds));
        else if (_impactRemaining > 0)
        {
            _impactRemaining -= delta;
            _telegraphMaterial.SetShaderParameter("impact", (float)Math.Max(0, _impactRemaining / 0.28));
            if (_impactRemaining <= 0) _ring.Visible = false;
        }
    }
    public bool Tick(double delta)
    {
        if (Phase == BossPhase.Ended) return false;
        _remaining -= delta;
        if (Phase == BossPhase.Pursuit)
        {
            if (_remaining > 0 || _actor.GlobalPosition.DistanceTo(_actor.Target.GlobalPosition) > 0.65f) return false;
            LockedCenter = _actor.Target.GlobalPosition;
            _ring.GlobalPosition = LockedCenter + Vector3.Up * 0.004f; _ring.Visible = true;
            _telegraphMaterial.SetShaderParameter("impact", 0f); _telegraphMaterial.SetShaderParameter("progress", 0f);
            AudioPresentation.Find(this)?.PlayCue("burrow_warning");
            Phase = BossPhase.Telegraph; _remaining = WindupSeconds; Telegraphs++;
            return true;
        }
        if (_remaining > 0) return true;
        if (Phase == BossPhase.Telegraph)
        {
            _impactRemaining = 0.28; _telegraphMaterial.SetShaderParameter("impact", 1f);
            AudioPresentation.Find(this)?.PlayCue("burrow_impact");
            Strikes++; _actor.Visual?.Attack();
            if (_actor.Target.Alive && _actor.Target.GlobalPosition.DistanceTo(LockedCenter) <= Radius &&
                _actor.Navigation.HasLineOfSight(_actor.AimPosition, _actor.Target.AimPosition))
                _actor.Target.Health.ApplyDamage(Damage, DamageType.Kinetic);
            if (Phase == BossPhase.Ended) return true;
            Phase = BossPhase.Recovery; _remaining = 1.0;
        }
        else { Phase = BossPhase.Pursuit; _remaining = CooldownSeconds; }
        return true;
    }
    public void End() { Phase = BossPhase.Ended; _impactRemaining = 0; if (_ring != null) _ring.Visible = false; }
    public override void _ExitTree() { if (GodotObject.IsInstanceValid(_ring)) _ring.QueueFree(); }
}
