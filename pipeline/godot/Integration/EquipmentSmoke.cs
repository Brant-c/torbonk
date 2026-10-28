using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using SwtorSurvivors.Equipment;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Integration;
public partial class EquipmentSmoke : Node
{
    public override async void _Ready()
    {
        try {
            string path=ProjectSettings.GlobalizePath("user://equipment-test-"+Guid.NewGuid()+".json");
            var p=new PersistentInventory(path);
            void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
            Check(!p.Equip("sorcerer","implant1","might_implant"),"class rejection");
            Check(!p.Equip("sorcerer","earpiece","resolve_implant"),"slot rejection");
            Check(!p.Upgrade("missing"),"ownership rejection");
            Check(p.Equip("sorcerer","implant1","resolve_implant"),"equip");
            Check(p.Equip("sorcerer","implant2","resolve_implant") && p.Equipped("sorcerer","implant1")=="","unique instance");
            Check(p.Equip("sorcerer","implant1","universal_implant"),"second implant");
            Check(p.Equip("sorcerer","relic1","relic_dark") && p.Equip("sorcerer","relic2","relic_light"),"two relics");
            Check(p.Upgrade("resolve_implant") && p.Upgrade("resolve_implant") && p.Upgrade("resolve_implant") && !p.Upgrade("resolve_implant"),"rank cap");
            p=new PersistentInventory(path);Check(p.State.Credits==5000 && p.State.Owned["resolve_implant"]==3 && p.Equipped("sorcerer","implant2")=="resolve_implant" && !p.BagItems("sorcerer").Any(i=>i.Id=="resolve_implant"),"reload");
            Check(p.Unequip("sorcerer","implant2"),"unequip");
            p.State.Credits=0;Check(!p.Upgrade("earpiece"),"insufficient credits");
            File.Delete(path);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var ui=EquipmentWindows.Instance;
            ui.Open();
            EquipmentItemCell Find(string id,string source="")=>ui.FindChildren("*","",true,false).OfType<EquipmentItemCell>().Single(c=>c.ItemId==id && c.SourceSlot==source);
            Variant Payload(string id,string slot="")=>new Godot.Collections.Dictionary<string,string>{{"item",id},{"slot",slot},{"class","sorcerer"}};
            // Exercise the native right-click handler and the same drop receivers used by Godot.
            foreach(string slot in PersistentInventory.Slots)ui.Inventory.Unequip("sorcerer",slot);
            ui.Close();ui.Open();
            Find("resolve_implant")._GuiInput(new InputEventMouseButton {Pressed=true,ButtonIndex=MouseButton.Right});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(ui.Inventory.Equipped("sorcerer","implant1")=="resolve_implant" && !ui.Inventory.BagItems("sorcerer").Any(i=>i.Id=="resolve_implant"),"right click equips and removes bag cell");
            Check(!ui.CanTransfer(Payload("might_implant"),"implant2"),"incompatible drop");
            Check(!ui.CanTransfer(Payload("earpiece"),"implant2"),"wrong slot drop");
            var empty=ui.FindChildren("*","",true,false).OfType<EquipmentItemCell>().Single(c=>c.SourceSlot=="implant2");
            Check(empty._CanDropData(Vector2.Zero,Payload("universal_implant")),"native cell drop acceptance");
            empty._DropData(Vector2.Zero,Payload("universal_implant"));
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(ui.Inventory.Equipped("sorcerer","implant2")=="universal_implant","drag to second implant");
            Check(!ui.CanTransfer(Payload("universal_implant"),"implant1"),"stale drag rejected");
            ui.Transfer(Payload("resolve_implant","implant1"),"inventory");
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(ui.Inventory.BagItems("sorcerer").Any(i=>i.Id=="resolve_implant"),"drag returns item to bag");
            Find("universal_implant","implant2")._GuiInput(new InputEventMouseButton {Pressed=true,ButtonIndex=MouseButton.Right});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(ui.Inventory.Equipped("sorcerer","implant2")=="","right click unequips");
            ui.Transfer(Payload("earpiece"),"character");
            ui.Transfer(Payload("resolve_implant"),"implant1");
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(ui.Inventory.Equipped("sorcerer","earpiece")=="earpiece","drop onto character pane");
            // Send real pointer events through Godot's GUI dispatch to verify native drag routing.
            async Task Pointer(Vector2 position,MouseButton button=MouseButton.None,bool pressed=false)
            {
                Input.ParseInputEvent(new InputEventMouseMotion {Position=position,GlobalPosition=position,ButtonMask=pressed?MouseButtonMask.Left:0});
                if(button!=MouseButton.None)Input.ParseInputEvent(new InputEventMouseButton {Position=position,GlobalPosition=position,ButtonIndex=button,Pressed=pressed});
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            }
            async Task Drag(EquipmentItemCell source,EquipmentItemCell target)
            {
                var from=source.GetGlobalRect().GetCenter();var to=target.GetGlobalRect().GetCenter();
                await Pointer(from);
                await Pointer(from,MouseButton.Left,true);
                Input.ParseInputEvent(new InputEventMouseMotion {Position=from+new Vector2(35,0),GlobalPosition=from+new Vector2(35,0),Relative=new Vector2(35,0),ButtonMask=MouseButtonMask.Left});
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(source.Modulate.A<.5f,"drag source is translucent");
                await Pointer(to,pressed:true);
                await Pointer(to,MouseButton.Left,false);
            }
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await Drag(Find("universal_implant"),ui.FindChildren("*","",true,false).OfType<EquipmentItemCell>().Single(c=>c.SourceSlot=="implant2"));
            Check(ui.Inventory.Equipped("sorcerer","implant2")=="universal_implant","pointer drag equips");
            await Drag(Find("universal_implant","implant2"),ui.FindChildren("*","",true,false).OfType<EquipmentItemCell>().First(c=>c.ItemId=="" && c.SourceSlot==""));
            Check(ui.Inventory.Equipped("sorcerer","implant2")=="" && ui.Inventory.BagItems("sorcerer").Any(i=>i.Id=="universal_implant"),"pointer drag unequips");
                        // A rejected drop restores the source without changing its ownership or slot.
            var cancelled=Find("might_implant");
            await Drag(cancelled,ui.FindChildren("*","",true,false).OfType<EquipmentItemCell>().Single(c=>c.SourceSlot=="earpiece"));
            Check(cancelled.Modulate.A==1 && ui.Inventory.BagItems("sorcerer").Any(i=>i.Id=="might_implant"),"cancelled drag restores source");
            Check(ProjectSettings.GetSetting("gui/timers/tooltip_delay_sec").AsDouble()==0,"immediate tooltip setting");
            Check(!ui.FindChildren("*","",true,false).OfType<Button>().Any(b=>b.Text=="Unequip" || b.Text=="Close" || b.Text.StartsWith("Equip →")),"no redundant buttons");
            var hoverCell=Find("might_implant");
            Check(hoverCell._GetTooltip(Vector2.Zero)=="","native popup disabled before hover");
            ui.ShowItemTooltip(hoverCell);
            var tooltip=ui.ActiveItemTooltip!;
            Check(tooltip.Modulate.A==0,"unlaid-out tooltip never becomes visible");
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(tooltip.Modulate.A==1 && tooltip.Size.Y<600 && tooltip.GetWindow() is not PopupPanel,"first hover has fitted card without native popup");
            var initialSize=tooltip.Size;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(tooltip.Size==initialSize,"tooltip size is stable from its first frame");
            if(DisplayServer.GetName()!="headless")GetViewport().GetTexture().GetImage().SavePng("res://../manifests/equipment-review.png");
            GD.Print("EQUIPMENT PASS: compatibility, slots, ownership, ranks, credits, persistence, simultaneous windows, right click, native pointer drag both directions, bag exclusion");GetTree().Quit();
        }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}










