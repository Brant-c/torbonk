using System;
using System.Collections.Generic;
using Godot;
using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.UI;

// Sections map to implemented item data; absent stats/effects are omitted rather than fabricated.
public sealed record EquipmentTooltipData(string Name,string Kind,string Rarity,Color RarityColor,
    Texture2D Icon,int Rank,string Description,string Requirements,bool Compatible,
    string[] Stats,string[] Effects,KeyValuePair<string,string>[] Comparison,int? UpgradeCost,bool Affordable);

public partial class EquipmentTooltip : PanelContainer
{
    // Complete container layout before exposing the hover card to the renderer.
    public void FitBeforeShowing()
    {
        void Layout(Node node)
        {
            if(node is Container container)container.Notification((int)Container.NotificationSortChildren);
            foreach(var child in node.GetChildren())Layout(child);
        }
        void IgnoreMouse(Node node)
        {
            if(node is Control control)control.MouseFilter=MouseFilterEnum.Ignore;
            foreach(var child in node.GetChildren())IgnoreMouse(child);
        }
        IgnoreMouse(this);
        Size=new Vector2(364,0);
        for(int pass=0;pass<12;pass++){Layout(this);Size=GetCombinedMinimumSize();}
    }
    private static readonly Color Cyan=new("63e5ff"),Gold=new("ffdf25"),Green=new("35ff58"),Red=new("ff4444");
    private static StyleBoxFlat Frame(Color background,Color border,int width=1,int margin=8)=>new() {
        BgColor=background,BorderColor=border,BorderWidthLeft=width,BorderWidthTop=width,BorderWidthRight=width,BorderWidthBottom=width,
        ContentMarginLeft=margin,ContentMarginRight=margin,ContentMarginTop=margin,ContentMarginBottom=margin,
        CornerRadiusTopLeft=3,CornerRadiusTopRight=3,CornerRadiusBottomLeft=3,CornerRadiusBottomRight=3 };
    private static Label Text(string value,Color color,int size=16,bool right=false)
    {
        var label=FrontendSkin.Label(value,size,true);label.AddThemeColorOverride("font_color",color);
        label.HorizontalAlignment=right?HorizontalAlignment.Right:HorizontalAlignment.Left;
        return label;
    }
    public EquipmentTooltip(EquipmentTooltipData item)
    {
        Name="EquipmentTooltip";Theme=SwtorSkin.MakeTheme();MouseFilter=MouseFilterEnum.Ignore;
        AddThemeStyleboxOverride("panel",Frame(new Color("000509"),new Color("1e789a"),1,2));
        var body=new VBoxContainer {CustomMinimumSize=new Vector2(360,0),MouseFilter=MouseFilterEnum.Ignore};body.AddThemeConstantOverride("separation",0);AddChild(body);
        var header=new PanelContainer();header.AddThemeStyleboxOverride("panel",Frame(new Color(item.RarityColor.R*.16f,item.RarityColor.G*.16f,item.RarityColor.B*.16f),item.RarityColor,2,8));body.AddChild(header);
        var row=new HBoxContainer();header.AddChild(row);
        var iconFrame=new PanelContainer {CustomMinimumSize=new Vector2(58,58)};var iconBox=SwtorSkin.Box("bwaui_buttons_381",6,6,6,6);iconBox.ModulateColor=item.RarityColor;iconFrame.AddThemeStyleboxOverride("panel",iconBox);row.AddChild(iconFrame);
        iconFrame.AddChild(new TextureRect {Texture=item.Icon,CustomMinimumSize=new Vector2(46,46),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered});
        var title=new VBoxContainer {SizeFlagsHorizontal=SizeFlags.ExpandFill};title.AddThemeConstantOverride("separation",2);row.AddChild(title);
        title.AddChild(Text(item.Name,item.RarityColor,18));title.AddChild(Text("Upgrade Rank +"+item.Rank,Cyan,21));
        VBoxContainer Section()
        {
            var margin=new MarginContainer();foreach(string side in new[]{"left","right","top","bottom"})margin.AddThemeConstantOverride("margin_"+side,7);body.AddChild(margin);
            var section=new VBoxContainer();section.AddThemeConstantOverride("separation",2);margin.AddChild(section);return section;
        }
        void Divider()=>body.AddChild(new ColorRect {Color=new Color("124257"),CustomMinimumSize=new Vector2(0,1),MouseFilter=MouseFilterEnum.Ignore});
        var identity=Section();identity.AddChild(Text(item.Rarity,Cyan));identity.AddChild(Text(char.ToUpperInvariant(item.Kind[0])+item.Kind[1..],Cyan));
        if(item.Stats.Length>0){Divider();var stats=Section();stats.AddChild(Text("Total Stats",Gold));foreach(string line in item.Stats)stats.AddChild(Text("    "+line,Cyan));}
        if(item.Effects.Length>0){Divider();var effects=Section();foreach(string line in item.Effects)effects.AddChild(Text("Equip: "+line,Green));}
        if(item.Description!=""){Divider();Section().AddChild(Text(item.Description,new Color("a3c3cc")));}
        Divider();Section().AddChild(Text(item.Requirements,item.Compatible?Cyan:Red,16,true));
        if(item.Comparison.Length>0){Divider();var comparison=Section();comparison.AddChild(Text("Currently Equipped",Gold));foreach(var pair in item.Comparison){comparison.AddChild(Text(pair.Key,Cyan,15));comparison.AddChild(Text(pair.Value,SwtorSkin.TextColor,16,true));}}
        Divider();Section().AddChild(Text(item.UpgradeCost is int cost?cost.ToString("N0")+" Credits  ·  Upgrade to +"+(item.Rank+1):"Maximum upgrade rank",item.UpgradeCost==null?Cyan:item.Affordable?Green:Red,16,true));
    }
}




