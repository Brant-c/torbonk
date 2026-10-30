using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    public bool MarauderCarnagePreferred=>MrPoints("carnage")>Math.Max(MrPoints("annihilation"),MrPoints("marauder_rage")) || MrPoints("carnage")+MrPoints("annihilation")+MrPoints("marauder_rage")==0 && _world.Abilities.Find("massacre")?.Unlocked==true;
    // The tree and drops share names, rules and tuning. Existing IDs remain save-compatible.
    public IEnumerable<UpgradeDefinition> MarauderCarnageDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("marauder").Where(t=>t.Tree=="carnage")) {
            var e=t.Effects.Single(x=>x.Kind=="hook");
            string suffix=t.Id.Split('.')[1];
            yield return new UpgradeDefinition {
                Id=suffix switch {"cleave"=>"marauder_cleave","flurry"=>"marauder_flurry",_=>"marauder_carnage_"+suffix},
                Title=t.Name,Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),Spec="carnage",
                Effect=UpgradeEffect.Mechanic,Mechanic=e.Key,MaximumRank=1,MechanicalDescription=t.Description
            };
        }
    }
    public bool MarauderUpgradeUsable(string key)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Saber()=>_world.Abilities.Abilities.Any(a=>a.Unlocked && !a.Definition.IsBasicAttack && a.Definition.Id!="viciousslash" && a.Definition.PresentationKind=="saber" && a.Definition.Effect==AbilityEffect.Damage);
        bool Cleave()=>Has("massacre") || Has("ravage");
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
            _=>true
        };
    }
    private string MarauderUpgradeTree(UpgradeDefinition u)=>TalentCatalog.Current.ForClass("marauder")
        .FirstOrDefault(t=>t.Effects.Any(e=>e.Key!="" && e.Key==u.Mechanic))?.Tree ?? (u.Mechanic=="mr_blademaster"?"carnage":u.AbilityId switch {
            "batteringassault" or "massacre" or "gore" or "ravage" or "ataruform" or "berserk" or "predation" or "frenzy"=>"carnage",
            "forcecrush" or "smash" or "forcescream" or "saberthrow" or "intimidatingroar"=>"marauder_rage",
            "rupture" or "deadlysaber" or "annihilate" or "sweepingslash"=>"annihilation",_=>""});
}
