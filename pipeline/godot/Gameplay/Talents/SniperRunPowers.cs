using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

public sealed partial class TalentRuntime
{
    private bool IsSniper=>_world.RunConfiguration.AdvancedClassId=="sniper";
    private double _snVolley,_snChainT,_snCharge;
    private float _snSpent,_snWindowBonus;
    private bool _snWindowReady;
    private float _snVolleyPower;
    private int _snLinks,_snCurrentLinks;
    private bool _snWeakeningEmpowered,_snPoisonCover;
    private readonly List<(double delay,Combatant target,AutomaticAbility source,float scale)> _agEchoes=new();

    // Single-axis scoped cards: one number per card, nothing rank-capped, rarity multiplies it.
    private static readonly (string id,string title,float amount)[] SniperDamageCards={
        ("ambush","High Caliber",.9f),("seriesofshots","Sustained Barrage",.9f),("followthrough","Finishing Salvo",.9f),("suppressivefire","Saturation Fire",.9f),
        ("orbitalstrike","Heavy Ordnance",.9f),("plasmaprobe","Plasma Saturation",.9f),("explosiveprobe","Overcharged Payload",.9f),("interrogationprobe","Interrogation Matrix",.9f),
        ("empdischarge","Disruptive Pulse",.9f),("fragmentationgrenade","Fragmentation Cluster",.9f),("dart","Concentrated Toxin",1f),("corrosivegrenade","Corrosive Cloud",1f),
        ("agentcull","Lethal Barrage",1f),("weakeningblast","Exploited Weakness",1f),("overloadshot","Close Quarters Payload",.9f)};
    private static readonly (string id,string title,float amount)[] SniperRadiusCards={
        ("suppressivefire","Wider Saturation",.12f),("orbitalstrike","Wider Ordnance",.12f),("plasmaprobe","Wider Plasma",.12f),("explosiveprobe","Wider Payload",.12f),
        ("interrogationprobe","Wider Matrix",.12f),("empdischarge","Wider Pulse",.12f),("fragmentationgrenade","Wider Fragments",.12f),("corrosivegrenade","Wider Cloud",.15f),("agentcull","Wider Barrage",.12f)};
    // Dart and Interrogation Probe are single-target-style DoTs: no recovery card.
    private static readonly (string id,string title,float amount)[] SniperCooldownCards={
        ("ambush","Quickened Ambush",.18f),("seriesofshots","Quickened Barrage",.18f),("followthrough","Quickened Salvo",.18f),("suppressivefire","Quickened Saturation",.18f),
        ("orbitalstrike","Quickened Ordnance",.18f),("plasmaprobe","Quickened Plasma",.18f),("explosiveprobe","Quickened Payload",.18f),("empdischarge","Quickened Pulse",.18f),
        ("fragmentationgrenade","Quickened Fragments",.18f),("corrosivegrenade","Quickened Cloud",.18f),("agentcull","Quickened Barrage",.18f),("weakeningblast","Quickened Weakness",.18f),("overloadshot","Quickened Payload",.18f)};
    private static readonly (string id,string title)[] SniperTickCards={
        ("dart","Toxin Pulse"),("corrosivegrenade","Cloud Pulse"),("interrogationprobe","Matrix Pulse")};

