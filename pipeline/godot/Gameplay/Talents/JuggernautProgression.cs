using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    public IEnumerable<UpgradeDefinition> JuggernautDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("juggernaut")) {
            var e=t.Effects.Single();
            bool area=e.Key is "jg_grip" or "jg_sweep" or "jg_resonance";
            yield return new UpgradeDefinition {Id=e.Key=="jg_shockwave"?"juggernaut_talent_shockwave":"juggernaut_"+e.Key[3..],Title=t.Name,
                Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),Spec=t.Tree,
                Effect=UpgradeEffect.Mechanic,Mechanic=e.Key,MaximumRank=area?3:1,
                MechanicalDescription=area?e.Key switch {
                    "jg_grip"=>"Force Choke gains one nearby target per rank, up to four, at full damage.",
                    "jg_sweep"=>"Ravage gains one frontal target per rank, up to four; secondary hits deal 60% damage.",
                    _=>"Force Scream gains two nearby targets per rank, up to seven; secondary hits deal 65% damage."
                }:t.Description};
        }
    }
    public bool JuggernautUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Saber()=>_world.Abilities.Abilities.Any(a=>a.Unlocked && JgSaber(a));
        var talent=TalentCatalog.Current.ForClass("juggernaut").FirstOrDefault(t=>t.Effects.Any(e=>e.Key==u.Mechanic));
        if(talent?.Tier==2 && (_world.Progression?.Level??1)<8)return false;
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
            _=>true};
    }
    private float JuggernautOfferWeight(UpgradeDefinition u)
    {
        string tree=u.Spec!=""?u.Spec:TalentCatalog.Current.ForClass("juggernaut").FirstOrDefault(t=>t.Effects.Any(e=>e.Key==u.Mechanic))?.Tree??"";
        return 1+.25f*TalentCatalog.Current.ForClass("juggernaut").Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
    }
}
