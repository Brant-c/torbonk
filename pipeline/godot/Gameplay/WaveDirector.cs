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
    [Export] public int EliteEverySpawns {get;set;} = 20;
    [Export] public int EliteFirstWave {get;set;} = 2;
    [Export] public BossEncounterDefinition? BossDefinition { get; set; }
    [Export] public string RosterPath { get; set; } = "res://Gameplay/Enemies/roster.json";
    [Export] public float HealthPerWave { get; set; } = 0.12f;
    [Export] public float DamagePerWave { get; set; } = 0.1f;
    public EnemySpawner Spawner { get; set; } = null!;
    public PlanetRunDefinition? PlanetRun { get; set; }
    public Func<int>? PlayerLevel { get; set; }
    public Func<bool>? CanStartMilestone { get; set; }
    public int MilestonesCompleted { get; private set; }
    public int MilestonesStarted { get; private set; }
    public bool CoreComplete => PlanetRun != null && MilestonesCompleted == PlanetRun.Milestones.Length;
    private bool _milestoneLoaded;
    private double _encounterRecovery;
    private readonly Dictionary<string,PackedScene> _planetScenes = new();
    private readonly Dictionary<string,BossEncounterDefinition> _planetEncounters = new();
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
        if(PlanetRun!=null)
        {
            foreach(var e in PlanetRun.Stages.SelectMany(s=>s.Enemies))
                _planetScenes.TryAdd(e.Scene,GD.Load<PackedScene>(e.Scene));
            foreach(var m in PlanetRun.Milestones)
                _planetEncounters.TryAdd(m.Encounter,GD.Load<BossEncounterDefinition>(m.Encounter));
            return;
        }
        using var config = JsonDocument.Parse(FileAccess.GetFileAsString(RosterPath));
        foreach (var item in config.RootElement.GetProperty("enemies").EnumerateArray())
            _pool.Add(new Entry(GD.Load<PackedScene>(item.GetProperty("scene").GetString()!), item.GetProperty("unlockWave").GetInt32(),
                item.GetProperty("weight").GetSingle(), item.GetProperty("weightPerWave").GetSingle()));
    }
    private PackedScene SelectEnemy()
    {
        if(PlanetRun!=null)
        {
            var entries=PlanetRun.StageAt(PlayerLevel?.Invoke()??1).Enemies;
            float choice=_random.Randf()*entries.Sum(e=>e.Weight);
            foreach(var e in entries) {choice-=e.Weight;if(choice<=0)return _planetScenes[e.Scene];}
            return _planetScenes[entries.Last().Scene];
        }
        var eligible = _pool.Where(e => Wave >= e.Unlock).ToArray();
        float Weight(Entry e) => Math.Max(0, e.Weight + (Wave - e.Unlock) * e.Growth);
        float roll = _random.Randf() * eligible.Sum(Weight);
        foreach (var entry in eligible) { roll -= Weight(entry); if (roll <= 0) return entry.Scene; }
        return eligible.Last().Scene;
    }

    public void ResetWaves()
    {
        Encounter.ResetEncounter(); Spawner.ResetSpawner(); Wave = 1; ApplyWave(); _spawnIn = 0.5; Running = true;
        MilestonesCompleted=MilestonesStarted=0;_milestoneLoaded=false;_encounterRecovery=0;
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
        if(PlanetRun==null) Encounter.Tick(delta, Wave >= BossWave);
        else TickMilestones(delta);
        _spawnIn -= delta;
        if (_spawnIn > 0) return;
        // One attempt per interval, with bounded retries when blocked; never catch up in a burst.
        bool allowed = Encounter.AllowRegularSpawn(Spawner.ActiveLimit);
        bool spawned = allowed && Spawner.TrySpawn(SelectEnemy(),elite:PlanetRun==null && Wave>=EliteFirstWave && EliteEverySpawns>0 && (Spawner.Spawned+1)%EliteEverySpawns==0);
        _spawnIn = spawned || !allowed || Spawner.ActiveCount >= Spawner.ActiveLimit ? SpawnInterval * Encounter.CadenceMultiplier : 0.5;
    }

    public void Stop() { Running = false; Encounter.Fail(); Spawner.Stop(); }

    // The same single controller owns every milestone. Levels reached during a fight remain pending.
    public void TickMilestones(double delta)
    {
        if(PlanetRun==null || CoreComplete)return;
        if(_milestoneLoaded && Encounter.State==EncounterState.Defeated)
        {
            MilestonesCompleted++;_milestoneLoaded=false;_encounterRecovery=5;
            if(CoreComplete){Running=false;Spawner.Stop();}
            return;
        }
        if(Encounter.State==EncounterState.Failed)return;
        _encounterRecovery=Math.Max(0,_encounterRecovery-delta);
        if(!_milestoneLoaded)
        {
            if(_encounterRecovery>0 || CanStartMilestone?.Invoke()==false)return;
            var next=PlanetRun.Milestones[MilestonesCompleted];
            if((PlayerLevel?.Invoke()??1)<next.Level)return;
            Encounter.ResetEncounter();Encounter.Definition=_planetEncounters[next.Encounter];_milestoneLoaded=true;
        }
        bool before=Encounter.Spawned;Encounter.Tick(delta,true);
        if(!before && Encounter.Spawned)MilestonesStarted++;
    }
}
