using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One short integration session; progression is stepped directly, never a simulated 30-level playthrough.</summary>
public partial class D4Smoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool pass) {if(!pass)throw new InvalidOperationException(name);_checks.Add(name);GD.Print("D4: "+name);}
    private async Task Frames(int n=1) {for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    public async void Run(GameWorld w)
    {
        string? error=null;
        try
        {
            await Frames(2);w.ResetCombatWorld();
            w.Waves!.SetPhysicsProcess(false);w.Abilities.SetPhysicsProcess(false);w.Abilities.Abort();
            w.Player.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            Check("Korriban alone opts into planet stages",w.PlanetRun!=null && Integration.MapRegistry.All.Where(m=>m.Id!="korriban")
                .All(m=>!FileAccess.GetFileAsString(m.WorldScene).Contains("PlanetRunPath")));
            Check("six authored terrain sectors loaded",w.EnvironmentRoot.GetNode("Terrain").GetChildCount()==6);
            Check("expanded footprint preserves source units",w.Arena.Maximum.X==58 && w.Arena.Maximum.Y==31.4f && w.Arena.Minimum.X==20.2f);
            Check("all four caches have safe standing points",w.Journey!.Caches.Count==4 && w.Journey.Caches.All(c=>w.Spawner!.Navigation.TryStandingPoint(c.GlobalPosition,out _)));
            if(OS.GetCmdlineUserArgs().Contains("--d4-routes"))
            {
                var routeStart=w.Player.SpawnPosition;
                foreach(var c in w.Journey.Caches)
                {
                    bool reachable=PlanetRouteProbe.Reach(w.Spawner!.Navigation,routeStart,c.GlobalPosition,out int visited);
                    GD.Print($"D4 route {c.Definition.Id}: {routeStart} -> {c.GlobalPosition}; {visited} probes");
                    Check("reachable exploration route: "+c.Definition.Id,reachable);routeStart=c.GlobalPosition;
                }
            }
            Check("original burial scenery is placed around open courts",w.Journey.BurialProps>=4);
            Check("all eight classes retain their existing kits",ClassProfile.All.Length==8 && ClassProfile.All.All(p=>p.Ready));
            Check("level 1-30 schedule has six unique milestones",w.PlanetRun!.Milestones.Select(m=>m.Level).SequenceEqual(new[]{5,10,15,20,25,30}));
            Check("stage mixtures introduce three new silhouette families",w.PlanetRun.Stages.SelectMany(s=>s.Enemies).Select(e=>e.Scene).Distinct().Count()==6);
            foreach(var path in w.PlanetRun.Stages.SelectMany(s=>s.Enemies).Select(e=>e.Scene).Distinct())
            {
                var root=GD.Load<PackedScene>(path).Instantiate<CharacterBody3D>();var enemy=root.GetNode<EnemyAgent>("Actor");
                w.AddChild(root);enemy.SetPhysicsProcess(false);
                Check("rig and five original clips: "+path,enemy.Visual!=null && new[]{"Idle","Run","Attack","Hit","Death"}.All(c=>enemy.Visual.Animator.HasAnimation(c)));
                root.Free();
            }
            w.Spawner!.ResetSpawner();w.Spawner.Running=true;
            int level=4;w.Waves.PlayerLevel=()=>level;
            w.Waves.TickMilestones(.1);Check("level 4 cannot start an elite",w.Waves.MilestonesStarted==0);
            var seen=new List<string>();int rewards=0;
            foreach(int milestone in new[]{5,10,15,20,25,30})
            {
                level=30; // Multiple future level milestones are already due throughout this fight.
                for(int retry=0;retry<16 && !w.Waves.Encounter.InEncounter;retry++)w.Waves.TickMilestones(1.1);
                var controller=w.Waves.Encounter;var boss=controller.Boss!;
                Check("one actor at milestone "+milestone,controller.InEncounter && w.Spawner.ActiveCount==1 && boss.GuaranteedReward);
                boss.SetPhysicsProcess(false);string id=controller.Definition.Id;seen.Add(id);
                for(int i=0;i<5;i++)w.Waves.TickMilestones(.1);
                boss.SetPhysicsProcess(false);
                Check("level jumps cannot overlap trial "+milestone,w.Waves.Encounter.Boss==boss && w.Waves.MilestonesStarted==seen.Count);
                int tier=milestone%10==0?2:1;Check("classification "+milestone,boss.RewardTier==tier);
                var attack=boss.GetNodeOrNull<TombAttack>("TombAttack");
                if(attack!=null)
                {
                    attack.TriggerRange=2;
                    attack.Tick(4);Check("readable windup "+milestone,attack.Phase==BossPhase.Telegraph);
                    if(milestone==30)
                    {
                        boss.Health.ApplyDamage(boss.Health.Maximum*.51f);Check("final beast phase transition",attack.SecondPhase);
                        attack.Tick(2);attack.Tick(2);attack.Tick(6);
                        Check("Ancient Fury presents three locked warnings",attack.ActiveWarnings==3);
                    }
                }
                if(milestone==30 && OS.GetCmdlineUserArgs().Contains("--d4-capture"))
                {
                    await Frames(2);GetViewport().GetTexture().GetImage().SavePng("res://../generated/d4-korriban.png");
                }
                boss.Health.ApplyDamage(100000);w.Artifacts!.Defeated(boss); // Repeated payment request must be ignored.
                Check("defeat is synchronous "+milestone,controller.State==EncounterState.Defeated && (attack==null || attack.Phase==BossPhase.Ended));
                await Frames(2);
                Check("three correct-tier reward choices "+milestone,w.Artifacts.Selecting && w.Artifacts.Offers.Count==3 && w.Artifacts.EncounterTier==tier);
                if(tier==2)Check("major reward guarantees eligible defining relic "+milestone,w.Artifacts.Offers.Any(a=>a.MinimumTier>=2 && !a.PowerItem));
                w.Waves.TickMilestones(6);w.Waves.TickMilestones(6);
                Check("reward choice holds next encounter "+milestone,w.Waves.MilestonesStarted==seen.Count);
                int selected=Enumerable.Range(0,3).FirstOrDefault(i=>!w.Artifacts.Offers[i].PowerItem);
                Check("choose reward "+milestone,w.Artifacts.Choose(selected));
                if(w.Artifacts.Replacing)Check("full artifact inventory uses replacement "+milestone,w.Artifacts.Equip(0));
                rewards++;await Frames(2);
                Check("single payment per actor "+milestone,!w.Artifacts.Pending && w.Artifacts.RewardsClaimed==rewards);
                // Remove corpses synchronously between direct schedule steps.
                foreach(var e in w.Spawner.Enemies.Where(e=>!e.Alive).ToArray()) {w.Spawner.RemoveChild(e.Body);e.Body.QueueFree();}
                await Frames();
            }
            w.Waves.TickMilestones(6);
            Check("three distinct major models and six completed trials",seen.Distinct().Count()==6 && w.Waves.CoreComplete && w.Waves.MilestonesCompleted==6);
            var paths=new[]{"Broodmatron","ProtectorBoss","AncientBeast"}.Select(n=>FileAccess.GetFileAsString($"res://Gameplay/Planets/Korriban/{n}.tscn")).ToArray();
            Check("major bosses use separate original model resources",paths[0].Contains("broodmatron.glb") && paths[1].Contains("protector.glb") && paths[2].Contains("beast.glb"));
            w.ResetCombatWorld();w.Waves.SetPhysicsProcess(false);w.Spawner.Running=true;w.Abilities.SetPhysicsProcess(false);
            Check("restart clears trials, caches, relics and pending rewards",w.Waves.MilestonesStarted==0 && w.Waves.MilestonesCompleted==0 && w.Journey.Caches.All(c=>!c.Claimed) && !w.Artifacts.Pending && w.Artifacts.Equipped.Count==0);
            var cache=w.Journey.Caches.First();Check("optional cache pays once",cache.Claim() && !cache.Claim());
            w.Progression!.CancelPresentation();
            Check("ordinary enemy spawn resolves on supported ground",w.Spawner.TrySpawn(GD.Load<PackedScene>("res://Gameplay/Planets/Korriban/SpittingTukata.tscn")));
            var actor=w.Spawner.Enemies.Last();actor.SetPhysicsProcess(false);int hits=0;
            int flytext=0;w.Events.Resolved+=e=>{if(e.Change.Kind==CombatEventKind.Damage)hits++;};
            w.Flytext.EntryCreated+=e=>{if(e.Change.Kind==CombatEventKind.Damage)flytext++;};
            actor.Health.SetMaximum(1000,1000);actor.Health.ApplyDamage(1);actor.Health.ApplyDamage(2);actor.Health.ApplyDamage(3);
            Check("each actual hit publishes its own combat event",hits==3 && actor.Health.DamageEvents==3);
            await Frames(2);Check("each hit has its own floating combat text",flytext==3);
            w.PlayerActor.Health.ApplyDamage(100000);
            Check("death stops spawns and clears active telegraphs",!w.Waves.Running && !w.Spawner.Running && w.Spawner.Enemies.All(e=>!e.Running));
            w.ResetCombatWorld();Check("restart after death restores playable run",w.PlayerActor.Alive && w.Waves.Running && w.Waves.MilestonesCompleted==0);
            await Frames(2);
        }
        catch(Exception e) {error=e.ToString();GD.PushError(error);}
        using var file=FileAccess.Open("res://../manifests/d4-smoke.json",FileAccess.ModeFlags.Write);
        file.StoreString(JsonSerializer.Serialize(new {passed=error==null,checks=_checks,error,
            burialProps=w.Journey?.BurialProps,caches=w.Journey?.Caches.Select(c=>new{id=c.Definition.Id,position=c.GlobalPosition.ToString()})},new JsonSerializerOptions{WriteIndented=true}));
        GD.Print(error==null?"D4_SMOKE_PASSED":"D4_SMOKE_FAILED");GetTree().Quit(error==null?0:1);
    }
}
