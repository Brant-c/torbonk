using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    // Scoped damage needs a larger budget than the old all-kit training rank.
    public IEnumerable<UpgradeDefinition> AssassinRunPowers()
    {
        foreach(var (id,title,damage) in new (string,string,float)[] {
            ("lacerate","Razor Tempest",1.19f),("voltaicslash","Charged Blades",1.26f),("maul","Killing Edge",1.26f),("spike","Sudden Violence",1.58f),
            ("shock","Unstable Voltage",1.19f),("discharge","Surging Detonation",1.19f),("creepingterror","Ravenous Terror",1.19f),("affliction","Lingering Torment",1.19f),
            ("crushingdarkness","Crushing Agony",1.26f),("deathfield","Devouring Reach",1.19f),("wither","Gathering Darkness",1.26f) }) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="assassin_power_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,DamagePerRank=damage};
        }
        foreach(var (id,title,amount) in new (string,string,float)[] {
            ("spike","Wider Violence",.19f),("discharge","Wider Detonation",.15f),("deathfield","Wider Reach",.15f),("wither","Wider Darkness",.15f),("lacerate","Wider Tempest",.15f) }) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="assassin_radius_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,RadiusPerRank=amount};
        }
        // Single-target DoTs, Lacerate and Wither deliberately have no recovery card.
        foreach(var (id,title,amount) in new (string,string,float)[] {
            ("voltaicslash","Quickened Blades",.18f),("maul","Quickened Edge",.18f),("spike","Quickened Violence",.22f),("shock","Quickened Voltage",.18f) }) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="assassin_cooldown_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,CooldownPerRank=amount};
        }
        foreach(var (id,title) in new[] {("affliction","Lingering Pulse"),("creepingterror","Terror Pulse"),("crushingdarkness","Agony Pulse")}) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="assassin_tick_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.TickRate,AmountPerRank=.15f};
        }
        foreach(var (id,title) in new[] {("affliction","Festering Echo"),("creepingterror","Terror Echo"),("crushingdarkness","Agony Echo")}) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="assassin_duplicate_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.Mechanic,Mechanic="as_dup_"+id,
                AmountPerRank=.2f,MaximumRank=0,MechanicalDescription=$"{a.Definition.Title}: each tick has a +20% chance per rank to strike again (100% guarantees an extra tick). Scales with rarity."};
        }
        foreach(var (id,title) in new[] {("affliction","Festering Legion"),("creepingterror","Terror Legion"),("crushingdarkness","Agony Legion")}) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="assassin_multi_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.Mechanic,Mechanic="as_multi_"+id,
                MaximumRank=0,MechanicalDescription=$"{a.Definition.Title}: +1 simultaneous instance per rank on each enemy. Extra casts add a new instance instead of refreshing, until slots are full."};
        }
        foreach(var (key,title,ability,description) in new (string,string,string,string)[] {
            ("as_conduit","Voltaic Conduit","voltaicslash","Voltaic Slash hits recover 1 second of learned Shock and Discharge cooldowns, at most once per 0.5 seconds."),
            ("as_ambush","Recurring Nightmare","maul","Spike and Force Cloak recover 4 seconds of Maul cooldown. Maul gains 50% additional damage during a stealth opening."),
            ("as_chain","Forked Lightning","shock","Shock jumps to two additional enemies with 80% damage retained per jump. With Chain Shock, it reaches five enemies total. Each enemy is hit once."),
            ("as_echo","Agony Resonance","shock","Shock echoes one full tick of all your active DoTs on each struck enemy, instead of a quarter tick. Does not consume or refresh them; stacks with Raze."),
            ("as_spread","Razor Wake","lacerate","Lacerate also spreads your Terror, Affliction and Crushing Darkness. Each newly afflicted enemy takes an extra 75% of Lacerate damage, once per cast."),
            ("as_harvest","Deathmark Harvest","deathfield","Death Field gains 30% additional damage per distinct DoT you own on its target, up to 90%."),
            ("as_wither","Withering Blades","wither","Wither hits recover 2 seconds of learned Maul and Voltaic Slash cooldowns. Once per 0.5 seconds, regardless of targets."),
            ("as_guard","Shrouded Edge","wither","Activating Force Shroud or Deflection grants 60% additional Lacerate and Wither damage for 5 seconds. Reapplication refreshes the window."),
            ("as_decay","Quickening Decay","creepingterror","Your Terror, Affliction and Crushing Darkness tick 35% faster, with unchanged duration. Applies to effects already running and to spread copies.") }) {
            yield return new UpgradeDefinition {Id="assassin_"+key[3..],Title=title,
                Icon=_world.Abilities.Find(ability)!.Definition.Icon,AbilityId=key is "as_guard" or "as_decay"?"":ability,
                Effect=UpgradeEffect.Mechanic,Mechanic=key,MaximumRank=1,MechanicalDescription=description};
        }
    }

    private bool As(string key)=>IsAssassin && (_world.Progression?.Upgrades.Any(u=>u.Mechanic==key &&
        _world.Progression.Rank(u)>0 && AssassinUpgradeUsable(u) && (u.AbilityId=="" || _world.Abilities.Find(u.AbilityId)?.Unlocked==true))??false);

    public bool AssassinUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Dot()=>new[]{"discharge","creepingterror","affliction","crushingdarkness"}.Any(Has);
        return u.Mechanic switch {
            "as_conduit"=>Has("voltaicslash") && (Has("shock") || Has("discharge")),
            "as_ambush"=>Has("maul") && (Has("spike") || Has("forcecloak")),
            "as_echo"=>Has("shock") && Dot(),
            "as_spread"=>Has("lacerate") && new[]{"creepingterror","affliction","crushingdarkness"}.Any(Has),
            "as_harvest"=>Has("deathfield") && Dot(),
            "as_wither"=>Has("wither") && (Has("maul") || Has("voltaicslash")),
            "as_guard"=>(Has("lacerate") || Has("wither")) && (Has("forceshroud") || Has("deflection")),
            "as_decay"=>new[]{"creepingterror","affliction","crushingdarkness"}.Any(Has),
            _=>!(u.Mechanic.StartsWith("as_dup_") || u.Mechanic.StartsWith("as_multi_")) || Has(u.AbilityId)
        };
    }

    private float AssassinMechanicPower(string prefix,string id)
    {
        if(!IsAssassin || _world.Progression==null || !Has(id))return 0;
        return _world.Progression.Upgrades.Where(u=>u.Mechanic==prefix+id).Sum(u=>_world.Progression.RankPower(u));
        bool Has(string a)=>_world.Abilities.Find(a)?.Unlocked==true;
    }
    public float AssassinDuplicateChance(AutomaticAbility a)=>AssassinMechanicPower("as_dup_",a.Definition.Id)*.2f;
    public int AssassinDotSlots(AutomaticAbility a)=>1+(_world.Progression==null?0:_world.Progression.Upgrades.Where(u=>u.Mechanic=="as_multi_"+a.Definition.Id).Sum(u=>_world.Progression.Rank(u)));
}
