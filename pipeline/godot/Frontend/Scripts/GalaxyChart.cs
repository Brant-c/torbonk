using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Frontend;

/// <summary>World destinations and operation encounters share the existing selection chrome.</summary>
public partial class GalaxyChart : Control
{
    public Action<string>? DestinationSelected;
    public Action<string>? DifficultySelected;
    public Action<bool>? CategorySelected;
    public Action? NavigationChanged;
    public Action? CourseRequested;
    public Func<string?>? AvailabilityReason;
    public IReadOnlyList<DestinationChoice> Destinations=>_destinations;
    public Button CourseButton {get;private set;}=null!;
    public IEnumerable<Button> NavigationButtons => new[]{_worldTab,_operationsTab}
        .Concat(_destinations.Where(b=>b.Visible).Cast<Button>())
        .Concat(_bosses.Where(b=>b.Visible))
        .Concat(_difficultyButtons.Where(b=>b.IsVisibleInTree())).Append(CourseButton);
    public bool OperationsSelected {get;private set;}
    public string Difficulty {get;private set;}="Hard";
    public OperationModelPreview ModelPreview=>_preview;
    private readonly List<DestinationChoice> _destinations=new();
    private readonly List<Button> _bosses=new(),_difficultyButtons=new();
    private readonly Dictionary<string,bool> _expanded=new();
    private readonly Dictionary<string,Button> _bossByMap=new();
    private readonly Dictionary<string,Label> _disclosures=new();
    private readonly Dictionary<string,Control> _connectors=new();
    private readonly Dictionary<bool,string> _remembered=new();
    private TextureRect _artwork=null!;
    private OperationModelPreview _preview=null!;
    private Control _difficulty=null!;
    private Button _worldTab=null!,_operationsTab=null!;
    private Label _title=null!,_region=null!,_description=null!,_availability=null!;
    private FrontendCatalog _catalog=null!;
    private MapDefinition _selected=null!;
    private static readonly Color Rule=new(.28f,.7f,1,.45f);

