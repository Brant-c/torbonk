using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;
public sealed partial class TalentRuntime
{
    public bool IsAgent=>_world.RunConfiguration.AdvancedClassId is "operative" or "sniper";
    private bool IsOperative=>_world.RunConfiguration.AdvancedClassId=="operative";
    public int TacticalAdvantage {get;private set;}
    private double _koltoLife,_koltoTick,_agentGuard,_revitalized;
    private double _advantageLife,_agentOpening,_followup,_stationary;
    private double _counterstrike;
    private readonly System.Collections.Generic.Dictionary<ulong,double> _debilitated=new();
    public bool AgentCounterstrikeActive=>IsOperative && Hook("agent_counterstrike")>0 && _counterstrike>0;
    public bool AgentCounterstrikeReady=>IsOperative && Hook("agent_counterstrike")>0;
    public bool AgentConcealment=>IsOperative && Ranks.Any(p=>p.Value>0 && p.Key.StartsWith("concealment.",StringComparison.Ordinal));
    public bool AgentVenomousBlades=>IsOperative && Hook("agent_venomous_blades")>0;
    public int AgentPriority(AutomaticAbility a)=>a.Definition.Id switch {
        "debilitate"=>0,"hiddenstrike"=>5,"backstab"=>10,"laceration"=>20,
        "carbineburst"=>25,"severtendon"=>30,"agentcull"=>35,"weakeningblast"=>36,"corrosivegrenade"=>37,
        "shiv"=>80,"dart"=>90,_=>50};
    public void ReplaceAgentDart()
    {
        if(!AgentVenomousBlades || _world.Abilities.Find("dart") is not {Unlocked:true} dart || _world.Abilities.Find("laceration") is not {} lac)return;
        bool free=dart.InheritsFreeSlot;dart.SetUnlocked(false);dart.InheritsFreeSlot=false;lac.InheritsFreeSlot|=free;lac.SetUnlocked(true);
    }
    public static bool AgentCoverAbility(string id)=>id is "snipershot" or "ambush" or "seriesofshots" or "followthrough" or "suppressivefire" or "plasmaprobe" or "orbitalstrike" or "explosiveprobe" or "agentcull";
    public bool AgentOpening=>_agentOpening>0;
    public bool AgentPositioned=>IsAgent && !IsOperative && _stationary>=.6 && _world.Player.IsOnFloor() && _world.Player.ActualVelocity.Length()<.03f;
    public bool CanBeginAgent(AutomaticAbility a)=>!IsAgent || (!IsOperative && AgentCoverAbility(a.Definition.Id) && !AgentPositioned?false:a.Definition.Id switch {
        "carbineburst"=>!AgentConcealment,
        "blowforblow"=>IsOperative && Hook("agent_counterstrike")>0,
        "dart"=>!AgentVenomousBlades && (!IsOperative || _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,a.Range,a.Range).Any(t=>_world.Periodic.Remaining(a,t)<=2)),
        "hiddenstrike"=>_agentOpening>0, "surgicalprobe"=>TacticalAdvantage>0,
        "followthrough"=>_followup>0, _=>true});
    private void Advantage(){TacticalAdvantage=Math.Min(2,TacticalAdvantage+1);_advantageLife=12;}
    private void SpendAdvantage()
    {
        if(TacticalAdvantage<=0)return;
        TacticalAdvantage--;
        if(Hook("agent_revitalizers")>0){_world.PlayerActor.Health.ApplyHealing(4,false,"revitalizers");_revitalized=4;}
    }
    private void AgentStats(AutomaticAbility a)
    {
        if(IsOperative && a.Definition.Id=="debilitate" && Hook("agent_mass_debilitation")>0){a.Definition.Area=AbilityArea.CasterRadial;a.Range=a.Radius=.55f;a.Definition.MaximumTargets=0;}
    }
    private void AgentDamaged()
    {
        var h=_world.PlayerActor.Health;
        if(IsAgent && Hook("agent_emergency")>0 && h.IsAlive && h.Current<h.Maximum*.3f && Ready("agent_emergency",25)){
            h.ApplyHealing(15,false,"emergency_response");_world.Abilities.Find("koltoinjection")?.ReduceCooldown(100);
        }
    }
    private void AgentActivated(AutomaticAbility a)
    {
        if(!IsAgent)return;
        switch(a.Definition.Id){
            case "blowforblow":if(Hook("agent_counterstrike")>0)_counterstrike=4;break;
            case "entrench":_agentGuard=6;break;
            case "agentescape":_world.PlayerActor.Health.VulnerabilitySeconds=0;break;
            case "koltoprobe":_koltoLife=8;_koltoTick=0;break;
            case "shiv": case "hiddenstrike": Advantage();break;
            case "backstab":if(Hook("agent_opportunity")>0)Advantage();break;
            case "koltoinjection":if(Hook("agent_medical")>0)Advantage();break;
            case "surgicalprobe":if(!(Hook("agent_surgical")>0 && _world.PlayerActor.Health.Current<_world.PlayerActor.Health.Maximum*.3f))SpendAdvantage();break;
            case "laceration":_agentLacerationBonus=TacticalAdvantage>0?.5f:0;SpendAdvantage();break;
            case "agentstealth":case "cloakingscreen":_agentOpening=6;break;
            case "snipershot":case "ambush":case "seriesofshots":_followup=6;break;
            case "followthrough":_followup=0;if(Hook("agent_reactive_shots")>0){_world.Abilities.Find("ambush")?.ReduceCooldown(2);_world.Abilities.Find("seriesofshots")?.ReduceCooldown(2);}break;
            case "adrenalineprobe":foreach(var b in _world.Abilities.Abilities.Where(b=>b!=a))b.ReduceCooldown(3);break;
            case "stimboost":Advantage();_world.PlayerActor.Health.ApplyHealing(8,false,a.Definition.Id);break;
            case "empdischarge":foreach(string id in new[]{"interrogationprobe","plasmaprobe"})_world.Abilities.Find(id)?.ReduceCooldown(6);if(Hook("agent_methodology")>0)_world.Abilities.Find("explosiveprobe")?.ReduceCooldown(12);break;
        }
        if(a.Definition.Id is "koltoinjection" or "surgicalprobe" && Hook("agent_medical_screen")>0 && Ready("agent_medical_screen",6))_world.PlayerActor.Health.ApplyShield(10,6);
        if(a.Definition.Id=="empdischarge"){
            if(Hook("agent_probe_control")>0)_world.PlayerActor.Health.ApplyHealing(12,false,"empdischarge");
            if(Hook("agent_energy_overrides")>0){_world.Abilities.Find("orbitalstrike")?.ReduceCooldown(4);_world.Abilities.Find("ballisticshield")?.ReduceCooldown(4);}
        }
        if(a.Definition.Id=="hiddenstrike")_agentOpening=0;
        if(a.Definition.Id is "backstab" or "hiddenstrike" or "laceration" && Hook("agent_opportunity")>0 && Every("agent_opportunity",4))_agentOpening=4;
    }
    private float _agentLacerationBonus;
    private float AgentBonus(AutomaticAbility a,Combatant target)
    {
        if(!IsAgent)return 0;
        float bonus=AgentPositioned?.15f:0;
        if(AgentPositioned && a.Definition.Id is "ambush" or "seriesofshots" or "followthrough" or "suppressivefire" && Hook("agent_nest")>0)bonus+=.25f;
        bool marked=_debilitated.GetValueOrDefault(target.GetInstanceId())>0;
        if(marked)bonus+=.4f;
        if(a.Definition.Id=="laceration" && Hook("agent_controlled_burst")>0 && (marked || target is EnemyAgent {StunRemaining:>0}))bonus+=.5f;
        if(a.Definition.Id=="laceration")bonus+=_agentLacerationBonus;
        if(a.Definition.Id=="agentcull"){
            bonus+=.35f*_world.Periodic.On(target).Count(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade");
            if(Hook("agent_microbes")>0 && target.Health.Current<target.Health.Maximum*.3f)bonus+=.2f;
        }
        return bonus;
    }
    private void AgentHit(AutomaticAbility a,Combatant target)
    {
        if(!IsAgent)return;
        if(a.Definition.Id is "ambush" or "seriesofshots" or "followthrough" or "suppressivefire" && Hook("agent_cover_screen")>0 && Ready("agent_cover_screen",6))_world.PlayerActor.Health.ApplyShield(8,6);
        if(a.Definition.Id is "interrogationprobe" or "plasmaprobe" or "explosiveprobe" && Hook("agent_probe_control")>0 && target is EnemyAgent probeTarget)probeTarget.Slow(.65f,3);
        bool poisoned=_world.Periodic.On(target).Any(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade");
        if(poisoned && Hook("agent_toxic_resilience")>0 && Ready("agent_toxic_resilience",2)){
            _world.PlayerActor.Health.ApplyHealing(2,false,"toxic_resilience");_world.Abilities.Find("shieldprobe")?.ReduceCooldown(.5f);
        }
        if(a.Definition.Id=="agentcull" && Hook("agent_toxic_spread")>0 && Ready("agent_toxic_spread",2)){
            var poisons=_world.Periodic.On(target).Where(p=>p.Caster==a.Caster && p.Definition.Id is "dart" or "corrosivegrenade").ToArray();
            var nearby=_world.Targeting.InArea(target.GlobalPosition,.35f,a.Range).Where(t=>t!=target).OrderBy(t=>t.GlobalPosition.DistanceSquaredTo(target.GlobalPosition)).Take(2).ToArray();
            foreach(var t in nearby)foreach(var p in poisons)_world.Periodic.Apply(p,t);
        }
        if(a.Definition.Id is "flashbang" or "debilitate" && target is EnemyAgent e)e.Stun(a.Definition.Id=="flashbang"?4:3,false);
        if(a.Definition.Id=="debilitate" && Hook("agent_mass_debilitation")>0 && target.Alive)_debilitated[target.GetInstanceId()]=6;
        bool bladePoison=a.Definition.Id=="laceration" && AgentVenomousBlades || a.Definition.Id=="backstab" && Hook("agent_acid")>0;
        if(bladePoison && target.Alive && _world.Abilities.Find("dart") is {} poison){
            if(a.Definition.Id=="backstab"){
                if(_world.Periodic.DetonateOwned(poison,target,"acid_blade",out _) && a.Presentation is D3AbilityPresentation visual)visual.PresentAcidBladeDetonation(target);
                if(target.Alive)_world.Periodic.Apply(poison,target,relicApplied:true);
            }else if(_world.Periodic.Has(poison,target))_world.Periodic.Extend(poison,target,poison.Definition.PeriodicDuration,poison.Definition.PeriodicDuration);
            else _world.Periodic.Apply(poison,target,relicApplied:true);
        }
        if(a.Definition.Id=="seriesofshots" && Hook("agent_railgun")>0 && _world.Abilities.Find("interrogationprobe") is {Unlocked:true} source && target.Alive)_world.Periodic.Apply(source,target);
    }
    private void AgentPeriodic(AutomaticAbility a)
    {
        if(!IsAgent || a.Definition.Id is not ("dart" or "corrosivegrenade"))return;
        SniperPoisonTick();
        if(Hook("agent_purpose")>0 && Ready("agent_purpose",2))_world.Abilities.Find("agentcull")?.ReduceCooldown(1);
        if(Hook("agent_enduring")>0 && Ready("agent_enduring",1))_world.PlayerActor.Health.ApplyHealing(2,false,"enduring_kolto");
    }
    private float AgentIncoming=>(_revitalized>0?.9f:1)*(_agentGuard>0?.6f:1)*(AgentPositioned && Hook("agent_dampers")>0?.85f:1);
    private void AdvanceAgent(double delta)
    {
        if(!IsAgent)return;
        _counterstrike=Math.Max(0,_counterstrike-delta);
        foreach(var key in _debilitated.Keys.ToArray()){_debilitated[key]-=delta;if(_debilitated[key]<=0)_debilitated.Remove(key);}
        _agentGuard=Math.Max(0,_agentGuard-delta);
        _revitalized=Math.Max(0,_revitalized-delta);
        if(_koltoLife>0){_koltoLife=Math.Max(0,_koltoLife-delta);_koltoTick+=delta;if(_koltoTick>=1){_koltoTick-=1;_world.PlayerActor.Health.ApplyHealing(4*(1+Sum("healing","koltoprobe")),false,"koltoprobe");}}
        _stationary=_world.Player.ActualVelocity.Length()<.03?_stationary+delta:0;
        AdvanceSniper(delta);
        _agentOpening=Math.Max(0,_agentOpening-delta);_followup=Math.Max(0,_followup-delta);
        _advantageLife=Math.Max(0,_advantageLife-delta);if(_advantageLife==0)TacticalAdvantage=0;
    }
    // Called only by an original enemy attack after health/shield resolution. This
    // damage never enters EnemyAgent.DealAttack, so reflection cannot recurse.
    public void AgentRetaliate(EnemyAgent attacker,float received)
    {
        if(!AgentCounterstrikeActive || received<=0 || !float.IsFinite(received) || !GodotObject.IsInstanceValid(attacker) || !attacker.Alive)return;
        attacker.Health.ApplyDamage(received*1.5f,DamageType.Internal,false,"blowforblow");
    }
    private void ResetAgent(){ResetSniper();TacticalAdvantage=0;_counterstrike=0;_debilitated.Clear();_revitalized=_agentGuard=_koltoLife=_koltoTick=_advantageLife=_followup=_stationary=0;_agentOpening=6;_agentLacerationBonus=0;}
}
