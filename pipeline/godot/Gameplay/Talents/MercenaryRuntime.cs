using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

/// <summary>Mercenary talent and run-card hooks. Survivors adaptation of the April Arsenal, Pyrotech (shown as Innovative Ordnance) and Bodyguard trees.</summary>
public sealed partial class TalentRuntime
{
    private bool IsMercenary=>_world.RunConfiguration.AdvancedClassId=="mercenary";
    private bool IsBountyHunter=>IsPowertech || IsMercenary;
    private readonly Dictionary<ulong,(Combatant target,int stacks,double remaining)> _heat=new();
    private readonly HashSet<ulong> _pinned=new();
    private int _barrageCasts;
    private double _shell,_supercharge,_field;
    private int _shellCharges;
    private Vector3 _fieldAt;
    private float _fieldRadius;
    private float _lift;
    private float Mc(string key)=>!IsMercenary?0:Hook(key)>0?Hook(key):BountyCardOwned(key)?BountyCatalogAmount("mercenary",key):0;
    /// <summary>Arsenal's out-of-run Tracer Missile talent converts the Power Shot starter before the run is built.</summary>
    public static string BountyStartingAbility(ClassProfile profile)
    {
        // Arsenal turns Power Shot into Tracer Missile; Advanced Prototype turns Flame Sweep into Retractable Blade.
        var (from,key,to)=profile.Id switch {"mercenary"=>("powershot","mc_tracer_conversion","tracer"),"powertech"=>("flamesweep","pt_dart_conversion","retractableblade"),_=>("","","")};
        if(key=="" || profile.StartingAbility!=from)return profile.StartingAbility;
        var ranks=TalentProfiles.Load(profile.Id);
        bool converted=TalentCatalog.Current.ForClass(profile.Id).Any(t=>ranks.GetValueOrDefault(t.Id)>0 && t.Effects.Any(e=>e.Kind=="hook" && e.Key==key));
        return converted?to:profile.StartingAbility;
    }
    public int HeatSignatureLimit=>5+(int)Mc("mc_heat_max")+BountyCardRanks("mc_heat_rank");
    public int HeatSignature(Combatant t)=>_heat.TryGetValue(t.GetInstanceId(),out var h)?h.stacks:0;
    public bool KoltoShellActive=>IsMercenary && _shell>0;
    public bool InKoltoField=>IsMercenary && _field>0 && _world.PlayerActor.GlobalPosition.DistanceSquaredTo(_fieldAt)<=_fieldRadius*_fieldRadius;
    // Death From Above hovers: immune, rooted and lifted for its whole wind-up and channel.
    public bool DeathFromAboveActive=>IsMercenary && _world.Abilities.Find("deathfromabove") is {State:LightningState.Starting or LightningState.Channeling};
    public bool CanBeginBountyHunter(AutomaticAbility a)=>a.Definition.Id switch {
        "thermalsensoroverride"=>_world.Abilities.Abilities.Any(o=>o.Unlocked && !o.Definition.IsBasicAttack && o.Definition.Category==AbilityCategory.Offensive && o.CooldownRemaining>4),
        "deathfromabove"=>!IsMercenary || _world.Player.StationaryForAbility,
        _=>true};
    private float MercenaryIncoming=>!IsMercenary?1:DeathFromAboveActive?0:(InKoltoField?.85f:1)*(_shell>0 || _world.PlayerActor.Health.Shielded?1-Math.Min(.15f,Mc("mc_reactive")):1);
    public string[] MercenaryEarlyAbilities {
        get {
            int arsenal=BhPoints("arsenal"),io=BhPoints("innovative_ordnance");
            return arsenal>io?new[]{"heatseeker","missileblast","unload","deathfromabove"}:
                io>0?new[]{"incendiarymissile","fusionmissile","explosivedart","sweepingblasters"}:new[]{"incendiarymissile","heatseeker","explosivedart","missileblast"};
        }
    }
    private void AddHeat(Combatant target,int amount)
    {
        if(!target.Alive || amount<=0)return;
        int stacks=Math.Min(HeatSignatureLimit,HeatSignature(target)+amount);
        _heat[target.GetInstanceId()]=(target,stacks,8);
        if(target is EnemyAgent enemy)BountyStatusOverlay.Ensure(enemy,this);
    }
    private void MercenaryStats(AutomaticAbility a)
    {
        if(!IsMercenary)return;
        if(a.Definition.Id=="sweepingblasters")a.Definition.SpreadsPeriodicEffects=Mc("mc_sweep_spread")>0;
    }
    private void MercenaryActivated(AutomaticAbility a)
    {
        if(!IsMercenary)return;
        string id=a.Definition.Id;
        if(a.Definition.Category==AbilityCategory.Utility && Mc("mc_supercharge")>0)_supercharge=5;
        if(id is "unload" or "missileblast")_pinned.Clear();
        if(id is "tracer" or "powershot" && ++_barrageCasts%(Mc("mc_barrage")>0?2:4)==0)_world.Abilities.Find("unload")?.ReduceCooldown(99);
        if(id=="koltoshell"){_shell=20;_shellCharges=4+(int)Mc("mc_shell_charges");}
        if(id=="koltomissile"){_field=6;_fieldAt=a.GroundCenter;_fieldRadius=a.Radius*1.2f;KoltoFieldVisual.Spawn(_world,a.GroundCenter,_fieldRadius,6);}
        if(id=="thermalsensoroverride") {
            var offensive=_world.Abilities.Abilities.Where(o=>o.Unlocked && !o.Definition.IsBasicAttack && o.Definition.Category==AbilityCategory.Offensive).ToArray();
            var longest=offensive.OrderByDescending(o=>o.CooldownRemaining).FirstOrDefault();
            foreach(var o in offensive)o.ReduceCooldown(o==longest?99:3);
            if(Mc("mc_tso_fusion")>0)_world.Abilities.Find("fusionmissile")?.ReduceCooldown(99);
        }
    }
    private void MercenaryHit(AutomaticAbility a,Combatant target)
    {
        if(!IsMercenary || !GodotObject.IsInstanceValid(target))return;
        string id=a.Definition.Id;bool primary=target==a.CastTarget;
        if(id is "tracer" or "powershot")AddHeat(target,primary?1+(Mc("mc_upgraded")>0?1:0):id=="tracer" && Mc("mc_tracer_splash")>0?1:0);
        if(id=="missileblast"){AddHeat(target,1);if(Mc("mc_pinning")>0 && target is EnemyAgent slowed && slowed.Alive)slowed.Slow(.6f,2);}
        if(id=="heatseeker" && _heat.TryGetValue(target.GetInstanceId(),out var heat)) {
            int left=Mc("mc_light_em_up")>0?heat.stacks/2:0;
            if(left>0)_heat[target.GetInstanceId()]=(target,left,heat.remaining);else _heat.Remove(target.GetInstanceId());
        }
        if(id=="unload" && Mc("mc_pinning")>0 && target is EnemyAgent pinned && pinned.Alive) {
            pinned.Slow(.6f,2);
            if(_pinned.Add(target.GetInstanceId()))AddHeat(target,1);
        }
        if(id is "powershot" or "railshot" && target.Alive) {
            if(Mc("mc_gas")>0 && PtOwned("incendiarymissile") is {} gas && !PowertechBurning(target))_world.Periodic.Apply(gas,target,.5f);
            if(id=="railshot" && Mc("mc_superheated_rail")>0 && PowertechBurning(target) && Ready("mc_superheated_rail",.5))
                BountySpread(target,(int)Mc("mc_superheated_rail"),new[]{"incendiarymissile","fusionmissile","retractableblade"});
            if(id=="powershot" && Mc("mc_accelerator")>0 && Ready("mc_accelerator",.3))_world.Abilities.Find("railshot")?.ReduceCooldown(Mc("mc_accelerator"));
        }
        if(id=="deathfromabove" && Mc("mc_dfa_burn")>0 && target.Alive && PtOwned("incendiarymissile") is {} rain && !_world.Periodic.Has(rain,target))_world.Periodic.Apply(rain,target);
        if(id=="concussionmissile" && target is EnemyAgent dazed && dazed.Alive && dazed.Stun(2.5f,false) && Mc("mc_warden")>0) {
            AddHeat(target,2);if(PtOwned("incendiarymissile") is {} burn)_world.Periodic.Apply(burn,target);
        }
        if(id=="jetboost" && target is EnemyAgent thrown && thrown.Alive) {
            thrown.Root(2);
            if(Mc("mc_jet_burn")>0){AddHeat(target,1);if(PtOwned("incendiarymissile") is {} burn)_world.Periodic.Apply(burn,target);}
        }
    }
    private float MercenaryBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsMercenary)return 0;
        string id=a.Definition.Id;float bonus=0;int heat=HeatSignature(t);
        if(id=="heatseeker")bonus+=(.15f+.05f*BountyCardRanks("mc_heat_rank"))*heat;
        if(id=="deathfromabove")bonus+=Mc("mc_terminal")*heat;
        if(id is "powershot" or "railshot") {
            int dots=_world.Periodic.On(t).Where(x=>x.Caster==a.Caster).Select(x=>x.Definition.Id).Distinct().Count();
            bonus+=dots*((id=="powershot"?.15f:0)+Mc("mc_dot_bonus"));
        }
        if(InKoltoField)bonus+=Mc("mc_field_damage");
        if(_supercharge>0)bonus+=Mc("mc_supercharge");
        return bonus;
    }
    private void MercenaryDamaged()
    {
        if(!IsMercenary || _shell<=0 || _shellCharges<=0 || !_world.PlayerActor.Alive || !Ready("mc_shell",2))return;
        _shellCharges--;_world.PlayerActor.Health.ApplyHealing(1);
        PowertechStatusVisual.Flash(_world.PlayerActor,"res://Assets/BountyHunter/proj_radial_swirl_01.png",1);
    }
    private void AdvanceMercenary(double delta)
    {
        if(!IsMercenary)return;
        _shell=Math.Max(0,_shell-delta);_supercharge=Math.Max(0,_supercharge-delta);_field=Math.Max(0,_field-delta);
        foreach(var key in _heat.Keys.ToArray()) {
            var h=_heat[key];h.remaining-=delta;
            if(h.remaining<=0 || !GodotObject.IsInstanceValid(h.target) || !h.target.Alive)_heat.Remove(key);else _heat[key]=h;
        }
        // Real jetpack lift on the presentation; the gameplay body stays rooted on its footprint.
        bool hover=DeathFromAboveActive;
        _world.Player.MovementLocked=hover;
        if(GodotObject.IsInstanceValid(_world.Player.Presentation)) {
            float lift=Mathf.MoveToward(_lift,hover?.12f:0,(float)delta*(hover?.3f:.45f));
            _world.Player.Presentation.Position+=Vector3.Up*(lift-_lift);_lift=lift;
        }
    }
    private void ResetMercenary()
    {
        _heat.Clear();_pinned.Clear();_barrageCasts=0;_shell=_supercharge=_field=0;_shellCharges=0;
        if(IsMercenary && GodotObject.IsInstanceValid(_world.Player)) {
            _world.Player.MovementLocked=false;
            if(GodotObject.IsInstanceValid(_world.Player.Presentation))_world.Player.Presentation.Position-=Vector3.Up*_lift;
        }
        _lift=0;
    }
    private int BhPoints(string tree)=>TalentCatalog.Current.ForClass(_world.RunConfiguration.AdvancedClassId).Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
    public string[] BountyEarlyAbilities=>IsPowertech?PowertechEarlyAbilities:IsMercenary?MercenaryEarlyAbilities:Array.Empty<string>();
    public float BountySpeed=>!IsBountyHunter?1:1+Math.Min(.09f,Pt("pt_jet_speed")+.03f*BountyCardRanks("bh_thrusters"));

    // Explosive Dart: sticks for a fuse, then detonates where its target is (or died).
    private readonly List<(Combatant target,AutomaticAbility source,double fuse,Vector3 at)> _darts=new();
    private bool _detonating;
    public int PendingDarts=>_darts.Count;
    public bool DartAttached(Combatant t)=>_darts.Any(d=>d.target==t);
    private const double DartFuse=2;
    private void BountyDartHit(AutomaticAbility a,Combatant target)
    {
        if(!IsBountyHunter || _detonating || a.Definition.Id is not ("explosivedart" or "thermaldetonator") || !target.Alive)return;
        _darts.Add((target,a,a.Definition.Id=="thermaldetonator"?1:DartFuse,target.GlobalPosition));
        PowertechStatusVisual.Attach(target,"res://Assets/BountyHunter/bw_flare_gray.png",1,()=>DartAttached(target));
    }
    // The sticking hit only scratches; the detonation carries the ability's damage.
    private float BountyDartBonus(AutomaticAbility a)=>!IsBountyHunter || a.Definition.Id is not ("explosivedart" or "thermaldetonator")?0:!_detonating?-.85f:
        a.Definition.Id=="thermaldetonator"?_detonatorBonus:Pt("pt_blade_focus");
    private float _detonatorBonus;
    private void AdvanceDarts(double delta)
    {
        for(int i=_darts.Count-1;i>=0;i--) {
            var d=_darts[i];d.fuse-=delta;
            bool alive=GodotObject.IsInstanceValid(d.target) && d.target.Alive;
            if(alive)d.at=d.target.GlobalPosition;
            if(d.fuse>0 && alive){_darts[i]=d;continue;}
            _darts.RemoveAt(i);
            if(d.source.Unlocked && d.source.Caster.Alive)Detonate(d.source,d.at);
        }
    }
    private void Detonate(AutomaticAbility source,Vector3 at)
    {
        _detonating=true;
        try {
            var hit=_world.Targeting.InArea(at,source.Radius,source.Radius,true).Where(t=>t.Alive).ToArray();
            // Prototype Cylinders trades every other DoT for a bigger, harder Retractable Blade blast.
            bool detonator=source.Definition.Id=="thermaldetonator",focus=!detonator && Pt("pt_blade_focus")>0;
            // Thermal Detonator never spreads; it cashes in the Blade bleeds already in its blast.
            var spread=detonator?Array.Empty<AutomaticAbility>():_world.Periodic.SpreadSources(_world.PlayerActor,hit).Where(s=>!focus || s.Definition.Id=="retractableblade").ToArray();
            _detonatorBonus=detonator?Math.Min(5,hit.Count(PowertechBleeding))*(Pt("pt_td_systems")>0?Pt("pt_td_systems"):.15f):0;
            foreach(var t in hit) {
                source.ApplyPeriodicHit(t,source.DamagePerTick);
                if(!t.Alive)continue;
                if(detonator) {
                    if(Pt("pt_td_systems")>0 && PtOwned("retractableblade") is {} bleed && _world.Periodic.Has(bleed,t))_world.Periodic.Apply(bleed,t);
                }
                else if(IsPowertech) {
                    if(PtOwned("retractableblade") is {} blade && !_world.Periodic.Has(blade,t)) {
                        _world.Periodic.Apply(blade,t);
                        if(focus)blade.ApplyPeriodicHit(t,blade.DamagePerTick*.5f);
                    }
                    if(!focus && Pt("pt_incendiary_dart")>0 && PtOwned("incendiarymissile") is {} burn)_world.Periodic.Apply(burn,t);
                }
            }
            foreach(var t in hit.Where(t=>t.Alive))
                foreach(var s in spread)if(!_world.Periodic.Has(s,t))_world.Periodic.Apply(s,t);
            if(source.Presentation is D3AbilityPresentation visual)visual.BountyDetonate(at);
        } finally {_detonating=false;}
    }
}
