using System;
using Godot;
namespace SwtorSurvivors.UI;

// Native Godot dragging keeps cancellation and drops outside either pane non-destructive.
public partial class EquipmentItemCell : SwtorButton
{
    public Func<EquipmentTooltipData>? TooltipData {get;set;}
    // Item cards are rendered in the equipment canvas, never in Godot's themed tooltip popup.
    public override string _GetTooltip(Vector2 position)=>TooltipData!=null?"":base._GetTooltip(position);
    public string ItemId {get;set;}="";
    public string SourceSlot {get;set;}="";
    public string CharacterClass {get;set;}="";
    public Texture2D? ItemTexture {get;set;}
    public Action? RightClicked {get;set;}
    public bool CanEquip {get;set;}=true;
    public Func<Variant,bool>? CanReceive {get;set;}
    public Action<Variant>? Receive {get;set;}
    public override void _GuiInput(InputEvent e)
    {
        if(e is InputEventMouseButton {Pressed:true,ButtonIndex:MouseButton.Right}){if(CanEquip || SourceSlot!="")RightClicked?.Invoke();AcceptEvent();}
    }
    private bool _dragSource;
    public override void _Notification(int what)
    {
        if(what==NotificationDragEnd && _dragSource){Modulate=Colors.White;_dragSource=false;}
    }
    public override Variant _GetDragData(Vector2 position)
    {
        if(ItemId=="" || !CanEquip && SourceSlot=="")return default;
        EquipmentWindows.Instance.HideItemTooltip();
        _dragSource=true;Modulate=new Color(1,1,1,.35f);
        SetDragPreview(new TextureRect {Texture=ItemTexture,CustomMinimumSize=new Vector2(48,48),Size=new Vector2(48,48),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,MouseFilter=MouseFilterEnum.Ignore});
        return new Godot.Collections.Dictionary<string,string> {{"item",ItemId},{"slot",SourceSlot},{"class",CharacterClass}};
    }
    public override bool _CanDropData(Vector2 position,Variant data)=>CanReceive?.Invoke(data)==true;
    public override void _DropData(Vector2 position,Variant data)=>Receive?.Invoke(data);
}



