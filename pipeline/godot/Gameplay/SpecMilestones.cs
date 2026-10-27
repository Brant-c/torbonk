using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;

/// <summary>Explicit milestone alternatives. Each choice stands alone; no permanent spec lock.</summary>
public static class SpecMilestones
{
    private sealed record Choice(int Level,string Spec,string Ability,float Damage=0,float Shield=0,float Recovery=0,string Role="dps");
    private static readonly Choice[] Assassin={
        new(5,"darkness","forceshroud",Shield:.25f,Role:"tank"),new(5,"deception","discharge"),new(5,"hatred","affliction"),
        new(10,"darkness","forceshroud",Shield:.3f,Role:"tank"),new(10,"deception","lacerate",.4f),new(10,"hatred","deathfield"),
        new(15,"darkness","forceshroud",Recovery:.25f,Role:"tank"),new(15,"deception","discharge",.4f),new(15,"hatred","creepingterror"),
        new(20,"darkness","forceshroud",Shield:.3f,Role:"tank"),new(20,"deception","lacerate",.4f),new(20,"hatred","affliction",.4f),
        new(25,"darkness","forceshroud",Recovery:.25f,Role:"tank"),new(25,"deception","discharge",.4f),new(25,"hatred","deathfield",.4f),
        new(30,"darkness","forceshroud",Shield:.3f,Role:"tank"),new(30,"deception","lacerate",.4f),new(30,"hatred","creepingterror",.4f)
    };
    private static Choice[] Choices(string id)=>id=="assassin"?Assassin:Array.Empty<Choice>();
    public static string SpecFor(string classId,string ability)=>Choices(classId).FirstOrDefault(c=>c.Ability==ability)?.Spec??"";
    public static bool Reserved(GameWorld world,UpgradeDefinition upgrade)=>!(world.RunConfiguration.AdvancedClassId=="assassin" && upgrade.AbilityId is "creepingterror" or "deathfield" or "discharge" or "spike") && !(world.RunConfiguration.AdvancedClassId=="assassin" && upgrade.AbilityId=="creepingterror" && world.Build.Pregame.Mechanic("early_terror")>0) && upgrade.Effect==UpgradeEffect.UnlockAbility && Choices(world.RunConfiguration.AdvancedClassId).Any(c=>c.Ability==upgrade.AbilityId);
    public static IEnumerable<UpgradeDefinition> Catalog(GameWorld world)
    {
        foreach(var choice in Choices(world.RunConfiguration.AdvancedClassId))
        {
            var ability=world.Abilities.Find(choice.Ability);if(ability==null)continue;
            yield return new UpgradeDefinition {Id=$"spec_{choice.Level}_{choice.Spec}",Title=ability.Definition.Title,
                Icon=ability.Definition.Icon,Spec=choice.Spec,Role=choice.Role,MilestoneLevel=choice.Level,
                AbilityId=choice.Ability,MaximumRank=1,Rarity=PowerRarity.Prototype,Effect=UpgradeEffect.AbilityPower,
                DamagePerRank=choice.Damage,ShieldPerRank=choice.Shield,CooldownPerRank=choice.Recovery};
        }
    }
    public static void Prepare(UpgradeDefinition upgrade,AutomaticAbility ability)
    {
        if(!ability.Unlocked)
        {
            upgrade.Effect=UpgradeEffect.UnlockAbility;
            float total=ability.DamagePerTick+ability.Definition.PeriodicDamage*(ability.DamagePerTick/Math.Max(.001f,ability.Definition.Damage))*Mathf.Floor(ability.Definition.PeriodicDuration/ability.Definition.PeriodicInterval);
            upgrade.ChoiceDescription=ability.Definition.Id switch {
                "shock"=>$"Deal {ability.DamagePerTick:0.#} electrical damage with a quick strike.",
                "affliction"=>$"{total:0.#} internal damage over {ability.Definition.PeriodicDuration:0.#}s.",
                "storm"=>$"Deal {ability.DamagePerTick:0.#} area damage every {ability.TickSeconds:0.##}s for {ability.ChannelSeconds:0.#}s.",
                "deathfield"=>$"{ability.DamagePerTick:0.#} area damage. Spread all your DoTs to every enemy hit.",
                "chain"=>$"Deal {ability.DamagePerTick:0.#} damage, then jump to {ability.Definition.BounceCount} nearby enemies.",
                "creepingterror"=>$"{total:0.#} internal damage over {ability.Definition.PeriodicDuration:0.#}s. Damaging ticks restore 1 HP each second.",
                "lightning"=>$"Channel {ability.DamagePerTick:0.#} damage every {ability.TickSeconds:0.##}s for {ability.ChannelSeconds:0.#}s.",
                "thunderingblast"=>$"{ability.DamagePerTick:0.#} internal damage. Guaranteed critical against your Affliction.",
                _=>ability.Definition.Description};
        }
        else
        {
            upgrade.Effect=UpgradeEffect.AbilityPower;
            // Returning to an ability after mixing paths always gives an effective improvement.
            if(upgrade.DamagePerRank==0 && upgrade.ShieldPerRank==0 && upgrade.CooldownPerRank==0)upgrade.DamagePerRank=.4f;
            upgrade.ChoiceDescription=upgrade.ShieldPerRank>0?$"Increase shield capacity by {upgrade.ShieldPerRank:P0}.":
                upgrade.CooldownPerRank>0?$"Increase cooldown recovery by {upgrade.CooldownPerRank:P0}.":$"Increase damage by {upgrade.DamagePerRank:P0}.";
        }
    }
}
