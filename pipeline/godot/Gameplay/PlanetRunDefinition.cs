using Godot;
using System;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors;

public sealed class RunDifficulty
{
    public float HealthPerLevel { get; set; } = .08f;
    public float DamagePerLevel { get; set; } = .045f;
    public float SpeedPerLevel { get; set; } = .01f;
    public float MaximumSpeedBonus { get; set; } = .25f;
    public float AttackFrequencyPerLevel { get; set; } = .005f;
    public float MaximumAttackFrequencyBonus { get; set; } = .15f;
    public int InitialPopulation { get; set; } = 16;
    public int PopulationPerLevel { get; set; } = 3;
    public int MaximumPopulation { get; set; } = 100;
    public float InitialSpawnInterval { get; set; } = .5f;
    public float MinimumSpawnInterval { get; set; } = .15f;
    public float RecoverySeconds { get; set; } = 20;
    public float ReturnRampSeconds { get; set; } = 8;
    public int Population(int level) => (int)Math.Clamp((long)InitialPopulation + Math.Max(0L, (long)level - 1) * PopulationPerLevel, 1, Math.Max(1, MaximumPopulation));
    public double SpawnInterval(int level, int coreLevel) => Math.Max(.1, InitialSpawnInterval +
        (MinimumSpawnInterval - InitialSpawnInterval) * Math.Clamp((level - 1.0) / Math.Max(1, coreLevel - 1), 0, 1));
}

public sealed class PlanetEnemyWeight
{
    public string Scene { get; set; } = "";
    public float Weight { get; set; } = 1;
}
public sealed class PlanetStage
{
    public int Level { get; set; } = 1;
    public string Region { get; set; } = "";
    public string Description { get; set; } = "";
    public float[] Focus { get; set; } = [36,-17.1f,12.6f];
    public float AmbientGain { get; set; } = -20;
    public PlanetEnemyWeight[] Enemies { get; set; } = [];
}
public sealed class PlanetMilestone
{
    public int Level { get; set; }
    public string Encounter { get; set; } = "";
}
public sealed class PlanetCacheDefinition
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "Ancient Sith Cache";
    public float[] Position { get; set; } = [];
    public int Level { get; set; } = 1;
    public string Reward { get; set; } = "xp";
    public string Guard { get; set; } = "";
    public string Model { get; set; } = "res://Assets/Korriban/D4/item_con_crate_ancient_01.glb";
}
/// <summary>Optional planet data. Worlds without this resource retain their existing wave/roster/reward rules.</summary>
public sealed class PlanetRunDefinition
{
    public RunDifficulty Difficulty { get; set; } = new();
    public int CoreLevel { get; set; } = 30;
    public int FirstLevelExperience { get; set; } = 16;
    public float ExperienceGrowth { get; set; } = 1.105f;
    public string ArtifactCatalog { get; set; } = "";
    public string AudioTuning { get; set; } = "res://Gameplay/presentation-audio.json";
    public PlanetStage[] Stages { get; set; } = [];
    public PlanetMilestone[] Milestones { get; set; } = [];
    public PlanetCacheDefinition[] Caches { get; set; } = [];
    public PlanetStage StageAt(int level) => Stages.Last(s => s.Level <= level);
    public static PlanetRunDefinition Load(string path)
    {
        var d=JsonSerializer.Deserialize<PlanetRunDefinition>(FileAccess.GetFileAsString(path))!;
        if(d.Stages.Length==0 || d.Stages[0].Level!=1 || d.Stages.Any(s=>s.Enemies.Length==0 || s.Focus.Length!=3 || s.Enemies.Any(e=>e.Weight<=0))
            || !d.Stages.Select(s=>s.Level).SequenceEqual(d.Stages.Select(s=>s.Level).Distinct().Order())
            || !d.Milestones.Select(s=>s.Level).SequenceEqual(d.Milestones.Select(s=>s.Level).Distinct().Order()))
            throw new InvalidOperationException("Invalid planet run data: "+path);
        return d;
    }
}
