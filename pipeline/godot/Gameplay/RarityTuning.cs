using Godot;
namespace SwtorSurvivors;
[GlobalClass]
public partial class RarityTuning : Resource
{
    [Export] public float Standard { get; set; } = 0.4f;
    [Export] public float Premium { get; set; } = 1;
    [Export] public float Prototype { get; set; } = 1.6f;
    [Export] public float Artifact { get; set; } = 2f;
    // Standard, green, blue, purple. Rolled for every repeatable level-up reward.
    public PowerRarity Roll(float unit) => unit < .40f ? PowerRarity.Standard : unit < .75f ? PowerRarity.Premium : unit < .93f ? PowerRarity.Prototype : PowerRarity.Artifact;
    public float Multiplier(PowerRarity rarity) => Mathf.Max(0,rarity switch
    { PowerRarity.Standard=>Standard,PowerRarity.Prototype=>Prototype,PowerRarity.Artifact=>Artifact,_=>Premium });
}
