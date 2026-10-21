using Godot;

namespace SwtorSurvivors;

/// <summary>Schedule and presentation data; attack rules remain on the existing boss scene.</summary>
[GlobalClass]
public partial class BossEncounterDefinition : Resource
{
    [Export] public string Id { get; set; } = "broodmatron";
    [Export] public string Title { get; set; } = "K'lor'slug Broodmatron";
    [Export] public PackedScene Scene { get; set; } = null!;
    [Export] public float IntroductionSeconds { get; set; } = 1.2f;
    [Export] public bool SpawnRegularEnemies { get; set; } = true;
    [Export] public float RegularSpawnIntervalMultiplier { get; set; } = 2.5f;
    [Export] public float RegularCapFraction { get; set; } = 0.55f;
}
