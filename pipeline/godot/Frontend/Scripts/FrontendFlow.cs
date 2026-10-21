using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwtorSurvivors.Frontend;

/// <summary>Session-only selection controller; the host retains all gameplay compatibility checks.</summary>
public partial class FrontendFlow : Control
{
    [Signal] public delegate void ConfigurationReadyEventHandler(string configurationJson);
    public SelectionConfiguration CurrentConfiguration => _session.Configuration.Snapshot();
    public Func<SelectionConfiguration, string?>? ValidateForPlay { get; set; }
    public Func<SelectionConfiguration, string?>? PlayRequested { get; set; }
    private string? UnavailableReason => ValidateForPlay?.Invoke(CurrentConfiguration)
        ?? (ValidateForPlay == null || PlayRequested == null ? "Gameplay unavailable." : null);
    private bool _starting, _transitioning;
    private SelectionSession _session = new();
    private FrontendCatalog _catalog = null!;
    private Control _canvas = null!;
    private TextureRect _brand = null!;
    private Control _navigation = null!;
    private Button _back = null!, _menu = null!;
    private Button? _next;
    private string? _chosenFaction;
    private SelectionPage? _page;
    private readonly string[] _steps = ["MainMenu", "Faction", "BaseClass", "AdvancedClass", "Galaxy", "Ready"];
    private int _step;
    private AudioStreamPlayer _sound = null!;
    private bool _muted;
    public float MenuMusicVolume { get; private set; } = .5f;
    public event Action<float>? MenuMusicVolumeChanged;
    private Control? _settings;
    private bool _collectionsOpen;
    public override void _Ready()
    {
        Theme = FrontendSkin.MakeTheme(); _catalog = FrontendCatalog.Load();
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
        BuildNavigation();
        Resized += Fit; Fit(); ShowStep(0);
    }
    private void BuildNavigation()
    {
        // Keep shared chrome alive while replacing page/preview trees.
        _brand=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_101.png",new Vector2(240,76));
        _brand.Position=new Vector2(76,25); _brand.ZIndex=10; _canvas.AddChild(_brand);
        _navigation=new Control {Position=new Vector2(76,824),Size=new Vector2(1448,56),ZIndex=10}; _canvas.AddChild(_navigation);
        _back=FrontendSkin.Button("Back",()=> {if (_transitioning) return; Sound("back"); ShowStep(_step-1);});
        _back.Size=new Vector2(180,52); _navigation.AddChild(_back);
        _menu=FrontendSkin.Button("Main Menu",()=> {if (_transitioning) return; Sound("back"); ShowStep(0);});
        _menu.Position=new Vector2(200,0); _menu.Size=new Vector2(200,52); _navigation.AddChild(_menu);
    }
    private void Fit()
    {
        if (_canvas == null) return;
        float scale = Mathf.Min(Size.X/1600,Size.Y/900);
        _canvas.Scale = Vector2.One * scale;
        _canvas.Position = (Size-new Vector2(1600,900)*scale)*.5f;
    }
    public void RestoreConfiguration(SelectionConfiguration config)
    { if (_transitioning) return; _session.Restore(config, _catalog); if (_page != null) ShowStep(_step); }
    private void Sound(string name = "select")
    {
        string path = FrontendSkin.Assets + "Audio/" + name + ".wav";
        if (!_muted && ResourceLoader.Exists(path)) { _sound.Stream = GD.Load<AudioStream>(path); _sound.Play(); }
    }
    private static IEnumerable<Button> Buttons(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Button button) yield return button;
            foreach (var nested in Buttons(child)) yield return nested;
        }
    }
    private void Choose(Action select, int destination, string cue = "select")
    {
        if (_transitioning || _starting) return;
        select(); Sound(cue);
        // Rebuild immediately with the selected frame, icon and details before leaving.
        ShowStep(_step); _transitioning=true;
        foreach (var button in Buttons(_canvas)) button.Disabled=true;
        var page=_page;
        GetTree().CreateTimer(.34).Timeout += () => {
            if (!IsInsideTree() || _page != page) return;
            _transitioning=false; ShowStep(destination);
        };
    }
    private void Browse(Action select, string cue = "select")
    {
        if (_transitioning || _starting) return;
        select(); Sound(cue); ShowStep(_step);
    }
    private void ContinueButton(int destination, string text = "Continue", Action? action = null)
    {
        _next=FrontendSkin.Button(text,()=> {
            if (_transitioning || _starting) return;
            if (action!=null) action(); else { Sound(); ShowStep(destination); }
        });
        _next.CustomMinimumSize=new Vector2(200,52);
        if (text=="Play")
        {
            _next.Disabled=UnavailableReason!=null;
            _next.TooltipText=UnavailableReason ?? "";
        }
        _next.Position=new Vector2(_navigation.Size.X-_next.CustomMinimumSize.X,0);
        _next.Size=_next.CustomMinimumSize;
        _navigation.AddChild(_next);
    }
    private void ShowStep(int step)
    {
        _collectionsOpen=false;
        _step = Mathf.Clamp(step,0,5);
        if (_next!=null) { _navigation.RemoveChild(_next); _next.QueueFree(); }
        _next=null;
        foreach (var child in _canvas.GetChildren())
            if (child!=_brand && child!=_navigation) { _canvas.RemoveChild(child); child.QueueFree(); }
        _brand.Visible=_step>1;
        _navigation.Visible=_step>0;
        _back.Disabled=_menu.Disabled=false;
        _page = _step == 0 ? new SelectionPage { StepId = "MainMenu" }
            : GD.Load<PackedScene>("res://Frontend/Scenes/"+_steps[_step]+".tscn").Instantiate<SelectionPage>();
        _page.Position = new Vector2(76,150); _page.Size = new Vector2(1448,610); _canvas.AddChild(_page);
        if (_step == 0) { MainMenu(); return; }
        if (_step == 1)
        {
            var allegiance=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_104.png",new Vector2(710,42));
            allegiance.Position=new Vector2(445,60); _canvas.AddChild(allegiance);
        }
        else
        {
            string[] headings=["", "", "Select Class", "Advanced Class", "Galaxy Map", "Ready"];
            var title=FrontendSkin.Heading(headings[_step],28); title.Position=new Vector2(420,45);
            title.Size=new Vector2(780,52); title.HorizontalAlignment=HorizontalAlignment.Center; _canvas.AddChild(title);
        }
        switch (_step)
        {
            case 1: Factions(); break;
            case 2: Classes(); break;
            case 3: Advanced(); break;
            case 4: Galaxy(); break;
            case 5: ReadyScreen(); break;
        }
        _page.Modulate = new Color(1,1,1,0);
        CreateTween().TweenProperty(_page,"modulate:a",1,.16);
        var selected=Buttons(_page).FirstOrDefault(b=>b.HasMeta("selected") && b.GetMeta("selected").AsBool());
        (selected ?? (_next is { Disabled:false } ? _next : _back)).GrabFocus();
    }
    private static VBoxContainer Column(int width) => new() { CustomMinimumSize = new Vector2(width,0) };
    private void Place(Control control,float x,float y,float w,float h)
    { control.Position=new Vector2(x,y); control.Size=new Vector2(w,h); _page!.AddChild(control); }
    private void MainMenu()
    {
        var planet=FrontendSkin.Planet(FrontendSkin.Assets+"Recovered/char_select_tython_high_res_d.png",870);
        Place(planet,630,-100,870,870);
        var logo=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_101.png",new Vector2(540,175));
        Place(logo,10,-75,540,175);
        var menu=Column(420); menu.AddThemeConstantOverride("separation",18);
        menu.AddChild(FrontendSkin.Button("New Run",()=> { _session=new SelectionSession(); _chosenFaction=null; Sound(); ShowStep(1); }));
        menu.AddChild(FrontendSkin.Button("Collections",ShowCollections));
        menu.AddChild(FrontendSkin.Button("Settings",ShowSettings));
        menu.AddChild(FrontendSkin.Button("Exit",()=> GetTree().Quit()));
        Place(menu,50,245,420,280); menu.GetChild<Control>(0).GrabFocus();
    }
    public void ShowCollections()
    {
        ShowStep(0);Sound();_collectionsOpen=true;
        foreach(var child in _page!.GetChildren()) {_page.RemoveChild(child);child.QueueFree();}
        var screen=new CollectionsScreen {Name="Collections",Size=new Vector2(1448,650),Closed=()=>ShowStep(0)};
        _page.AddChild(screen);
    }
    private void CloseSettings()
    {
        _settings?.QueueFree(); _settings=null;
        foreach (var button in Buttons(_page!)) button.FocusMode=FocusModeEnum.All;
        Buttons(_page!).First(b=>b.Text=="Settings").GrabFocus();
    }
    private void ShowSettings()
    {
        if (_settings!=null) return;
        foreach (var button in Buttons(_page!)) button.FocusMode=FocusModeEnum.None;
        _settings=new Control { Size=new Vector2(1600,900), MouseFilter=MouseFilterEnum.Stop }; _canvas.AddChild(_settings);
        _settings.AddChild(new ColorRect { Color=new Color(0,0,0,.72f), Size=_settings.Size });
        var body=Column(480); body.AddThemeConstantOverride("separation",24);
        Button mute=null!;
        mute=FrontendSkin.Button("Interface Sounds: "+(_muted?"Off":"On"),()=> {
            _muted=!_muted; mute.Text="Interface Sounds: "+(_muted?"Off":"On"); Sound();
        }); body.AddChild(mute);
        var musicLabel=FrontendSkin.Label($"Menu Music: {MenuMusicVolume*100:0}%",20);
        body.AddChild(musicLabel);
        var musicVolume=new HSlider { Name="MenuMusicVolume",MinValue=0,MaxValue=100,Step=1,
            Value=MenuMusicVolume*100,CustomMinimumSize=new Vector2(480,36),FocusMode=FocusModeEnum.All };
        musicVolume.ValueChanged+=value=> {
            MenuMusicVolume=(float)value/100;
            musicLabel.Text=$"Menu Music: {value:0}%";
            MenuMusicVolumeChanged?.Invoke(MenuMusicVolume);
        };
        body.AddChild(musicVolume);
        var close=FrontendSkin.Button("Close",CloseSettings); body.AddChild(close);
        _settings.AddChild(FrontendSkin.TitledPanel("Settings",body,new Vector2(530,250),new Vector2(540,400)));
        // Keep tab traversal inside the modal.
        Control[] controls=[mute,musicVolume,close];
        for(int i=0;i<controls.Length;i++)
        {
            var previous=controls[(i+controls.Length-1)%controls.Length].GetPath();
            var next=controls[(i+1)%controls.Length].GetPath();
            controls[i].FocusPrevious=previous; controls[i].FocusNext=next;
            controls[i].FocusNeighborTop=previous; controls[i].FocusNeighborBottom=next;
        }
        mute.GrabFocus();
    }
    private void Factions()
    {
        for (int i=0;i<_catalog.Factions.Length;i++)
        {
            var faction=_catalog.Factions[i]; bool selected=_chosenFaction==faction.Id;
            var column=Column(472); column.AddThemeConstantOverride("separation",24);
            column.AddChild(FrontendSkin.Art(faction.Emblem,new Vector2(472,172)));
            var description=FrontendSkin.Label(faction.Description,20,true); description.CustomMinimumSize=new Vector2(0,126); column.AddChild(description);
            column.AddChild(FrontendSkin.Button(faction.Name,()=>Choose(()=> { _chosenFaction=faction.Id; _session.SelectFaction(faction.Id,_catalog); },2),selected));
            _page!.AddChild(FrontendSkin.Panel(column,new Vector2(i==0 ? 104:800,24),new Vector2(548,548),creation:true));
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
        var choices=Column(320);
        foreach (var cls in _catalog.Classes.Where(c=>c.FactionId==_session.Configuration.FactionId))
        {
            var b=FrontendSkin.Button(cls.Name,()=>Browse(()=>_session.SelectClass(cls.Id,_catalog)),cls.Id==selected.Id);
            b.Icon=FrontendSkin.Texture(cls.Icon); b.ExpandIcon=true; b.AddThemeConstantOverride("icon_max_width",54);
            b.AddThemeColorOverride("icon_normal_color",cls.Id==selected.Id ? Colors.White : new Color(.65f,.7f,.75f));
            b.AddThemeColorOverride("icon_hover_color",Colors.White);
            b.CustomMinimumSize=new Vector2(320,90); choices.AddChild(b);
        }
        Place(choices,0,20,320,550); Preview(selected.Artwork,null);
        var details=new VBoxContainer(); details.AddChild(FrontendSkin.Label(selected.Description,20,true));
        details.AddChild(FrontendSkin.Heading("Advanced Classes",19));
        foreach (var a in selected.AdvancedClasses) details.AddChild(FrontendSkin.Label(a.Name,20,true));
        if (!selected.AdvancedClasses.Any(a=>a.GameplayImplemented)) details.AddChild(FrontendSkin.Label("Preview Only",18));
        _page!.AddChild(FrontendSkin.TitledPanel(selected.Name,details,new Vector2(1040,20),new Vector2(408,550)));
        ContinueButton(3);
    }
    private void Advanced()
    {
        var cls=_catalog.Class(_session.Configuration.BaseClassId); var selected=_catalog.Advanced(_session.Configuration.AdvancedClassId);
        var choices=Column(320); choices.AddChild(FrontendSkin.Art(cls.Icon,new Vector2(90,90)));
        foreach (var a in cls.AdvancedClasses)
            choices.AddChild(FrontendSkin.Button(a.Name,()=>Browse(()=>_session.SelectAdvanced(a.Id,_catalog)),a.Id==selected.Id));
        Place(choices,0,20,320,500); Preview(selected.Artwork,selected);
        var details=new VBoxContainer(); details.AddChild(FrontendSkin.Label(selected.Description,20,true));
        if (!selected.GameplayImplemented) details.AddChild(FrontendSkin.Label("Preview Only",18));
        _page!.AddChild(FrontendSkin.TitledPanel(selected.Name,details,new Vector2(1040,20),new Vector2(408,550)));
        ContinueButton(4);
    }
    private void Galaxy()
    {
        var map=_catalog.Map(_session.Configuration.MapId);
        var chart=new GalaxyChart { Size=new Vector2(960,610) }; _page!.AddChild(chart);
        chart.DestinationSelected += id=>Browse(()=>_session.SelectMap(id,_catalog),"map"); chart.Populate(_catalog,map.Id);
        var col=new VBoxContainer(); col.AddChild(FrontendSkin.Heading(map.Region,18));
        col.AddChild(FrontendSkin.Label(map.Description,20,true));
        if (UnavailableReason!=null) col.AddChild(FrontendSkin.Label("Unavailable for this class",18,true));
        _page.AddChild(FrontendSkin.TitledPanel(map.Name,col,new Vector2(990,0),new Vector2(458,610)));
        ContinueButton(5);
    }
    private void ReadyScreen()
    {
        var c=_session.Configuration; var advanced=_catalog.Advanced(c.AdvancedClassId);
        Preview(advanced.Artwork,advanced,0,660);
        var col=new VBoxContainer(); col.AddThemeConstantOverride("separation",18);
        col.AddChild(FrontendSkin.Heading(advanced.Name,24));
        col.AddChild(FrontendSkin.Label(_catalog.Factions.Single(f=>f.Id==c.FactionId).Name,20));
        if (advanced.Id=="sorcerer") col.AddChild(FrontendSkin.Label("Human · Male · Body Type 2",18));
        col.AddChild(FrontendSkin.Heading(_catalog.Map(c.MapId).Name,22));
        col.AddChild(FrontendSkin.Art(_catalog.Map(c.MapId).Artwork,new Vector2(580,160)));
        if (UnavailableReason!=null) col.AddChild(FrontendSkin.Label("Preview Only — Play Unavailable",20,true));
        _page!.AddChild(FrontendSkin.TitledPanel("Your Character",col,new Vector2(705,0),new Vector2(743,610)));
        ContinueButton(5,"Play",StartRun);
        EmitSignal(SignalName.ConfigurationReady,c.ToJson());
    }
    private void StartRun()
    {
        if (_starting) return;
        string? failure = UnavailableReason;
        if (failure == null)
        {
            _starting = true; failure = PlayRequested!(CurrentConfiguration);
            if (failure == null) { _sound.Stop(); return; }
            _starting = false;
        }
        var dialog = new AcceptDialog { Title = "Unable to start run", DialogText = failure, Theme=Theme };
        AddChild(dialog); dialog.Confirmed += () => dialog.QueueFree();
        dialog.CloseRequested += () => dialog.QueueFree(); dialog.PopupCentered();
    }
    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed:true, Echo:false, Keycode:Key.Escape })
        {
            if (_collectionsOpen) {Sound("back");ShowStep(0);}
            else if (_settings!=null) CloseSettings();
            else if (_step>0 && !_transitioning) { Sound("back"); ShowStep(_step-1); }
            GetViewport().SetInputAsHandled();
        }
    }
}
