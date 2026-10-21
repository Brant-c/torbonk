using Godot;
namespace SwtorSurvivors;
[GlobalClass]
public partial class RarityTuning : Resource
{
    [Export] public float Premium { get; set; } = 1;
    [Export] public float Prototype { get; set; } = 1.25f;
    [Export] public float Artifact { get; set; } = 1.5f;
    public float Multiplier(PowerRarity rarity) => Mathf.Max(0,rarity switch
    { PowerRarity.Prototype=>Prototype,PowerRarity.Artifact=>Artifact,_=>Premium });
}
