using Godot;
using System;
using System.IO;
using System.Linq;
namespace SwtorSurvivors.Equipment;
public partial class EvLootSmoke : Node
{
    public override void _Ready()
    {
        string path=ProjectSettings.GlobalizePath("res://../logs/ev-loot-test-"+Guid.NewGuid()+".json");
        try {
            void Check(bool ok,string message){if(!ok)throw new Exception(message);}
            var inventory=new PersistentInventory(path);
            Check(inventory.Award(10000,Array.Empty<(string,int)>(),0,out _),"test funding");
            Check(!inventory.State.Owned.Keys.Any(id=>inventory.Items.Single(i=>i.Id==id).Kind=="relic"),"relics must be earned");
            var relic=inventory.Items.First(i=>i.Kind=="relic" && PersistentInventory.Compatible(i,"sorcerer"));
            Check(inventory.Award(10,new[]{(relic.Id,0)},200,out _) && inventory.State.Owned[relic.Id]==0,"unlock +0");
            Check(inventory.Award(0,new[]{(relic.Id,2)},200,out _) && inventory.State.Owned[relic.Id]==2,"upgrade duplicate");
            int credits=inventory.State.Credits;
            Check(inventory.Award(0,new[]{(relic.Id,1)},200,out _) && inventory.State.Credits==credits+200 && inventory.State.Owned[relic.Id]==2,"lower duplicate");
            Check(inventory.Upgrade(relic.Id) && !inventory.Upgrade(relic.Id),"credit upgrade cap");
            Check(inventory.DifficultyUnlocked("Normal") && inventory.DifficultyUnlocked("Hard") && inventory.DifficultyUnlocked("Nightmare"),"all difficulties available without a clear");
            Check(!inventory.DifficultyUnlocked("Invalid"),"invalid difficulty rejected");
            Check(!inventory.CompleteOperation("Normal",new[]{"xrr3"}) && !inventory.CompleteOperation("Hard",new[]{"soa"}),"isolated bosses do not record a full clear");
            Check(inventory.CompleteOperation("Hard",new[]{"xrr3","gharj","ancient_pylons","infernal_council","soa"}),"full clear");
            inventory=new PersistentInventory(path);
            Check(inventory.DifficultyUnlocked("Nightmare") && inventory.State.Owned[relic.Id]==3,"difficulty access and persistent rank");
            Check(inventory.BagItems("assassin").Any(i=>i.Id==relic.Id),"base class inventory shared");
            GD.Print("EV_LOOT_SMOKE_PASSED");GetTree().Quit();
        } catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
        finally {if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
    }
}
