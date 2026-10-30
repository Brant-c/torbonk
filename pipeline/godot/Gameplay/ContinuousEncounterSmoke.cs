using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Real director/spawner transition checks with a persistent living pack.</summary>
public partial class ContinuousEncounterSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string label,bool ok){if(!ok)throw new Exception(label);_checks.Add(label);GD.Print("PRESSURE_CHECK "+label);}
    private async System.Threading.Tasks.Task Frame()=>await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    public async void Run(GameWorld world)
    {
        string? failure=null;
        try
        {
            await Frame();
            var waves=world.Waves!;var spawner=world.Spawner!;
            Check("continuous operation policy loaded",world.PlanetRun!.ContinuousEncounters && waves.Encounter.MaintainRegularPressure);
            waves.SetPhysicsProcess(false);world.Player.SetPhysicsProcess(false);world.Journey?.SetPhysicsProcess(false);
            foreach(var ability in world.Abilities.Abilities)ability.CombatEnabled=false;
            world.Abilities.SetPhysicsProcess(false);world.Combat.SetPhysicsProcess(false);
            world.PlayerActor.Health.Maximum=100000;world.PlayerActor.Health.Restore();
            int level=1;waves.PlayerLevel=()=>level;waves.CanStartMilestone=()=>true;
            waves.ResetWaves();
            var source=GD.Load<PackedScene>(world.PlanetRun.Stages[0].Enemies[0].Scene);
            EnemyAgent SpawnNormal()
            {
                for(int i=0;i<40;i++)if(spawner.TrySpawn(source))
                {var actor=spawner.Enemies.Last();actor.SetPhysicsProcess(false);return actor;}
                throw new Exception("No safe regular spawn: "+spawner.LastPlacement);
            }
            void Freeze(){foreach(var actor in spawner.Enemies.Where(e=>e.Alive))actor.SetPhysicsProcess(false);}
            var pack=Enumerable.Range(0,3).Select(_=>SpawnNormal()).ToArray();
            pack[0].Health.ApplyDamage(1);float wounded=pack[0].Health.Current;
            bool PackAlive()=>pack.All(e=>GodotObject.IsInstanceValid(e) && e.Alive && !e.IsQueuedForDeletion());
            level=4;waves._PhysicsProcess(.01);Freeze();
            Check("roster change has no recovery or ramp",!waves.Recovering && waves.ReturnRampRemaining==0 && spawner.ReinforcementsEnabled);
            Check("old-stage mobs remain alive and wounded",PackAlive() && pack[0].Health.Current==wounded);
            level=10;
            for(int i=0;i<50 && !waves.Encounter.Spawned;i++){waves._PhysicsProcess(.1);Freeze();await Frame();}
            Check("first boss enters alongside existing pack",waves.Encounter.Spawned && PackAlive());
            Check("regular spawn allowed during boss introduction",waves.Encounter.State==EncounterState.Introducing && waves.Encounter.AllowRegularSpawn(spawner.ActiveLimit));
            int before=spawner.Spawned;
            waves._PhysicsProcess(waves.SpawnInterval+.05);Freeze();
            Check("director keeps spawning through introduction",spawner.Spawned>before);
            Check("no boss reinforcement slowdown",waves.Encounter.CadenceMultiplier==1);
            Check("boss keeps full mob budget",waves.Encounter.RegularEnemyCap(spawner.ActiveLimit)==Math.Min(spawner.ActiveLimit,spawner.HardActiveLimit)-1);
            waves.Encounter.Boss!.Health.ApplyDamage(waves.Encounter.Boss.Health.Maximum*2);
            waves._PhysicsProcess(.01);Freeze();
            Check("boss death keeps the same living pack",waves.MilestonesCompleted==1 && PackAlive());
            Check("boss death has no recovery break",!waves.Recovering && waves.ReturnRampRemaining==0 && spawner.ReinforcementsEnabled);
            before=spawner.Spawned;
            for(int i=0;i<10 && spawner.Spawned==before;i++){waves._PhysicsProcess(waves.SpawnInterval+.05);Freeze();await Frame();}
            Check("regular waves continue immediately after boss",spawner.Spawned>before);
            level=25;
            for(int i=0;i<50 && waves.MilestonesStarted<2;i++){waves._PhysicsProcess(.2);Freeze();await Frame();}
            Check("second encounter has no stage-transition break",waves.MilestonesStarted==2 && !waves.Recovering && PackAlive());
            if(world.RunConfiguration.MapId=="soa_p3")
            {
                for(int i=0;i<30 && waves.Encounter.ActiveMembers.Count<3;i++){waves._PhysicsProcess(.2);Freeze();await Frame();}
                Check("Council fills three boss slots without clearing pack",waves.Encounter.ActiveMembers.Count==3 && PackAlive());
                Check("Council permits ordinary reinforcements",waves.Encounter.AllowRegularSpawn(spawner.ActiveLimit));
                before=spawner.Spawned;SpawnNormal();
                Check("Council regular spawn reaches real spawner",spawner.Spawned>before);
                Check("Council members are counted as occupied reservations",waves.Encounter.ActiveMembers.All(m=>m.IsEncounterMember) && spawner.ReservedSlots==3);
                for(int i=0;i<120 && spawner.ActiveCount<spawner.HardActiveLimit;i++)
                    if(spawner.TrySpawn(source))Freeze();
                Check("Council and regular pack can fill the difficulty cap",spawner.ActiveCount==spawner.HardActiveLimit);
                var expendable=spawner.Enemies.First(e=>e.Alive && !e.IsEncounterMember && !pack.Contains(e));
                expendable.Health.ApplyDamage(expendable.Health.Maximum*2);
                Check("natural mob death opens reinforcement slot",waves.Encounter.AllowRegularSpawn(spawner.ActiveLimit));
                SpawnNormal();
                Check("occupied boss reservations are not double counted",spawner.ActiveCount==spawner.HardActiveLimit && PackAlive());
                for(int i=0;i<100 && waves.Encounter.InEncounter;i++)
                {
                    foreach(var member in waves.Encounter.ActiveMembers.ToArray())member.Health.ApplyDamage(member.Health.Maximum*2);
                    waves._PhysicsProcess(.1);Freeze();await Frame();
                }
                Check("Council completes while ordinary pack persists",waves.MilestonesCompleted==2 && PackAlive());
                Check("Council exits without recovery or leftover slot reservation",!waves.Recovering && waves.ReturnRampRemaining==0 && spawner.ReservedSlots==1);
            }
            // Reach the finale without despawning any regulars at a milestone boundary.
            level=50;
            for(int milestone=waves.MilestonesCompleted;milestone<world.PlanetRun.Milestones.Length;milestone++)
            {
                for(int i=0;i<50 && waves.MilestonesStarted<=milestone;i++){waves._PhysicsProcess(.2);Freeze();await Frame();}
                Check("late boss enters with living pack "+milestone,waves.MilestonesStarted==milestone+1 && PackAlive());
                var boss=waves.Encounter.Boss!;boss.Health.ClearShield();boss.Health.ApplyDamage(boss.Health.Maximum*2);
                waves._PhysicsProcess(.01);Freeze();
            }
            Check("final victory remains finite",waves.CoreComplete && !waves.Running);
            world.ResetCombatWorld();
            Check("restart resets pressure policy and reservation",waves.Running && !waves.Recovering && waves.Encounter.MaintainRegularPressure && spawner.ReservedSlots==1);
        }
        catch(Exception e){failure=e.ToString();GD.PrintErr(e);}
        using(var file=FileAccess.Open($"res://../manifests/continuous-{world.RunConfiguration.MapId}-{world.RunConfiguration.OperationDifficulty}.json",FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure}));
        GD.Print(failure==null?"CONTINUOUS_ENCOUNTER_PASSED":"CONTINUOUS_ENCOUNTER_FAILED");GetTree().Quit(failure==null?0:1);
    }
}
