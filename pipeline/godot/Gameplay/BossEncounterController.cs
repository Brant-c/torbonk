using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SwtorSurvivors;

public enum EncounterState { Scheduled, Introducing, Active, Defeated, Failed }

/// <summary>Owns one encounter lifecycle. Never awards XP: EnemySpawner owns the single reward path.</summary>
public partial class BossEncounterController : Node
{
    [Export] public BossEncounterDefinition Definition { get; set; } = null!;
    public EnemySpawner Spawner { get; set; } = null!;
    public EncounterState State { get; private set; } = EncounterState.Scheduled;
    public EnemyAgent? Boss { get; private set; }
    public bool Spawned { get; private set; }
    public bool MaintainRegularPressure {get;set;}
    public bool InEncounter => State is EncounterState.Introducing or EncounterState.Active;
    public event Action<EncounterState>? StateChanged;
    private double _retry, _introduction;
    private readonly List<EnemyAgent> _members = new();
    private int _nextMember;
    private bool Group => Definition.AdditionalScenes.Count > 0;
    public int MembersRemaining => Group ? _members.Count + Definition.AdditionalScenes.Count - _nextMember : Boss?.Alive == true ? 1 : 0;
    public IReadOnlyList<EnemyAgent> ActiveMembers => _members;

    public void Tick(double delta, bool due)
    {
        if (!Spawner.Player.Alive) { Fail(); return; }
        if (InEncounter && (Group ? _members.Any(m=>!GodotObject.IsInstanceValid(m) || m.IsQueuedForDeletion()) : !GodotObject.IsInstanceValid(Boss) || Boss!.IsQueuedForDeletion())) { Fail(); return; }
        if (InEncounter && Group && _nextMember < Definition.AdditionalScenes.Count
            && _members.Count < Math.Max(1,Definition.MaximumConcurrentMembers))
        {
            if(Spawner.TrySpawn(Definition.AdditionalScenes[_nextMember],true,encounterMember:true))
            {
                _nextMember++;
                var member=Spawner.Enemies.Last();RegisterMember(member);
                if(State==EncounterState.Introducing)member.SetPhysicsProcess(false);
                if(Boss==null || !Boss.Alive)Boss=member;
            }
        }
        if (State == EncounterState.Introducing)
        {
            _introduction -= delta;
            if (_introduction <= 0) {
                if(Group)foreach(var member in _members)member.SetPhysicsProcess(true);
                else Boss!.SetPhysicsProcess(true);
                Transition(EncounterState.Active);
            }
        }
        if (State != EncounterState.Scheduled || !due || Spawned) return;
        _retry -= delta;
        if (_retry > 0) return;
        _retry = 1;
        if(Definition.ClearRegularEnemiesOnStart && !MaintainRegularPressure)Spawner.ClearRegularEnemies();
        if (!Spawner.TrySpawn(Definition.Scene, true,encounterMember:true)) return;
        Boss = Spawner.Enemies.Last(); Spawned = true;
        if(Group)RegisterMember(Boss);else Boss.Health.Died += Defeat;
        // Brief introduction freezes only this boss. Existing ordinary enemies keep fighting.
        Boss.SetPhysicsProcess(false); _introduction = Math.Clamp(Definition.IntroductionSeconds, 0, 2);
        Transition(EncounterState.Introducing);
    }

    public bool AllowRegularSpawn(int normalCap)
    {
        if (!InEncounter) return true;
        if(MaintainRegularPressure)
            return Spawner.Enemies.Count(e=>GodotObject.IsInstanceValid(e) && e.Alive && !e.IsEncounterMember)<RegularEnemyCap(normalCap);
        if (State == EncounterState.Introducing || !Definition.SpawnRegularEnemies) return false;
        int cap = RegularEnemyCap(normalCap);
        return Spawner.Enemies.Count(e => GodotObject.IsInstanceValid(e) && e != Boss && !_members.Contains(e) && e.Alive) < cap;
    }
    private bool FinalBoss => Boss?.GetNodeOrNull<Xrr3Attack>("Xrr3Attack")!=null || Boss?.GetNodeOrNull<SoaAttack>("SoaAttack") is {Image:false};
    public int RegularEnemyCap(int normalCap) {
        if(MaintainRegularPressure)return Math.Max(0,Math.Min(normalCap,Spawner.HardActiveLimit)-Spawner.ReservedSlots);
        var mode=Spawner.OperationMode;
        float configured=FinalBoss?mode.FinalBossAddCapFraction:mode.BossAddCapFraction;
        float fraction=Math.Max(Definition.RegularCapFraction,configured);
        return Math.Min(Math.Max(0,Spawner.HardActiveLimit-1),Math.Max(1,(int)Math.Ceiling(normalCap*Math.Clamp(fraction,0,1))));
    }
    public double CadenceMultiplier {
        get {
            if(!InEncounter)return 1;
            if(MaintainRegularPressure)return 1;
            var mode=Spawner.OperationMode;
            float configured=FinalBoss?mode.FinalBossAddCadence:mode.BossAddCadence;
            return Math.Max(1,configured>0?configured:Definition.RegularSpawnIntervalMultiplier);
        }
    }

    private void Defeat()
    {
        if (!InEncounter) return;
        Boss?.GetNodeOrNull<BossEncounter>("BossEncounter")?.End();
        Unsubscribe(); Transition(EncounterState.Defeated);
    }
    private readonly Dictionary<EnemyAgent,Action> _memberDeaths=new();
    private void RegisterMember(EnemyAgent member)
    {
        _members.Add(member);
        member.RewardTier=1;member.GuaranteedReward=false;
        Action died=()=>{
            _members.Remove(member);
            member.Health.Died-=_memberDeaths[member];_memberDeaths.Remove(member);
            if(Boss==member)Boss=_members.FirstOrDefault()??member;
            RefreshGroupReward();
            if(_members.Count==0 && _nextMember==Definition.AdditionalScenes.Count)Defeat();
        };
        _memberDeaths[member]=died;member.Health.Died+=died;
        RefreshGroupReward();
    }
    private void RefreshGroupReward()
    {
        if(_nextMember==Definition.AdditionalScenes.Count && _members.Count==1)
        {_members[0].RewardTier=2;_members[0].GuaranteedReward=true;}
    }
    public void Fail()
    {
        if (State is EncounterState.Defeated or EncounterState.Failed) return;
        if (GodotObject.IsInstanceValid(Boss)) Boss!.GetNodeOrNull<BossEncounter>("BossEncounter")?.End();
        Unsubscribe(); Transition(EncounterState.Failed);
    }
    public void ResetEncounter()
    {
        Unsubscribe();
        _members.Clear();_nextMember=0;
        if (GodotObject.IsInstanceValid(Boss)) Boss!.GetNodeOrNull<BossEncounter>("BossEncounter")?.End();
        Boss = null; Spawned = false; _retry = 0; _introduction = 0;
        Transition(EncounterState.Scheduled);
    }
    private void Transition(EncounterState state) { State = state; StateChanged?.Invoke(state); }
    private void Unsubscribe() {
        if (GodotObject.IsInstanceValid(Boss)) Boss!.Health.Died -= Defeat;
        foreach(var pair in _memberDeaths)if(GodotObject.IsInstanceValid(pair.Key))pair.Key.Health.Died-=pair.Value;
        _memberDeaths.Clear();
    }
    public override void _ExitTree() => Unsubscribe();
}
