using Godot;
using System;

namespace SwtorSurvivors;

/// <summary>Opt-in reaction audio, independent of animation holds and damage rules.</summary>
public partial class NpcVocalPresentation : Node
{
    [Export] public string[] HurtClips { get; set; } = Array.Empty<string>();
    [Export] public string[] DeathClips { get; set; } = Array.Empty<string>();
    [Export] public float HurtGainDb { get; set; } = -18;
    [Export] public float DeathGainDb { get; set; } = -14;
    private EnemyAgent _actor = null!;
    private Health _health = null!;
    private double _cooldown, _pending;
    private int _lastHurt = -1, _lastDeath = -1;
    private bool _first = true;
    public override void _Ready()
    {
        _actor = GetParent<EnemyAgent>();
        _health = _actor.GetNode<Health>("Health");
        _health.Damaged += Hurt;
        _health.Died += Death;
    }
    private void Hurt(float amount)
    {
        if (!_actor.Health.IsAlive || _cooldown > 0 || _pending > 0) return;
        _cooldown = 1.35; // Failed rolls also throttle damage-over-time and rapid hits.
        bool selected = _first || GD.Randf() < .30f; _first = false;
        if (selected) _pending = .035; // A death later in this frame cancels the grunt.
    }
    public override void _Process(double delta)
    {
        _cooldown = Math.Max(0, _cooldown - delta);
        if (_pending <= 0) return;
        _pending -= delta;
        if (_pending <= 0 && _actor.Alive) Play(false);
    }
    private void Death() { _pending = 0; Play(true); }
    private void Play(bool death)
    {
        var mixer = AudioPresentation.Find(this);
        if (mixer == null) return;
        var vocals = mixer.GetNodeOrNull<NpcVocalMixer>("NpcVocals");
        if (vocals == null) { vocals = new NpcVocalMixer { Name = "NpcVocals" }; mixer.AddChild(vocals); }
        var clips = death ? DeathClips : HurtClips;
        if (clips.Length == 0) return;
        int last = death ? _lastDeath : _lastHurt;
        int index = GD.RandRange(0, clips.Length - 1);
        if (clips.Length > 1 && index == last) index = (index + 1) % clips.Length;
        if (vocals.Request(_actor, clips[index], death, death ? DeathGainDb : HurtGainDb))
        { if (death) _lastDeath = index; else _lastHurt = index; }
    }
    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_health)) return;
        _health.Damaged -= Hurt; _health.Died -= Death;
    }
}