    public IEnumerable<UpgradeDefinition> SniperRunPowers()
    {
        foreach(var u in AgentAxisCards("sniper",SniperDamageCards,SniperRadiusCards,SniperCooldownCards,SniperTickCards))yield return u;
        foreach(var (id,title) in new[]{("ambush","Ambush Ricochets"),("seriesofshots","Barrage Ricochets"),("followthrough","Salvo Ricochets")}) {
            var a=_world.Abilities.Find(id);if(a==null)continue;
            yield return new UpgradeDefinition {Id="sniper_targets_"+id,Title=title,Icon=a.Definition.Icon,AbilityId=id,Spec="marksmanship",Effect=UpgradeEffect.Mechanic,Mechanic="snm_target_"+id,
                AmountPerRank=0,MaximumRank=0,MechanicalDescription=$"{a.Definition.Title} ricochets to one additional enemy per rank. No rank cap."};
        }
        UpgradeDefinition M(string id,string title,string icon,string ability,string spec,string key,float amount,string text)=>AgentMech("sniper",id,title,icon,ability,spec,key,amount,text);
        // Marksmanship
        yield return M("crossfire","Crossfire Discipline","ambush","ambush","marksmanship","crossfire",1,"Ambush recovers 1.5 seconds of Followthrough cooldown per rank. Followthrough recovers 2 seconds of Ambush cooldown per rank. Each refund happens once per activation.");
        yield return M("ricochet","Penetrating Rounds","seriesofshots","","marksmanship","ricochet",.05f,"Ambush, Series of Shots and Followthrough retain an extra 5% damage per ricochet jump, per rank (never above 100%).");
        yield return M("entrench","Entrenched Salvo","entrench","entrench","marksmanship","entrench",.25f,"During Entrench, Ambush, Series of Shots, Followthrough and Suppressive Fire deal 25% additional damage per rank. Activating Entrench recovers Series of Shots and Suppressive Fire by 2 seconds per rank.");
        yield return M("chain","Firing Chain","seriesofshots","","marksmanship","chain",.06f,"Each shot of the Ambush, Series of Shots, Followthrough sequence inside its 6 second window adds a link: the next shot deals 6% more per link per rank. The chain resets when it breaks.");
        yield return M("deadeye","Dead Eye","ambush","","marksmanship","deadeye",.25f,"Ambush deals 25% more per rank to enemies above 70% health. Followthrough deals 25% more per rank to enemies below 30% health.");
        yield return M("echoround","Echo Round","followthrough","followthrough","marksmanship","echoround",0,"Followthrough fires again 1 second later at 50% damage, one more repeat per rank. At most once every 2 seconds.");
        yield return M("patience","Sniper's Patience","seriesofshots","","marksmanship","patience",.08f,"Each second you stand still in cover charges your next Ambush, Series of Shots or Followthrough for 8% more damage per rank, up to 6 seconds. The charge is spent by that shot and resets when you move.");
        yield return M("window","Killing Window","ambush","","marksmanship","window",.6f,"The first Ambush, Series of Shots, Followthrough or Suppressive Fire after you enter cover deals 60% more damage per rank. Once every 10 seconds.");
        yield return M("killzone","Kill Zone","suppressivefire","","marksmanship","killzone",.4f,"While in cover, Ambush, Series of Shots, Followthrough and Suppressive Fire splash nearby enemies for 40% of their damage per rank.");
        yield return M("overwatch","Overwatch","suppressivefire","","marksmanship","overwatch",.06f,"While in cover, Ambush, Series of Shots, Followthrough and Suppressive Fire recover 6% faster per rank.");
        yield return M("dampers","Ballistic Dampers","ambush","","marksmanship","dampers",.01f,"Damage taken is divided by 1 plus 1% per rank, so mitigation can never reach 100%.");
        // Engineering
        yield return M("overload","Probe Overload","explosiveprobe","explosiveprobe","engineering","overload",.25f,"Explosive Probe detonates each victim's own Interrogation Probe for its remaining scheduled damage plus 25% more per rank, then reapplies it to survivors. Other casters' probes are untouched.");
        yield return M("suppression","Targeted Suppression","suppressivefire","","engineering","suppression",.3f,"Suppressive Fire and Plasma Probe deal 30% additional damage per rank to enemies carrying your Interrogation Probe.");
        yield return M("targeting","Probe Targeting","interrogationprobe","interrogationprobe","engineering","targeting",.25f,"Series of Shots and Explosive Probe deal 25% additional damage per rank to enemies carrying your Interrogation Probe.");
        yield return M("bombardment","Coordinated Bombardment","orbitalstrike","orbitalstrike","engineering","bombardment",2,"Activating Suppressive Fire recovers 2 seconds of Orbital Strike cooldown per rank. Orbital Strike gains 15% base radius per rank.");
        yield return M("emp","Overclocked EMP","empdischarge","empdischarge","engineering","emp",3,"EMP Discharge also recovers Explosive Probe and Orbital Strike by 3 seconds per rank, once per activation.");
        yield return M("cascade","Cascade Detonation","explosiveprobe","explosiveprobe","engineering","cascade",.3f,"Explosive Probe also pays 30% per rank of up to 6 seconds of remaining Interrogation Probe damage on every probed enemy it strikes.");
        yield return M("relay","Relay Network","plasmaprobe","","engineering","relay",0,"Orbital Strike and Plasma Probe hits on a probed enemy spread your Interrogation Probe to one more nearby enemy per rank, once per second.");
        yield return M("lingering","Lingering Plasma","plasmaprobe","plasmaprobe","engineering","lingering",1,"Plasma Probe fields last 1 second longer per rank.");
        yield return M("probeshield","Probe Shielding","interrogationprobe","","engineering","probeshield",3,"Probe hits grant a shield of 3 per rank, once every 6 seconds.");
        // Lethality
        yield return M("cull","Concentrated Volley","agentcull","agentcull","agent_lethality","cull",.5f,"Cull gains full secondary damage and 50% base splash radius per rank. Activating Cull empowers the next Weakening Blast within 6 seconds for 50% additional damage per rank.");
        yield return M("decay","Zeroing In","dart","","agent_lethality","decay",.2f,"While in cover, your Dart and Corrosive Grenade tick 20% faster per rank without shortening duration. Includes running effects and spread copies.");
        yield return M("weakening","Virulent Weakness","weakeningblast","weakeningblast","agent_lethality","weakening",1,"Weakening Blast echoes one tick per rank of each of your Dart and Corrosive Grenade effects on its victim without consuming or refreshing them.");
        yield return M("contamination","Contaminated Rounds","ambush","ambush","agent_lethality","contamination",.25f,"Ambush applies your learned Dart to unpoisoned victims. Against victims already carrying your Dart it echoes 25% of a poison tick per rank without consuming or refreshing it.");
        yield return M("focus","Toxic Focus","agentcull","agentcull","agent_lethality","focus",3,"Cull hits while in cover grant a shield of 3 per rank, once every 6 seconds.");
        foreach(var u in AgentLethalityCards("sniper"))yield return u;
        // Shield capacity axis card
        if(_world.Abilities.Find("shieldprobe") is {} shield)
            yield return new UpgradeDefinition {Id="sniper_shield_shieldprobe",Title="Deployed Cover",Icon=shield.Definition.Icon,AbilityId="shieldprobe",Spec="marksmanship",Effect=UpgradeEffect.AbilityPower,ShieldPerRank=.1f};
    }
    public IEnumerable<UpgradeDefinition> SniperExtraMechanics()
    {
        yield return AgentMech("sniper","kill_reset","Clean Kill","ambush","ambush","marksmanship","kill_reset",1,"Defeating an enemy recovers 1 second per rank of Ambush and Followthrough cooldown, at most once per second.");
        yield return AgentMech("sniper","frag_probe","Shrapnel Feed","fragmentationgrenade","fragmentationgrenade","engineering","frag_probe",1.5f,"Fragmentation Grenade hits recover 1.5 seconds per rank of Explosive Probe cooldown, at most once per 0.5 seconds.");
    }
    public bool SniperUpgradeUsable(UpgradeDefinition u)
    {
        bool Has(string id)=>_world.Abilities.Find(id)?.Unlocked==true;
        bool Poison()=>Has("dart") || Has("corrosivegrenade");
        bool Precision()=>new[]{"ambush","seriesofshots","followthrough"}.Any(Has);
        bool Probe()=>Has("interrogationprobe");
        if(u.Mechanic.StartsWith("snm_dup_",StringComparison.Ordinal) || u.Mechanic.StartsWith("snm_multi_",StringComparison.Ordinal) || u.Mechanic.StartsWith("snm_target_",StringComparison.Ordinal))return Has(u.AbilityId);
        return u.Mechanic switch {
            "snm_crossfire"=>Has("ambush") && Has("followthrough"),
            "snm_ricochet"=>Precision(),
            "snm_chain"=>new[]{"ambush","seriesofshots","followthrough"}.Count(Has)>=2,
            "snm_deadeye"=>Has("ambush") || Has("followthrough"),
            "snm_echoround"=>Has("followthrough"),
            "snm_patience"=>Precision(),
            "snm_window" or "snm_killzone" or "snm_overwatch" or "snm_dampers"=>Precision() || Has("suppressivefire"),
            "snm_suppression"=>Probe() && (Has("suppressivefire") || Has("plasmaprobe")),
            "snm_bombardment"=>Has("suppressivefire") && Has("orbitalstrike"),
            "snm_overload" or "snm_cascade"=>Has("explosiveprobe") && Probe(),
            "snm_emp"=>Has("empdischarge") && (Has("explosiveprobe") || Has("orbitalstrike")),
            "snm_targeting"=>Probe() && (Has("seriesofshots") || Has("explosiveprobe")),
            "snm_relay"=>Probe() && (Has("orbitalstrike") || Has("plasmaprobe")),
            "snm_lingering"=>Has("plasmaprobe"),
            "snm_probeshield"=>Probe() && (Has("explosiveprobe") || Has("plasmaprobe") || Has("orbitalstrike") || Has("seriesofshots")),
            "snm_cull"=>Has("agentcull") && Has("weakeningblast") && Poison(),
            "snm_decay" or "snm_pandemic" or "snm_mending"=>Poison(),
            "snm_weakening"=>Has("weakeningblast") && Poison(),
            "snm_contamination"=>Has("ambush") && Has("dart"),
            "snm_entrench"=>Has("entrench") && (Precision() || Has("suppressivefire")),
            "snm_septic"=>Has("agentcull") && Poison(),
            "snm_tempo"=>Poison() && (Has("agentcull") || Has("weakeningblast")),
            "snm_focus"=>Has("agentcull"),
            "snm_kill_reset"=>Has("ambush") || Has("followthrough"),
            "snm_frag_probe"=>Has("fragmentationgrenade") && Has("explosiveprobe"),
            _=>true
        };
    }
    private void SniperStats(AutomaticAbility a)
    {
        if(!IsSniper)return;
        string id=a.Definition.Id;
        if(id is "ambush" or "seriesofshots" or "followthrough") {
            a.ExtraBounces+=AgR("target_"+id);
            a.Definition.BounceFalloff=Math.Min(1,SniperBase(id).BounceFalloff+.05f*AgP("ricochet"));
        }
        if(id=="orbitalstrike")a.Radius*=1+.15f*AgP("bombardment");
        if(id=="empdischarge")a.CooldownSeconds=Math.Max(a.CooldownSeconds,Math.Min(8,SniperBase(id).CooldownSeconds));
        if(id=="plasmaprobe")a.ChannelSeconds+=AgP("lingering");
        if(id=="agentcull") {
            float volley=AgP("cull");
            a.Definition.SplashMultiplier=volley>0?1:SniperBase(id).SplashMultiplier;
            a.Radius*=1+.5f*volley;
        }
        if(id is "dart" or "corrosivegrenade" && AgentPositioned)a.TickSeconds/=1+.2f*AgP("decay");
        if(id=="weakeningblast") {
            bool spread=Hook("sn_lethal_spread")>0;
            a.Definition.Area=spread?AbilityArea.TargetSplash:SniperBase(id).Area;
            a.Definition.SpreadsPeriodicEffects=spread;
            a.Radius=spread?.3f:a.Radius;
            a.Definition.SplashMultiplier=spread?1:SniperBase(id).SplashMultiplier;
        }
    }
    private static AbilityDefinition SniperBase(string id)=>GD.Load<AbilityDefinition>($"res://Gameplay/Abilities/{id}.tres");
    private void SniperActivated(AutomaticAbility a)
    {
        if(!IsSniper)return;
        string id=a.Definition.Id;
        void Recover(string ability,float seconds){if(seconds>0 && _world.Abilities.Find(ability) is {Unlocked:true} next)next.ReduceCooldown(seconds);}
        float crossfire=AgP("crossfire");
        if(id=="ambush")Recover("followthrough",1.5f*crossfire);
        if(id=="followthrough")Recover("ambush",2*crossfire);
        if(id=="suppressivefire")Recover("orbitalstrike",2*AgP("bombardment"));
        if(id=="empdischarge" && AgP("emp") is var emp && emp>0){Recover("explosiveprobe",3*emp);Recover("orbitalstrike",3*emp);}
        if(id=="entrench" && AgP("entrench") is var entrench && entrench>0){Recover("seriesofshots",2*entrench);Recover("suppressivefire",2*entrench);}
        if(id=="agentcull" && AgP("cull") is var volley && volley>0){_snVolley=6;_snVolleyPower=volley;}
        if(id=="weakeningblast"){_snWeakeningEmpowered=_snVolley>0 && _snVolleyPower>0;_snVolley=0;}
        bool marksman=id is "ambush" or "seriesofshots" or "followthrough" or "suppressivefire";
        if(marksman) {
            // Charge and window are fixed at activation so the whole shot, ricochets included, keeps them.
            _snSpent=id=="suppressivefire"?0:(float)_snCharge;if(id!="suppressivefire")_snCharge=0;
            _snWindowBonus=0;
            if(AgentPositioned && _snWindowReady && AgP("window") is var window && window>0 && Ready("snm_window",10)){_snWindowBonus=.6f*window;_snWindowReady=false;}
        }
        if(id is "ambush" or "seriesofshots" or "followthrough") {
            // The chain bonus applies to the shot after the link is added, so this shot keeps the links it found.
            _snCurrentLinks=_snChainT>0?_snLinks:0;_snLinks=_snCurrentLinks+1;_snChainT=6;
            if(id=="followthrough"){_snLinks=0;_snChainT=0;}
        }
    }
    private float SniperBonus(AutomaticAbility a,Combatant t)
    {
        if(!IsSniper)return 0;
        string id=a.Definition.Id;
        bool probe=_world.Periodic.On(t).Any(p=>p.Caster==a.Caster && p.Definition.Id=="interrogationprobe");
        bool shot=id is "ambush" or "seriesofshots" or "followthrough";
        float health=t.Health.Maximum>0?t.Health.Current/t.Health.Maximum:1;
        float bonus=0;
        if(probe && id is "suppressivefire" or "plasmaprobe")bonus+=.3f*AgP("suppression");
        if(probe && id is "seriesofshots" or "explosiveprobe")bonus+=.25f*AgP("targeting");
        if(AgentPositioned && Hook("sn_lethal_cover")>0 && id is "dart" or "corrosivegrenade")bonus+=.2f;
        if(id=="weakeningblast" && _snWeakeningEmpowered)bonus+=.5f*_snVolleyPower;
        if(_agentGuard>0 && id is "ambush" or "seriesofshots" or "followthrough" or "suppressivefire")bonus+=.25f*AgP("entrench");
        if(shot)bonus+=.08f*AgP("patience")*_snSpent;
        if(shot || id=="suppressivefire")bonus+=_snWindowBonus;
        if(shot && _snCurrentLinks>0)bonus+=.06f*AgP("chain")*_snCurrentLinks;
        if(id=="ambush" && health>.7f)bonus+=.25f*AgP("deadeye");
        if(id=="followthrough" && health<.3f)bonus+=.25f*AgP("deadeye");
        return bonus;
    }
    private void SniperHit(AutomaticAbility a,Combatant t)
    {
        if(!IsSniper || !t.Alive)return;
        string id=a.Definition.Id;
        if(id is "ambush" or "seriesofshots" or "followthrough" or "suppressivefire" && AgentPositioned && AgP("killzone") is var zone && zone>0)
            foreach(var near in _world.Targeting.InArea(t.GlobalPosition,.12f,a.Range).Where(n=>n!=t && n.Alive).ToArray())
                near.Health.ApplyDamage(a.DamagePerTick*.4f*zone*_world.Build.DamageMultiplier(a,near),a.Definition.DamageType,false,id);
        var probeAbility=_world.Abilities.Find("interrogationprobe");
        if(id=="explosiveprobe" && probeAbility is {Unlocked:true} probe) {
            float cascade=AgP("cascade");
            if(cascade>0 && _world.Periodic.Has(probe,t)) {
                float extra=_world.Periodic.DetonatePartial(probe,t,.3f*cascade,6,double.MaxValue);
                if(extra>0)t.Health.ApplyDamage(extra,DamageType.Internal,false,"explosiveprobe");
            }
            float overload=AgP("overload");
            if(overload>0 && t.Alive) {
                if(_world.Periodic.DetonateOwned(probe,t,"probe_overload",out float paid)) {
                    if(paid>0 && t.Alive)t.Health.ApplyDamage(paid*overload*.25f,DamageType.Internal,false,"probe_overload");
                    if(a.Presentation is D3AbilityPresentation v)v.TargetImpact(t);
                }
                if(t.Alive)_world.Periodic.Apply(probe,t);
            }
        }
        if(id is "orbitalstrike" or "plasmaprobe" && probeAbility is {} relayProbe && t.Alive && _world.Periodic.Has(relayProbe,t) && AgR("relay") is var jumps && jumps>0 && Ready("snm_relay",1))
            foreach(var next in _world.Targeting.InArea(t.GlobalPosition,.35f,a.Range).Where(n=>n!=t && !_world.Periodic.Has(relayProbe,n)).OrderBy(n=>n.GlobalPosition.DistanceSquaredTo(t.GlobalPosition)).Take(jumps).ToArray())
                _world.Periodic.Apply(relayProbe,next);
        if(id=="followthrough" && AgR("echoround") is var repeats && repeats>0 && Ready("snm_echoround",2))
            for(int i=1;i<=repeats;i++)_agEchoes.Add((i,t,a,.5f));
        if(id=="weakeningblast" && Hook("sn_lethal_extend")>0)
            foreach(var poison in _world.Periodic.On(t).Where(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade"))
                _world.Periodic.Extend(poison,t,2,PeriodicDuration(poison)+4);
        float echo=id=="weakeningblast"?AgP("weakening"):id=="agentcull" && Hook("sn_lethal_echo")>0?.25f:0;
        if(echo>0) {
            float damage=_world.Periodic.On(t).Where(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade")
                .Sum(p=>p.Definition.PeriodicDamage*p.DamagePerTick/Math.Max(.001f,p.Definition.Damage)*_world.Build.DamageMultiplier(p,t)*PeriodicMultiplier(t));
            if(damage>0)t.Health.ApplyDamage(damage*echo,DamageType.Internal,abilityId:id);
        }
        if(id=="agentcull" && AgentPositioned && Hook("sn_lethal_focus")>0 && Ready("sn_lethal_shield",6))_world.PlayerActor.Health.ApplyShield(8,6);
        if(id=="agentcull" && AgentPositioned && AgP("focus") is var focus && focus>0 && Ready("snm_focus",6))_world.PlayerActor.Health.ApplyShield(3*focus,6);
        if(id=="weakeningblast" && Hook("sn_lethal_focus")>0 && Ready("sn_lethal_recovery",2))_world.Abilities.Find("shieldprobe")?.ReduceCooldown(1);
        if(id=="ambush" && AgP("contamination") is var contamination && contamination>0 && _world.Abilities.Find("dart") is {} dart) {
            if(!_world.Periodic.Has(dart,t))_world.Periodic.Apply(dart,t);
            else t.Health.ApplyDamage(dart.Definition.PeriodicDamage*dart.DamagePerTick/Math.Max(.001f,dart.Definition.Damage)*.25f*contamination*_world.Build.DamageMultiplier(dart,t)*PeriodicMultiplier(t),DamageType.Internal,abilityId:"ambush");
        }
    }
    private void SniperPoisonTick()
    {
        if(!IsSniper || !AgentPositioned || Hook("sn_lethal_alignment")==0 || !Ready("sn_lethal_alignment",2))return;
        _world.Abilities.Find("agentcull")?.ReduceCooldown(.75f);_world.Abilities.Find("weakeningblast")?.ReduceCooldown(.5f);
    }
    private void AdvanceSniper(double delta)
    {
        if(!IsSniper)return;
        _snVolley=Math.Max(0,_snVolley-delta);
        _snChainT=Math.Max(0,_snChainT-delta);if(_snChainT==0){_snLinks=0;_snCurrentLinks=0;}
        bool covered=AgentPositioned;
        if(covered && !_snPoisonCover)_snWindowReady=true;
        _snCharge=covered?Math.Min(6,_snCharge+delta):0;
        if(!covered)_snWindowReady=false;
        if(covered!=_snPoisonCover){_snPoisonCover=covered;if(AgP("decay")>0)_world.Progression?.Recalculate();}
        float watch=AgP("overwatch");
        if(covered && watch>0)
            foreach(string id in new[]{"ambush","seriesofshots","followthrough","suppressivefire"})_world.Abilities.Find(id)?.ReduceCooldown((float)(delta*.06f*watch));
    }
    private void ResetSniper(){_snVolley=_snChainT=_snCharge=0;_snSpent=_snWindowBonus=0;_snWindowReady=false;_snVolleyPower=0;_snLinks=_snCurrentLinks=0;_snWeakeningEmpowered=_snPoisonCover=false;_agEchoes.Clear();}
}
