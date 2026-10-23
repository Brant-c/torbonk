using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public partial class DropSymbolSmoke : Node
{
    private async Task Frames(int n=3){for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private void Check(bool ok,string message){if(!ok)throw new Exception(message);GD.Print("DROP: "+message);}
    public override async void _Ready()
    {
        try {
            await Frames();var menu=(MainMenu)GetTree().CurrentScene;
            Check(GameNavigation.TryStart(menu,new SelectionConfiguration(),out _),"run starts");
            GameWorld world=null!;
            for(int i=0;i<600;i++){await Frames(1);if(GetTree().CurrentScene is GameWorld w && w.Artifacts!=null){world=w;break;}}
            Check(world!=null,"world initialized");await Frames(10);
            world!.Waves!.SetPhysicsProcess(false);world.Spawner!.SetPhysicsProcess(false);world.Player.SetPhysicsProcess(false);
            var p=world.Progression!;var origin=world.PlayerActor.GlobalPosition;
            typeof(RunProgression).GetMethod("Drop",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(p,new object[]{origin+Vector3.Right*.2f,1});
            var xp=p.GetChildren().OfType<ExperiencePickup>().Last();xp.SetPhysicsProcess(false);
            Check(xp.VisualInitialized && xp.GetChild<Node3D>(0).GetChild<Sprite3D>(0).Texture.ResourcePath.EndsWith("bwaui_buttons_298.png"),"XP has original star symbol");
            p.Collect(xp);await Frames(20);Check(p.Experience==1 && p.PickupCount==0,"XP collected once with unchanged value");
            var artifacts=world.Artifacts!;
            foreach(int tier in new[]{1,2}) {
                Check(world.Spawner.TrySpawn(GD.Load<PackedScene>("res://Gameplay/Enemies/CryptLooter.tscn"),ignoreLimit:true,anchor:origin+Vector3.Right*.35f),"reward enemy spawned");
                var enemy=world.Spawner.Enemies.Last();enemy.RewardTier=tier;enemy.GuaranteedReward=true;enemy.SetPhysicsProcess(false);
                enemy.Health.ApplyDamage(enemy.Health.Maximum+1);await Frames(4);
                var marker=artifacts.GetChildren().OfType<DropSymbol>().Last();
                Check(marker.Kind==(tier==1?DropSymbolKind.Item:DropSymbolKind.Special),"correct marker for tier "+tier);
                Check(artifacts.Selecting && GetTree().Paused,"reward chooser timing retained");
                Check(artifacts.Choose(0),"reward can still be claimed");await Frames(4);
            }
            artifacts.Clear();await Frames(3);Check(!artifacts.GetChildren().OfType<DropSymbol>().Any(),"reset clears reward markers");
            // Stable in-world comparison using the same production marker and XP art.
            foreach(var (kind,offset) in new[]{(DropSymbolKind.Experience,-.16f),(DropSymbolKind.Item,0f),(DropSymbolKind.Special,.16f)}) {
                var visual=new Node3D();world.AddChild(visual);visual.GlobalPosition=origin+Vector3.Right*offset+Vector3.Forward*.16f+Vector3.Up*.08f;
                visual.AddChild(DropSymbol.Icon(kind,.075f));
            }
            await Frames(10);GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/drop-symbols-world.png"));
            GD.Print("DROP SYMBOL SMOKE PASSED");GetTree().Quit();
        }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
