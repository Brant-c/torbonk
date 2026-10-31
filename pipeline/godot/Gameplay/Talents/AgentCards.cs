using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors.Talents;

// Run-card mechanics (snm_ / opm_) shared by Sniper and Operative. Card numbers are rarity-weighted rank power from AgP/AgR.
public sealed partial class TalentRuntime
{
    private double _opGambit;
    private bool _agTickHooked;
    private readonly Dictionary<ulong,double> _opMarks=new();

    private static bool ConcealmentAttack(string id)=>id is "backstab" or "hiddenstrike" or "laceration" or "shiv" or "severtendon";

    private float AgentCardBonus(AutomaticAbility a,Combatant target)
    {
        if(!IsOperative)return 0;
        string id=a.Definition.Id;
        if(!ConcealmentAttack(id))return 0;
        float bonus=0;
        if(_opGambit>0)bonus+=.2f*AgP("gambit");
        if(id is "backstab" or "laceration" && _opMarks.GetValueOrDefault(target.GetInstanceId())>0)bonus+=.15f*AgP("hamstring");
        if(target is EnemyAgent {StunRemaining:>0} || _debilitated.GetValueOrDefault(target.GetInstanceId())>0)bonus+=.15f*AgP("exploit");
        return bonus;
    }
    private void AgentCardsActivated(AutomaticAbility a)
    {
        if(IsOperative && a.Definition.Id=="hiddenstrike" && AgP("gambit")>0)_opGambit=4;
    }
    private void AgentCardsHit(AutomaticAbility a,Combatant t)
    {
        if(!IsAgent)return;
        string id=a.Definition.Id;
        if(id=="agentcull" && t.Alive && AgP("septic") is var septic && septic>0)
            foreach(string poisonId in new[]{"dart","corrosivegrenade"})
                if(_world.Abilities.Find(poisonId) is {} poison && _world.Periodic.Has(poison,t)) {
                    float paid=_world.Periodic.DetonatePartial(poison,t,.35f*septic,6,2);
                    if(paid>0)t.Health.ApplyDamage(paid,DamageType.Internal,false,"agentcull");
                }
        if(IsSniper && id is "interrogationprobe" or "plasmaprobe" or "explosiveprobe" && AgP("probeshield") is var shield && shield>0 && Ready("snm_probeshield",6))
            _world.PlayerActor.Health.ApplyShield(3*shield,6);
        if(!IsOperative)return;
        if(id=="severtendon" && t.Alive && AgP("hamstring")>0)_opMarks[t.GetInstanceId()]=6;
        if(id=="laceration" && t.Alive) {
            for(int i=1;i<=AgR("twin");i++)_agEchoes.Add((.25*i,t,a,.5f));
            if(AgentVenomousBlades && AgP("venomous") is var chance && chance>0 && _world.Abilities.Find("dart") is {} dart) {
                for(int extra=(int)chance+(Random.Shared.NextSingle()<chance-(int)chance?1:0);extra>0;extra--)_world.Periodic.Apply(dart,t,relicApplied:true);
            }
        }
    }
    private void AgentCardsDefeated(Combatant dead)
    {
        if(!IsAgent)return;
        foreach(string poisonId in new[]{"dart","corrosivegrenade"})
            if(_world.Abilities.Find(poisonId) is {} poison && _world.Periodic.Has(poison,dead))Pandemic(poison,dead);
        if(IsOperative) {
            if(_opMarks.Remove(dead.GetInstanceId(),out double left) && left>0.1) {
                var next=_world.Targeting.InArea(dead.GlobalPosition,.35f,1.15f).Where(x=>x!=dead && x.Alive && _opMarks.GetValueOrDefault(x.GetInstanceId())<=0)
                    .OrderBy(x=>x.GlobalPosition.DistanceSquaredTo(dead.GlobalPosition)).FirstOrDefault();
                if(next!=null)_opMarks[next.GetInstanceId()]=left;
            }
            if(AgP("recovery") is var recovery && recovery>0 && Ready("opm_recovery",1))_world.PlayerActor.Health.ApplyHealing(2*recovery,false,"close_quarters_recovery");
        }
    }
    private void Pandemic(AutomaticAbility source,Combatant from)
    {
        int jumps=AgR("pandemic");
        if(jumps<=0)return;
        var next=_world.Targeting.InArea(from.GlobalPosition,.4f,source.Range).Where(x=>x!=from && x.Alive && !_world.Periodic.Has(source,x))
            .OrderBy(x=>x.GlobalPosition.DistanceSquaredTo(from.GlobalPosition)).Take(jumps).ToArray();
        if(next.Length==0 || !Ready("agpandemic_"+source.Definition.Id,2))return;
        foreach(var target in next)_world.Periodic.Apply(source,target,relicApplied:source.Definition.Id=="dart" && AgentVenomousBlades);
    }
    private void AgentTicked(AutomaticAbility source,Combatant target,int tick)
    {
        if(!IsAgent || source.Caster!=_world.PlayerActor || source.Definition.Id is not ("dart" or "corrosivegrenade"))return;
        if(_world.Periodic.Remaining(source,target)<=source.Definition.PeriodicInterval*.5)Pandemic(source,target);
    }
    private void AgentCardsPoisonTick()
    {
        if(AgP("tempo") is var tempo && tempo>0 && Ready("agtempo",.5)){
            _world.Abilities.Find("agentcull")?.ReduceCooldown(.15f*tempo);_world.Abilities.Find("weakeningblast")?.ReduceCooldown(.15f*tempo);
        }
        if(AgP("mending") is var mending && mending>0 && Ready("agmending",1))_world.PlayerActor.Health.ApplyHealing(mending,false,"toxic_mending");
    }
    private void AdvanceAgentCards(double delta)
    {
        if(!_agTickHooked && _world.Periodic!=null){_world.Periodic.Ticked+=AgentTicked;_agTickHooked=true;}
        _opGambit=Math.Max(0,_opGambit-delta);
        foreach(var key in _opMarks.Keys.ToArray()){_opMarks[key]-=delta;if(_opMarks[key]<=0)_opMarks.Remove(key);}
        for(int i=_agEchoes.Count-1;i>=0;i--) {
            var echo=_agEchoes[i];echo.delay-=delta;
            if(echo.delay>0){_agEchoes[i]=echo;continue;}
            _agEchoes.RemoveAt(i);
            if(!GodotObject.IsInstanceValid(echo.target) || !echo.target.Alive || !_world.PlayerActor.Alive)continue;
            echo.target.Health.ApplyDamage(echo.source.DamagePerTick*echo.scale*_world.Build.DamageMultiplier(echo.source,echo.target),echo.source.Definition.DamageType,false,echo.source.Definition.Id);
        }
    }
    private void ResetAgentCards(){_opGambit=0;_opMarks.Clear();_agEchoes.Clear();}
}
