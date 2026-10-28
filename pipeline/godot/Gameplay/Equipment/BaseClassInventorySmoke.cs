using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Equipment;

public partial class BaseClassInventorySmoke : Node
{
    public override void _Ready()
    {
        string path=Path.Combine(ProjectSettings.GlobalizePath("res://../work"),"base-class-inventory-"+Guid.NewGuid()+".json");
        int checks=0;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
        try
        {
            var inv=new PersistentInventory(path);
            foreach(var family in FrontendCatalog.Load().Classes)
            foreach(var cls in family.AdvancedClasses)
            {
                var sibling=family.AdvancedClasses.First(a=>a.Id!=cls.Id);
                var exclusive=inv.Items[0] with {Classes=new[]{sibling.Id}};
                Check(PersistentInventory.VisibleForClass(exclusive,cls.Id),"sibling visible "+cls.Id);
                Check(!PersistentInventory.Compatible(exclusive,cls.Id),"sibling restricted "+cls.Id);
                Check(PersistentInventory.Compatible(exclusive with {Classes=new[]{family.Id}},cls.Id),"explicit base shared "+cls.Id);
                Check(PersistentInventory.Compatible(exclusive with {Classes=new[]{cls.Id,sibling.Id}},cls.Id),"explicit advanced shared "+cls.Id);
            }
            var sorcerer=inv.Items.First(i=>i.Classes.SequenceEqual(new[]{"sorcerer"}));
            var assassin=inv.Items.First(i=>i.Classes.SequenceEqual(new[]{"assassin"}));
            Check(inv.Equip("sorcerer","relic1",sorcerer.Id),"sorcerer loadout: "+inv.Notice);
            Check(inv.Equip("assassin","relic1",assassin.Id),"assassin loadout");
            Check(inv.Upgrade(sorcerer.Id),"upgrade");
            string before=JsonSerializer.Serialize(inv.State),disk=File.ReadAllText(path);
            foreach(var cls in FrontendCatalog.Load().Classes.SelectMany(c=>c.AdvancedClasses))
            {
                var bag=inv.BagItems(cls.Id).ToArray();
                Check(bag.All(i=>PersistentInventory.VisibleForClass(i,cls.Id)),"bag family "+cls.Id);
            }
            Check(inv.BagItems("assassin").Contains(sorcerer),"sibling owned item visible");
            Check(!inv.BagItems("juggernaut").Contains(sorcerer),"other family hidden");
            Check(!inv.Equip("assassin","relic2",sorcerer.Id),"incompatible equip rejected");
            Check(before==JsonSerializer.Serialize(inv.State) && disk==File.ReadAllText(path),"filter and rejection never mutate save");
            var reload=new PersistentInventory(path);
            Check(reload.Equipped("sorcerer","relic1")==sorcerer.Id && reload.Equipped("assassin","relic1")==assassin.Id && reload.State.Owned[sorcerer.Id]==1,"loadouts and upgrades preserved");
            Check(PersistentInventory.Compatible(inv.Items.First(i=>i.Classes.Length==0),"juggernaut"),"universal equipment");
            GD.Print($"Base-class inventory passed {checks} checks.");GetTree().Quit();
        }
        catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
        finally{if(File.Exists(path))File.Delete(path);}
    }
}

