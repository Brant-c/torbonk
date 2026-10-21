using Godot;

namespace SwtorSurvivors;

public enum UpgradeEffect { Damage, TickRate, Range, Cooldown, Health, Speed, Radius, UnlockAbility }
public enum PowerRarity { Premium, Prototype, Artifact }

/// <summary>Survivors tuning, not restored SWTOR ability rules.</summary>
[GlobalClass]
public partial class UpgradeDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Title { get; set; } = "";
    [Export] public string Icon { get; set; } = "sithsorcerer";
    [Export] public PowerRarity Rarity { get; set; } = PowerRarity.Premium;
    [Export] public UpgradeEffect Effect { get; set; }
    [Export] public float AmountPerRank { get; set; } = 0.2f;
    [Export] public string AbilityId { get; set; } = "";
    [Export] public RarityTuning? RarityRules { get; set; }
    public float EffectiveAmount => AmountPerRank*(RarityRules?.Multiplier(Rarity)??1);
    public string Scope => AbilityId switch { "shock"=>"Shock","storm"=>"Force Storm","lightning"=>"Force Lightning",_=>"All abilities" };
    // Unbounded by default so three distinct meaningful choices remain available indefinitely.
    [Export] public int MaximumRank { get; set; }
    public string Description => Effect switch
    {
        UpgradeEffect.UnlockAbility => $"Unlock {Scope}. Adds its automatic attack to your action bar for this run.",
        UpgradeEffect.Damage => $"{Scope}: +{EffectiveAmount:P0} base damage per rank.",
        UpgradeEffect.TickRate => $"{Scope}: +{EffectiveAmount:P0} tick frequency per rank (channels).",
        UpgradeEffect.Range => $"{Scope}: +{EffectiveAmount:P0} base range per rank.",
        UpgradeEffect.Cooldown => $"{Scope}: +{EffectiveAmount:P0} recovery rate per rank.",
        UpgradeEffect.Radius => $"{Scope}: +{EffectiveAmount:P0} base area radius per rank.",
        UpgradeEffect.Health => $"Maximum health +{EffectiveAmount:0} per rank. Heal that amount now.",
        _ => $"Movement speed +{EffectiveAmount:P0} of base per rank."
    };
}
