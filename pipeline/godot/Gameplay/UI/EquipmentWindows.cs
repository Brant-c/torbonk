using System.Collections.Generic;
using System;
using System.Linq;
using Godot;
using SwtorSurvivors.Frontend;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors.UI;

public partial class EquipmentWindows : CanvasLayer
{
    public static EquipmentWindows Instance {get;private set;}=null!;
    public PersistentInventory Inventory {get;private set;}=null!;
    private Control _canvas=null!,_sheet=null!,_bag=null!;
    private VBoxContainer _slots=null!,_details=null!;
    private GridContainer _grid=null!;
    private Label _credits=null!,_stats=null!;
    private string _selected="",_class="sorcerer";
    private bool _character,_inventory;
    private Node? _scene;
    private EquipmentTooltip? _itemTooltip;
    public EquipmentTooltip? ActiveItemTooltip=>_itemTooltip;
    public void HideItemTooltip()
    {
        if(_itemTooltip==null)return;
        _itemTooltip.GetParent()?.RemoveChild(_itemTooltip);_itemTooltip.QueueFree();_itemTooltip=null;
    }
    public async void ShowItemTooltip(EquipmentItemCell cell)
    {
        HideItemTooltip();
        if(_canvas==null || cell.TooltipData==null || GetViewport().GuiIsDragging())return;
        _itemTooltip=new EquipmentTooltip(cell.TooltipData()) {Modulate=new Color(1,1,1,0),ZIndex=100};
        var card=_itemTooltip;
        _canvas.AddChild(card);card.FitBeforeShowing();
        // Allow text shaping/container queues to finish while the card is still invisible.
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        if(_itemTooltip!=card || _canvas==null || !IsInstanceValid(card))return;
        card.FitBeforeShowing();
        var mouse=_canvas.GetLocalMousePosition()+new Vector2(16,18);
        _itemTooltip.Position=new Vector2(Mathf.Clamp(mouse.X,0,Mathf.Max(0,_canvas.Size.X-_itemTooltip.Size.X)),Mathf.Clamp(mouse.Y,0,Mathf.Max(0,_canvas.Size.Y-_itemTooltip.Size.Y)));
        _itemTooltip.Modulate=Colors.White;
    }
    public override void _Ready(){Instance=this;Layer=30;ProcessMode=ProcessModeEnum.Always;Inventory=new();}
    public override void _Process(double delta)
    {
        if(_scene!=GetTree().CurrentScene){_scene=GetTree().CurrentScene;Close();}
        if(_canvas!=null){FitPane(_sheet);FitPane(_bag);var size=GetViewport().GetVisibleRect().Size;float scale=Mathf.Min(size.X/1600,size.Y/900);_canvas.Scale=Vector2.One*scale;_canvas.Position=(size-new Vector2(1600,900)*scale)/2;}
        if(_character && _stats!=null && _scene is GameWorld world && world.Initialized)
            _stats.Text=$"Health {world.PlayerActor.Health.Current:0} / {world.PlayerActor.Health.Maximum:0}";
    }
    public override void _Input(InputEvent e)
    {
        if(e is not InputEventKey {Pressed:true,Echo:false} key || key.CtrlPressed || key.AltPressed)return;
        if(GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit)return;
        if(key.PhysicalKeycode==Key.C){Open(!_character,_inventory);GetViewport().SetInputAsHandled();}
        else if(key.PhysicalKeycode==Key.I){Open(_character,!_inventory);GetViewport().SetInputAsHandled();}
        else if(key.PhysicalKeycode==Key.Escape && (_character||_inventory)){Close();GetViewport().SetInputAsHandled();}
    }
    public void Close(){HideItemTooltip();_character=_inventory=false;if(_canvas!=null){RemoveChild(_canvas);_canvas.QueueFree();_canvas=null!;}}
    private static void FitPane(Control pane)
    {
        // Use the container's content minimum so selection and upgrade controls can grow the bag.
        var frame=(PanelContainer)pane.GetChild(0);
        float height=Mathf.Ceil(frame.GetCombinedMinimumSize().Y);
        pane.Size=new Vector2(pane.Size.X,height);
        frame.Size=pane.Size;
    }
    private void CloseIcon(Control pane,Action close)
    {
        var x=new TextureButton {Name="CloseIcon",TextureNormal=SwtorSkin.Slice("bwaui_buttons_266"),TextureHover=SwtorSkin.Slice("bwaui_buttons_266"),IgnoreTextureSize=true,StretchMode=TextureButton.StretchModeEnum.KeepAspectCentered,TooltipText="Close",MouseDefaultCursorShape=Control.CursorShape.PointingHand};
        pane.AddChild(x);x.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);x.Position=new Vector2(pane.Size.X-54,27);x.Size=new Vector2(32,32);x.Pressed+=close;
    }
    private void DropSurface(Control surface,string destination)
    {
        surface.SetDragForwarding(Callable.From<Vector2,Variant>(_=>default),Callable.From<Vector2,Variant,bool>((_,data)=>CanTransfer(data,destination)),Callable.From<Vector2,Variant>((_,data)=>Transfer(data,destination)));
    }
    public void Open(bool character=true,bool inventory=true)
    {
        _scene=GetTree().CurrentScene;Close();_character=character;_inventory=inventory;if(!character && !inventory)return;
        var selection=Integration.GameNavigation.LastSelection ?? new SelectionConfiguration();
        if(GetTree().CurrentScene is FrontendFlow flow)selection=flow.CurrentConfiguration;
        if(GetTree().CurrentScene is GameWorld w && w.Initialized)selection.AdvancedClassId=w.RunConfiguration.AdvancedClassId;
        _class=selection.AdvancedClassId;
        _canvas=new Control {Size=new Vector2(1600,900),Theme=FrontendSkin.MakeTheme(),MouseFilter=Control.MouseFilterEnum.Ignore};AddChild(_canvas);
        var sheetBody=new VBoxContainer();
        _sheet=FrontendSkin.TitledPanel("Character",sheetBody,new Vector2(24,40),new Vector2(740,820));_canvas.AddChild(_sheet);_sheet.Visible=character;
        CloseIcon(_sheet,()=>Open(false,_inventory));DropSurface(_sheet,"character");
        sheetBody.AddChild(FrontendSkin.Heading(FrontendCatalog.Load().Advanced(_class).Name,24));
        var row=new HBoxContainer();sheetBody.AddChild(row);
        _slots=new VBoxContainer {CustomMinimumSize=new Vector2(225,0)};row.AddChild(_slots);
        var preview=new CharacterPreview {CustomMinimumSize=new Vector2(430,470)};row.AddChild(preview);DropSurface(preview,"character");
        var advanced=FrontendCatalog.Load().Advanced(_class);preview.ShowClass(advanced.Artwork,advanced);

        _stats=FrontendSkin.Label("",18,true);sheetBody.AddChild(_stats);
        var bagBody=new VBoxContainer();
        _bag=FrontendSkin.TitledPanel("Inventory",bagBody,new Vector2(780,40),new Vector2(796,820));_canvas.AddChild(_bag);_bag.Visible=inventory;
        CloseIcon(_bag,()=>Open(_character,false));DropSurface(_bag,"inventory");DropSurface(bagBody,"inventory");
        _credits=FrontendSkin.Label("",20);bagBody.AddChild(_credits);
        _grid=new GridContainer {Columns=8};bagBody.AddChild(_grid);DropSurface(_grid,"inventory");
        _details=new VBoxContainer();bagBody.AddChild(_details);
        Refresh();
    }
    private Color Rarity(Accessory item)=>new(item.Rarity=="Artifact"?"bc70ff":item.Rarity=="Prototype"?"54aaff":"5de580");
    private static string SlotName(string slot)=>slot switch {"implant1"=>"Implant 1","implant2"=>"Implant 2","relic1"=>"Relic 1","relic2"=>"Relic 2",_=>"Earpiece"};
    private EquipmentTooltipData TooltipData(Accessory item)
    {
        int rank=Inventory.State.Owned[item.Id];
        var comparison=PersistentInventory.Slots.Where(s=>PersistentInventory.Kind(s)==item.Kind).Select(s=>{
            string id=Inventory.Equipped(_class,s);
            return new KeyValuePair<string,string>(SlotName(s),id==""?"Empty":Inventory.Items.Single(i=>i.Id==id).Name+" +"+Inventory.State.Owned[id]);
        }).ToArray();
        string requirements=item.Classes.Length==0?"Usable by all classes":"Requires "+string.Join(", ",item.Classes.Select(id=>System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_',' '))));
        return new(item.Name,item.Kind,item.Rarity,Rarity(item),GD.Load<Texture2D>("res://Assets/Inventory/"+item.Icon+".dds"),rank,item.Description,requirements,PersistentInventory.Compatible(item,_class),item.Stats,item.Effects,comparison,rank<3?Inventory.Costs[rank]:null,rank<3 && Inventory.State.Credits>=Inventory.Costs[rank]);
    }
    private void RefreshLater()=>Callable.From(()=>{if(_canvas!=null && IsInstanceValid(_canvas))Refresh();}).CallDeferred();
    private EquipmentItemCell Cell(Accessory? item,string slot="")
    {
        string destination=slot==""?"inventory":slot;
        var b=new EquipmentItemCell {Name=item==null?"Empty_"+slot:"Item_"+item.Id,ItemId=item?.Id??"",SourceSlot=slot,CharacterClass=_class,CustomMinimumSize=new Vector2(78,78),CanReceive=data=>CanTransfer(data,destination),Receive=data=>Transfer(data,destination)};
        b.MouseEntered+=()=>ShowItemTooltip(b);b.MouseExited+=HideItemTooltip;
        b.Pressed+=()=>{_selected=item?.Id??"";RefreshLater();};
        b.RightClicked=()=>{if(item==null)return;if(slot=="")AutoEquip(item.Id);else Inventory.Unequip(_class,slot);RefreshLater();};
        foreach(string state in new[]{"normal","hover","pressed","disabled","focus"}){var box=SwtorSkin.Box("bwaui_buttons_381",14,14,14,14);box.ModulateColor=item==null?new Color("53677d"):Rarity(item);b.AddThemeStyleboxOverride(state,box);}
        if(item!=null){b.ItemTexture=GD.Load<Texture2D>("res://Assets/Inventory/"+item.Icon+".dds");var art=FrontendSkin.Art("res://Assets/Inventory/"+item.Icon+".dds",new Vector2(48,48));art.Position=new Vector2(15,12);b.AddChild(art);b.TooltipText=item.Name;b.TooltipData=()=>TooltipData(item);var rank=FrontendSkin.Label("+"+Inventory.State.Owned[item.Id],16);rank.Position=new Vector2(44,52);b.AddChild(rank);}
        else if(slot!="")b.TooltipText=SlotName(slot);
        return b;
    }
    private string Destination(string id)=>PersistentInventory.Slots.Where(s=>PersistentInventory.Kind(s)==Inventory.Items.Single(i=>i.Id==id).Kind).OrderBy(s=>Inventory.Equipped(_class,s)!="").First();
    private void AutoEquip(string id){if(Inventory.Items.Any(i=>i.Id==id))Inventory.Equip(_class,Destination(id),id);}
    public bool CanTransfer(Variant data,string destination)
    {
        if(data.VariantType!=Variant.Type.Dictionary)return false;
        var d=data.AsGodotDictionary();
        if(!d.ContainsKey("item") || !d.ContainsKey("slot") || !d.ContainsKey("class") || d["class"].AsString()!=_class)return false;
        string id=d["item"].AsString(),source=d["slot"].AsString();
        var item=Inventory.Items.FirstOrDefault(i=>i.Id==id);
        if(item==null || !Inventory.State.Owned.ContainsKey(id))return false;
        if(source=="" ? Inventory.State.Loadouts.GetValueOrDefault(_class)?.ContainsValue(id)==true : !PersistentInventory.Slots.Contains(source) || Inventory.Equipped(_class,source)!=id)return false;
        if(destination=="inventory")return source!="";
        return PersistentInventory.Compatible(item,_class) && (destination=="character" || PersistentInventory.Slots.Contains(destination) && PersistentInventory.Kind(destination)==item.Kind) && destination!=source;
    }
    public void Transfer(Variant data,string destination)
    {
        if(!CanTransfer(data,destination))return;
        var d=data.AsGodotDictionary();string id=d["item"].AsString();
        if(destination=="inventory")Inventory.Unequip(_class,d["slot"].AsString());
        else Inventory.Equip(_class,destination=="character"?Destination(id):destination,id);
        _selected=id;RefreshLater();
    }
    private static void Clear(Node n){foreach(var c in n.GetChildren()){n.RemoveChild(c);c.QueueFree();}}
    private void Refresh()
    {
        HideItemTooltip();Clear(_slots);Clear(_grid);Clear(_details);_credits.Text=$"Credits: {Inventory.State.Credits:N0}";
        foreach(string slot in new[]{"earpiece","implant1","implant2","relic1","relic2"})
        {
            string id=Inventory.Equipped(_class,slot);var item=Inventory.Items.FirstOrDefault(i=>i.Id==id);
            var row=new HBoxContainer();_slots.AddChild(row);row.AddChild(Cell(item,slot));row.AddChild(FrontendSkin.Label(SlotName(slot),17));DropSurface(row,slot);
        }
        var bag=Inventory.BagItems(_class).ToArray();
        foreach(var item in bag)_grid.AddChild(Cell(item));
        for(int n=bag.Length;n<24;n++)_grid.AddChild(Cell(null));
        var selected=Inventory.Items.FirstOrDefault(i=>i.Id==_selected && Inventory.State.Owned.ContainsKey(i.Id));
        if(selected!=null)
        {
            int rank=Inventory.State.Owned[selected.Id];
            _details.AddChild(FrontendSkin.Heading("Equipment Upgrades",21));
            var name=FrontendSkin.Label(selected.Name+" +"+rank,20);name.AddThemeColorOverride("font_color",Rarity(selected));_details.AddChild(name);
            _details.AddChild(FrontendSkin.Label(rank==3?"Maximum rank":"+"+rank+" → +"+(rank+1)+"   ·   "+Inventory.Costs[rank].ToString("N0")+" Credits",18));
            var upgrade=FrontendSkin.Button(rank==3?"Fully upgraded":"Upgrade",()=>{Inventory.Upgrade(selected.Id);RefreshLater();});upgrade.Disabled=rank==3 || Inventory.State.Credits<Inventory.Costs[rank];_details.AddChild(upgrade);
        }
        if(Inventory.Notice!="Saved" && Inventory.Notice!="")_details.AddChild(FrontendSkin.Label(Inventory.Notice,16,true));
    }
}








