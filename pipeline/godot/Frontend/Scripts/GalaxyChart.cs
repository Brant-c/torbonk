using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace SwtorSurvivors.Frontend;

/// <summary>Browse original planet landscapes without leaving the destination screen.</summary>
public partial class GalaxyChart : Control
{
    public Action<string>? DestinationSelected;
    public Action? CourseRequested;
    public Func<string?>? AvailabilityReason;
    public IReadOnlyList<DestinationChoice> Destinations=>_destinations;
    public Button CourseButton {get;private set;}=null!;
    private readonly List<DestinationChoice> _destinations=new();
    private TextureRect _artwork=null!;
    private Label _title=null!,_region=null!,_description=null!,_availability=null!;
    public void Populate(FrontendCatalog catalog,string selected)
    {
        Label Line(string value,int size,Vector2 at,Vector2 dimensions,Color color,bool bold=false,bool wrap=false)
        {
            var l=FrontendSkin.Label(value,size,wrap);l.Position=at;l.Size=dimensions;l.AddThemeColorOverride("font_color",color);
            if(bold)l.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));AddChild(l);return l;
        }
        Line("DESTINATIONS",19,new Vector2(24,40),new Vector2(280,35),new Color("81cbed"),true);
        foreach(var map in catalog.Maps.Where(m=>m.GameplayImplemented))
        {
            var b=new DestinationChoice {Name="Destination_"+map.Id,Map=map,Position=new Vector2(0,100+_destinations.Count*115),Size=new Vector2(310,98)};
            b.Pressed+=()=>{DestinationSelected?.Invoke(map.Id);Select(map);};AddChild(b);_destinations.Add(b);
        }
        var panel=new Panel {Position=new Vector2(350,40),Size=new Vector2(1098,600),MouseFilter=MouseFilterEnum.Ignore};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});AddChild(panel);
        foreach(float y in new[]{0f,598f})panel.AddChild(new ColorRect {Position=new Vector2(50,y),Size=new Vector2(998,2),Color=new Color(.28f,.7f,1,.45f),MouseFilter=MouseFilterEnum.Ignore});
        _artwork=new TextureRect {Name="DestinationLandscape",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Position=new Vector2(372,55),Size=new Vector2(1054,375),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};AddChild(_artwork);
        _title=Line("",34,new Vector2(400,442),new Vector2(700,50),new Color("e4edf2"),true);_title.Name="DestinationName";
        _region=Line("",19,new Vector2(400,495),new Vector2(690,32),new Color("81cbed"));
        _description=Line("",22,new Vector2(400,539),new Vector2(680,90),new Color("bdcbd4"),wrap:true);
        _availability=Line("",18,new Vector2(400,615),new Vector2(700,26),new Color("bdcbd4"));
        CourseButton=FrontendSkin.Button("Set Course",()=>CourseRequested?.Invoke());CourseButton.Icon=SwtorSurvivors.UI.SwtorSkin.Slice("bwaui_buttons_132");
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})CourseButton.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        CourseButton.AddThemeFontSizeOverride("font_size",26);CourseButton.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));CourseButton.AddThemeColorOverride("font_color",new Color("a2e6ff"));
        foreach(string color in new[]{"font_hover_color","font_focus_color","font_pressed_color"})CourseButton.AddThemeColorOverride(color,Colors.White);
        CourseButton.Position=new Vector2(1169,528);CourseButton.Size=new Vector2(250,52);AddChild(CourseButton);
        for(int i=0;i<_destinations.Count;i++)
        {
            if(i>0)_destinations[i].FocusNeighborTop=_destinations[i-1].GetPath();
            if(i+1<_destinations.Count)_destinations[i].FocusNeighborBottom=_destinations[i+1].GetPath();
            _destinations[i].FocusNeighborRight=CourseButton.GetPath();
        }
        CourseButton.FocusNeighborLeft=_destinations.First(b=>b.Map.Id==selected).GetPath();Select(catalog.Map(selected));
    }
    private void Select(MapDefinition map)
    {
        foreach(var b in _destinations)b.Selected=b.Map.Id==map.Id;
        string art=map.Id switch {"korriban"=>"korriban_overlay","hutta"=>"hutta_overlay","dromund"=>"dromund_kaas_overlay","balmorra"=>"bal_overlay",_=>""};
        _artwork.Texture=FrontendSkin.Texture(art==""?map.Artwork:"res://Assets/UI/loading/"+art+".png");
        _title.Text=map.Name;_region.Text=map.Region.ToUpperInvariant();_description.Text=map.Description;
        string? reason=AvailabilityReason?.Invoke();
        _availability.Text=reason!=null?"Unavailable for this class":"";
        CourseButton.Disabled=reason!=null;
        CourseButton.TooltipText=reason ?? "";
        CourseButton.FocusNeighborLeft=_destinations.First(b=>b.Map.Id==map.Id).GetPath();
    }
}
