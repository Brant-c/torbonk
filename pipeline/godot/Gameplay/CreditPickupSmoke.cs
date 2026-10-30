using Godot;
using System;
using System.Linq;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;
public partial class CreditPickupSmoke : Node
{
    public async void Run(GameWorld world)
    {
        string path=ProjectSettings.GlobalizePath("res://../logs/credit-pickup-test-"+Guid.NewGuid()+".json");
        try {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            void Check(bool ok,string message){if(!ok)throw new Exception(message);}
            world.Waves!.SetPhysicsProcess(false);world.Spawner!.SetPhysicsProcess(false);world.Player.SetPhysicsProcess(false);
            var loot=world.EvLoot!;loot.ResetRun();
            int original=UI.EquipmentWindows.Instance.Inventory.State.Credits;
            world.Spawner.TrySpawn(GD.Load<PackedScene>("res://Gameplay/Planets/EternityVault/IceCat.tscn"),ignoreLimit:true);
            var enemy=world.Spawner.Enemies.Last();enemy.SetPhysicsProcess(false);enemy.Health.ApplyDamage(enemy.Health.Maximum*2);
            var pickup=loot.GetChildren().OfType<CreditPickup>().Single();pickup.SetPhysicsProcess(false);
            Check(pickup.Value>=3 && pickup.Value<=9,"mob drop amount");
            Check(UI.EquipmentWindows.Instance.Inventory.State.Credits==original,"no Credits before collection");
            var icon=pickup.GetChild<Node3D>(0).GetChild<Sprite3D>(0);
            Check(icon.Texture.ResourcePath.EndsWith("symbols/credits.png") && icon.Modulate==Colors.White,"green Credit icon");
            var inventory=new PersistentInventory(path);
            pickup.SaveCredits=amount=>inventory.Award(amount,Array.Empty<(string,int)>(),0,out _);
            int value=pickup.Value;
            Check(pickup.TryCollect() && !pickup.TryCollect(),"single collection");
            Check(new PersistentInventory(path).State.Credits==value,"collection persists Credits");
            loot.DropCredits(world.PlayerActor.GlobalPosition+Vector3.Right*2,150);
            var failed=loot.GetChildren().OfType<CreditPickup>().Last();failed.SetPhysicsProcess(false);failed.SaveCredits=_=>false;
            Check(!failed.TryCollect() && !failed.Collecting,"failed save retains pickup");
            for(int i=0;i<150;i++)loot.DropCredits(world.PlayerActor.GlobalPosition+Vector3.Right*2,1);
            Check(loot.PendingCredits==300,"bounded pickup merging preserves Credits");
            loot.ResetRun();await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(!loot.GetChildren().OfType<CreditPickup>().Any(),"restart clears pickups");
            Check(UI.EquipmentWindows.Instance.Inventory.State.Credits==original,"real profile untouched");
            GD.Print("CREDIT_PICKUP_SMOKE_PASSED");GetTree().Quit();
        }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
        finally {if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
    }
}
