using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Bounded presentation voices and six independently tunable buses. Never gates combat events.</summary>
public partial class AudioPresentation : Node
{
    public const string Group = "b7_audio";
    public static AudioPresentation? Find(Node node) => node.GetTree().GetFirstNodeInGroup(Group) as AudioPresentation;
    [Export] public string TuningPath { get; set; } = "res://Gameplay/presentation-audio.json";
    public int PlayingVoices => _voices.Count(v => v.Player.Playing);
    public int SuppressedCues { get; private set; }
    private record Cue(string Category, string Path, float Gain, double Cooldown, double Duration, float Pitch, bool Priority);
    private sealed class Voice
    {
        public AudioStreamPlayer Player = null!;
        public string Category = "";
        public double Age, Duration;
        public float Gain;
    }
    private readonly Dictionary<string, Cue> _cues = new();
    private readonly Dictionary<string, (int bus, float gain, int limit)> _categories = new();
    private readonly Dictionary<string, double> _last = new();
    private readonly Dictionary<string, AudioStream> _streams = new();
    private readonly List<Voice> _voices = new();
    private readonly List<AudioStreamPlayer3D> _combatVoices = new();
    private int _maximumCombatVoices;
    private double _clock, _duckFor;
    private float _duck, _combatDuckDb, _musicDuckDb, _worldDuckDb;
    private double _combatRepeatSeconds;
    private int _voiceLimit;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; AddToGroup(Group);
        using var config = JsonDocument.Parse(FileAccess.GetFileAsString(TuningPath));
        var root = config.RootElement;
        _voiceLimit = root.GetProperty("maximumPresentationVoices").GetInt32();
        _maximumCombatVoices = root.GetProperty("maximumCombatVoices").GetInt32();
        _combatRepeatSeconds = root.GetProperty("identicalCombatMinimumSeconds").GetDouble();
        _combatDuckDb = root.GetProperty("combatDuckDb").GetSingle();
        _musicDuckDb = root.GetProperty("musicDuckDb").GetSingle();
        _worldDuckDb = root.GetProperty("worldDuckDb").GetSingle();
        foreach (var cat in root.GetProperty("categories").EnumerateObject())
        {
            string name = "B7" + cat.Name;
            int bus = AudioServer.GetBusIndex(name);
            if (bus < 0) { AudioServer.AddBus(); bus = AudioServer.BusCount - 1; AudioServer.SetBusName(bus, name); AudioServer.SetBusSend(bus, "Master"); }
            float gain = cat.Value.GetProperty("gainDb").GetSingle();
            _categories[cat.Name] = (bus, gain, cat.Value.GetProperty("voices").GetInt32());
            AudioServer.SetBusVolumeDb(bus, gain);
        }
        foreach (var cue in root.GetProperty("cues").EnumerateObject())
        {
            var c = cue.Value;
            _cues[cue.Name] = new(c.GetProperty("category").GetString()!, c.GetProperty("path").GetString()!,
                c.GetProperty("gainDb").GetSingle(), c.GetProperty("cooldown").GetDouble(), c.GetProperty("duration").GetDouble(),
                c.GetProperty("pitch").GetSingle(), c.GetProperty("priority").GetBoolean());
        }
    }

    public bool PlayCue(string name)
    {
        if (!_cues.TryGetValue(name, out var cue)) return false;
        if (_last.TryGetValue(name, out double last) && _clock - last < cue.Cooldown) { SuppressedCues++; return false; }
        _last[name] = _clock;
        var active = _voices.Where(v => v.Player.Playing).ToArray();
        int limit = _categories[cue.Category].limit;
        Voice? voice = _voices.FirstOrDefault(v => !v.Player.Playing);
        if (active.Count(v => v.Category == cue.Category) >= limit)
        {
            if (!cue.Priority) { SuppressedCues++; return false; }
            voice = active.Where(v => v.Category == cue.Category).OrderByDescending(v => v.Age).First();
        }
        if (voice == null && _voices.Count >= _voiceLimit)
        {
            if (!cue.Priority) { SuppressedCues++; return false; }
            voice = active.OrderBy(v => v.Category is "Boss" or "Reward" ? 1 : 0).ThenByDescending(v => v.Age).First();
        }
        if (voice == null)
        {
            voice = new Voice { Player = new AudioStreamPlayer { ProcessMode = ProcessModeEnum.Always } };
            AddChild(voice.Player); _voices.Add(voice);
        }
        if (!_streams.TryGetValue(cue.Path, out var stream)) _streams[cue.Path] = stream = GD.Load<AudioStream>(cue.Path);
        voice.Player.Stop(); voice.Player.Stream = stream; voice.Player.Bus = "B7" + cue.Category;
        voice.Player.PitchScale = cue.Pitch; voice.Player.VolumeDb = cue.Gain;
        voice.Category = cue.Category; voice.Age = 0; voice.Gain = cue.Gain;
        voice.Duration = cue.Duration > 0 ? Math.Min(cue.Duration, stream.GetLength() / cue.Pitch) : stream.GetLength() / cue.Pitch;
        voice.Player.Play();
        if (cue.Priority) _duckFor = Math.Max(_duckFor, voice.Duration);
        return true;
    }
    // Audio-only rejection; callers must always continue VFX, damage and flytext.
    public bool PermitCombat(string name)
    {
        if (_combatVoices.Count(v => GodotObject.IsInstanceValid(v) && v.Playing) >= _maximumCombatVoices) return false;
        string key = "combat/" + name;
        if (_last.TryGetValue(key, out double last) && _clock - last < _combatRepeatSeconds) return false;
        _last[key] = _clock; return true;
    }
    public void RegisterCombatVoice(AudioStreamPlayer3D voice) => _combatVoices.Add(voice);
    public void SetCategoryGain(string category, float db)
    {
        if (!_categories.TryGetValue(category, out var entry)) return;
        _categories[category] = (entry.bus, Math.Clamp(db, -60, 6), entry.limit);
    }
    public override void _Process(double delta)
    {
        _clock += delta; _duckFor = Math.Max(0, _duckFor - delta);
        _duck = Mathf.MoveToward(_duck, _duckFor > 0 ? 1 : 0, (float)delta * (_duckFor > 0 ? 12 : 3));
        foreach (var (category, item) in _categories)
        {
            float duck = category switch { "Combat" => _combatDuckDb, "Music" => _musicDuckDb, "World" => _worldDuckDb, _ => 0 };
            AudioServer.SetBusVolumeDb(item.bus, item.gain + duck * _duck);
        }
        foreach (var voice in _voices)
        {
            if (!voice.Player.Playing) continue;
            voice.Age += delta;
            if (voice.Age >= voice.Duration) { voice.Player.Stop(); continue; }
            float fade = (float)Math.Clamp((voice.Duration - voice.Age) / 0.08, 0.001, 1);
            voice.Player.VolumeDb = voice.Gain + Mathf.LinearToDb(fade);
        }
    }
    public void Clear()
    {
        foreach (var v in _voices) v.Player.Stop();
        foreach (var v in _combatVoices) if (GodotObject.IsInstanceValid(v)) v.Stop();
        _last.Clear(); _duckFor = 0; _duck = 0;
        foreach (var c in _categories.Values) AudioServer.SetBusVolumeDb(c.bus, c.gain);
    }
    public override void _ExitTree() => Clear();
}
