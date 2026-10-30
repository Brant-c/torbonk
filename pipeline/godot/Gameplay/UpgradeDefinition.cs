using Godot;
using System.Linq;

namespace SwtorSurvivors;

public enum UpgradeEffect { Damage, TickRate, Range, Cooldown, Health, Speed, Radius, UnlockAbility, AbilityPower, Mechanic }
public enum PowerRarity { Standard = -1, Premium = 0, Prototype = 1, Artifact = 2 }

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
    [Export] public int MilestoneLevel {get;set;}
    [Export] public string Spec {get;set;}="";
    [Export] public string Role {get;set;}="dps";
    [Export] public string ChoiceDescription {get;set;}="";
    public string AbilityTitle {get;set;}="";
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
    public string Scope => AbilityTitle!=""?AbilityTitle:AbilityId switch { "shock"=>"Shock","storm"=>"Force Storm","lightning"=>"Force Lightning",
        "deathfield"=>"Death Field","barrier"=>"Static Barrier","darkheal"=>"Dark Heal","chain"=>"Chain Lightning","overload"=>"Overload",_=>AbilityId!=""?AbilityId:"All abilities" };
    // Unbounded by default so three distinct meaningful choices remain available indefinitely.
    [Export] public int MaximumRank { get; set; }
    public string Description => Effect switch
    {
        UpgradeEffect.Mechanic => MechanicalDescription,
        UpgradeEffect.UnlockAbility => AbilityId=="chain"
            ? "Unlock a separate automatic Chain Lightning cast with its own 6-second cooldown. Hits one enemy, then jumps to nearby hostiles. Uses one offensive slot."
            : AbilityId is "thunderingblast" or "affliction"
            ? Godot.GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{AbilityId}.tres").Description+" Uses one offensive slot."
            : $"Unlock {Scope}. Adds its automatic ability to your action bar for this run.",
        UpgradeEffect.AbilityPower when MechanicalDescription!="" => MechanicalDescription,
        UpgradeEffect.AbilityPower => PowerDescription(),
        UpgradeEffect.Damage => $"{Scope}: +{EffectiveAmount:P0} base damage per rank.",
        UpgradeEffect.TickRate => $"{Scope}: +{EffectiveAmount:P0} tick frequency per rank (channels and periodic damage; durations unchanged).",
        UpgradeEffect.Range => $"{Scope}: +{EffectiveAmount:P0} base range per rank.",
        UpgradeEffect.Cooldown => $"{Scope}: +{EffectiveAmount:P0} recovery rate per rank.",
        UpgradeEffect.Radius => $"{Scope}: +{EffectiveAmount:P0} base area radius per rank.",
        UpgradeEffect.Health => $"Maximum health +{EffectiveAmount:0} per rank. Heal that amount now.",
        _ => $"Movement speed +{EffectiveAmount:P0} of base per rank."
    };

    // One clause per non-zero axis, so a single-axis card reads as exactly its one effect.
    private string PowerDescription()
    {
        float m=RarityMultiplier;var axes=new System.Collections.Generic.List<string>();
        if(DamagePerRank>0)axes.Add($"+{DamagePerRank*m:P0} base damage");
        if(RadiusPerRank>0)axes.Add($"+{RadiusPerRank*m:P0} base radius");
        if(CooldownPerRank>0)axes.Add($"+{CooldownPerRank*m:P0} cooldown recovery rate");
        if(HealingPerRank>0)axes.Add($"+{HealingPerRank*m:P0} healing");
        if(ShieldPerRank>0)axes.Add($"+{ShieldPerRank*m:P0} capacity");
        if(DurationPerRank>0)axes.Add($"+{DurationPerRank*m:P0} duration");
        if(ThresholdPerRank>0)axes.Add($"trigger {ThresholdPerRank*m*100:0.#} percentage points earlier (up to 80% HP)");
        return $"{Scope}: {string.Join(", ",axes)} per rank.";
    }
}