    private Label Line(string value,int size,Vector2 at,Vector2 dimensions,Color color,bool bold=false,bool wrap=false)
    {
        var l=FrontendSkin.Label(value,size,wrap);l.Position=at;l.Size=dimensions;l.AddThemeColorOverride("font_color",color);
        if(bold)l.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));AddChild(l);return l;
    }
    public void Populate(FrontendCatalog catalog,string selected,string difficulty="Hard")
    {
        _catalog=catalog;_selected=catalog.Map(selected);OperationsSelected=_selected.IsOperation;
        Difficulty=new[]{"Normal","Hard","Nightmare"}.Contains(difficulty)?difficulty:"Hard";
        _worldTab=Tab("Worlds",false,24);_operationsTab=Tab("Operations",true,148);
        foreach(var map in catalog.Maps.Where(m=>m.GameplayImplemented))
        {
            var b=new DestinationChoice {Name="Destination_"+map.Id,Map=map,Size=new Vector2(310,98)};
            b.Pressed+=()=>{
                if(map.IsOperation) { _expanded[map.Id]=!_expanded[map.Id];LayoutChoices();NavigationChanged?.Invoke(); }
                else Choose(map);
            };AddChild(b);_destinations.Add(b);
            if(!map.IsOperation)continue;
            _expanded[map.Id]=true;
            var disclosure=FrontendSkin.Label("▾",21);disclosure.Position=new Vector2(286,32);
            disclosure.AddThemeColorOverride("font_color",SwtorSkin.Blue);b.AddChild(disclosure);_disclosures[map.Id]=disclosure;
            var boss=FrontendSkin.Button(map.EncounterName.Replace(" XRR-3","\nXRR-3"),()=>Choose(map));
            boss.Name="Encounter_"+map.EncounterId;boss.Size=new Vector2(284,88);boss.CustomMinimumSize=new Vector2(284,88);
            boss.Icon=FrontendSkin.Texture("res://Assets/WarriorLibrary/Textures/gfx/icons/slicedroid.png");
            boss.AddThemeConstantOverride("icon_max_width",44);boss.AddThemeFontSizeOverride("font_size",19);
            boss.TooltipText="01 · "+map.EncounterRegion;AddChild(boss);
            // SwtorButton applies its default icon size on entering the tree.
            boss.AddThemeConstantOverride("icon_max_width",44);
            _bosses.Add(boss);_bossByMap[map.Id]=boss;
            var connector=new Control {MouseFilter=MouseFilterEnum.Ignore};AddChild(connector);_connectors[map.Id]=connector;
            connector.AddChild(new ColorRect {Size=new Vector2(1,46),Color=Rule,MouseFilter=MouseFilterEnum.Ignore});
            connector.AddChild(new ColorRect {Position=new Vector2(0,45),Size=new Vector2(15,1),Color=Rule,MouseFilter=MouseFilterEnum.Ignore});
        }
        var panel=new Panel {Position=new Vector2(350,40),Size=new Vector2(1098,600),MouseFilter=MouseFilterEnum.Ignore};
        panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18});AddChild(panel);
        foreach(float y in new[]{0f,598f})panel.AddChild(new ColorRect {Position=new Vector2(50,y),Size=new Vector2(998,2),Color=Rule,MouseFilter=MouseFilterEnum.Ignore});
        _artwork=new TextureRect {Name="DestinationLandscape",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Position=new Vector2(372,55),Size=new Vector2(1054,375),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=MouseFilterEnum.Ignore};AddChild(_artwork);
        _preview=new OperationModelPreview {Name="OperationModelPreview",Position=_artwork.Position,Size=_artwork.Size,Visible=false};AddChild(_preview);
        _title=Line("",34,new Vector2(400,442),new Vector2(998,50),new Color("e4edf2"),true);_title.Name="DestinationName";
        _region=Line("",19,new Vector2(400,495),new Vector2(690,32),new Color("81cbed"));
        _description=Line("",22,new Vector2(400,539),new Vector2(680,90),new Color("bdcbd4"),wrap:true);
        _availability=Line("",18,new Vector2(400,615),new Vector2(700,26),new Color("bdcbd4"));
        _difficulty=new Control {Name="OperationDifficulty",Position=new Vector2(400,562),Size=new Vector2(680,45)};AddChild(_difficulty);
        var label=FrontendSkin.Heading("DIFFICULTY",18);label.Size=new Vector2(112,40);_difficulty.AddChild(label);
        foreach(string level in new[]{"Normal","Hard","Nightmare"})
        {
            var button=FrontendSkin.Button(level,()=>{Difficulty=level;RefreshDifficulty();DifficultySelected?.Invoke(level);});
            button.Name="Difficulty_"+level;button.Position=new Vector2(118+_difficultyButtons.Count*173,0);
            button.TooltipText=level switch {
                "Normal"=>"Lower enemy health and damage, fewer enemies and longer attack warnings.",
                "Nightmare"=>"Stronger enemies, more pressure and shorter attack warnings.",
                _=>"The original encounter tuning."};
            button.Size=button.CustomMinimumSize=new Vector2(166,40);button.AddThemeFontSizeOverride("font_size",18);
            _difficulty.AddChild(button);_difficultyButtons.Add(button);
        }
        CourseButton=FrontendSkin.Button("Set Course",()=>CourseRequested?.Invoke());CourseButton.Icon=SwtorSkin.Slice("bwaui_buttons_132");
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})CourseButton.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        CourseButton.AddThemeFontSizeOverride("font_size",26);CourseButton.AddThemeFontOverride("font",GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));CourseButton.AddThemeColorOverride("font_color",new Color("a2e6ff"));
        foreach(string color in new[]{"font_hover_color","font_focus_color","font_pressed_color"})CourseButton.AddThemeColorOverride(color,Colors.White);
        CourseButton.Position=new Vector2(1169,528);CourseButton.Size=new Vector2(250,52);AddChild(CourseButton);
        LayoutChoices();Select(_selected);
    }
    private Button Tab(string text,bool operation,float x)
    {
        var button=FrontendSkin.Button(text.ToUpperInvariant(),()=>ShowCategory(operation));
        button.Name=operation?"OperationsTab":"WorldsTab";button.Position=new Vector2(x,32);
        button.Size=button.CustomMinimumSize=new Vector2(operation?166:116,43);button.AddThemeFontSizeOverride("font_size",19);
        foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})button.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
        var underline=new ColorRect {Name="SelectedRule",Position=new Vector2(0,40),Size=new Vector2(operation?156:106,2),Color=new Color("a2e6ff"),MouseFilter=MouseFilterEnum.Ignore};button.AddChild(underline);
        AddChild(button);return button;
    }
    public void ShowCategory(bool operation)
    {
        if(OperationsSelected==operation)return;
        _remembered[OperationsSelected]=_selected.Id;OperationsSelected=operation;
        var map=_catalog.Maps.FirstOrDefault(m=>m.GameplayImplemented && m.IsOperation==operation && _remembered.GetValueOrDefault(operation)==m.Id)
            ?? _catalog.Maps.FirstOrDefault(m=>m.GameplayImplemented && m.IsOperation==operation);
        if(map==null)return;
        if(operation)_expanded[map.Id]=true;
        Choose(map);LayoutChoices();CategorySelected?.Invoke(operation);NavigationChanged?.Invoke();
    }
    private void Choose(MapDefinition map) {DestinationSelected?.Invoke(map.Id);Select(map);NavigationChanged?.Invoke();}
    private void LayoutChoices()
    {
        foreach(var tab in new[]{_worldTab,_operationsTab})
        {bool selected=(tab==_operationsTab)==OperationsSelected;tab.GetNode<Control>("SelectedRule").Visible=selected;
            tab.AddThemeColorOverride("font_color",selected?new Color("a2e6ff"):SwtorSkin.Blue);tab.SetMeta("selected",false);}
        float y=100;
        foreach(var b in _destinations)
        {
            var map=b.Map;b.Visible=map.IsOperation==OperationsSelected;b.Position=new Vector2(0,y);
            if(b.Visible)y+=103;
            if(!map.IsOperation)continue;
            var boss=_bossByMap[map.Id];boss.Visible=b.Visible && _expanded[map.Id];boss.Position=new Vector2(26,y);
            _connectors[map.Id].Position=new Vector2(11,y-9);_connectors[map.Id].Visible=boss.Visible;
            _disclosures[map.Id].Text=_expanded[map.Id]?"▾":"▸";
            if(boss.Visible)y+=96;
        }
        if(CourseButton!=null)
        {
            var parent=_destinations.First(b=>b.Map.Id==_selected.Id);
            CourseButton.FocusNeighborLeft=(_selected.IsOperation && _bossByMap[_selected.Id].Visible
                ? _bossByMap[_selected.Id] : parent).GetPath();
        }
    }
    private void RefreshDifficulty()
    {
        foreach(var button in _difficultyButtons)
        {
            bool selected=button.Text==Difficulty;button.SetMeta("selected",selected);
            foreach(string state in new[]{"normal","hover","pressed","hover_pressed"})
                button.AddThemeStyleboxOverride(state,FrontendSkin.ButtonArtwork(hovered:selected || state!="normal",selected:selected));
            button.AddThemeColorOverride("font_color",selected?new Color("a2e6ff"):FrontendSkin.TextColor);
            button.AccessibilityDescription=selected?"Selected difficulty":"Select difficulty";
        }
    }
    private void Select(MapDefinition map)
    {
        _selected=map;
        foreach(var b in _destinations)b.Selected=!map.IsOperation && b.Map.Id==map.Id;
        foreach(var pair in _bossByMap)
        {
            bool selected=pair.Key==map.Id;pair.Value.SetMeta("selected",selected);
            foreach(string state in new[]{"normal","hover","pressed","hover_pressed"})
                pair.Value.AddThemeStyleboxOverride(state,FrontendSkin.ButtonArtwork(hovered:selected || state!="normal",selected:selected));
        }
        string art=map.Id switch {"korriban"=>"korriban_overlay","hutta"=>"hutta_overlay","dromund"=>"dromund_kaas_overlay","balmorra"=>"bal_overlay",_=>""};
        string artwork=art==""?map.Artwork:"res://Assets/UI/loading/"+art+".png";
        _artwork.Texture=string.IsNullOrEmpty(artwork)?null:FrontendSkin.Texture(artwork);
        _preview.Visible=map.IsOperation && !string.IsNullOrEmpty(map.PreviewModel);
        if(_preview.Visible)_preview.ShowModel(map.PreviewModel);
        _artwork.Visible=!_preview.Visible;
        _difficulty.Visible=map.IsOperation;RefreshDifficulty();
        _title.Text=map.IsOperation?map.EncounterName:map.Name;
        _region.Text=map.IsOperation?(map.Name+" · "+map.EncounterRegion).ToUpperInvariant():map.Region.ToUpperInvariant();
        _description.Position=new Vector2(400,map.IsOperation?530:539);
        _description.Size=new Vector2(map.IsOperation?740:680,map.IsOperation?32:90);
        _description.AddThemeFontSizeOverride("font_size",map.IsOperation?19:22);
        _description.Text=map.IsOperation?"Survive the frozen surface and face "+map.EncounterName+".":map.Description;
        string? reason=AvailabilityReason?.Invoke();_availability.Text=reason!=null?"Unavailable for this class":"";
        CourseButton.Disabled=reason!=null;CourseButton.TooltipText=reason ?? "";
        var target=map.IsOperation && _bossByMap[map.Id].Visible?_bossByMap[map.Id]:_destinations.First(b=>b.Map.Id==map.Id);
        CourseButton.FocusNeighborLeft=target.GetPath();
    }
}
