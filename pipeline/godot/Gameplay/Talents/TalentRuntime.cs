using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors.Talents;

/// <summary>Validated immutable run snapshot; all modifiers are derived, never accumulated.</summary>
public sealed partial class TalentRuntime
{
    private readonly GameWorld _world;
    public IReadOnlyDictionary<string,int> Ranks {get;}
    private readonly (TalentEffect effect,int rank)[] _effects;
    private readonly Dictionary<string,double> _locks=new();
    private readonly Dictionary<string,int> _counts=new();
    private readonly Dictionary<ulong,double> _deathmarks=new();
    private double _barrage;
    private bool _barrier;
    public TalentRuntime(GameWorld world)
    {
        _world=world;
        Ranks=TalentProfiles.Load(world.RunConfiguration.AdvancedClassId);
        _effects=TalentCatalog.Current.ForClass(world.RunConfiguration.AdvancedClassId)
            .SelectMany(t=>t.Effects.Select(e=>(e,Ranks.GetValueOrDefault(t.Id)))).Where(p=>p.Item2>0).ToArray();
    }
    public float Sum(string kind,string ability="",string key="")=>_effects
        .Where(p=>p.effect.Kind==kind && (p.effect.Ability=="" || p.effect.Ability==ability) && (key=="" || p.effect.Key==key))
        .Sum(p=>p.effect.Amount*p.rank);
    public int Mechanic(string key)=>(int)Sum("mechanic",key:key);
    private float Hook(string key)=>Sum("hook",key:key);
    private bool Ready(string key,double seconds)
    {
        if(_locks.GetValueOrDefault(key)>0)return false;
        _locks[key]=seconds;return true;
    }
    private bool Every(string key,int count)
    {int next=_counts.GetValueOrDefault(key)+1;_counts[key]=next;return next%count==0;}
    public void ApplyStats(AutomaticAbility a)
    {
        string id=a.Definition.Id;
        if(IsAgent && id=="agentcull")a.Range=IsOperative?1.15f:2.4f;
        a.DamagePerTick*=1+Sum("damage",id);
        AgentStats(a);AssassinStats(a);JuggernautStats(a);MarauderStats(a);SniperStats(a);
        a.TickSeconds/=Acceleration;
        a.TickSeconds/=1+(_barrage>0 && id=="lightning"?Hook("barrage"):0);
        a.CooldownSeconds/=Acceleration;
        a.CooldownSeconds/=1+Sum("cooldown",id);
        a.Range*=1+Sum("range",id);
        a.Radius*=1+Sum("radius",id);
        a.Healing*=1+Sum("healing",id);
        a.ShieldCapacity*=1+Sum("shield",id);
        a.ExtraBounces+=(int)Sum("bounces",id);
    }
    public float PeriodicDuration(AutomaticAbility a)=>a.Definition.PeriodicDuration+Sum("duration",a.Definition.Id)+(AssassinDeception && a.Definition.Id=="discharge"?2:0);
    public float PeriodicInterval(AutomaticAbility a)=>a.Definition.PeriodicInterval/(1+Sum("periodic_rate",a.Definition.Id))
        *a.TickSeconds/Math.Max(.001f,a.Definition.TickSeconds);
    public bool PolarityReady=>Hook("polarity")>0 || _world.Build.Talent("polarity")>0;
    public bool PolarityActive=>_polarity>0;
    private double _polarity;
    public float Acceleration=>_polarity>0?1.25f:1;
    public float IncomingMultiplier=>AgentIncoming*MarauderIncoming*JuggernautIncoming*AssassinIncoming*(_world.PlayerActor.Health.Current<_world.PlayerActor.Health.Maximum*.35f?1-Math.Min(.2f,Hook("low_defense")):1);
    public float DirectBonus(AutomaticAbility a,Combatant target)=>AgentBonus(a,target)+MarauderBonus(a,target)+JuggernautBonus(a,target)+AssassinBonus(a,target)+SniperBonus(a,target)+(
        a.Definition.Id=="thunderingblast" && _world.Abilities.Find("affliction") is {} affliction && _world.Periodic.Has(affliction,target)?Hook("afflicted_blast"):0);
    public float PeriodicMultiplier(Combatant target)=>1+(_deathmarks.GetValueOrDefault(target.GetInstanceId())>0?(Hook("deathmark")+(IsAssassin?.1f:0)):0);
    public void Activated(AutomaticAbility a)
    {
        AgentActivated(a);AssassinActivated(a);JuggernautActivated(a);MarauderActivated(a);SniperActivated(a);
        if(a.Definition.Id=="polarityshift"){_polarity=6;_world.Progression?.Recalculate();return;}
        if(a.Definition.Id!="barrier")return;
        _barrier=true;
        if(Hook("barrier_heal")>0 && _world.PlayerActor.Health.Current<_world.PlayerActor.Health.Maximum*.5f && Ready("resurgence",12))
            _world.PlayerActor.Health.ApplyHealing(Hook("barrier_heal"));
        _world.Abilities.Find("darkheal")?.ReduceCooldown(Hook("barrier_mending"));
    }
    public void Damaged(float amount)
    {
        AgentDamaged();JuggernautDamaged(amount);MarauderDamaged(amount);
        var health=_world.PlayerActor.Health;
        if(IsAssassin && health.IsAlive && Hook("reactive")>0 && Ready("assassin_reactive",5))health.ApplyHealing(Hook("reactive"));
        if(health.IsAlive && Hook("emergency")>0 && health.Current<health.Maximum*.25f && Ready("emergency",30))
        {health.ApplyHealing(12);_world.Abilities.Find("barrier")?.ReduceCooldown(4);}
    }
    public void ShieldEnded(bool depleted)
    {
        bool barrier=_barrier;_barrier=false;
        if(!barrier || !depleted || Hook("shield_recovery")==0 || !_world.PlayerActor.Alive || !Ready("reconstruct",20))return;
        _world.PlayerActor.Health.ApplyHealing(6);
        foreach(var enemy in _world.Targeting.InArea(_world.PlayerActor.GlobalPosition,.35f,.35f).ToArray())
            enemy.Health.ApplyDamage(8,DamageType.Energy);
    }
    public void DirectHit(AutomaticAbility a,Combatant target)
    {
        AgentHit(a,target);AssassinHit(a,target);JuggernautHit(a,target);MarauderHit(a,target);SniperHit(a,target);
        string id=a.Definition.Id;
        if(id=="shock" && Hook("shock_extend")>0 && _world.Abilities.Find("affliction") is {} affliction)
            _world.Periodic.Extend(affliction,target,Hook("shock_extend"),PeriodicDuration(affliction)+4);
        if(id=="deathfield" && target.Alive && (IsAssassin || Hook("deathmark")>0))_deathmarks[target.GetInstanceId()]=6;
    }
    public void Hit(AutomaticAbility a)
    {
        string id=a.Definition.Id;
        if(id is "lightning" or "shock" or "chain" or "storm" or "thunderingblast" or "lightningstrike")
            if(Hook("conduction")>0 && Every("conduction",6) && Ready("conduction",1))
                _world.Abilities.Find("chain")?.ReduceCooldown(Hook("conduction"));
        if(id is "shock" or "storm" or "lightningstrike")
            if(Hook("lightning_storm")>0 && Every("storm",6) && Ready("storm",8))
                _world.Abilities.Find("chain")?.ReduceCooldown(Hook("lightning_storm"));
        if(id=="lightning" && Hook("wrath")>0 && Every("wrath",6) && Ready("wrath",1))
        {
            _world.Abilities.Find("affliction")?.ReduceCooldown(Hook("wrath"));
            _world.Abilities.Find("creepingterror")?.ReduceCooldown(Hook("wrath"));
            _world.Abilities.Find("crushingdarkness")?.ReduceCooldown(Hook("wrath"));
        }
    }
    public void PeriodicTick(AutomaticAbility a)
    {
        if(a.Caster!=_world.PlayerActor)return;
        AgentPeriodic(a);JuggernautPeriodic(a);MarauderPeriodic(a);
        if(IsAssassin && Hook("drain")>0 && Ready("assassin_drain",1.5))_world.PlayerActor.Health.ApplyHealing(Hook("drain"));
        var health=_world.PlayerActor.Health;
        if(Hook("periodic_heal")>0 && health.Current<health.Maximum*.5f && Ready("parasitism",2))
            health.ApplyHealing(Hook("periodic_heal"));
        if(a.Definition.Id=="affliction" && Hook("barrage")>0 && Ready("barrage",10))
        {_barrage=4;_world.Progression?.Recalculate();}
    }
    public IEnumerable<UpgradeDefinition> RunUpgrades()
    {
        foreach(var t in TalentCatalog.Current.Talents.Where(t=>Ranks.GetValueOrDefault(t.Id)>0))
            foreach(var e in t.Effects.Where(e=>e.Kind=="upgrade"))
                if(_world.Abilities.Find(e.Ability) is {} a)
                    yield return new UpgradeDefinition {Id="talent_training_"+t.Id,Title=t.Name,
                        ChoiceDescription=$"Increase {a.Definition.Title} damage by 15% of base per rank.",
                        Icon=a.Definition.Icon,AbilityId=e.Ability,Effect=UpgradeEffect.AbilityPower,
                        DamagePerRank=e.Amount,MaximumRank=2,Rarity=PowerRarity.Premium};
    }
    public float OfferWeight(UpgradeDefinition u)
    {
        if(IsAgent)return AgentOfferWeight(u);
        if(IsJuggernaut)return JuggernautOfferWeight(u);
        if(IsMarauder){
            string mrTree=MarauderUpgradeTree(u);
            if(mrTree!="")return 1+.4f*MrPoints(mrTree);
        }
        if(IsAssassin && u.AbilityId=="creepingterror" && Mechanic("early_terror")>0)return 5;
        // Modest preference only: off-tree unlocks retain full eligibility.
        float affinity=_effects.Where(p=>p.effect.Ability!="" && p.effect.Ability==u.AbilityId).Sum(p=>p.rank);
        string tree=u.AbilityId switch {"lightningstrike" or "shock" or "chain" or "storm" or "thunderingblast" or "polarityshift"=>"lightning", "affliction" or "crushingdarkness" or "creepingterror" or "deathfield"=>"madness",_=>""};
        int investment=TalentCatalog.Current.ForClass(_world.RunConfiguration.AdvancedClassId).Where(t=>t.Tree==tree).Sum(t=>Ranks.GetValueOrDefault(t.Id));
        return 1+Math.Min(1.2f,affinity*.1f+investment*.08f);
    }
    public void Advance(double delta)
    {
        AdvanceAgent(delta);AdvanceMarauder(delta);AdvanceAssassin(delta);AdvanceJuggernaut(delta);
        if(_polarity>0){_polarity=Math.Max(0,_polarity-delta);if(_polarity==0)_world.Progression?.Recalculate();}
        foreach(var key in _locks.Keys.ToArray())_locks[key]=Math.Max(0,_locks[key]-delta);
        foreach(var key in _deathmarks.Keys.ToArray()){_deathmarks[key]-=delta;if(_deathmarks[key]<=0)_deathmarks.Remove(key);}
        if(_barrage>0){_barrage=Math.Max(0,_barrage-delta);if(_barrage==0)_world.Progression?.Recalculate();}
    }
    public void Reset(){ResetAgent();ResetMarauder();ResetJuggernaut();ResetAssassin();_locks.Clear();_counts.Clear();_deathmarks.Clear();_barrage=_polarity=0;_barrier=false;}
}
