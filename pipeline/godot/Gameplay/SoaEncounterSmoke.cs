using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

public partial class SoaEncounterSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string label,bool condition){if(!condition)throw new Exception(label);_checks.Add(label);GD.Print("SOA_CHECK "+label);}
    private async System.Threading.Tasks.Task Frame()=>await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try
        {
            await Frame();
            Check("level-one finite run",!w.ExplorationOnly && w.PlanetRun?.CoreLevel==50 && w.Progression!.Level==1);
            var waves=w.Waves!;var spawner=w.Spawner!;var p=w.Progression!;
            waves.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
            foreach(var ability in w.Abilities.Abilities)ability.CombatEnabled=false;
            w.Abilities.SetPhysicsProcess(false);w.Combat.SetPhysicsProcess(false);
            waves.CanStartMilestone=()=>true;
            w.PlayerActor.Health.Maximum=100000;w.PlayerActor.Health.Restore();
            Check("requested jungle selectors",w.PlanetRun!.Stages[0].SourceEncounter.StartsWith("a:") && w.PlanetRun.Stages[1].SourceEncounter.StartsWith("f:") && w.PlanetRun.Stages[2].SourceEncounter.StartsWith("d:"));
            Check("milestone levels",w.PlanetRun.Milestones.Select(m=>m.Level).SequenceEqual(new[]{10,25,40,50}));
            foreach(var path in w.PlanetRun.Stages.SelectMany(s=>s.Enemies).Select(e=>e.Scene).Distinct())
            {
                bool placed=false;
                for(int retry=0;retry<30 && !placed;retry++)placed=spawner.TrySpawn(GD.Load<PackedScene>(path),true);
                Check("native-art enemy spawns: "+path,placed);
                var actor=spawner.Enemies.Last();actor.SetPhysicsProcess(false);
                foreach(var clip in new[]{"Idle","Run","Attack","Hit","Death"})Check(actor.DisplayName+" animation "+clip,actor.Visual!.Animator.HasAnimation(clip));
                actor.Health.ApplyDamage(actor.Health.Maximum*2);await Frame();
            }
            for(int level=2;level<=50;level++)
            {
                p.AddExperience(p.RequiredExperience);p._Process(10);
                if(p.CanChoose)p.Choose(0);if(p.Replacing)p.Replace(0);p._Process(10);w.Artifacts?.Clear();
                Check("progression level "+level,p.Level==level);
            }
            Check("level cap",p.Level==50);
            for(int milestone=0;milestone<4;milestone++)
            {
                spawner.ClearRegularEnemies();await Frame();
                foreach(var corpse in spawner.Enemies.Where(e=>!e.Alive).ToArray())corpse.Body.QueueFree();
                await Frame();await Frame();
                for(int retry=0;retry<60 && waves.MilestonesStarted<=milestone;retry++){waves.TickMilestones(10);await Frame();}
                Check("milestone starts once "+milestone,waves.MilestonesStarted==milestone+1 && waves.Encounter.Boss!=null);
                var encounter=waves.Encounter;
                encounter.Tick(3,false);
                if(milestone==1)
                {
                    var defeated=new HashSet<string>();
                    for(int attempt=0;attempt<100 && encounter.InEncounter;attempt++)
                    {
                        encounter.Tick(.2,false);
                        Check("Council concurrent cap",encounter.ActiveMembers.Count<=3);
                        foreach(var member in encounter.ActiveMembers.ToArray())
                        {
                            if(encounter.MembersRemaining>1)Check("Council guaranteed reward waits for last member",!member.GuaranteedReward);
                            member.SetPhysicsProcess(false);defeated.Add(member.DisplayName);
                            member.Health.ApplyDamage(member.Health.Maximum*2);
                        }
                        if(encounter.MembersRemaining>0)Check("Council cannot finish early",encounter.State!=EncounterState.Defeated);
                        await Frame();
                    }
                    Check("all sixteen Council challengers defeated",defeated.Count==16 && defeated.Any(n=>n.Contains("Luthro")) && defeated.Any(n=>n.Contains("Kahesh")) && defeated.Any(n=>n.Contains("Jekhop")));
                }
                else
                {
                    var boss=encounter.Boss!;boss.SetPhysicsProcess(false);
                    Check("boss safe grounded spawn "+milestone,boss.GlobalPosition.Y>-39 && boss.GlobalPosition.Y<-38);
                    if(boss.GetNodeOrNull<SoaAttack>("SoaAttack") is {} soa)
                    {
                        Check("image then Soa",soa.Image==(milestone==2));
                        int strikes=soa.Strikes;soa.Tick(5);
                        Check("Soa warns before damage",soa.Phase==BossPhase.Telegraph && soa.Strikes==strikes);
                        soa.Tick(2);Check("Soa strike resolves",soa.Strikes==strikes+1);
                        if(!soa.Image){soa.Tick(20);Check("Soa breakable shield",soa.ShieldsCast>0 && boss.Health.Shielded);boss.Health.ClearShield();}
                        if(DisplayServer.GetName()!="headless")
                        {
                            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                            GetViewport().GetTexture().GetImage().SavePng("res://../manifests/soa-encounter-"+(soa.Image?"image":"final")+".png");
                        }
                    }
                    boss.Health.ApplyDamage(boss.Health.Maximum*2);
                    if(boss.GetNodeOrNull<SoaAttack>("SoaAttack") is {} ended)Check("Soa death clears warning surfaces",ended.Phase==BossPhase.Ended);
                }
                waves.TickMilestones(.1);
                Check("milestone complete once "+milestone,waves.MilestonesCompleted==milestone+1);w.Artifacts?.Clear();
            }
            Check("finite victory",waves.CoreComplete && !waves.Running && !spawner.Running);
            w.ResetCombatWorld();Check("restart clears group and progression",p.Level==1 && waves.MilestonesCompleted==0 && waves.Encounter.ActiveMembers.Count==0 && waves.Running);
        }
        catch(Exception e){failure=e.ToString();GD.PrintErr(e);}
        using(var file=FileAccess.Open($"res://../manifests/soa-encounter-{w.RunConfiguration.AdvancedClassId}-{w.RunConfiguration.OperationDifficulty}-smoke.json",FileAccess.ModeFlags.Write))file.StoreString(JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure}));
        GD.Print(failure==null?"SOA_ENCOUNTER_SMOKE_PASSED":"SOA_ENCOUNTER_SMOKE_FAILED");GetTree().Quit(failure==null?0:1);
    }
}
