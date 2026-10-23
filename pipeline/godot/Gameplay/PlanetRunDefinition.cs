using Godot;
using System;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors;

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
