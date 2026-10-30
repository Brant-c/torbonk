using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Production-path level, roster, milestone, telegraph and restart checks.</summary>
public partial class EternityVaultEncounterSmoke : Node
{
    private readonly List<string> _failures=new();
    private void Check(string name,bool ok){if(!ok){_failures.Add(name);GD.Print("EV_CHECK_FAILED "+name);}}
    public async void Run(GameWorld w) {
        try {
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            Check("level one start and cap fifty",w.Progression!.Level==1 && w.Progression.MaximumLevel==50);
            Check("combat encounter mode",!w.ExplorationOnly && w.PlanetRun!=null && w.Waves!=null);
            var waves=w.Waves!;var spawner=w.Spawner!;var p=w.Progression;
            waves.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
            foreach(var ability in w.Abilities.Abilities)ability.CombatEnabled=false;
            w.Abilities.SetPhysicsProcess(false);w.Combat.SetPhysicsProcess(false);
            waves.CanStartMilestone=()=>true;
            Check("double enemy XP configured",spawner.ExperienceMultiplier==2 && spawner.ExperienceDropEvery==2);
            int awarded=0;Action<Vector3,int> reward=(point,value)=>awarded+=value;spawner.EnemyReward+=reward;
            for(int sample=0;sample<2;sample++) {
                bool spawned=false;
                for(int attempt=0;attempt<16 && !spawned;attempt++)spawned=spawner.TrySpawn(GD.Load<PackedScene>("res://Gameplay/Planets/EternityVault/IceCat.tscn"),ignoreLimit:true);
                Check("XP sample spawn "+sample,spawned);if(!spawned)break;
                var enemy=spawner.Enemies.Last();Check("XP reward doubled",enemy.ExperienceReward==8);
                enemy.Health.ApplyDamage(enemy.Health.Maximum*2);
            }
            spawner.EnemyReward-=reward;Check("two kills award eight XP instead of four",awarded==8);
            Check("wilderness B then A then prison guards",w.PlanetRun!.Stages[0].SourceEncounter.StartsWith("b:") && w.PlanetRun.Stages[1].SourceEncounter.StartsWith("a:") && w.PlanetRun.Stages[2].SourceEncounter.EndsWith("enc_trash_prison_guards"));
            Check("Korriban scaling coefficients",w.PlanetRun.Difficulty.HealthPerLevel==.08f && w.PlanetRun.Difficulty.DamagePerLevel==.045f && w.PlanetRun.ExperienceGrowth==1.075f);
            for(int level=2;level<=50;level++) {
                p.AddExperience(p.RequiredExperience);p._Process(10);
                Check("level "+level,p.Level==level && p.CanChoose && p.Offers.Count>0);
                if(p.CanChoose)p.Choose(0);
                if(p.Replacing)p.Replace(0);
                p._Process(10);w.Artifacts?.Clear();
            }
            p.AddExperience(int.MaxValue);Check("hard cap",p.Level==50 && p.Experience==0);
            w.PlayerActor.Health.Maximum=100000;w.PlayerActor.Health.Restore();
            foreach(var stage in w.PlanetRun.Stages) foreach(var entry in stage.Enemies)Check("scene "+entry.Scene,ResourceLoader.Exists(entry.Scene));
            spawner.ClearRegularEnemies();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            for(int milestone=0;milestone<4;milestone++) {
                for(int retry=0;retry<16 && waves.MilestonesStarted<=milestone;retry++) {
                    waves.TickMilestones(10);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                }
                Check("milestone spawned "+milestone,waves.MilestonesStarted==milestone+1 && waves.Encounter.Boss!=null);
                var boss=waves.Encounter.Boss;if(boss==null)break;
                boss.SetPhysicsProcess(false);waves.Encounter.Tick(3,false);boss.SetPhysicsProcess(false);
                float scale=milestone switch {0=>3.2f,1=>2.6f,2=>2.9f,_=>4.5f};
                Check("boss size "+milestone,Math.Abs(boss.GetNode<Node3D>("Visual/Original").Scale.X-scale)<.001f);
                var visual=boss.Visual!;
                visual.Present(new Vector3(boss.MoveSpeed,0,0),boss.Target.GlobalPosition,1.0/60);
                Check("walk playback follows movement "+milestone,visual.RunClip=="Walk" && visual.LocomotionStrideSpeed>0 && Math.Abs(visual.Animator.SpeedScale-boss.MoveSpeed/visual.LocomotionStrideSpeed)<.001);
                visual.Present(Vector3.Zero,boss.Target.GlobalPosition,1.0/60);Check("idle normal speed "+milestone,visual.Animator.SpeedScale==1);
                visual.Attack();Check("attacks normal speed "+milestone,visual.Animator.SpeedScale==1);
                if(milestone==3) {
                    Check("correct finale",boss.GetNodeOrNull<Xrr3Attack>("Xrr3Attack")!=null && boss.DisplayName.Contains("XRR-3"));
                    var raid=boss.GetNode<Xrr3Attack>("Xrr3Attack");
                    var old=w.Player.GlobalPosition;
                    raid.Tick(5);Check("warning precedes damage",raid.Phase==BossPhase.Telegraph && raid.Strikes==0 && raid.Warnings==1);
                    w.Player.GlobalPosition=old+new Vector3(0,0,-2);
                    raid.Tick(raid.WarningSeconds+.01);Check("locked missile can be dodged",raid.Dodged==1 && raid.Strikes==1);
                    w.Player.GlobalPosition=old;
                    boss.Health.ApplyDamage(boss.Health.Maximum*.4f);Check("phase two",raid.PhaseNumber==2);
                    // Advanced modes have multiple cannon strikes. Finish the current cycle
                    // before starting the next, rather than assuming three fixed two-second ticks.
                    void FinishCycle(){for(int i=0;i<12 && raid.Phase!=BossPhase.Pursuit;i++)raid.Tick(20);}
                    FinishCycle();int beforeCannon=raid.Strikes;
                    raid.Tick(20);raid.Tick(raid.WarningSeconds+.01);Check("cannon cycle",raid.Strikes>beforeCannon);
                    boss.Health.ApplyDamage(boss.Health.Maximum*.3f);Check("phase three",raid.PhaseNumber==3);
                    FinishCycle();int beforeSwipe=raid.Strikes;
                    raid.Tick(20);raid.Tick(raid.WarningSeconds+.01);Check("swipe cycle",raid.Strikes>beforeSwipe);
                    if(DisplayServer.GetName()!="headless") {
                        await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                        GetViewport().GetTexture().GetImage().SavePng("res://../logs/eternity-encounter-review.png");
                    }
                }
                boss.Health.ApplyDamage(boss.Health.Maximum*2);waves.TickMilestones(.1);
                Check("milestone completed once "+milestone,waves.MilestonesCompleted==milestone+1);
                w.Artifacts?.Clear();
            }
            Check("finite victory",waves.CoreComplete && !waves.Running && !spawner.Running);
            w.ResetCombatWorld();Check("restart resets level and encounter",p.Level==1 && p.Experience==0 && waves.MilestonesCompleted==0 && waves.Running);
        } catch(Exception ex){_failures.Add(ex.ToString());GD.PrintErr(ex);}
        using(var file=FileAccess.Open($"res://../manifests/eternity-encounter-{w.RunConfiguration.AdvancedClassId}-smoke.json",FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new{passed=_failures.Count==0,failures=_failures,classId=w.RunConfiguration.AdvancedClassId}));
        GD.Print(_failures.Count==0?"ETERNITY_ENCOUNTER_SMOKE_PASSED":"ETERNITY_ENCOUNTER_SMOKE_FAILED");GetTree().Quit(_failures.Count==0?0:1);
    }
}
