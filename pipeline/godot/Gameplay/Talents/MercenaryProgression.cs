using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    public IEnumerable<UpgradeDefinition> MercenaryRunPowers()
    {
        foreach(var (id,title,damage,radius,recovery) in new (string,string,float,float,float)[] {
            ("tracer","Tracer Payload",.6f,.1f,0),
            ("heatseeker","Seeker Warheads",.9f,.1f,.08f),
            ("unload","Hair Trigger",.8f,0,.1f),
            ("missileblast","Concussive Payload",.8f,.1f,.08f),
            ("railshot","Magnetic Rails",.9f,0,.1f),
            ("deathfromabove","Orbital Barrage",.7f,.1f,.06f),
            ("sweepingblasters","Twin Barrels",.7f,.1f,.08f),
            ("explosivedart","Shaped Charge",.8f,.1f,.08f),
            ("incendiarymissile","Searing Payload",.8f,0,.08f),
            ("fusionmissile","Fusion Core",.8f,.1f,.08f),
            ("powershot","Overcharged Cylinder",.9f,0,.1f)})
            if(BountyPower("mercenary",id,title,damage,radius,recovery) is {} power)yield return power;
        foreach(var (id,key,title,radius,text) in new (string,string,string,float,string)[] {
            ("tracer","mc_heat_rank","Heat Signature Lock",.15f,"Tracer Missile: +{0} splash radius per rank. Heat Signature holds one more stack and each stack adds 5% more Heatseeker damage per rank."),
            ("deathfromabove","","Saturation Bombing",.18f,"Death From Above: +{0} barrage radius per rank."),
            ("explosivedart","","Blast Radius",.18f,"Explosive Dart: +{0} blast radius per rank."),
            ("fusionmissile","","Wide Yield",.18f,"Fusion Missile: +{0} blast radius per rank."),
            ("sweepingblasters","","Broad Sweep",.15f,"Sweeping Blasters: +{0} reach per rank."),
            ("heatseeker","","Cluster Seekers",.15f,"Heatseeker Missiles: +{0} splash radius per rank."),
            ("missileblast","","Wide Warhead",.18f,"Missile Blast: +{0} blast radius per rank.")})
            if(_world.Abilities.Find(id) is {} a)
                yield return new UpgradeDefinition {Id="mercenary_reach_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,
                    RadiusPerRank=radius,Mechanic=key,MechanicalDescription=string.Format(text,radius.ToString("P0"))+" Bonuses add from base; no rank cap."};
        yield return BountyThrusters("mercenary");
    }
    public IEnumerable<UpgradeDefinition> MercenaryDrops()
    {
        foreach(var t in TalentCatalog.Current.ForClass("mercenary").Where(t=>t.Effects.Any(e=>e.Kind=="hook" && e.Key.StartsWith("mc_",StringComparison.Ordinal) && e.Key!="mc_tracer_conversion"))) {
            var e=t.Effects.First(x=>x.Kind=="hook");
            yield return new UpgradeDefinition {Id="mercenary_"+e.Key[3..],Title=t.Name,Icon=System.IO.Path.GetFileNameWithoutExtension(t.Icon),
                Spec=t.Tree,Effect=UpgradeEffect.Mechanic,Mechanic=e.Key,MaximumRank=1,MechanicalDescription=t.Description};
        }
        foreach(var (key,tree,title,icon,text) in new (string,string,string,string,string)[] {
            ("mc_tracer_splash","arsenal","Saturating Tracers","missilefan","Tracer Missile also gives 1 Heat Signature stack to every enemy caught in its splash."),
            ("mc_dfa_burn","innovative_ordnance","Incendiary Warheads","deathfromabove","Death From Above sets every enemy it hits alight with your Incendiary Missile burn."),
            ("mc_sweep_spread","innovative_ordnance","Scatter Fire","sweepinggunfire","Sweeping Blasters copies your burns and bleeds between every enemy each volley hits.")})
            yield return new UpgradeDefinition {Id="mercenary_"+key[3..],Title=title,Icon=icon,Spec=tree,Effect=UpgradeEffect.Mechanic,Mechanic=key,MaximumRank=1,MechanicalDescription=text};
    }
    public int MercenaryTalentRank(string key)=>IsMercenary && Hook(key)>0?1:0;
    public bool MercenaryUpgradeUsable(UpgradeDefinition u)
    {
        if(u.Mechanic.StartsWith("mc_",StringComparison.Ordinal) && u.Effect==UpgradeEffect.Mechanic && MercenaryTalentRank(u.Mechanic)>0)return false;
        bool Has(string id)=>PtHas(id);
        bool Burn()=>Has("incendiarymissile");
        return u.Mechanic switch {
            "mc_heat_max" or "mc_heat_rank"=>Has("heatseeker") || Has("deathfromabove"),
            "mc_upgraded"=>(Has("tracer") || Has("powershot")) && (Has("heatseeker") || Has("deathfromabove")),
            "mc_tracer_splash"=>Has("tracer") && (Has("heatseeker") || Has("deathfromabove")),
            "mc_barrage"=>Has("unload") && (Has("tracer") || Has("powershot")),
            "mc_light_em_up"=>Has("heatseeker"),
            "mc_terminal"=>Has("deathfromabove"),
            "mc_pinning"=>Has("unload") || Has("missileblast"),
            "mc_gas"=>(Has("powershot") || Has("railshot")) && Burn(),
            "mc_superheated_rail"=>Has("railshot") && (Burn() || Has("fusionmissile")),
            "mc_accelerator"=>Has("powershot") && Has("railshot"),
            "mc_tso_fusion"=>Has("thermalsensoroverride") && Has("fusionmissile"),
            "mc_dot_bonus"=>(Has("powershot") || Has("railshot")) && (Burn() || Has("fusionmissile")),
            "mc_shell_charges"=>Has("koltoshell"),
            "mc_field_damage"=>Has("koltomissile"),
            "mc_warden"=>Has("concussionmissile"),
            "mc_supercharge"=>_world.Abilities.Abilities.Any(a=>a.Unlocked && a.Definition.Category==AbilityCategory.Utility),
            "mc_reactive"=>Has("koltoshell") || Has("ionshield"),
            "mc_jet_burn"=>Has("jetboost"),
            "mc_dfa_burn"=>Has("deathfromabove") && Burn(),
            "mc_sweep_spread"=>Has("sweepingblasters") && (Burn() || Has("fusionmissile")),
            _=>true
        };
    }
    private static readonly Dictionary<string,string> MercenaryAbilityTree=new() {
        ["tracer"]="arsenal",["heatseeker"]="arsenal",["missileblast"]="arsenal",["unload"]="arsenal",["deathfromabove"]="arsenal",
        ["incendiarymissile"]="innovative_ordnance",["fusionmissile"]="innovative_ordnance",["explosivedart"]="innovative_ordnance",["sweepingblasters"]="innovative_ordnance",
        ["railshot"]="innovative_ordnance",["powershot"]="innovative_ordnance",
        ["thermalsensoroverride"]="innovative_ordnance",["koltomissile"]="bodyguard",["koltoshell"]="bodyguard",["ionshield"]="bodyguard",
        ["concussionmissile"]="bodyguard",["jetboost"]="bodyguard"};
}
