using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    // Shared by Juggernaut and Marauder cards: run-only mechanics read their rarity-weighted rank power straight from the upgrade list.
    private bool Owns(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
    private float CardPower(string mechanic)=>_world.Progression==null?0:_world.Progression.Upgrades.Where(u=>u.Mechanic==mechanic).Sum(u=>_world.Progression.RankPower(u));
    private int CardRanks(string mechanic)=>_world.Progression==null?0:_world.Progression.Upgrades.Where(u=>u.Mechanic==mechanic).Sum(u=>_world.Progression.Rank(u));
    // Single-axis radius cards are the only place a cone/splash floor needs to know the boost, because those floors replace the card-scaled radius.
    private float RadiusCardBoost(string prefix,string id)=>_world.Progression==null?0:_world.Progression.Upgrades.Where(u=>u.Id==prefix+"radius_"+id).Sum(u=>_world.Progression.RankPower(u)*u.RadiusPerRank);

    // Echo (+chance per rank that a tick strikes again) and Legion (+1 simultaneous instance per rank) for Juggernaut and Marauder DoTs.
    public float WarriorDuplicateChance(AutomaticAbility a)=>IsJuggernaut?CardPower("jgm_dup_"+a.Definition.Id)*.2f
        :IsMarauder && a.Definition.Id is "rupture" or "deadlysaber" or "forcecrush"?CardPower("mrm_dup")*.2f:0;
    public int WarriorDotSlots(AutomaticAbility a)=>IsJuggernaut?1+CardRanks("jgm_multi_"+a.Definition.Id)
        :IsMarauder && a.Definition.Id is "rupture" or "deadlysaber" or "forcecrush"?1+CardRanks("mrm_multi"):1;

    // Smash echoes (Juggernaut Aftershock, Marauder Aftershock): extra bursts around the player at reduced damage and radius.
    private readonly List<(double delay,float scale)> _smashEchoes=new();
    private void QueueSmashEchoes(int count){for(int i=1;i<=count;i++)_smashEchoes.Add((1.2*i,.5f));}
    private void AdvanceSmashEchoes(double d)
    {
        for(int i=_smashEchoes.Count-1;i>=0;i--)
        {
            var echo=_smashEchoes[i];echo.delay-=d;
            if(echo.delay>0){_smashEchoes[i]=echo;continue;}
            _smashEchoes.RemoveAt(i);
            if(_world.Abilities.Find("smash") is not {} smash || !_world.PlayerActor.Alive)continue;
            float radius=smash.Radius*.7f;
            foreach(var enemy in _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,radius,radius).ToArray())
                if(enemy.Health.ApplyDamage(smash.DamagePerTick*echo.scale*_world.Build.DamageMultiplier(smash,enemy),smash.Definition.DamageType,false,"smash")>0 && smash.Presentation is D3AbilityPresentation visual)visual.ProcImpact(enemy);
        }
    }

    public IEnumerable<UpgradeDefinition> JuggernautDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("juggernaut")) {
            var e=t.Effects.Single();
            bool area=e.Key is "jg_grip" or "jg_sweep" or "jg_resonance";
            // Target-count talents are uncapped repeatable cards: every rank adds the same number of targets.
            yield return new UpgradeDefinition {Id=e.Key=="jg_shockwave"?"juggernaut_talent_shockwave":"juggernaut_"+e.Key[3..],Title=t.Name,
                Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),Spec=t.Tree,
                Effect=UpgradeEffect.Mechanic,Mechanic=e.Key,MaximumRank=area?0:1,
                MechanicalDescription=area?e.Key switch {
                    "jg_grip"=>"Force Choke gains one nearby target per rank at full damage. No rank cap.",
                    "jg_sweep"=>"Ravage gains one frontal target per rank; secondary hits deal 60% damage. No rank cap.",
                    _=>"Force Scream gains two nearby targets per rank; secondary hits deal 65% damage. No rank cap."
                }:t.Description};
        }
    }
    public bool JuggernautUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Saber()=>_world.Abilities.Abilities.Any(a=>a.Unlocked && JgSaber(a));
        bool Bleed(string id)=>id switch {"forcescream"=>Jg("jg_scream_bleed")>0,"impale"=>Jg("jg_impale_bleed")>0,"smash"=>JuggernautAreaRank("jg_fissure")>0,_=>true};
        var talent=TalentCatalog.Current.ForClass("juggernaut").FirstOrDefault(t=>t.Effects.Any(e=>e.Key==u.Mechanic));
        if(talent?.Tier==2 && (_world.Progression?.Level??1)<8)return false;
        if(u.Id.StartsWith("juggernaut_tick_",StringComparison.Ordinal))return Bleed(u.AbilityId);
        if(u.Id=="juggernaut_radius_crushingblow")return Jg("jg_shatter")>0;
        if(u.Id=="juggernaut_radius_ravage")return JuggernautAreaRank("jg_sweep")>0;
        return u.Mechanic switch {
            "jg_grip"=>Has("forcechoke"),"jg_sweep"=>Has("ravage"),"jg_resonance"=>Has("forcescream"),
            "jg_barricade" or "jg_revenge"=>Has("retaliation") || u.Mechanic=="jg_revenge" && Has("saberward"),
            "jg_sonic"=>Has("forcescream"),"jg_invincible"=>Has("saberward"),
            "jg_unleashed"=>Has("saberward") && (Has("forcechoke") || Has("intimidatingroar")),
            "jg_undying"=>Has("enrageddefense") && Has("endurepain"),
            "jg_hatred" or "jg_bloodmaster" or "jg_unstoppable"=>Saber(),
            "jg_scream_bleed"=>Has("forcescream"),"jg_impale_bleed"=>Has("impale"),
            "jg_savagery"=>Has("impale") && Has("forcescream"),"jg_rampage"=>Has("impale") && Has("ravage"),
            "jg_shatter"=>Has("crushingblow") && (Has("impale") || Has("forcecrush") || Has("forcescream")),
            "jg_decimate" or "jg_fissure"=>Has("smash"),"jg_dominate"=>Has("forcescream") && Has("smash"),
            "jg_shockwave" or "jg_crushingwave"=>Has("forcecrush") && Has("smash"),
            "jg_alacrity" or "jg_obliteration"=>_world.Abilities.Abilities.Any(a=>a.Unlocked && a.Definition.Effect==AbilityEffect.Damage && a.Definition.PresentationKind!="saber"),
            "jgm_fury"=>Has("smash") && _world.Abilities.Abilities.Any(a=>a.Unlocked && a.Definition.Effect==AbilityEffect.Damage && a.Definition.Id!="smash" && !a.Definition.IsBasicAttack),
            "jgm_aftershock"=>Has("smash"),
            "jgm_reprisal"=>Has("retaliation") || Has("saberward"),
            "jgm_rend"=>Has("forcescream") && (Has("impale") || Has("ravage") || Has("crushingblow")),
            "jgm_finisher"=>Has("crushingblow"),
            "jgm_dread"=>(Has("intimidatingroar") || Has("forcechoke")) && (Has("smash") || Has("forcescream")),
            "jgm_cadence"=>Has("impale") && Has("ravage"),
            "jgm_quake"=>Has("smash") && Has("forcecrush"),
            "jgm_darkblood"=>Jg("jg_darkblood")>0,
            "jgm_rageheal"=>Jg("jg_rage_heal")>0,
            "jgm_soresu"=>true,
            _=>u.Mechanic.StartsWith("jgm_dup_",StringComparison.Ordinal) || u.Mechanic.StartsWith("jgm_multi_",StringComparison.Ordinal)
                ?Has(u.AbilityId) && Bleed(u.AbilityId):true};
    }
    private float JuggernautOfferWeight(UpgradeDefinition u)
    {
        string tree=u.Spec!=""?u.Spec:TalentCatalog.Current.ForClass("juggernaut").FirstOrDefault(t=>t.Effects.Any(e=>e.Key==u.Mechanic))?.Tree??"";
        if(tree=="" && u.Id.StartsWith("juggernaut_",StringComparison.Ordinal)) tree=JuggernautCardTree(u);
        return 1+.25f*TalentCatalog.Current.ForClass("juggernaut").Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
    }
    public string JuggernautCardTreeOf(UpgradeDefinition u)=>u.Spec!=""?u.Spec:JuggernautCardTree(u);
    // Run cards lean toward the spec whose talents already point at their ability.
    private static string JuggernautCardTree(UpgradeDefinition u)=>u.Mechanic switch {
        "jgm_fury" or "jgm_aftershock" or "jgm_quake" or "jgm_rageheal"=>"juggernaut_rage",
        "jgm_reprisal" or "jgm_dread" or "jgm_soresu"=>"immortal",
        "jgm_rend" or "jgm_finisher" or "jgm_cadence" or "jgm_darkblood"=>"vengeance",
        _=>u.AbilityId switch {
            "smash" or "forcecrush"=>"juggernaut_rage","forcescream"=>"juggernaut_rage",
            "impale" or "ravage" or "crushingblow" or "sunderingassault"=>"vengeance",
            "saberward" or "enrageddefense" or "forcechoke" or "intimidatingroar"=>"immortal",_=>""}};

    // Single-axis scoped cards. Nothing here is rank-capped; rarity multiplies the one number each card moves.
    private static readonly (string id,string title,float amount)[] JuggernautDamageCards={
        ("smash","Seismic Slam",.95f),("forcescream","Piercing Scream",.95f),("ravage","Savage Onslaught",1f),("impale","Rending Thrust",1f),
        ("crushingblow","Shattering Blow",1f),("sunderingassault","Sundering Fury",1f),("forcechoke","Crushing Grasp",.6f),
        ("forcecrush","Gravity Well",.9f),("intimidatingroar","Thundering Roar",.6f)};
    private static readonly (string id,string title,float amount)[] JuggernautRadiusCards={
        ("smash","Wider Slam",.12f),("forcescream","Wider Scream",.12f),("crushingblow","Wider Shatter",.12f),("intimidatingroar","Wider Roar",.12f),("ravage","Wider Sweep",.12f)};
    // Force Crush and Force Choke deliberately have no recovery card.
    private static readonly (string id,string title,float amount)[] JuggernautCooldownCards={
        ("smash","Quickened Slam",.18f),("forcescream","Quickened Scream",.18f),("ravage","Quickened Onslaught",.18f),("impale","Quickened Thrust",.18f),
        ("crushingblow","Quickened Blow",.18f),("sunderingassault","Quickened Fury",.18f),("intimidatingroar","Quickened Roar",.18f),
        ("saberward","Quickened Ward",.08f),("enrageddefense","Quickened Defense",.08f)};
    private static readonly (string id,string title)[] JuggernautTickCards={
        ("forcecrush","Crushing Pulse"),("forcescream","Screaming Pulse"),("impale","Rending Pulse"),("smash","Quaking Pulse")};

    public IEnumerable<UpgradeDefinition> JuggernautRunPowers()
    {
        foreach(var (id,title,amount) in JuggernautDamageCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="juggernaut_power_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,DamagePerRank=amount};
        }
        foreach(var (id,title,amount) in JuggernautRadiusCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="juggernaut_radius_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,RadiusPerRank=amount};
        }
        foreach(var (id,title,amount) in JuggernautCooldownCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="juggernaut_cooldown_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,CooldownPerRank=amount};
        }
        foreach(var (id,title) in JuggernautTickCards) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="juggernaut_tick_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.TickRate,AmountPerRank=.15f};
        }
        if(_world.Abilities.Find("forcecrush") is {} crush) {
            yield return new UpgradeDefinition {Id="juggernaut_echo_forcecrush",Title="Crush Echo",Icon=crush.Definition.Icon,AbilityId="forcecrush",Effect=UpgradeEffect.Mechanic,Mechanic="jgm_dup_forcecrush",
                AmountPerRank=.2f,MechanicalDescription="Force Crush: each tick has a +20% chance per rank to strike again (100% guarantees an extra tick). Scales with rarity."};
            yield return new UpgradeDefinition {Id="juggernaut_legion_forcecrush",Title="Crush Legion",Icon=crush.Definition.Icon,AbilityId="forcecrush",Effect=UpgradeEffect.Mechanic,Mechanic="jgm_multi_forcecrush",AmountPerRank=0,
                MechanicalDescription="Force Crush: +1 simultaneous instance per rank on each enemy. Extra casts add a new instance instead of refreshing, until slots are full."};
        }
        foreach(var (id,bleed) in new[]{("forcescream","Scream"),("impale","Impale")}) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="juggernaut_echo_"+id,Title=bleed+" Echo",Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.Mechanic,Mechanic="jgm_dup_"+id,
                AmountPerRank=.2f,MechanicalDescription=$"{a.Definition.Title} bleed: each tick has a +20% chance per rank to strike again. Requires the bleed talent. Scales with rarity."};
            yield return new UpgradeDefinition {Id="juggernaut_legion_"+id,Title=bleed+" Legion",Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.Mechanic,Mechanic="jgm_multi_"+id,AmountPerRank=0,
                MechanicalDescription=$"{a.Definition.Title} bleed: +1 simultaneous instance per rank on each enemy. Requires the bleed talent."};
        }
        // Defence and healing: small per-rank numbers, always a separate card.
        yield return Mech("soresu","Soresu Discipline","saberward","jgm_soresu",.01f,"Damage taken is divided by 1 plus 1% per rank, so mitigation can never reach 100%.","");
        yield return Mech("darkblood_surge","Dark Blood Surge","sunderingassault","jgm_darkblood",.15f,"Dark Blood's healing is 15% larger per rank.","");
        yield return Mech("rageheal","Undying Surge","smash","jgm_rageheal",.15f,"Undying Rage's healing is 15% larger per rank.","");
        yield return Mech("fury","Fury Bank","smash","jgm_fury",.04f,"Every damaging hit from another ability banks 1 Fury for 6 seconds. Smash spends all Fury for +4% damage per Fury, per rank.","smash");
        yield return Mech("aftershock","Aftershock","smash","jgm_aftershock",0,"Smash repeats once more per rank, 1.2 seconds apart, each echo at 50% damage and 70% radius. Smash cooldown never drops below 3 seconds.","smash");
        yield return Mech("reprisal","Reprisal","retaliation","jgm_reprisal",.25f,"Damage taken in the last 4 seconds is stored. Retaliation and Saber Ward release 25% of it per rank as a shockwave around you. At most once per second.","");
        yield return Mech("rend","Rend Mark","forcescream","jgm_rend",.15f,"Force Scream marks every enemy it hits for 6 seconds. Impale, Ravage and Crushing Blow deal 15% more per rank to marked enemies; Shatter spreads the mark.","forcescream");
        yield return Mech("finisher","Finishing Blow","crushingblow","jgm_finisher",.3f,"Crushing Blow deals 30% more per rank to enemies under 30% health. Defeating an enemy with it recovers 2 seconds of its cooldown.","crushingblow");
        yield return Mech("dread","Dread Presence","intimidatingroar","jgm_dread",.1f,"Enemies struck by Intimidating Roar or Force Choke take 10% more per rank from Smash and Force Scream for 5 seconds.","");
        yield return Mech("cadence","Blood Cadence","impale","jgm_cadence",.3f,"Impale kills and Impale bleed ticks shave 0.3 seconds per rank from Ravage's cooldown. At most once every 0.5 seconds.","impale");
        yield return Mech("quake","Rolling Quake","smash","jgm_quake",1.5f,"A Smash that hits three or more enemies recovers 1.5 seconds per rank of Force Crush cooldown and refreshes Crush on a struck enemy.","smash");
        UpgradeDefinition Mech(string id,string title,string iconAbility,string mechanic,float amount,string text,string owned)
        {
            var a=_world.Abilities.Find(iconAbility);
            return new UpgradeDefinition {Id="juggernaut_"+id,Title=title,Icon=a?.Definition.Icon??"juggernautsmash",AbilityId=owned,
                Effect=UpgradeEffect.Mechanic,Mechanic=mechanic,AmountPerRank=amount,MaximumRank=0,MechanicalDescription=amount>0?text+" Scales with rarity.":text};
        }
    }
}
