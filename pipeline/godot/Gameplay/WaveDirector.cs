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
    public double RecoveryRemaining => _encounterRecovery;
    public bool Recovering => _encounterRecovery > 0;
    public double ReturnRampRemaining { get; private set; }
    public PlanetStage? TravelDestination { get; private set; }
    private PlanetStage? _stage;
    private PlanetStage? _pendingTravel;
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
            Spawner.Difficulty = PlanetRun.Difficulty;
            Spawner.PlayerLevel = () => PlayerLevel?.Invoke() ?? 1;
            Spawner.HardActiveLimit = Math.Max(1, PlanetRun.Difficulty.MaximumPopulation);
            foreach(var stage in PlanetRun.Stages)
                foreach(var e in stage.Enemies)
                {
                    _planetScenes.TryAdd(e.Scene,GD.Load<PackedScene>(e.Scene));
                    Spawner.ReferenceLevels.TryAdd(e.Scene, stage.Level);
                }
            foreach(var m in PlanetRun.Milestones)
            {
                var definition = GD.Load<BossEncounterDefinition>(m.Encounter);
                _planetEncounters.TryAdd(m.Encounter, definition);
                Spawner.ReferenceLevels.TryAdd(definition.Scene.ResourcePath, m.Level);
            }
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
        ReturnRampRemaining=0;TravelDestination=null;_pendingTravel=null;
        _stage=PlanetRun?.StageAt(PlayerLevel?.Invoke()??1);
        Spawner.ReservedSlots=PlanetRun!=null?1:0;
        ApplyPlanetPressure();
    }

    private void ApplyWave()
    {
        WaveRemaining = Math.Max(1, WaveSeconds);
        SpawnInterval = Math.Max(Math.Max(0.1f, MinimumSpawnInterval), InitialSpawnInterval * Math.Pow(Math.Clamp(SpawnIntervalMultiplier, 0.1f, 1), Wave - 1));
        Spawner.ActiveLimit = Math.Clamp(InitialActiveLimit + (Wave - 1) * AdditionalActivePerWave, 1, Math.Max(1, MaximumActiveLimit));
        // Only newly spawned enemies scale. Existing health is never replenished at a wave boundary.
        Spawner.HealthMultiplier = Math.Clamp(1 + (Wave - 1) * HealthPerWave, 1, 3);
        Spawner.DamageMultiplier = Math.Clamp(1 + (Wave - 1) * DamagePerWave, 1, 2.5f);
        ApplyPlanetPressure();
    }

    public void ApplyPlanetPressure()
    {
        if (PlanetRun == null) return;
        int level = Math.Max(1, PlayerLevel?.Invoke() ?? 1);
        Spawner.ActiveLimit = PlanetRun.Difficulty.Population(level);
        SpawnInterval = PlanetRun.Difficulty.SpawnInterval(level, PlanetRun.CoreLevel);
        Spawner.HealthMultiplier = Spawner.DamageMultiplier = 1;
    }

    private void BeginRecovery(PlanetStage? destination)
    {
        if (PlanetRun == null || CoreComplete) return;
        TravelDestination = destination;
        // A stage unlocked during a reward/recovery belongs to the same break.
        if (!Recovering) _encounterRecovery = Math.Max(0, PlanetRun.Difficulty.RecoverySeconds);
        ReturnRampRemaining = 0;
        Spawner.ReinforcementsEnabled = !Recovering;
        _spawnIn = Math.Max(_spawnIn, SpawnInterval);
    }

    public bool BeginNextPhase()
    {
        if (!Running || !Recovering || TravelDestination == null || CanStartMilestone?.Invoke() == false ||
            Spawner.Player.GlobalPosition.DistanceTo(new Vector3(TravelDestination.Focus[0], TravelDestination.Focus[1], TravelDestination.Focus[2])) > .65f) return false;
        FinishRecovery();
        return true;
    }

    private void FinishRecovery()
    {
        _encounterRecovery = 0;
        ReturnRampRemaining = Math.Max(0, PlanetRun?.Difficulty.ReturnRampSeconds ?? 0);
        Spawner.ReinforcementsEnabled = true;
        _spawnIn = Math.Max(SpawnInterval, .5);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Running) return;
        if (!Spawner.Player.Alive) { Stop(); return; }
        if (PlanetRun != null)
        {
            ApplyPlanetPressure();
            bool recoveringBeforeStage = Recovering;
            var stage = PlanetRun.StageAt(PlayerLevel?.Invoke() ?? 1);
            if (_stage != stage)
            {
                _stage = stage;
                // Active bosses finish before a travel break can begin.
                if (!_milestoneLoaded) BeginRecovery(stage);
                else _pendingTravel=stage;
            }
            if (!recoveringBeforeStage && Recovering) return;
            bool wasRecovering = Recovering;
            TickMilestones(delta);
            if (!Running || wasRecovering || Recovering) return;
            ReturnRampRemaining = Math.Max(0, ReturnRampRemaining - delta);
        }
        WaveRemaining -= delta;
        if (WaveRemaining <= 0) { Wave++; ApplyWave(); _spawnIn = Math.Min(_spawnIn, SpawnInterval); }
        if(PlanetRun==null) Encounter.Tick(delta, Wave >= BossWave);
        _spawnIn -= delta;
        if (_spawnIn > 0) return;
        // One attempt per interval, with bounded retries when blocked; never catch up in a burst.
        bool allowed = Encounter.AllowRegularSpawn(Spawner.ActiveLimit);
        bool spawned = allowed && Spawner.TrySpawn(SelectEnemy(),elite:PlanetRun==null && Wave>=EliteFirstWave && EliteEverySpawns>0 && (Spawner.Spawned+1)%EliteEverySpawns==0);
        double ramp = 1 + 3 * ReturnRampRemaining / Math.Max(.01, PlanetRun?.Difficulty.ReturnRampSeconds ?? 1);
        _spawnIn = spawned || !allowed || Spawner.ActiveCount >= Spawner.ActiveLimit ? SpawnInterval * Encounter.CadenceMultiplier * ramp : 0.5;
    }

    public void Stop() { Running = false; Encounter.Fail(); Spawner.Stop(); }

    // The same single controller owns every milestone. Levels reached during a fight remain pending.
    public void TickMilestones(double delta)
    {
        if(PlanetRun==null || CoreComplete)return;
        if(_milestoneLoaded && Encounter.State==EncounterState.Defeated)
        {
            MilestonesCompleted++;_milestoneLoaded=false;
            if(CoreComplete){Running=false;Spawner.Stop();}
            else
            {
                var level = PlayerLevel?.Invoke() ?? 1;
                BeginRecovery(_pendingTravel ?? PlanetRun.Stages.FirstOrDefault(s => s.Level > level) ?? PlanetRun.StageAt(level));
                _pendingTravel=null;
            }
            return;
        }
        if(Encounter.State==EncounterState.Failed)return;
        if (Recovering)
        {
            if (CanStartMilestone?.Invoke() != false)
            {
                _encounterRecovery = Math.Max(0, _encounterRecovery - delta);
                if (!Recovering) FinishRecovery();
            }
            return;
        }
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
