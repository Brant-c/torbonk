using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Focused integration checks for spawn snapshots, population bounds and travel recovery.</summary>
public partial class DifficultySmoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool pass)
    {
        if (!pass) throw new InvalidOperationException(name);
        _checks.Add(name); GD.Print("DIFFICULTY: " + name);
    }
    private async Task Frames(int count=1)
    { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }

    public async void Run(GameWorld world)
    {
        string? error=null;
        try
        {
            await Frames(2);
            world.ResetCombatWorld();
            var waves=world.Waves!; var spawner=world.Spawner!; var run=world.PlanetRun!;
            waves.SetPhysicsProcess(false);world.Player.SetPhysicsProcess(false);
            world.Abilities.ProcessMode=ProcessModeEnum.Disabled;world.Periodic.SetPhysicsProcess(false);
            world.Journey!.SetPhysicsProcess(false);
            int level=1; bool choosing=false;
            waves.PlayerLevel=()=>level; waves.CanStartMilestone=()=>!choosing;
            Check("population curve reaches the shared 100 cap",new[]{1,5,10,20,30}.Select(run.Difficulty.Population).SequenceEqual(new[]{16,28,43,73,100}));
            Check("population remains bounded above level cap",run.Difficulty.Population(int.MaxValue)==100);
            Check("cadence reaches 0.15 seconds at level 30",Math.Abs(run.Difficulty.SpawnInterval(30,30)-.15)<.0001);
            var path=run.Stages[0].Enemies[0].Scene;
            var scene=GD.Load<PackedScene>(path);
            EnemyAgent Spawn(bool required=false)
            {
                for(int i=0;i<100;i++)
                    if(spawner.TrySpawn(scene,required))
                    {var enemy=spawner.Enemies.Last();enemy.SetPhysicsProcess(false);return enemy;}
                throw new InvalidOperationException("No safe placement for fixture");
            }
            var first=Spawn();float baseline=first.Health.Maximum, speed=first.MoveSpeed, interval=first.AttackInterval;
            first.Health.ApplyDamage(1);float injured=first.Health.Current;
            level=30;waves.ApplyPlanetPressure();
            var high=Spawn();
            Check("health follows level curve",Math.Abs(high.Health.Maximum/baseline-Math.Pow(1.08,29))<.001);
            Check("speed and attack cadence have gentle caps",Math.Abs(high.MoveSpeed/speed-1.25)<.001 && Math.Abs(interval/high.AttackInterval-1.145)<.001);
            Check("existing injured enemy retains spawn stats",first.SpawnLevel==1 && first.Health.Current==injured && first.Health.Maximum==baseline && first.MoveSpeed==speed);
            int xp=high.ExperienceReward;
            Check("XP reward does not scale",xp==first.ExperienceReward);
            world.PlayerActor.Health.SetMaximum(10000,10000);
            float before=world.PlayerActor.Health.Current;
            high.DealAttack(10);
            Check("special attack path scales outgoing damage exactly once",Math.Abs(before-world.PlayerActor.Health.Current-23.05)<.01);

            // A later species already carries its authored baseline at its introduction level.
            var later=spawner.ReferenceLevels.First(p=>p.Value>1 && run.Stages.SelectMany(s=>s.Enemies).Any(e=>e.Scene==p.Key));
            level=later.Value;var lateScene=GD.Load<PackedScene>(later.Key);
            var raw=lateScene.Instantiate();float authored=raw.GetNode<Health>("Actor/Health").Maximum;raw.Free();
            Check("later species safely spawns",spawner.TrySpawn(lateScene));
            var late=spawner.Enemies.Last();late.SetPhysicsProcess(false);
            Check("later species keeps authored baseline at reference level",late.ReferenceLevel==level && late.Health.Maximum==authored);

            spawner.ResetSpawner();level=30;waves.ApplyPlanetPressure();
            spawner.HardActiveLimit=3;spawner.ReservedSlots=1;
            Spawn();Spawn();
            Check("ambient spawns leave encounter reservation",!spawner.TrySpawn(scene));
            Spawn(true);
            Check("required encounters cannot exceed hard population cap",spawner.ActiveCount==3 && !spawner.TrySpawn(scene,true));

            spawner.ResetSpawner();spawner.HardActiveLimit=100;level=1;waves.ResetWaves();
            level=6;waves._PhysicsProcess(.1);
            Check("stage change starts one 20-second recovery",waves.Recovering && Math.Abs(waves.RecoveryRemaining-20)<.01);
            double clock=waves.WaveRemaining;int spawned=spawner.Spawned;
            choosing=true;waves._PhysicsProcess(8);
            Check("reward screens preserve recovery time",Math.Abs(waves.RecoveryRemaining-20)<.01);
            choosing=false;waves._PhysicsProcess(5);
            Check("recovery freezes waves and ordinary reinforcements",waves.WaveRemaining==clock && spawner.Spawned==spawned && !spawner.TrySpawn(scene));
            level=11;waves._PhysicsProcess(1);
            Check("overlapping stage unlock does not stack another break",Math.Abs(waves.RecoveryRemaining-14)<.01);
            if(DisplayServer.GetName()!="headless")
            {
                world.Journey._PhysicsProcess(.5);
                await Frames(2);
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../generated/difficulty-{world.RunConfiguration.MapId}-recovery.png"));
                if(OS.GetCmdlineUserArgs().Contains("--difficulty-preview"))
                {GD.Print("DIFFICULTY_PREVIEW_SAVED");GetTree().Quit();return;}
            }
            var destination=waves.TravelDestination!;
            var saved=world.Player.GlobalPosition;
            world.Player.GlobalPosition=new Vector3(destination.Focus[0]+2,destination.Focus[1],destination.Focus[2]);
            Check("early start requires reaching destination",!waves.BeginNextPhase());
            world.Player.GlobalPosition=new Vector3(destination.Focus[0],destination.Focus[1],destination.Focus[2]);
            Check("destination is supported ground",spawner.Navigation.TryStandingPoint(world.Player.GlobalPosition,out _));
            Check("early start enables eight-second spawn ramp",waves.BeginNextPhase() && !waves.Recovering && waves.ReturnRampRemaining==8 && spawner.ReinforcementsEnabled);
            world.Player.GlobalPosition=saved;

            // Exercise the actual boss death transition and pending higher milestones.
            level=1;waves.ResetWaves();level=run.Milestones[0].Level;
            waves.TickMilestones(.1);waves.TickMilestones(2);
            Check("required boss starts",waves.Encounter.InEncounter);
            var boss=waves.Boss!;boss.SetPhysicsProcess(false);
            Check("boss baseline uses intended milestone level",boss.ReferenceLevel==run.Milestones[0].Level && boss.LevelDamageMultiplier==1);
            level=30;waves._PhysicsProcess(.1);
            Check("stage changes do not interrupt active encounter",!waves.Recovering);
            boss.Health.ApplyDamage(boss.Health.Maximum+1);waves.TickMilestones(.1);
            Check("boss death stops reinforcements and starts recovery",waves.Recovering && !spawner.ReinforcementsEnabled);
            int started=waves.MilestonesStarted;waves.TickMilestones(5);
            Check("pending bosses wait through recovery",waves.MilestonesStarted==started);
            world.Progression!.CancelPresentation();world.Artifacts!.Clear();
            waves.TickMilestones(30);
            Check("natural recovery expiry starts ramp",!waves.Recovering && waves.ReturnRampRemaining==8);
            foreach(var milestone in run.Milestones.Skip(1))
            {
                for(int retry=0;retry<10 && !waves.Encounter.InEncounter;retry++)waves.TickMilestones(1.1);
                Check("serialized encounter starts at level "+milestone.Level,waves.Encounter.InEncounter && waves.Boss!.ReferenceLevel==milestone.Level);
                var current=waves.Boss!;current.SetPhysicsProcess(false);
                waves.TickMilestones(2);
                current.Health.ApplyDamage(current.Health.Maximum+1);waves.TickMilestones(.1);
                world.Artifacts.Clear();
                if(!waves.CoreComplete)waves.TickMilestones(21);
            }
            Check("all milestones complete without overlapping encounters",waves.CoreComplete && waves.MilestonesStarted==run.Milestones.Length && !waves.Running);

            level=1;world.ResetCombatWorld();waves.SetPhysicsProcess(false);
            Check("restart clears recovery, ramp and population",!waves.Recovering && waves.ReturnRampRemaining==0 && spawner.ActiveCount==0 && spawner.ActiveLimit==16 && spawner.ReinforcementsEnabled);
            // Real animated population at the agreed cap, with the required slot included.
            level=30;waves.ApplyPlanetPressure();spawner.MinimumSpawnDistance=.25f;spawner.MaximumSpawnDistance=1.8f;spawner.PlacementAttempts=64;
            for(int i=0;i<1200 && spawner.ActiveCount<99;i++)spawner.TrySpawn(scene);
            Check("99 animated ordinary enemies can be placed",spawner.ActiveCount==99);
            Spawn(true);
            Check("100 living enemies remain a hard ceiling",spawner.ActiveCount==100 && !spawner.TrySpawn(scene,true));
            world.PlayerActor.Health.SetMaximum(100000,100000);
            await Frames(120);
            Check("full crowd remains active through physics frames",spawner.ActiveCount==100 && spawner.Enemies.All(e=>e.Health.IsAlive));
            world.ResetCombatWorld();
        }
        catch(Exception exception){error=exception.ToString();GD.PushError(error);}
        using var report=FileAccess.Open($"res://../manifests/difficulty-{world.RunConfiguration.MapId}-smoke.json",FileAccess.ModeFlags.Write);
        report.StoreString(JsonSerializer.Serialize(new {passed=error==null,checks=_checks,error},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print(error==null?"DIFFICULTY_SMOKE_PASSED":"DIFFICULTY_SMOKE_FAILED");
        GetTree().Quit(error==null?0:1);
    }
}
