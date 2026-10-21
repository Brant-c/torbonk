using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Rig-independent adapter. Clips are mapped by each scene, never inferred from player bones.</summary>
public partial class EnemyPresentation : Node3D
{
    [Export] public string IdleClip { get; set; } = "Idle";
    [Export] public string RunClip { get; set; } = "Run";
    [Export] public string AttackClip { get; set; } = "Attack";
    [Export] public string HitClip { get; set; } = "Hit";
    [Export] public string DeathClip { get; set; } = "Death";
    [Export] public float ForwardOffsetDegrees { get; set; }
    [Export] public string Weapon { get; set; } = "";
    public AnimationPlayer Animator { get; private set; } = null!;
    public Skeleton3D Skeleton { get; private set; } = null!;
    public string State { get; private set; } = "";
    public bool Dead { get; private set; }
    private double _hold;
    private string _locomotion = "";
    public override void _Ready()
    {
        Animator = FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Single();
        Skeleton = FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
        foreach (var name in Animator.GetAnimationLibraryList())
        {
            var copy = (AnimationLibrary)Animator.GetAnimationLibrary(name).Duplicate(true);
            Animator.RemoveAnimationLibrary(name); Animator.AddAnimationLibrary(name, copy);
        }
        foreach (string clip in new[] { IdleClip, RunClip }) Animator.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;
        if (Weapon != "") EquipmentAttachment.Equip(Skeleton, Weapon, false);
        _locomotion = IdleClip; Play(IdleClip);
    }
    private void Play(string clip)
    {
        if (State == clip || !Animator.HasAnimation(clip)) return;
        State = clip; Animator.Play(clip, 0.09);
    }
    public void Present(Vector3 velocity, Vector3 target, double delta)
    {
        if (Dead) return;
        var flat = new Vector3(velocity.X, 0, velocity.Z);
        _locomotion = flat.Length() > 0.015f ? RunClip : IdleClip;
        var facing = flat.Length() > 0.015f ? flat : target - GlobalPosition;
        if (new Vector2(facing.X, facing.Z).Length() > 0.001f)
        {
            float angle = Mathf.Atan2(-facing.X, -facing.Z) + Mathf.DegToRad(ForwardOffsetDegrees);
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, angle, 1 - Mathf.Exp(-12 * (float)delta)), 0);
        }
        if (_hold <= 0) Play(_locomotion);
    }
    public override void _Process(double delta)
    {
        if (Dead || _hold <= 0) return;
        _hold -= delta; if (_hold <= 0) Play(_locomotion);
    }
    public void Attack() { if (!Dead) { Play(AttackClip); _hold = Animator.GetAnimation(AttackClip).Length; } }
    public void Reaction(bool death)
    {
        if (Dead) return;
        if (death) { Dead = true; Play(DeathClip); }
        else if (_hold <= 0) { Play(HitClip); _hold = Math.Min(0.25, Animator.GetAnimation(HitClip).Length); }
    }
}
