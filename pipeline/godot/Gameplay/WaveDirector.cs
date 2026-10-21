using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Time-based survival waves; all tuning is independent of models, terrain and effects.</summary>
public partial class WaveDirector : Node
{
    [Export] public float WaveSeconds { get; set; } = 30;
    [Export] public float InitialSpawnInterval { get; set; } = 0.75f;
    [Export] public float SpawnIntervalMultiplier { get; set; } = 0.94f;
    [Export] public float MinimumSpawnInterval { get; set; } = 0.30f;
    [Export] public int InitialActiveLimit { get; set; } = 8;
    [Export] public int AdditionalActivePerWave { get; set; } = 3;
    [Export] public int MaximumActiveLimit { get; set; } = 36;
    [Export] public int BossWave { get; set; } = 5;
    [Export] public int EliteEverySpawns {get;set;} = 12;
    [Export] public int EliteFirstWave {get;set;} = 2;
    [Export] public BossEncounterDefinition? BossDefinition { get; set; }
    [Export] public string RosterPath { get; set; } = "res://Gameplay/Enemies/roster.json";
    [Export] public float HealthPerWave { get; set; } = 0.12f;
    [Export] public float DamagePerWave { get; set; } = 0.1f;
    public EnemySpawner Spawner { get; set; } = null!;
    public int Wave { get; private set; }
    public double WaveRemaining { get; private set; }
    public double SpawnInterval { get; private set; }
    public bool Running { get; private set; }
    private double _spawnIn;
    public BossEncounterController Encounter { get; private set; } = null!;
    public bool BossSpawned => Encounter.Spawned;
    public EnemyAgent? Boss => Encounter.Boss;
    private record Entry(PackedScene Scene, int Unlock, float Weight, float Growth);
    private readonly List<Entry> _pool = new();
    private readonly RandomNumberGenerator _random = new();
    public override void _Ready()
    {
        _random.Randomize();
        Encounter = new BossEncounterController { Name = "BossEncounterController", Spawner = Spawner,
            Definition = BossDefinition ?? GD.Load<BossEncounterDefinition>("res://Gameplay/Enemies/BroodmatronEncounter.tres") };
        AddChild(Encounter);
        using var config = JsonDocument.Parse(FileAccess.GetFileAsString(RosterPath));
        foreach (var item in config.RootElement.GetProperty("enemies").EnumerateArray())
            _pool.Add(new Entry(GD.Load<PackedScene>(item.GetProperty("scene").GetString()!), item.GetProperty("unlockWave").GetInt32(),
                item.GetProperty("weight").GetSingle(), item.GetProperty("weightPerWave").GetSingle()));
    }
    private PackedScene SelectEnemy()
    {
        var eligible = _pool.Where(e => Wave >= e.Unlock).ToArray();
        float Weight(Entry e) => Math.Max(0, e.Weight + (Wave - e.Unlock) * e.Growth);
        float roll = _random.Randf() * eligible.Sum(Weight);
        foreach (var entry in eligible) { roll -= Weight(entry); if (roll <= 0) return entry.Scene; }
        return eligible.Last().Scene;
    }

    public void ResetWaves()
    {
        Encounter.ResetEncounter(); Spawner.ResetSpawner(); Wave = 1; ApplyWave(); _spawnIn = 0.5; Running = true;
    }

    private void ApplyWave()
    {
        WaveRemaining = Math.Max(1, WaveSeconds);
        SpawnInterval = Math.Max(Math.Max(0.1f, MinimumSpawnInterval), InitialSpawnInterval * Math.Pow(Math.Clamp(SpawnIntervalMultiplier, 0.1f, 1), Wave - 1));
        Spawner.ActiveLimit = Math.Clamp(InitialActiveLimit + (Wave - 1) * AdditionalActivePerWave, 1, Math.Max(1, MaximumActiveLimit));
        // Only newly spawned enemies scale. Existing health is never replenished at a wave boundary.
        Spawner.HealthMultiplier = Math.Clamp(1 + (Wave - 1) * HealthPerWave, 1, 3);
        Spawner.DamageMultiplier = Math.Clamp(1 + (Wave - 1) * DamagePerWave, 1, 2.5f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        if (!Spawner.Player.Alive) { Stop(); return; }
        WaveRemaining -= delta;
        if (WaveRemaining <= 0) { Wave++; ApplyWave(); _spawnIn = Math.Min(_spawnIn, SpawnInterval); }
        Encounter.Tick(delta, Wave >= BossWave);
        _spawnIn -= delta;
        if (_spawnIn > 0) return;
        // One attempt per interval, with bounded retries when blocked; never catch up in a burst.
        bool allowed = Encounter.AllowRegularSpawn(Spawner.ActiveLimit);
        bool spawned = allowed && Spawner.TrySpawn(SelectEnemy(),elite:Wave>=EliteFirstWave && EliteEverySpawns>0 && (Spawner.Spawned+1)%EliteEverySpawns==0);
        _spawnIn = spawned || !allowed || Spawner.ActiveCount >= Spawner.ActiveLimit ? SpawnInterval * Encounter.CadenceMultiplier : 0.5;
    }

    public void Stop() { Running = false; Encounter.Fail(); Spawner.Stop(); }
}
