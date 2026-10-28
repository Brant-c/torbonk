using Godot;
using System;
using System.Linq;

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
    public bool InEncounter => State is EncounterState.Introducing or EncounterState.Active;
    public event Action<EncounterState>? StateChanged;
    private double _retry, _introduction;

    public void Tick(double delta, bool due)
    {
        if (!Spawner.Player.Alive) { Fail(); return; }
        if (InEncounter && (!GodotObject.IsInstanceValid(Boss) || Boss!.IsQueuedForDeletion())) { Fail(); return; }
        if (State == EncounterState.Introducing)
        {
            _introduction -= delta;
            if (_introduction <= 0) { Boss!.SetPhysicsProcess(true); Transition(EncounterState.Active); }
        }
        if (State != EncounterState.Scheduled || !due || Spawned) return;
        _retry -= delta;
        if (_retry > 0) return;
        _retry = 1;
        if(Definition.ClearRegularEnemiesOnStart)Spawner.ClearRegularEnemies();
        if (!Spawner.TrySpawn(Definition.Scene, true)) return;
        Boss = Spawner.Enemies.Last(); Spawned = true;
        Boss.Health.Died += Defeat;
        // Brief introduction freezes only this boss. Existing ordinary enemies keep fighting.
        Boss.SetPhysicsProcess(false); _introduction = Math.Clamp(Definition.IntroductionSeconds, 0, 2);
        Transition(EncounterState.Introducing);
    }

    public bool AllowRegularSpawn(int normalCap)
    {
        if (!InEncounter) return true;
        if (State == EncounterState.Introducing || !Definition.SpawnRegularEnemies) return false;
        int cap = RegularEnemyCap(normalCap);
        return Spawner.Enemies.Count(e => GodotObject.IsInstanceValid(e) && e != Boss && e.Alive) < cap;
    }
    private bool FinalBoss => Boss?.GetNodeOrNull<Xrr3Attack>("Xrr3Attack")!=null;
    public int RegularEnemyCap(int normalCap) {
        var mode=Spawner.OperationMode;
        float configured=FinalBoss?mode.FinalBossAddCapFraction:mode.BossAddCapFraction;
        float fraction=Math.Max(Definition.RegularCapFraction,configured);
        return Math.Min(Math.Max(0,Spawner.HardActiveLimit-1),Math.Max(1,(int)Math.Ceiling(normalCap*Math.Clamp(fraction,0,1))));
    }
    public double CadenceMultiplier {
        get {
            if(!InEncounter)return 1;
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
    public void Fail()
    {
        if (State is EncounterState.Defeated or EncounterState.Failed) return;
        if (GodotObject.IsInstanceValid(Boss)) Boss!.GetNodeOrNull<BossEncounter>("BossEncounter")?.End();
        Unsubscribe(); Transition(EncounterState.Failed);
    }
    public void ResetEncounter()
    {
        Unsubscribe();
        if (GodotObject.IsInstanceValid(Boss)) Boss!.GetNodeOrNull<BossEncounter>("BossEncounter")?.End();
        Boss = null; Spawned = false; _retry = 0; _introduction = 0;
        Transition(EncounterState.Scheduled);
    }
    private void Transition(EncounterState state) { State = state; StateChanged?.Invoke(state); }
    private void Unsubscribe() { if (GodotObject.IsInstanceValid(Boss)) Boss!.Health.Died -= Defeat; }
    public override void _ExitTree() => Unsubscribe();
}
