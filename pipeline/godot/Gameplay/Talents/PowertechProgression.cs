using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    // Ability power is uncapped and rolls rarity; reach/spread/cleave cards scale area hard instead of damage.
    public IEnumerable<UpgradeDefinition> PowertechRunPowers()
    {
        foreach(var (id,title,damage,radius,recovery) in new (string,string,float,float,float)[] {
            ("flamesweep","Scorching Sweep",.6f,.12f,0),
            ("incendiarymissile","Searing Payload",.8f,0,.08f),
            ("flamethrower","Inferno Nozzle",.7f,.1f,.08f),
            ("flameburst","Burst Igniter",.8f,0,.1f),
            ("immolate","Immolation Charge",.9f,0,.1f),
            ("rocketpunch","Piston Gauntlet",.9f,0,.1f),
            ("retractableblade","Serrated Edge",.8f,0,.1f),
            ("railshot","Magnetic Rails",.9f,0,.1f),
            ("explosivedart","Shaped Charge",.8f,.1f,.08f),
            ("electrodart","Overcharged Capacitor",.7f,.1f,.06f),
            ("thermaldetonator","Overpressure Charge",.9f,.1f,.08f),
            ("jetcharge","Afterburner Impact",.7f,.1f,.08f)})
            if(BountyPower("powertech",id,title,damage,radius,recovery) is {} power)yield return power;
        foreach(var (id,key,title,radius,text) in new (string,string,string,float,string)[] {
            ("flamesweep","pt_spread_rank","Fanning Embers",.08f,"Flame Sweep spreads your burns to 2 more enemies and gains +{0} radius per rank."),
            ("rocketpunch","pt_cleave_rank","Shockwave Knuckles",.15f,"Rocket Punch cleaves around its target: +10% splash damage (up to 100%) and +{0} cleave radius per rank."),
            ("explosivedart","","Blast Radius",.18f,"Explosive Dart: +{0} blast radius per rank."),
            ("thermaldetonator","","Wide Detonation",.18f,"Thermal Detonator: +{0} blast radius per rank."),
            ("flamethrower","","Wide-Angle Nozzle",.15f,"Flame Thrower: +{0} reach per rank."),
            ("carbonize","","Carbonite Overspray",.14f,"Carbonize: +{0} radius per rank."),
            ("electrodart","pt_electro_rank","Arc Capacitor",.1f,"Electro Dart stuns one more enemy and gains +{0} radius per rank."),
            ("oilslick","pt_oil_fire_rank","Volatile Fuel",.15f,"Oil Slick: +{0} radius, and ignited slicks deal 35% more damage per rank.")})
            if(_world.Abilities.Find(id) is {} a)
                yield return new UpgradeDefinition {Id="powertech_reach_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,
                    RadiusPerRank=radius,Mechanic=key,MechanicalDescription=string.Format(text,radius.ToString("P0"))+" Bonuses add from base; no rank cap."};
        yield return BountyThrusters("powertech");
    }
    private UpgradeDefinition? BountyPower(string classId,string id,string title,float damage,float radius,float recovery)
    {
        var a=_world.Abilities.Find(id);if(a==null)return null;
        string description=$"{a.Definition.Title}: +{damage:P0} base damage per rank"+(a.Definition.PeriodicDamage>0?" (including its periodic damage)":"");
        if(radius>0)description+=$", +{radius:P0} base radius";
        if(recovery>0)description+=$", +{recovery:P0} cooldown recovery";
        return new UpgradeDefinition {Id=classId+"_power_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,
            DamagePerRank=damage,RadiusPerRank=radius,CooldownPerRank=recovery,
            MechanicalDescription=description+". Bonuses add from base and scale with the card's rolled rarity; no rank cap."};
    }
    // Movement stays a small, capped utility pick: 3% per rank, three ranks, never rolled up by rarity.
    private UpgradeDefinition BountyThrusters(string classId)=>new() {Id=classId+"_thrusters",Title="Jet Thrusters",Icon="jetboost",
        Effect=UpgradeEffect.Mechanic,Mechanic="bh_thrusters",MaximumRank=3,
        MechanicalDescription="Move 3% faster per rank, up to 3 ranks. Total Bounty Hunter movement bonus is capped at 9%."};

    // Talent hooks double as one-time run picks; card-only mechanics extend the same three trees.
    public IEnumerable<UpgradeDefinition> PowertechDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("powertech").Where(t=>t.Effects.Any(e=>e.Kind=="hook" && e.Key.StartsWith("pt_",StringComparison.Ordinal) && e.Key is not ("pt_jet_speed" or "pt_dart_conversion")))) {
            var e=t.Effects.First(x=>x.Kind=="hook");
            yield return new UpgradeDefinition {Id="powertech_"+e.Key[3..],Title=t.Name,Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),
                Spec=t.Tree,Effect=UpgradeEffect.Mechanic,Mechanic=e.Key,MaximumRank=1,MechanicalDescription=t.Description};
        }
        foreach(var (key,tree,title,icon,text) in new (string,string,string,string,string)[] {
            ("pt_carbon_shatter","shield_tech","Carbon Shatter","carbonitegrenade","Rocket Punch and Rail Shot deal 50% more damage to enemies held by Carbonize or Electro Dart."),
            ("pt_fuel_leak","pyrotech","Fuel Leak","oilslick","Once per Flame Thrower, the first enemy it hits gets an Oil Slick under its feet. Your next fire hit there ignites it."),
            ("pt_thermal_detonation","pyrotech","Thermal Detonation","burned","Enemies that die while burning from you explode for 30% of Immolate, Flame Burst or Incendiary Missile damage and set nearby enemies alight.")})
            yield return new UpgradeDefinition {Id="powertech_"+key[3..],Title=title,Icon=icon,Spec=tree,Effect=UpgradeEffect.Mechanic,Mechanic=key,MaximumRank=1,MechanicalDescription=text};
    }
    public int PowertechTalentRank(string key)=>IsPowertech && Hook(key)>0?1:0;
    public bool PowertechUpgradeUsable(UpgradeDefinition u)
    {
        if(u.Mechanic.StartsWith("pt_",StringComparison.Ordinal) && u.Effect==UpgradeEffect.Mechanic && PowertechTalentRank(u.Mechanic)>0)return false;
        bool Has(string id)=>PtHas(id);
        bool Fire()=>PtFire.Any(Has);
        return u.Mechanic switch {
            "pt_puncture"=>Has("railshot") && Has("retractableblade"),
            "pt_electro_surge" or "pt_advanced_tools"=>Has("electrodart"),
            "pt_iron_fist"=>Has("rocketpunch"),
            "pt_blade_focus"=>Has("explosivedart") && Has("retractableblade"),
            "pt_td_systems"=>Has("thermaldetonator") && Has("retractableblade"),
            "pt_flame_barrage"=>Has("rocketpunch") && Has("flamethrower"),
            "pt_fanned"=>Has("incendiarymissile") || Has("immolate"),
            "pt_superheated_rail"=>Has("railshot") && (Has("incendiarymissile") || Has("immolate")),
            "pt_rain_of_fire"=>Has("oilslick") && Fire(),
            "pt_accelerator"=>Has("flameburst") && (Has("railshot") || Has("immolate")),
            "pt_infrared"=>Has("immolate") || Has("flameburst"),
            "pt_incendiary_dart"=>Has("explosivedart") && Has("incendiarymissile"),
            "pt_supercooled"=>Has("carbonize"),
            "pt_flame_shield"=>Has("ionshield") && Has("incendiarymissile"),
            "pt_jet_refresh"=>Has("jetcharge") && (Has("retractableblade") || Has("incendiarymissile") || Has("immolate")),
            "pt_heat_screen"=>Has("ionshield") || Has("reactivearmor"),
            "pt_oil_duration"=>Has("oilslick"),
            "pt_heat_blast"=>Has("ionshield"),
            "pt_carbon_shatter"=>(Has("rocketpunch") || Has("railshot")) && (Has("carbonize") || Has("electrodart")),
            "pt_fuel_leak"=>Has("flamethrower") && Has("oilslick"),
            "pt_thermal_detonation"=>Has("immolate") || Has("flameburst") || Has("incendiarymissile"),
            "pt_cleave_rank"=>Has("rocketpunch"),
            "pt_electro_rank"=>Has("electrodart"),
            _=>true
        };
    }
    private static readonly Dictionary<string,string> PowertechAbilityTree=new() {
        ["retractableblade"]="advanced_prototype",["railshot"]="advanced_prototype",["rocketpunch"]="advanced_prototype",["flamethrower"]="advanced_prototype",
        ["explosivedart"]="advanced_prototype",["thermaldetonator"]="advanced_prototype",["electrodart"]="advanced_prototype",["incendiarymissile"]="pyrotech",["flamesweep"]="pyrotech",
        ["immolate"]="pyrotech",["flameburst"]="pyrotech",["oilslick"]="pyrotech",["ionshield"]="shield_tech",["reactivearmor"]="shield_tech",
        ["carbonize"]="shield_tech",["jetcharge"]="shield_tech"};
    private float BountyOfferWeight(UpgradeDefinition u)
    {
        var trees=IsPowertech?PowertechAbilityTree:MercenaryAbilityTree;
        string tree=u.Spec!=""?u.Spec:trees.GetValueOrDefault(u.AbilityId,"");
        if(tree=="")return 1;
        int points=BhPoints(tree);
        return u.Effect==UpgradeEffect.Mechanic?1+.4f*points:1+Math.Min(1.2f,.12f*points);
    }
}
