using Godot;
using System;
using System.Threading.Tasks;
namespace SwtorSurvivors;
public partial class KorribanEndlessSmoke : Node
{
    void Check(string label,bool pass) {if(!pass)throw new Exception(label);GD.Print("KORRIBAN ENDLESS PASS: "+label);}
    public async void Run(GameWorld w)
    {
        try {
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            var waves=w.Waves!;var p=w.Progression!;waves.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
            Check("level 50 cap and ten milestones",p.MaximumLevel==50 && w.PlanetRun!.Milestones.Length==10);
            Check("imperial camp spawn",new Vector2(w.Player.GlobalPosition.X-w.Arena.SpawnSeed.X,w.Player.GlobalPosition.Z-w.Arena.SpawnSeed.Y).Length()<1.5f);
            var nav=w.Spawner!.Navigation;var start=w.Player.GlobalPosition;
            Check("spawn has full body clearance",nav.TryStandingPoint(start,out _));
            int exits=0;
            for(int i=0;i<8;i++) {
                float angle=i*Mathf.Tau/8;var near=start+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.8f;
                if(nav.TryStandingPoint(near,out var feet) && nav.CanWalk(start,feet))exits++;
            }
            Check("spawn has at least seven clear walking exits",exits>=7);
            Check("spawn stays on the entrance side of the base",w.Player.GlobalPosition.X>=38 && w.Player.GlobalPosition.X<=42 && w.Player.GlobalPosition.Z>=21 && w.Player.GlobalPosition.Z<=26);
            Check("terrain apron loaded",w.GetNode<KorribanFrontier>("KorribanFrontier").GetChildCount()>0);
            foreach(var enemy in w.Spawner!.Enemies)enemy.SetPhysicsProcess(false);
            Check("population stays capped at 36 through level 50 and beyond",w.Spawner!.HardActiveLimit==36 && w.PlanetRun!.Difficulty.Population(26)==36 && w.PlanetRun.Difficulty.Population(50)==36 && w.PlanetRun.Difficulty.Population(500)==36);
            w.Spawner.ResetSpawner();w.Spawner.HardActiveLimit=1;
            int xpDrops=0;Action<Vector3,int> reward=(point,amount)=>xpDrops++;w.Spawner.EnemyReward+=reward;
            var scene=GD.Load<PackedScene>("res://Gameplay/Enemies/Broodling.tscn");
            for(int kill=0;kill<6;kill++) {
                bool spawned=false;for(int attempt=0;attempt<30 && !spawned;attempt++)spawned=w.Spawner.TrySpawn(scene,ignoreLimit:true);
                Check("spawn XP sample "+kill,spawned);
                Check("hard cap blocks regular and boss spawns",!w.Spawner.TrySpawn(scene) && !w.Spawner.TrySpawn(scene,ignoreLimit:true));
                var sample=w.Spawner.Enemies[w.Spawner.Enemies.Count-1];sample.Health.ApplyDamage(1000000);
            }
            w.Spawner.EnemyReward-=reward;w.Spawner.HardActiveLimit=36;
            Check("six kills drop exactly three XP pickups",xpDrops==3 && p.PickupCount==3);
            p.MaximumLevel=2;p.AddExperience(p.RequiredExperience);p._Process(10);
            Check("cap level can choose",p.Level==2 && p.CanChoose);p.Choose(0);p._Process(10);
            p.AddExperience(10000);Check("capped XP ignored and pickups cleared",p.Experience==0 && p.PickupCount==0);
            var growth=w.Artifacts!.Catalog.Find(a=>a.Effect=="growth")!;
            Check("XP chest reward excluded at cap",!w.Artifacts.Eligible(growth,2));
            p.MaximumLevel=50;p.ResetRun();
            for(int level=2;level<=50;level++) {
                p.AddExperience(p.RequiredExperience);p._Process(10);
                Check("level "+level+" offers an upgrade",p.Level==level && p.CanChoose);
                Check("level "+level+" choice applies",p.Choose(0));p._Process(10);
            }
            Check("full run reaches 50",p.Level==50 && p.Experience==0);
            waves.PlayerLevel=()=>50;waves.CanStartMilestone=()=>false;
            waves._PhysicsProcess(2400);
            Check("time pressure exceeds level cap",w.Spawner.PlayerLevel!()>50 && !waves.Recovering && w.Spawner.ReinforcementsEnabled && waves.Running);
            waves.ResetWaves();Check("restart clears time pressure",waves.PressureSeconds==0);
            GD.Print("KORRIBAN ENDLESS ALL PASSED");GetTree().Quit();
        } catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
