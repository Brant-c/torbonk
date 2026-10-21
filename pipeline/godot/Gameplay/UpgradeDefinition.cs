using Godot;
using System.Linq;

namespace SwtorSurvivors;

public enum UpgradeEffect { Damage, TickRate, Range, Cooldown, Health, Speed, Radius, UnlockAbility, AbilityPower, Mechanic }
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
    [Export] public string Mechanic {get;set;}="";
    [Export] public string MechanicalDescription {get;set;}="";
    [Export] public string RequiresUpgrade {get;set;}="";
    [Export] public bool D2Choice {get;set;}
    [Export] public float DamagePerRank { get; set; }
    [Export] public float RadiusPerRank { get; set; }
    [Export] public float HealingPerRank { get; set; }
    [Export] public float ShieldPerRank { get; set; }
    [Export] public float DurationPerRank { get; set; }
    [Export] public float CooldownPerRank { get; set; }
    [Export] public float ThresholdPerRank { get; set; }
    public float RarityMultiplier => RarityRules?.Multiplier(Rarity)??1;
    [Export] public RarityTuning? RarityRules { get; set; }
    public float EffectiveAmount => AmountPerRank*(RarityRules?.Multiplier(Rarity)??1);
    public string Scope => AbilityId switch { "shock"=>"Shock","storm"=>"Force Storm","lightning"=>"Force Lightning",
        "deathfield"=>"Death Field","barrier"=>"Static Barrier","darkheal"=>"Dark Heal","chain"=>"Chain Lightning","overload"=>"Overload",_=>ClassProfile.All.FirstOrDefault(p=>p.StartingAbility==AbilityId) is {} p?Godot.GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{p.StartingAbility}.tres").Title:"All abilities" };
    // Unbounded by default so three distinct meaningful choices remain available indefinitely.
    [Export] public int MaximumRank { get; set; }
    public string Description => Effect switch
    {
        UpgradeEffect.Mechanic => MechanicalDescription+" Authored Survivors rule; lasts this run.",
        UpgradeEffect.UnlockAbility => AbilityId=="chain"
            ? "Unlock a separate automatic Chain Lightning cast with its own 8-second cooldown. Hits one enemy, then jumps to nearby hostiles. Uses one offensive slot."
            : $"Unlock {Scope}. Adds its automatic ability to your action bar for this run.",
        UpgradeEffect.AbilityPower => AbilityId switch {
            "deathfield" => $"Death Field: +{DamagePerRank*RarityMultiplier:P0} damage, +{RadiusPerRank*RarityMultiplier:P0} radius, +{HealingPerRank*RarityMultiplier:P0} healing per target. Healing cap stays 15.",
            "barrier" => $"Static Barrier: +{ShieldPerRank*RarityMultiplier:P0} capacity, +{DurationPerRank*RarityMultiplier:P0} duration, +{CooldownPerRank*RarityMultiplier:P0} cooldown recovery rate.",
            _ => $"Dark Heal: +{HealingPerRank*RarityMultiplier:P0} healing, +{CooldownPerRank*RarityMultiplier:P0} cooldown recovery rate; trigger {ThresholdPerRank*RarityMultiplier*100:0.#} percentage points earlier (up to 80% HP)." },
        UpgradeEffect.Damage => $"{Scope}: +{EffectiveAmount:P0} base damage per rank.",
        UpgradeEffect.TickRate => $"{Scope}: +{EffectiveAmount:P0} tick frequency per rank (channels).",
        UpgradeEffect.Range => $"{Scope}: +{EffectiveAmount:P0} base range per rank.",
        UpgradeEffect.Cooldown => $"{Scope}: +{EffectiveAmount:P0} recovery rate per rank.",
        UpgradeEffect.Radius => $"{Scope}: +{EffectiveAmount:P0} base area radius per rank.",
        UpgradeEffect.Health => $"Maximum health +{EffectiveAmount:0} per rank. Heal that amount now.",
        _ => $"Movement speed +{EffectiveAmount:P0} of base per rank."
    };
}
