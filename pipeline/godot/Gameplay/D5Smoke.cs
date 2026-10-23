using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class D5Smoke : Node
{
    public async void Run(GameWorld world)
    {
        await ToSignal(GetTree().CreateTimer(2),SceneTreeTimer.SignalName.Timeout);
        var checks=new Dictionary<string,bool>();
        checks["Hutta identity"]=world.RunConfiguration.MapId=="hutta" && world.Hud!.ArenaName=="Hutta";
        checks["supported player"]=world.Arena.Supported(world.Player.GlobalPosition,world.Player.CapsuleRadius);
        checks["15 original patches"]=world.EnvironmentRoot.FindChildren("Terrain_*","MeshInstance3D",true,false).Count==15;
        checks["isolated roster"]=world.PlanetRun!.Stages.SelectMany(s=>s.Enemies).All(e=>e.Scene.StartsWith("res://Gameplay/Planets/Hutta/"));
        checks["12 queued milestones"]=world.PlanetRun.Milestones.Length==12;
        foreach(var e in world.PlanetRun.Stages.SelectMany(s=>s.Enemies).Select(e=>e.Scene).Distinct())
            checks[e]=world.Spawner!.TrySpawn(GD.Load<PackedScene>(e),true);
        foreach(var m in world.PlanetRun.Milestones)
        {var d=GD.Load<BossEncounterDefinition>(m.Encounter);var scene=d.Scene.Instantiate();checks[$"encounter {m.Level}"]=scene.GetNode<EnemyAgent>("Actor").GuaranteedReward;scene.Free();}
        checks["cache sites"]=world.Journey!.Caches.Count==4;
        checks["artifact catalog"]=world.Artifacts!=null;
        checks["no authored perimeter"]=world.GetChildren().OfType<HuttaAtmosphere>().Single().GetChildren().OfType<Node3D>().Count()==1;
        checks["central spawn"]=new Vector2(world.Player.GlobalPosition.X-31.85f,world.Player.GlobalPosition.Z-10.31f).Length()<2;
        foreach(var enemy in world.Spawner!.Enemies)
            checks["original animation "+enemy.DisplayName]=enemy.Visual!=null && enemy.Visual.Animator.HasAnimation("Attack") && enemy.Visual.Animator.GetAnimation("Run").GetTrackCount()>0;
        var gunner=world.Spawner!.Enemies.Last(e=>e.DisplayName.Contains("Sentry"));
        var attack=gunner.GetNode<TombAttack>("TombAttack");
        // Exercise the real locked-lane attack while avoiding a long automated gameplay run.
        gunner.Body.GlobalPosition=world.Player.GlobalPosition+Vector3.Right*.25f;
        attack.Tick(3);checks["ranged telegraph"]=attack.ActiveWarnings>0;
        attack.Tick(2);checks["ranged warning strikes"]=attack.Strikes>0;
        checks["firing animation"]=gunner.Visual!.State==gunner.Visual.AttackClip;
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        using var file=FileAccess.Open("res://../manifests/d5-smoke.json",FileAccess.ModeFlags.Write);
        bool passed=checks.Values.All(v=>v);file.StoreString(JsonSerializer.Serialize(new {passed,checks},new JsonSerializerOptions {WriteIndented=true}));
        GD.Print(passed?"D5_SMOKE_PASSED":"D5_SMOKE_FAILED");GetTree().Quit(passed?0:1);
    }
}
