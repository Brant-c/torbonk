using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    public bool MarauderCarnagePreferred=>MrPoints("carnage")>Math.Max(MrPoints("annihilation"),MrPoints("marauder_rage")) || MrPoints("carnage")+MrPoints("annihilation")+MrPoints("marauder_rage")==0 && _world.Abilities.Find("massacre")?.Unlocked==true;
    public string MarauderPreferredTree=>MarauderCarnagePreferred?"carnage":MrPoints("marauder_rage")>MrPoints("annihilation")?"marauder_rage":"annihilation";
    // The tree and drops share names, rules and tuning. Existing Carnage IDs remain save-compatible; Annihilation and Rage drops follow the same rule.
    public IEnumerable<UpgradeDefinition> MarauderDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("marauder")) {
            var hooks=t.Effects.Where(x=>x.Kind=="hook").ToArray();
            if(hooks.Length!=1 || t.Effects.Length!=1)continue; // pure stat talents (Ruthless Training, Force Training) have no drop
            string suffix=t.Id.Split('.')[1];
            yield return new UpgradeDefinition {
                Id=t.Tree=="carnage"?suffix switch {"cleave"=>"marauder_cleave","flurry"=>"marauder_flurry",_=>"marauder_carnage_"+suffix}:"marauder_"+(t.Tree=="marauder_rage"?"rage":t.Tree)+"_"+suffix,
                Title=t.Name,Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),Spec=t.Tree,
                Effect=UpgradeEffect.Mechanic,Mechanic=hooks[0].Key,MaximumRank=1,MechanicalDescription=t.Description
            };
        }
    }
    public bool MarauderUpgradeUsable(string key)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Saber()=>_world.Abilities.Abilities.Any(a=>a.Unlocked && !a.Definition.IsBasicAttack && a.Definition.Id!="viciousslash" && a.Definition.PresentationKind=="saber" && a.Definition.Effect==AbilityEffect.Damage);
        bool Cleave()=>Has("massacre") || Has("ravage");
        bool Bleed()=>Has("rupture") || Has("deadlysaber");
        bool Dot()=>Bleed() || Has("forcecrush");
        return key switch {
            "mr_cleave" or "mr_blademaster"=>Cleave(),
            "mr_flurry"=>Cleave(),
            "mr_overwhelm"=>Has("ravage") && Has("massacre") && Has("gore"),
            "mr_bloodletting"=>Has("massacre") && Has("gore"),
            "mr_precision"=>Has("gore") && Cleave() || MarauderAtaruActive && Has("forcescream"),
            "mr_gore"=>Has("gore") && Cleave(),
            "mr_relentless"=>Has("berserk") && Saber(),
            "mr_siphon"=>Saber(),
            "mr_frenzy"=>Has("berserk"),
            "mr_charges" or "mr_rhythm"=>Has("deadlysaber"),
            "mr_extend"=>Has("rupture"),
            "mr_spread"=>Has("sweepingslash") && Dot(),
            "mr_reset"=>Has("annihilate") && Has("rupture"),
            "mr_hemorrhage"=>Has("annihilate") && Bleed(),
            "mr_rageblade"=>Saber(),
            "mr_execution"=>Has("deadlythrow") || Has("viciousthrow"),
            "mr_dominate"=>Has("forcescream"),
            "mr_forceflow"=>Has("forcecrush") && Has("saberthrow"),
            "mr_crushreset"=>Has("forcecrush") && Saber(),
            "mr_shockwave" or "mr_fissure"=>Has("smash"),
            "mr_ricochet"=>Has("saberthrow"),
            "mrm_dup" or "mrm_multi"=>Dot(),
            "mrm_detonate"=>Has("annihilate") && Bleed(),
            "mrm_tempo"=>Cleave() && Bleed(),
            "mrm_gore"=>Has("gore"),
            "mrm_surge"=>Saber() && (MarauderAtaruActive || Has("ataruform")),
            "mrm_predator"=>Has("predation") && (Bleed() || Has("annihilate")),
            "mrm_chain"=>Has("berserk"),
            "mrm_pulse"=>Has("saberthrow"),
            "mrm_aftershock"=>Has("smash"),
            "mrm_scream_spread"=>Has("forcescream") && Dot(),
            "mrm_hunger"=>Mr("mr_heal")>0,
            "mrm_siphon"=>Mr("mr_siphon")>0,
            _=>true
        };
    }
    private string MarauderUpgradeTree(UpgradeDefinition u)=>TalentCatalog.Current.ForClass("marauder")
        .FirstOrDefault(t=>t.Effects.Any(e=>e.Key!="" && e.Key==u.Mechanic))?.Tree ?? (u.Mechanic switch {
            "mrm_gore" or "mrm_surge" or "mrm_chain" or "mrm_target_massacre" or "mrm_target_ravage" or "mr_blademaster"=>"carnage",
            "mrm_dup" or "mrm_multi" or "mrm_detonate" or "mrm_tempo" or "mrm_hunger" or "mrm_siphon" or "mrm_target_sweepingslash"=>"annihilation",
            "mrm_predator" or "mrm_aftershock" or "mrm_scream_spread" or "mrm_pulse" or "mrm_target_saberthrow"=>"marauder_rage",
            _=>u.AbilityId switch {
                "batteringassault" or "massacre" or "gore" or "ravage" or "ataruform" or "berserk" or "predation" or "frenzy"=>"carnage",
                "forcecrush" or "smash" or "forcescream" or "saberthrow" or "intimidatingroar"=>"marauder_rage",
                "rupture" or "deadlysaber" or "annihilate" or "sweepingslash"=>"annihilation",_=>""}});
    public string MarauderCardTree(UpgradeDefinition u)=>MarauderUpgradeTree(u);

    // Single-axis scoped cards. Nothing here is rank-capped; rarity multiplies the one number each card moves.
    private static readonly (string id,string title,float amount)[] MarauderDamageCards={
        ("viciousslash","Vicious Mastery",.9f),("massacre","Brutal Massacre",1.1f),("ravage","Savage Ravage",1.1f),("batteringassault","Battering Fury",1f),
        ("annihilate","Annihilating Strike",1.2f),("gore","Gory Wound",1f),("sweepingslash","Cruel Sweep",1f),("saberthrow","Whirling Sabers",1f),
        ("smash","Seismic Slam",1.2f),("forcescream","Piercing Scream",1f),("rupture","Deep Rupture",.9f),("deadlysaber","Deadly Venom",.9f),("forcecrush","Gravity Well",.9f)};
    private static readonly (string id,string title,float amount)[] MarauderRadiusCards={
        ("massacre","Wider Massacre",.12f),("ravage","Wider Ravage",.12f),("sweepingslash","Wider Sweep",.12f),("saberthrow","Wider Throw",.12f),("smash","Wider Slam",.12f)};
    // Rupture, Deadly Saber and Force Crush are single-target DoTs: no recovery card.
    private static readonly (string id,string title,float amount)[] MarauderCooldownCards={
        ("massacre","Quickened Massacre",.18f),("ravage","Quickened Ravage",.18f),("batteringassault","Quickened Assault",.18f),("annihilate","Quickened Annihilate",.18f),
        ("sweepingslash","Quickened Sweep",.18f),("saberthrow","Quickened Throw",.18f),("smash","Quickened Slam",.18f)};
    private static readonly (string id,string title)[] MarauderTickCards={
        ("rupture","Rupture Pulse"),("deadlysaber","Venom Pulse"),("forcecrush","Crushing Pulse")};
    private static readonly (string id,string title)[] MarauderTargetCards={
        ("massacre","Massacre Reach"),("ravage","Ravage Reach"),("saberthrow","Throwing Reach"),("sweepingslash","Sweeping Reach")};

    public IEnumerable<UpgradeDefinition> MarauderRunPowers()
    {
        foreach(var (id,title,amount) in MarauderDamageCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="marauder_power_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,DamagePerRank=amount};
        }
        foreach(var (id,title,amount) in MarauderRadiusCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="marauder_radius_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,RadiusPerRank=amount};
        }
        foreach(var (id,title,amount) in MarauderCooldownCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="marauder_cooldown_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,CooldownPerRank=amount};
        }
        foreach(var (id,title) in MarauderTickCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="marauder_tick_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.TickRate,AmountPerRank=.15f};
        }
        foreach(var (id,title) in MarauderTargetCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            var card=new UpgradeDefinition {Id="marauder_target_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.Mechanic,Mechanic="mrm_target_"+id,
                AmountPerRank=0,MaximumRank=0,MechanicalDescription=$"{a.Definition.Title} strikes one additional enemy per rank. No rank cap."};
            card.Spec=MarauderUpgradeTree(card);
            yield return card;
        }
        UpgradeDefinition Mech(string id,string title,string iconAbility,string ability,string mechanic,float amount,string text,int maximum=0)
        {
            var a=_world.Abilities.Find(iconAbility);
            var card=new UpgradeDefinition {Id="marauder_"+id,Title=title,Icon=a?.Definition.Icon??"juggernautsmash",AbilityId=ability,Effect=UpgradeEffect.Mechanic,Mechanic=mechanic,
                AmountPerRank=amount,MaximumRank=maximum,MechanicalDescription=amount>0?text+" Scales with rarity.":text};
            card.Spec=MarauderUpgradeTree(card);
            return card;
        }
        yield return Mech("open_wounds","Open Wounds","rupture","","mrm_dup",.2f,"Rupture, Deadly Saber and Force Crush: each tick has a +20% chance per rank to strike again (100% guarantees an extra tick).");
        yield return Mech("wound_legion","Wound Legion","deadlysaber","","mrm_multi",0,"Rupture, Deadly Saber and Force Crush: +1 simultaneous instance per rank on each enemy. Extra casts add a new instance instead of refreshing, until slots are full.");
        yield return Mech("detonate","Detonate","annihilate","annihilate","mrm_detonate",.35f,"Annihilate (and Gore at half strength) pays out 35% per rank of up to 6 seconds of remaining Rupture and Deadly Saber damage, then leaves 2 seconds of each.");
        yield return Mech("blood_tempo","Blood Tempo","massacre","","mrm_tempo",.4f,"Each Rupture or Deadly Saber tick on any enemy builds Tempo (at most every 0.5 seconds). At 10 Tempo your next Massacre or Ravage resets its cooldown and strikes for 40% more per rank.");
        yield return Mech("gore_hunger","Gore Hunger","gore","gore","mrm_gore",.5f,"Defeating an enemy during Gore extends it by 0.5 seconds per rank. One Gore can never last longer than 3 times its base length.");
        yield return Mech("ataru_surge","Ataru Surge","ataruform","","mrm_surge",.1f,"Ataru follow-ups strike one additional enemy per rank and wait 0.1 seconds less per rank between strikes, never below 0.6 seconds.");
        yield return Mech("predator_mark","Predator's Mark","predation","predation","mrm_predator",.1f,"Predation marks your target for 8 seconds. Marked enemies take 10% more per rank from Rupture, Deadly Saber and Annihilate, and pass the mark on when they die.");
        yield return Mech("berserk_chain","Berserk Chain","berserk","berserk","mrm_chain",.4f,"Defeating an enemy during Berserk extends it by 0.4 seconds per rank, up to 3 times its normal length.");
        yield return Mech("siphon_pulse","Siphon Pulse","saberthrow","saberthrow","mrm_pulse",2f,"Dual Saber Throw striking three or more enemies heals 2 health per rank, at most once every 2 seconds.");
        yield return Mech("smash_aftershock","Aftershock","smash","smash","mrm_aftershock",0,"Smash repeats once more per rank, 1.2 seconds apart, each echo at 50% damage and 70% radius. Smash cooldown never drops below 6 seconds.");
        yield return Mech("scream_contagion","Scream Contagion","forcescream","forcescream","mrm_scream_spread",0,"Force Scream spreads your Rupture, Deadly Saber and Force Crush from its target to one more nearby enemy per rank.");
        yield return Mech("hungering_surge","Hungering Surge","rupture","","mrm_hunger",.2f,"Hungering Wounds' healing is 20% larger per rank.");
        yield return Mech("siphon_surge","Siphon Surge","viciousslash","","mrm_siphon",.2f,"Saber Siphon's healing is 20% larger per rank.");
        yield return Mech("resolve","Marauder's Resolve","undyingrage","","mrm_resolve",.01f,"Damage taken is divided by 1 plus 1% per rank, so mitigation can never reach 100%.");
    }
}
