using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    // Scoped damage needs a larger budget than the old all-kit training rank.
    public IEnumerable<UpgradeDefinition> AssassinRunPowers()
    {
        foreach(var (id,title,damage,radius,recovery) in new (string,string,float,float,float)[] {
            ("lacerate","Razor Tempest",.6f,.12f,0),
            ("voltaicslash","Charged Blades",.8f,0,.1f),
            ("maul","Killing Edge",.8f,0,.12f),
            ("spike","Sudden Violence",1f,.15f,.15f),
            ("shock","Unstable Voltage",.8f,0,.1f),
            ("discharge","Surging Detonation",.7f,.12f,0),
            ("creepingterror","Ravenous Terror",.7f,0,.1f),
            ("affliction","Lingering Torment",.7f,0,.1f),
            ("crushingdarkness","Crushing Agony",.8f,0,.12f),
            ("deathfield","Devouring Reach",.7f,.15f,.1f),
            ("wither","Gathering Darkness",.8f,.15f,.1f),
            ("lowslash","Cruel Reprisal",1f,0,.15f),
            ("electrocute","Crackling Pursuit",.8f,0,.1f) }) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            string description=$"{a.Definition.Title}: +{damage:P0} base damage per rank"+(a.Definition.PeriodicDamage>0?" (including its periodic damage)":"");
            if(radius>0)description+=$", +{radius:P0} base radius";
            if(recovery>0)description+=$", +{recovery:P0} cooldown recovery";
            yield return new UpgradeDefinition {Id="assassin_power_"+id,Title=title,Icon=a.Definition.Icon,
                AbilityId=id,Effect=UpgradeEffect.AbilityPower,DamagePerRank=damage,RadiusPerRank=radius,
                CooldownPerRank=recovery,MechanicalDescription=description+". Bonuses add from base; no rank cap."};
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
            _=>true
        };
    }
}
