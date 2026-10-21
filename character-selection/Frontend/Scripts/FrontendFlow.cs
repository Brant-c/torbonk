using Godot;

namespace SwtorSurvivors.Frontend;

/// <summary>C1 menu controller. Owns no gameplay scenes, launch calls, autoloads or disk saves.</summary>
public partial class FrontendFlow : Control
{
    [Signal] public delegate void ConfigurationReadyEventHandler(string configurationJson);
    public SelectionConfiguration CurrentConfiguration => _session.Configuration.Snapshot();
    private SelectionSession _session = new();
    private FrontendCatalog _catalog = null!;
    private Control _canvas = null!;
    private SelectionPage? _page;
    private readonly string[] _steps = ["MainMenu", "Faction", "BaseClass", "AdvancedClass", "Customization", "Galaxy", "Ready"];
    private int _step;
    private AudioStreamPlayer _sound = null!;
    private Label _trail = null!;
    private Control _navigation = null!;
    private bool _muted;
    public override void _Ready()
    {
        Theme = FrontendSkin.MakeTheme(); _catalog = FrontendCatalog.Load();
        // The original star texture fades to transparency at its edges. Keep the
        // entire host dark beneath it, including any space outside the fitted UI.
        var backing = new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(backing); backing.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var stars = new TextureRect {
            Texture = FrontendSkin.Texture(FrontendSkin.Assets+"Recovered/char_select_stars.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Modulate = new Color(.4f,.5f,.65f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(stars); stars.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _canvas = new Control { Name = "DesignCanvas", Size = new Vector2(1600,900), MouseFilter = MouseFilterEnum.Ignore }; AddChild(_canvas);
        _sound = new AudioStreamPlayer { VolumeDb = -12 }; AddChild(_sound);
        Resized += Fit; Fit(); ShowStep(0);
    }
    private void Fit()
    {
        if (_canvas == null) return;
        float scale = Mathf.Min(Size.X/1600,Size.Y/900);
        _canvas.Scale = Vector2.One * scale;
        _canvas.Position = (Size-new Vector2(1600,900)*scale)*.5f;
    }
    public void RestoreConfiguration(SelectionConfiguration config)
    { _session.Restore(config, _catalog); if (_page != null) ShowStep(_step); }
    private void Sound(string name = "select")
    {
        string path = FrontendSkin.Assets + "Audio/" + name + ".wav";
        if (!_muted && ResourceLoader.Exists(path)) { _sound.Stream = GD.Load<AudioStream>(path); _sound.Play(); }
    }
    private void ShowStep(int step)
    {
        _step = Mathf.Clamp(step,0,6);
        foreach (var child in _canvas.GetChildren()) { _canvas.RemoveChild(child); child.QueueFree(); }
        _page = _step == 0 ? new SelectionPage { StepId = "MainMenu" }
            : GD.Load<PackedScene>("res://Frontend/Scenes/"+_steps[_step]+".tscn").Instantiate<SelectionPage>();
        _page.Position = new Vector2(76,150); _page.Size = new Vector2(1448,610); _canvas.AddChild(_page);
        if (_step == 0) { MainMenu(); return; }
        var brand = FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_101.png",new Vector2(240,76));
        brand.Position = new Vector2(76,25); _canvas.AddChild(brand);
        var crumb = FrontendSkin.Label(string.Join("   ›   ",_steps.Skip(1).Select(s=>s.ToUpper())),15);
        crumb.Position = new Vector2(630,55); _canvas.AddChild(crumb);
        _trail = FrontendSkin.Label("",17); _trail.Position = new Vector2(76,784); _canvas.AddChild(_trail); UpdateTrail();
        _navigation = new Control { Position = new Vector2(76,824), Size = new Vector2(1448,56) }; _canvas.AddChild(_navigation);
        var back = FrontendSkin.Button("‹  Back",()=> { Sound("back"); ShowStep(_step-1); }); back.Size = new Vector2(180,52); _navigation.AddChild(back);
        var menu = FrontendSkin.Button("Main Menu",()=> { Sound("back"); ShowStep(0); }); menu.Position = new Vector2(200,0); menu.Size = new Vector2(200,52); _navigation.AddChild(menu);
        var next = FrontendSkin.Button(_step==6 ? "Play — Integration Pending" : "Continue  ›",()=> { Sound(_step==5 ? "map" : "select"); ShowStep(_step+1); });
        next.Position = new Vector2(1088,0); next.Size = new Vector2(360,52); next.Disabled = _step==6; _navigation.AddChild(next);
        switch (_step)
        {
            case 1: Factions(); break;
            case 2: Classes(); break;
            case 3: Advanced(); break;
            case 4: Appearance(); break;
            case 5: Galaxy(); break;
            case 6: ReadyScreen(); break;
        }
        _page.Modulate = new Color(1,1,1,0);
        CreateTween().TweenProperty(_page,"modulate:a",1,.18);
        back.GrabFocus();
    }
    private void UpdateTrail()
    {
        var c = _session.Configuration;
        _trail.Text = _catalog.Factions.Single(f=>f.Id==c.FactionId).Name+"  /  "+_catalog.Class(c.BaseClassId).Name+"  /  "+_catalog.Advanced(c.AdvancedClassId).Name+"  /  "+_catalog.Map(c.MapId).Name;
    }
    private static VBoxContainer Column(int width) => new() { CustomMinimumSize = new Vector2(width,0) };
    private void Place(Control control,float x,float y,float w,float h)
    { control.Position=new Vector2(x,y); control.Size=new Vector2(w,h); _page!.AddChild(control); }
    private void MainMenu()
    {
        var planet=FrontendSkin.Planet(FrontendSkin.Assets+"Recovered/char_select_tython_high_res_d.png",870);
        Place(planet,630,-100,870,870);
        var logo=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_101.png",new Vector2(540,175)); Place(logo,10,-75,540,175);
        Place(FrontendSkin.Label("A galaxy of stories. One fight to survive.",21),25,183,570,40);
        var menu=Column(420);
        menu.AddChild(FrontendSkin.Button("New Run",()=> { _session=new SelectionSession(); Sound(); ShowStep(1); }));
        menu.AddChild(FrontendSkin.Button("Character Selection",()=> { Sound(); ShowStep(1); }));
        menu.AddChild(FrontendSkin.Button("Settings",()=> ShowSettings()));
        menu.AddChild(FrontendSkin.Button("Exit",()=> GetTree().Quit()));
        Place(menu,50,276,420,270);
        Place(FrontendSkin.Label("C1 FRONTEND PREVIEW",16),50,580,500,28);
        Place(FrontendSkin.Label("Gameplay integration pending. Your selections remain here while browsing.",17,true),50,614,590,60);
    }
    private void ShowSettings()
    {
        var dialog=new AcceptDialog { Title="Settings", DialogText="Full game settings will be connected during integration.\nThis preview has a local interface-sound toggle.", MinSize=new Vector2I(610,260) };
        AddChild(dialog);
        var mute=FrontendSkin.Button(_muted ? "Enable Interface Sounds" : "Mute Interface Sounds",()=> { _muted=!_muted; dialog.QueueFree(); });
        mute.Position=new Vector2(50,130); mute.Size=new Vector2(480,50); dialog.AddChild(mute);
        dialog.CloseRequested += ()=>dialog.QueueFree(); dialog.Confirmed += ()=>dialog.QueueFree(); dialog.PopupCentered();
    }
    private void Factions()
    {
        var heading=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_104.png",new Vector2(640,42)); Place(heading,404,0,640,42);
        for (int i=0;i<_catalog.Factions.Length;i++)
        {
            var faction=_catalog.Factions[i]; bool selected=_session.Configuration.FactionId==faction.Id;
            var column=Column(472);
            column.AddChild(FrontendSkin.Art(faction.Emblem,new Vector2(472,128)));
            var name=FrontendSkin.Label(faction.Name.ToUpper(),26); name.HorizontalAlignment=HorizontalAlignment.Center; column.AddChild(name);
            column.AddChild(FrontendSkin.Label(faction.Description,20,true));
            column.AddChild(FrontendSkin.Label(faction.Id=="empire" ? "Includes Sith Sorcerer • Gameplay content available" : "Four classic classes • Frontend preview only",17,true));
            column.AddChild(FrontendSkin.Button(selected ? "Allegiance Selected" : "Choose "+faction.Name,()=> { _session.SelectFaction(faction.Id,_catalog); Sound(); ShowStep(1); },selected));
            _page!.AddChild(FrontendSkin.Panel(column,new Vector2(i==0 ? 104:800,52),new Vector2(548,548),creation:true));
        }
    }
    private CharacterPreview Preview(string art,AdvancedClassDefinition? advanced,float x=365,float width=650)
    {
        var preview=GD.Load<PackedScene>("res://Frontend/Scenes/CharacterPreview.tscn").Instantiate<CharacterPreview>();
        preview.Position=new Vector2(x,0); preview.Size=new Vector2(width,610); _page!.AddChild(preview);
        preview.ShowClass(art,advanced); return preview;
    }
    private void Classes()
    {
        var selected=_catalog.Class(_session.Configuration.BaseClassId);
        var choices=Column(320); choices.AddChild(FrontendSkin.Label("CLASS",18));
        foreach (var cls in _catalog.Classes.Where(c=>c.FactionId==_session.Configuration.FactionId))
        {
            var b=FrontendSkin.Button(cls.Name,()=> { _session.SelectClass(cls.Id,_catalog); Sound(); ShowStep(2); },cls.Id==selected.Id);
            b.Icon=FrontendSkin.Texture(cls.Icon); b.ExpandIcon=true; b.AddThemeConstantOverride("icon_max_width",54); b.CustomMinimumSize=new Vector2(320,90); choices.AddChild(b);
        }
        choices.AddChild(FrontendSkin.Label("Classic April 2012 class structure",17,true)); Place(choices,0,20,320,550);
        Preview(selected.Artwork,null);
        var details=new VBoxContainer(); details.AddChild(FrontendSkin.Label(selected.Name.ToUpper(),24,true));
        details.AddChild(FrontendSkin.Label(selected.Description,22,true));
        details.AddChild(FrontendSkin.Label("ADVANCED CLASSES",18));
        foreach (var a in selected.AdvancedClasses) details.AddChild(FrontendSkin.Label(a.Name+"  ·  "+(a.GameplayImplemented ? "Content available" : "Not yet playable"),20,true));
        details.AddChild(FrontendSkin.Label("Continue to choose your advanced class.",18,true));
        _page!.AddChild(FrontendSkin.Panel(details,new Vector2(1040,20),new Vector2(408,550),insetTitle:true));
    }
    private void Advanced()
    {
        var cls=_catalog.Class(_session.Configuration.BaseClassId); var selected=_catalog.Advanced(_session.Configuration.AdvancedClassId);
        var choices=Column(320); choices.AddChild(FrontendSkin.Label(cls.Name.ToUpper(),23,true));
        choices.AddChild(FrontendSkin.Art(cls.Icon,new Vector2(90,90)));
        foreach (var a in cls.AdvancedClasses)
            choices.AddChild(FrontendSkin.Button(a.Name+"\n"+(a.GameplayImplemented ? "Gameplay content available" : "Not yet playable"),()=> { _session.SelectAdvanced(a.Id,_catalog); Sound(); ShowStep(3); },a.Id==selected.Id));
        Place(choices,0,20,320,500);
        Preview(selected.Artwork,selected);
        var details=new VBoxContainer(); details.AddChild(FrontendSkin.Label(selected.Name.ToUpper(),24,true));
        details.AddChild(FrontendSkin.Label(selected.Description,22,true));
        details.AddChild(FrontendSkin.Label(selected.GameplayImplemented ? "SORCERER COMBAT IMPLEMENTED\nFrontend integration pending" : "NOT YET PLAYABLE\nClass preview only",20,true));
        details.AddChild(FrontendSkin.Label(string.IsNullOrEmpty(selected.PreviewScene) ? "Original class artwork is shown. A compatible character model has not been staged." : "Original converted Sorcerer with the existing idle animation.",18,true));
        _page!.AddChild(FrontendSkin.Panel(details,new Vector2(1040,20),new Vector2(408,550),insetTitle:true));
    }
    private void Appearance()
    {
        var advanced=_catalog.Advanced(_session.Configuration.AdvancedClassId);
        Preview(advanced.Artwork,advanced,0,735);
        var col=Column(600); col.AddChild(FrontendSkin.Label("APPEARANCE PRESET",25));
        col.AddThemeConstantOverride("separation",8);
        col.AddChild(FrontendSkin.Label(advanced.Id=="sorcerer" ? "Original converted Sorcerer • Human / male / body type 2" : "Appearance unavailable for this class. The shared Sorcerer preset is retained for future use only.",19,true));
        var grid=new GridContainer { Columns=2 }; grid.AddThemeConstantOverride("h_separation",20); grid.AddThemeConstantOverride("v_separation",4);
        foreach (var row in new[] {("Species","Human"),("Gender / body presentation","Male"),("Body type","2 · bmn"),("Head / face","Source head A01"),("Hair","Source hair A01 / V01"),("Skin tone","Original material"),("Complexion","Source default"),("Equipment appearance","Sorcerer SS A04 / C01 / S01")})
        {
            grid.AddChild(FrontendSkin.Label(row.Item1,18));
            var control=FrontendSkin.Button(row.Item2,()=>{}); control.Disabled=true; control.CustomMinimumSize=new Vector2(320,34);
            control.AddThemeFontSizeOverride("font_size",17);
            control.AddThemeStyleboxOverride("disabled",FrontendSkin.ButtonArtwork(verticalPadding:3));
            control.TooltipText="Unavailable in C1: requires compatible source variations and conversion / material support."; grid.AddChild(control);
        }
        col.AddChild(grid); col.AddChild(FrontendSkin.Label("Appearance editing is unavailable in C1. No unsupported visual combinations are applied.",17,true));
        _page!.AddChild(FrontendSkin.Panel(col,new Vector2(765,0),new Vector2(683,610)));
    }
    private void Galaxy()
    {
        var map=_catalog.Map(_session.Configuration.MapId);
        var chart=new GalaxyChart { Size=new Vector2(960,610) }; _page!.AddChild(chart);
        chart.DestinationSelected += id=> { _session.SelectMap(id,_catalog); Sound("map"); ShowStep(5); }; chart.Populate(_catalog,map.Id);
        var col=Column(410); col.AddChild(FrontendSkin.Label(map.Name.ToUpper(),30));
        col.AddChild(FrontendSkin.Label(map.Region.ToUpper(),17));
        col.AddChild(FrontendSkin.Art(map.Artwork,new Vector2(410,170)));
        col.AddChild(FrontendSkin.Label(map.Description,20,true));
        col.AddChild(FrontendSkin.Label(map.GameplayImplemented ? "KORRIBAN — PLAYABLE CONTENT" : "UNAVAILABLE — FUTURE MAP",20,true));
        col.AddChild(FrontendSkin.Label(map.GameplayImplemented ? "Requires Sith Inquisitor → Sorcerer. Gameplay launching is pending integration." : "You can retain this destination for preview. No gameplay environment is included.",18,true));
        _page.AddChild(FrontendSkin.Panel(col,new Vector2(990,0),new Vector2(458,610),true));
    }
    private void ReadyScreen()
    {
        var c=_session.Configuration; var advanced=_catalog.Advanced(c.AdvancedClassId);
        Preview(advanced.Artwork,advanced,0,660);
        var col=Column(680); col.AddChild(FrontendSkin.Label("YOUR CHARACTER",27));
        col.AddThemeConstantOverride("separation",6);
        col.AddChild(FrontendSkin.Label(_catalog.Factions.Single(f=>f.Id==c.FactionId).Name+"\n"+_catalog.Class(c.BaseClassId).Name+"  →  "+advanced.Name,24,true));
        string appearance=advanced.Id=="sorcerer" ? "Human • Male • Body type 2\nHead A01 • Hair A01 / V01 • Original skin / complexion\nSorcerer SS A04 / C01 / S01 equipment" : "Class appearance unavailable\nSorcerer preset retained; not applied to this class";
        col.AddChild(FrontendSkin.Label(appearance,18,true));
        col.AddChild(FrontendSkin.Label("DESTINATION  /  "+_catalog.Map(c.MapId).Name.ToUpper(),25,true));
        col.AddChild(FrontendSkin.Art(_catalog.Map(c.MapId).Artwork,new Vector2(580,100)));
        col.AddChild(FrontendSkin.Label(c.Playable ? "CONTENT AVAILABLE • INTEGRATION PENDING" : "PREVIEW ONLY • NOT YET PLAYABLE",20,true));
        col.AddChild(FrontendSkin.Label(c.Playable ? "This is the supported Sorcerer / Korriban combination. Play remains disabled until manual integration." : "Only Sith Inquisitor → Sorcerer → Korriban has gameplay content. You may go back and change your selections.",18,true));
        col.AddChild(FrontendSkin.Button("Review Selection Configuration",ShowConfiguration));
        _page!.AddChild(FrontendSkin.Panel(col,new Vector2(705,0),new Vector2(743,610)));
        EmitSignal(SignalName.ConfigurationReady,c.ToJson());
    }
    private void ShowConfiguration()
    {
        var dialog=new AcceptDialog { Title="Selection Configuration • Schema 1", MinSize=new Vector2I(900,650) }; AddChild(dialog);
        var text=new TextEdit { Text=_session.Configuration.ToJson(), Editable=false, CustomMinimumSize=new Vector2(860,570) }; dialog.AddChild(text);
        dialog.Confirmed+=()=>dialog.QueueFree(); dialog.CloseRequested+=()=>dialog.QueueFree(); dialog.PopupCentered();
    }
    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed:true, Echo:false, Keycode:Key.Escape } && _step>0)
        { Sound("back"); ShowStep(_step-1); GetViewport().SetInputAsHandled(); }
    }
}
