using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

/// <summary>Powertech talent and run-card hooks. Survivors adaptation of the April Advanced Prototype, Pyrotech and Shield Tech trees.</summary>
public sealed partial class TalentRuntime
{
    private bool IsPowertech=>_world.RunConfiguration.AdvancedClassId=="powertech";
    private readonly Dictionary<ulong,(Combatant target,AutomaticAbility source,double remaining,double next,float damage)> _electro=new();
    private readonly Dictionary<ulong,double> _carbonized=new();
    private static readonly string[] PtFire={"flamesweep","flamethrower","flameburst","immolate","incendiarymissile"};
    private static readonly string[] PtBurns={"incendiarymissile","immolate"};
    // Talent hooks and their one-time run cards share keys; a card supplies the talent's catalog amount.
    private float Pt(string key)=>!IsPowertech?0:Hook(key)>0?Hook(key):BountyCardOwned(key)?BountyCatalogAmount("powertech",key):0;
    private float BountyCatalogAmount(string classId,string key)=>TalentCatalog.Current.ForClass(classId)
        .SelectMany(t=>t.Effects).FirstOrDefault(e=>e.Kind=="hook" && e.Key==key)?.Amount??1;
    private bool BountyCardOwned(string key)=>_world.Progression?.Upgrades.Any(u=>u.Mechanic==key && _world.Progression.Rank(u)>0)??false;
    private int BountyCardRanks(string key)=>_world.Progression?.Upgrades.Where(u=>u.Mechanic==key).Sum(u=>_world.Progression.Rank(u))??0;
    private bool PtHas(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
    private AutomaticAbility? PtOwned(string id)=>_world.Abilities.Find(id) is {Unlocked:true} a?a:null;
    public bool PowertechBurning(Combatant t)=>_world.Periodic.On(t).Any(a=>a.Caster==_world.PlayerActor && a.Definition.Id is "incendiarymissile" or "immolate" or "fusionmissile")
        || t is EnemyAgent e && _world.GetChildren().OfType<PowertechOilSlick>().Any(s=>s.Burning && s.Contains(e));
    public bool PowertechBleeding(Combatant t)=>_world.Abilities.Find("retractableblade") is {} blade && _world.Periodic.Has(blade,t);
    public bool PowertechElectrified(Combatant t)=>_electro.ContainsKey(t.GetInstanceId());
    public bool PowertechCarbonized(Combatant t)=>_carbonized.GetValueOrDefault(t.GetInstanceId())>0;
    // Flame Sweep is the Pyrotech spreader for every Powertech; the old card and Fanned Flames widen it.
    public bool PowertechSweepSpreads=>IsPowertech;
    public int PowertechSpreadLimit=>(int)Pt("pt_fanned")+2*BountyCardRanks("pt_spread_rank")+(_world.Build.Talent("combustible_spread")>0?2:0);
    public bool PowertechFlameShield=>Pt("pt_flame_shield")>0;
    public float OilSlickSeconds=>8+Pt("pt_oil_duration");
    public float OilFireSeconds=>6+(Pt("pt_rain_of_fire")>0?3:0);
    public float OilFireDamage=>3*(1+Pt("pt_rain_of_fire")+.35f*BountyCardRanks("pt_oil_fire_rank"));
    private float PowertechIncoming=>!IsPowertech?1:
        _world.PlayerActor.Health.Shielded || _world.Build.Buffed?1-Math.Min(.2f,Pt("pt_heat_screen")):1;
    public string[] PowertechEarlyAbilities {
        get {
            int ap=BhPoints("advanced_prototype"),pyro=BhPoints("pyrotech");
            return ap>pyro?new[]{"retractableblade","explosivedart","thermaldetonator","railshot"}:
                pyro>0?new[]{"incendiarymissile","immolate","flameburst","oilslick"}:new[]{"incendiarymissile","retractableblade","railshot","immolate"};
        }
    }
    private void PowertechStats(AutomaticAbility a)
    {
        if(!IsPowertech)return;
        string id=a.Definition.Id;
        if(id=="rocketpunch") {
            int cleave=BountyCardRanks("pt_cleave_rank");bool fist=Pt("pt_iron_fist")>0;
            a.Definition.Area=fist || cleave>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            a.Definition.SplashMultiplier=Math.Min(1,.6f+.1f*cleave);
            if(fist || cleave>0)a.Radius=Math.Max(a.Radius,.2f*(1+.15f*cleave));
        }
        if(id=="explosivedart" && Pt("pt_blade_focus")>0)a.Radius*=1.3f;
        if(id=="electrodart")a.Definition.MaximumTargets=3+(Pt("pt_electro_surge")>0?1:0)+BountyCardRanks("pt_electro_rank");
    }
    private void PowertechActivated(AutomaticAbility a)
    {
        if(!IsPowertech)return;
        if(a.Definition.Id=="flamethrower")_fuelLeak=false;
    }
    private bool _fuelLeak;
    private void PowertechHit(AutomaticAbility a,Combatant target)
    {
        if(!IsPowertech || !GodotObject.IsInstanceValid(target))return;
        string id=a.Definition.Id;
        if(id=="electrodart" && target is EnemyAgent shocked && shocked.Alive && shocked.Stun(4+(Pt("pt_electro_surge")>0?1:0),false)) {
            _electro[target.GetInstanceId()]=(target,a,shocked.StunRemaining,.5,a.DamagePerTick*.6f);
            PowertechStatusVisual.Attach(target,"res://Assets/BountyHunter/512_singlearc_bw_02.png",1,()=>_electro.ContainsKey(target.GetInstanceId()));
        }
        if(id=="carbonize" && target is EnemyAgent frozen && frozen.StunRemaining>0)_carbonized[target.GetInstanceId()]=frozen.StunRemaining;
        if(id=="jetcharge" && target.Alive) {
            if(target==a.CastTarget && target is EnemyAgent slammed)slammed.Stun(1.5f,false);
            if(Pt("pt_jet_refresh")>0)_world.Periodic.RefreshAll(target);
        }
        if(id=="rocketpunch" && target.Alive) {
            if(Pt("pt_iron_fist")>0 && PtOwned("retractableblade") is {} blade)_world.Periodic.Apply(blade,target);
            if(Pt("pt_flame_barrage")>0 && Ready("pt_flame_barrage",.5))_world.Abilities.Find("flamethrower")?.ReduceCooldown(Pt("pt_flame_barrage"));
        }
        if(id=="railshot" && target.Alive && Pt("pt_superheated_rail")>0 && PowertechBurning(target) && Ready("pt_superheated_rail",.5))
            BountySpread(target,(int)Pt("pt_superheated_rail"),PtBurns);
        if(id=="flameburst" && Pt("pt_accelerator")>0 && PowertechBurning(target) && Ready("pt_accelerator",.5))
            foreach(string next in new[]{"railshot","immolate"})_world.Abilities.Find(next)?.ReduceCooldown(Pt("pt_accelerator"));
        if(id=="flamethrower" && Pt("pt_fuel_leak")>0 && !_fuelLeak && PtOwned("oilslick") is {} oil) {
            _fuelLeak=true;var slick=new PowertechOilSlick {Source=oil};_world.AddChild(slick);slick.GlobalPosition=target.GlobalPosition;
        }
        // Any fire hit on an enemy standing in your slick sets the whole slick alight.
        if(PtFire.Contains(id) && target is EnemyAgent soaked && soaked.OilRemaining>0)
            foreach(var slick in _world.GetChildren().OfType<PowertechOilSlick>().Where(s=>s.Contains(soaked)))slick.Ignite();
    }
    /// <summary>Copies the caster's named DoTs from one enemy to the nearest fresh neighbours.</summary>
    private void BountySpread(Combatant from,int limit,string[] ids)
    {
        var sources=_world.Periodic.On(from).Where(s=>s.Caster==_world.PlayerActor && s.Unlocked && ids.Contains(s.Definition.Id)).DistinctBy(s=>s.Definition.Id).ToArray();
        if(sources.Length==0)return;
        foreach(var near in _world.Targeting.InArea(from.GlobalPosition,.25f,.25f,true).Where(t=>t!=from && t.Alive)
            .OrderBy(t=>t.GlobalPosition.DistanceSquaredTo(from.GlobalPosition)).ThenBy(t=>t.SelectionKey,StringComparer.Ordinal).Take(limit).ToArray())
            foreach(var s in sources)if(!_world.Periodic.Has(s,near))_world.Periodic.Apply(s,near);
    }
    private float PowertechBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsPowertech)return 0;
        string id=a.Definition.Id;float bonus=0;
        bool burning=id is "immolate" or "flameburst" or "rocketpunch" or "railshot" && PowertechBurning(t);
        if(id=="immolate" && burning)bonus+=.5f+Pt("pt_infrared");
        if(id=="flameburst" && burning)bonus+=.4f+Pt("pt_infrared");
        if(id=="railshot" && PowertechBleeding(t))bonus+=Pt("pt_puncture");
        if(id=="flamethrower" && Pt("pt_flame_barrage")>0 && PowertechBleeding(t))bonus+=.25f;
        if(PowertechCarbonized(t))bonus+=Pt("pt_supercooled");
        if(PowertechElectrified(t))bonus+=Pt("pt_advanced_tools");
        if(id is "rocketpunch" or "railshot" && (PowertechCarbonized(t) || PowertechElectrified(t)) && Pt("pt_carbon_shatter")>0)bonus+=.5f;
        return bonus;
    }
    /// <summary>Flame Shield extends the original Pyro Shield retaliation with a burn.</summary>
    public void PowertechRetaliated(EnemyAgent attacker)
    {
        if(PowertechFlameShield && attacker.Alive && PtOwned("incendiarymissile") is {} burn)_world.Periodic.Apply(burn,attacker);
    }
    public void PowertechEnergyShieldEnded()
    {
        if(!IsPowertech || Pt("pt_heat_blast")<=0 || !_world.PlayerActor.Alive || PtOwned("ionshield") is not {} shield)return;
        foreach(var enemy in _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,.32f,.32f).ToArray()) {
            shield.ApplyPeriodicHit(enemy,Pt("pt_heat_blast"));
            if(!enemy.Alive)continue;
            if(PtOwned("incendiarymissile") is {} burn)_world.Periodic.Apply(burn,enemy);
            if(PtOwned("retractableblade") is {} blade)_world.Periodic.Apply(blade,enemy);
            PowertechStatusVisual.Flash(enemy,"4x4_flame_lick_02_bw",16);
        }
    }
    public void PowertechKilled(Combatant dead)
    {
        if(!IsPowertech || Pt("pt_thermal_detonation")<=0 || !PowertechBurning(dead) || !Ready("pt_thermal_detonation",.25))return;
        var source=PtOwned("immolate")??PtOwned("flameburst")??PtOwned("incendiarymissile");if(source==null)return;
        foreach(var near in _world.Targeting.InArea(dead.GlobalPosition,.2f,.2f,true).Where(t=>t!=dead && t.Alive).ToArray()) {
            source.ApplyPeriodicHit(near,source.DamagePerTick*.3f);
            if(near.Alive && PtOwned("incendiarymissile") is {} burn && !_world.Periodic.Has(burn,near))_world.Periodic.Apply(burn,near);
        }
    }
    private void AdvancePowertech(double delta)
    {
        if(!IsPowertech)return;
        foreach(var key in _carbonized.Keys.ToArray()){_carbonized[key]-=delta;if(_carbonized[key]<=0)_carbonized.Remove(key);}
        foreach(var key in _electro.Keys.ToArray()) {
            var e=_electro[key];e.remaining-=delta;e.next-=delta;
            var target=e.target;
            if(!GodotObject.IsInstanceValid(target) || !target.Alive || e.remaining<=0 || target is EnemyAgent stunned && stunned.StunRemaining<=0){_electro.Remove(key);continue;}
            while(e.next<=0 && target.Alive){e.next+=.5;e.source.ApplyPeriodicHit(target,e.damage);}
            _electro[key]=e;
        }
    }
    private void ResetPowertech(){_electro.Clear();_carbonized.Clear();_fuelLeak=false;}
}
