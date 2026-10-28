using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Three scene-owned voices: corpse cleanup cannot cut off death tails.</summary>
public partial class NpcVocalMixer : Node
{
    private sealed class Voice
    {
        public AudioStreamPlayer3D Player = null!;
        public bool Death;
        public ulong Actor;
        public double Started;
    }
    private readonly List<Voice> _voices = new();
    private readonly Dictionary<string, AudioStream> _streams = new();
    private double _clock, _hurtAt = -100, _deathAt = -100;
    public int StartedVoices { get; private set; }
    public int PlayingVoices => _voices.Count(v => v.Player.Playing);
    public override void _Process(double delta) => _clock += delta;
    public bool Request(EnemyAgent actor, string path, bool death, float gain)
    {
        if (!GodotObject.IsInstanceValid(actor.Target) || !ResourceLoader.Exists(path)) return false;
        float distance = actor.GlobalPosition.DistanceTo(actor.Target.GlobalPosition);
        if (distance >= 1.5f) return false;
        ulong id = actor.GetInstanceId();
        // Always stop this actor's grunt when it dies, even if the death call is suppressed.
        if (death) foreach (var v in _voices.Where(v => v.Actor == id && !v.Death)) v.Player.Stop();
        if (_clock - (death ? _deathAt : _hurtAt) < (death ? .14 : .24)) return false;
        if (!death && _voices.Count(v => v.Player.Playing && !v.Death) >= 2) return false;
        var voice = _voices.FirstOrDefault(v => !v.Player.Playing);
        if (voice == null && _voices.Count < 3)
        {
            voice = new Voice { Player = new AudioStreamPlayer3D { Bus = "B7Combat",
                AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled } };
            AddChild(voice.Player); _voices.Add(voice);
        }
        // Deaths may replace pain reactions, but never stack beyond the shared cap.
        voice ??= death ? _voices.Where(v => !v.Death).OrderBy(v => v.Started).FirstOrDefault() : null;
        if (voice == null) return false;
        if (!_streams.TryGetValue(path, out var stream)) _streams[path] = stream = GD.Load<AudioStream>(path);
        voice.Player.Stop(); voice.Player.Stream = stream;
        voice.Player.GlobalPosition = actor.AimPosition;
        // Player-distance falloff suits the miniature world and overhead camera.
        voice.Player.VolumeDb = gain + Mathf.LinearToDb(Math.Max(.01f, 1 - distance / 1.5f));
        voice.Player.PitchScale = (float)GD.RandRange(.97, 1.03);
        voice.Actor = id; voice.Death = death; voice.Started = _clock; voice.Player.Play();
        if (death) _deathAt = _clock; else _hurtAt = _clock;
        StartedVoices++; return true;
    }
    public void Clear()
    {
        foreach (var v in _voices) v.Player.Stop();
        _hurtAt = _deathAt = -100;
    }
}
