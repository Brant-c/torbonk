using Godot;
using System;
using System.Linq;
namespace SwtorSurvivors.UI;

/// <summary>Reuses the B7/B10 panel, font, card, rarity frames and button states.</summary>
public partial class ArtifactRewardsHud : CanvasLayer
{
    public GameWorld World {get;set;}=null!;
    private Control _root=null!,_modal=null!,_layout=null!;
    private HBoxContainer _cards=null!,_active=null!;
    private Label _title=null!,_detail=null!,_inventoryTitle=null!;
    private SwtorButton _back=null!;
    public int ChoiceCount=>_cards.GetChildCount();
    public bool ModalVisible=>_modal.Visible;
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;Layer=6;
        _root=new Control {Theme=SwtorSkin.MakeTheme(),MouseFilter=Control.MouseFilterEnum.Ignore};AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var inspect=new PanelContainer {Name="ActiveRunArtifacts",MouseFilter=Control.MouseFilterEnum.Pass};_root.AddChild(inspect);
        inspect.AddThemeStyleboxOverride("panel",SwtorSkin.Box("bwaui_tooltip_1",8,12,8,12));
        inspect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);inspect.OffsetLeft=-236;inspect.OffsetRight=-16;inspect.OffsetTop=54;inspect.OffsetBottom=152;
        var column=new VBoxContainer();inspect.AddChild(column);
        _inventoryTitle=SwtorSkin.Text("RUN ARTIFACTS",13,true);column.AddChild(_inventoryTitle);
        _active=new HBoxContainer();column.AddChild(_active);
        _modal=new Control {Visible=false,MouseFilter=Control.MouseFilterEnum.Stop};_root.AddChild(_modal);_modal.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade=new ColorRect {Color=new Color(0,.015f,.035f,.72f),MouseFilter=Control.MouseFilterEnum.Stop};_modal.AddChild(shade);shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _layout=new Control {Size=new Vector2(840,550)};_modal.AddChild(_layout);
        var frame=new SwtorPanel();_layout.AddChild(frame);frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var box=SwtorSkin.Box("bwaui_standard_window_1",128,72,184,64);
        box.ContentMarginLeft=24;box.ContentMarginRight=24;box.ContentMarginTop=24;box.ContentMarginBottom=48;frame.AddThemeStyleboxOverride("panel",box);
        var body=new VBoxContainer();body.AddThemeConstantOverride("separation",14);frame.AddChild(body);
        _title=SwtorSkin.Text("CHOOSE ONE RUN REWARD",20,true);_title.HorizontalAlignment=HorizontalAlignment.Center;body.AddChild(_title);
        _detail=SwtorSkin.Text("",14);_detail.HorizontalAlignment=HorizontalAlignment.Center;_detail.AutowrapMode=TextServer.AutowrapMode.WordSmart;body.AddChild(_detail);
        _cards=new HBoxContainer();_cards.SizeFlagsVertical=Control.SizeFlags.ExpandFill;body.AddChild(_cards);
        _back=new SwtorButton {Text="BACK TO REWARDS [B]",Visible=false};body.AddChild(_back);_back.Pressed+=()=>World.Artifacts!.Back();
        World.Artifacts!.Changed+=Refresh;_root.Resized+=Layout;Refresh();Layout();
    }
    private void Layout()
    {
        float scale=Math.Max(.25f,Math.Min(1,Math.Min((_root.Size.X-24)/_layout.Size.X,(_root.Size.Y-24)/_layout.Size.Y)));
        _layout.Scale=Vector2.One*scale;_layout.Position=(_root.Size-_layout.Size*scale)/2;
    }
    private static void Clear(Node node) {foreach(var child in node.GetChildren()){node.RemoveChild(child);child.QueueFree();}}
    private static UpgradeDefinition CardData(RunArtifactDefinition item,string? title=null,string? description=null) => new() {
        Id=item.Id,Title=title??item.Name,Icon=item.Icon,Rarity=item.Rarity,Effect=UpgradeEffect.Mechanic,
        MechanicalDescription=description??item.Description };
    private void Refresh()
    {
        var items=World.Artifacts!;Clear(_active);Clear(_cards);
        _inventoryTitle.Text=$"RUN ARTIFACTS  {items.Equipped.Count}/{items.SlotLimit}";
        for(int i=0;i<items.SlotLimit;i++)
        {
            var item=i<items.Equipped.Count?items.Equipped[i]:null;
            _active.AddChild(new AbilityIcon {IconName=item?.Icon??"",SlotSize=42,IconPadding=4,
                TooltipText=item==null?"Empty run artifact slot. Choose one reward after an elite or boss.":$"{item.Name} · {item.Rarity} run item\n{item.Description}\nThis run only. Replace at the next encounter reward.\nAuthored item name and rule; original SWTOR icon."});
        }
        _modal.Visible=items.Selecting;_back.Visible=items.Replacing;
        if(!items.Selecting)return;
        _title.Text=items.Replacing?"REPLACE ONE ARTIFACT":"CHOOSE ONE RUN REWARD";
        _detail.Text=items.Replacing?$"Selected: {items.Selected!.Name} — {items.Selected.Description}\nChoose the item to remove. Its effect ends immediately.":
            $"{(items.EncounterTier>=2?"MAJOR BOSS":"ELITE")}  ·  Take one of three  ·  {items.Equipped.Count}/{items.SlotLimit} equipped\nTemporary run items. Premium / Prototype / Artifact indicates rarity.";
        var entries=items.Replacing?items.Equipped:items.Offers;
        for(int i=0;i<entries.Count;i++)
        {
            int index=i;var item=entries[i];
            var data=items.Replacing?CardData(item,"Replace "+item.Name,"LOSE: "+item.Description):CardData(item);
            var card=new UpgradeCard(data,0,i+1,items.Replacing?"REMOVE":item.PowerItem?"POWER ITEM":"RUN ITEM");
            card.TooltipText=$"{item.Name} · {item.Rarity}\n{data.MechanicalDescription}\n"+
                (items.Replacing?$"Replace with {items.Selected!.Name}. Unrelated talents remain.":item.PowerItem?"Applies immediately. Uses no equipment slot.":items.Equipped.Count>=items.SlotLimit?"Next: choose one equipped artifact to replace. Nothing is removed until confirmed.":"Equips immediately in an empty slot.")+"\nAuthored Survivors item; original SWTOR icon.";
            card.Pressed+=()=>{if(items.Replacing)items.Equip(index);else items.Choose(index);};_cards.AddChild(card);
        }
        _cards.GetChild<Control>(0).GrabFocus();
    }
    public override void _Input(InputEvent input)
    {
        if(!World.Artifacts!.Selecting || input is not InputEventKey {Pressed:true,Echo:false} key)return;
        if(key.PhysicalKeycode is >=Key.Key1 and <=Key.Key3)
        {int i=(int)key.PhysicalKeycode-(int)Key.Key1;if(World.Artifacts.Replacing)World.Artifacts.Equip(i);else World.Artifacts.Choose(i);GetViewport().SetInputAsHandled();}
        else if(key.PhysicalKeycode==Key.B && World.Artifacts.Replacing) {World.Artifacts.Back();GetViewport().SetInputAsHandled();}
    }
    public override void _ExitTree() {if(GodotObject.IsInstanceValid(World.Artifacts))World.Artifacts!.Changed-=Refresh;}
}
