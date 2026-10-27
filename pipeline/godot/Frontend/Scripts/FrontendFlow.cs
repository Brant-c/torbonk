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
    private string? _chosenFaction;
    private string? _chosenClass;
    private string? _chosenAdvanced;
    private SelectionPage? _page;
    private readonly string[] _steps = ["MainMenu", "Faction", "BaseClass", "AdvancedClass", "Galaxy"];
    private int _step;
    private AudioStreamPlayer _sound = null!;
    private bool _muted;
    private Control? _settings;
    private bool _collectionsOpen;
    private bool _talentsOpen;
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
    private void ShowStep(int step)
    {
        _collectionsOpen=false;
        _talentsOpen=false;
        if(step==1 && _step!=1)_chosenFaction=null;
        if(step==2 && _step!=2)_chosenClass=null;
        if(step==3 && _step!=3)_chosenAdvanced=null;
        _step = Mathf.Clamp(step,0,4);
        foreach (var child in _canvas.GetChildren())
            if (child!=_brand && child!=_navigation) { _canvas.RemoveChild(child); child.QueueFree(); }
        _brand.Visible=_step>1;
        _brand.Position=new Vector2(76,25);_brand.Size=_brand.CustomMinimumSize=new Vector2(240,76);
        _navigation.Visible=_step>0;
        _back.Disabled=_menu.Disabled=false;
        foreach(var button in new[]{_back,_menu})
        {
            button.FocusNext=button.FocusPrevious=button.FocusNeighborTop=new NodePath("");
            foreach(string state in new[]{"normal","hover","pressed","hover_pressed","disabled","focus"})
                if(_step is 1 or 2 or 3 or 4)button.AddThemeStyleboxOverride(state,new StyleBoxEmpty());
                else button.RemoveThemeStyleboxOverride(state);
            foreach(string color in new[]{"font_hover_color","font_focus_color","font_pressed_color"})
                if(_step is 1 or 2 or 3 or 4)button.AddThemeColorOverride(color,SwtorSurvivors.UI.SwtorSkin.Blue);
                else button.RemoveThemeColorOverride(color);
        }
        _page = _step == 0 ? new SelectionPage { StepId = "MainMenu" }
            : GD.Load<PackedScene>("res://Frontend/Scenes/"+_steps[_step]+".tscn").Instantiate<SelectionPage>();
        _page.Position = new Vector2(76,150); _page.Size = new Vector2(1448,610); _canvas.AddChild(_page);
        if (_step == 0) { MainMenu(); return; }
        if (_step == 1)
        {
            var allegiance=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_104.png",new Vector2(710,42));
            allegiance.Position=new Vector2(445,90); _canvas.AddChild(allegiance);
        }
        else
        {
            string[] headings=["", "", "Select Class", "Advanced Class", "Choose Destination"];
            var title=FrontendSkin.Heading(headings[_step],28); title.Position=new Vector2(420,45);
            title.Size=new Vector2(780,52); title.HorizontalAlignment=HorizontalAlignment.Center; _canvas.AddChild(title);
            if(_step is 2 or 3 or 4)
            {
                title.Position=new Vector2(500,48);title.Size=new Vector2(600,58);title.AddThemeFontSizeOverride("font_size",38);title.AddThemeColorOverride("font_color",new Color("dce9ef"));
                var faction=_catalog.Factions.Single(f=>f.Id==_session.Configuration.FactionId);
                var subtitle=FrontendSkin.Label((_step==4?"Select a world to explore":_step==3?_catalog.Class(_session.Configuration.BaseClassId).Name:faction.Name).ToUpperInvariant(),19);subtitle.Position=new Vector2(500,110);subtitle.Size=new Vector2(600,32);subtitle.HorizontalAlignment=HorizontalAlignment.Center;
                subtitle.AddThemeColorOverride("font_color",_step!=4 && faction.Id=="empire"?new Color(1,.25f,.22f):new Color("81cbed"));_canvas.AddChild(subtitle);
                if(_step==4){title.Position=new Vector2(450,48);title.Size=new Vector2(700,58);}
                _brand.Position=new Vector2(76,28);_brand.Size=_brand.CustomMinimumSize=new Vector2(260,84);
            }
        }
        switch (_step)
        {
            case 1: Factions(); break;
            case 2: Classes(); break;
            case 3: Advanced(); break;
            case 4: Galaxy(); break;
        }
        _page.Modulate = new Color(1,1,1,0);
        CreateTween().TweenProperty(_page,"modulate:a",1,.16);
        var selected=Buttons(_page).FirstOrDefault(b=>b.HasMeta("selected") && b.GetMeta("selected").AsBool());
        (selected ?? _back).GrabFocus();
    }
    private static VBoxContainer Column(int width) => new() { CustomMinimumSize = new Vector2(width,0) };
    private void Place(Control control,float x,float y,float w,float h)
    { control.Position=new Vector2(x,y); control.Size=new Vector2(w,h); _page!.AddChild(control); }
    private void MainMenu()
    {
        var logo=FrontendSkin.Art(FrontendSkin.Assets+"Slices/bwaui_character_create_window_101.png",new Vector2(540,175));
        Place(logo,454,-85,540,175);
        MainMenuChoice Entry(string title,string description,string icon,float x,float y,Action action)
        {
            var button=new MainMenuChoice {Text=title,Description=description,IconSlice=icon};
            button.Pressed+=action;Place(button,x,y,580,140);return button;
        }
        var start=Entry("New Run","Choose your allegiance. Begin a new journey.","bwaui_buttons_132",114,200,()=> { _session=new SelectionSession(); _chosenFaction=null; Sound(); ShowStep(1); });
        var collection=Entry("Collections","Explore your unlocked appearances.","bwaui_buttons_135",754,200,ShowCollections);
        var talents=Entry("Talents","Configure your combat style.","bwaui_buttons_135",114,365,ShowTalents);
        var settings=Entry("Settings","Adjust your display and interface sounds.","bwaui_buttons_90",754,365,ShowSettings);
        var exit=FrontendSkin.Button("Exit",()=>GetTree().Quit());Place(exit,624,540,200,52);
        talents.FocusNeighborTop=start.GetPath();talents.FocusNeighborRight=settings.GetPath();
        start.FocusNeighborBottom=talents.GetPath();
        start.FocusNeighborRight=collection.GetPath();start.FocusNeighborBottom=settings.GetPath();
        collection.FocusNeighborLeft=start.GetPath();collection.FocusNeighborBottom=exit.GetPath();
        settings.FocusNeighborTop=collection.GetPath();settings.FocusNeighborLeft=talents.GetPath();
        exit.FocusNeighborTop=collection.GetPath();exit.FocusNeighborLeft=settings.GetPath();
        start.FocusNeighborBottom=talents.GetPath();collection.FocusNeighborBottom=settings.GetPath();
        talents.FocusNeighborBottom=exit.GetPath();settings.FocusNeighborBottom=exit.GetPath();
        Control[] menuOrder=[start,collection,talents,settings,exit];
        for(int i=0;i<menuOrder.Length;i++)
        {menuOrder[i].FocusNext=menuOrder[(i+1)%menuOrder.Length].GetPath();menuOrder[i].FocusPrevious=menuOrder[(i+menuOrder.Length-1)%menuOrder.Length].GetPath();}
        start.GrabFocus();
    }
    public void ShowTalents()
    {
        ShowStep(0);Sound();_talentsOpen=true;
        foreach(var child in _page!.GetChildren()){_page.RemoveChild(child);child.QueueFree();}
        var screen=new TalentsScreen {Name="Talents",Size=new Vector2(1448,700),Closed=()=>ShowStep(0)};
        _page.AddChild(screen);
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
        body.AddChild(FrontendSkin.Label("Display Mode",20));
        var mode=new OptionButton { CustomMinimumSize=new Vector2(480,48) };
        mode.AddItem("Windowed"); mode.AddItem("Fullscreen");
        mode.Select(DisplaySettings.Fullscreen ? 1 : 0); body.AddChild(mode);
        body.AddChild(FrontendSkin.Label("Resolution",20));
        var resolution=new OptionButton { CustomMinimumSize=new Vector2(480,48) };
        var screen=DisplayServer.ScreenGetSize();
        var sizes=new[] { new Vector2I(1000,800),new Vector2I(1280,720),new Vector2I(1366,768),
            new Vector2I(1600,900),new Vector2I(1920,1080),new Vector2I(2560,1440),new Vector2I(3840,2160),
            DisplaySettings.Resolution,screen }
            .Where(s=>s.X>0 && s.Y>0 && s.X<=screen.X && s.Y<=screen.Y).Distinct().OrderBy(s=>s.X).ThenBy(s=>s.Y).ToArray();
        foreach(var size in sizes) resolution.AddItem($"{size.X} × {size.Y}");
        resolution.Select(Array.IndexOf(sizes,DisplaySettings.Resolution)); body.AddChild(resolution);
        var status=FrontendSkin.Label("Fullscreen uses this rendering resolution.",17,true); body.AddChild(status);
        void ApplyDisplay()
        {
            if(resolution.Selected<0) return;
            var error=DisplaySettings.Set(GetTree().Root,mode.Selected==1,sizes[resolution.Selected]);
            status.Text=error==Error.Ok ? "Display settings saved." : "Applied, but settings could not be saved.";
        }
        mode.ItemSelected+=_=>ApplyDisplay(); resolution.ItemSelected+=_=>ApplyDisplay();
        var close=FrontendSkin.Button("Close",CloseSettings); body.AddChild(close);
        _settings.AddChild(FrontendSkin.TitledPanel("Settings",body,new Vector2(530,180),new Vector2(540,540)));
        // Keep tab traversal inside the modal.
        Control[] controls=[mute,mode,resolution,close];
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
        var choices=new List<FactionChoice>();
        for (int i=0;i<_catalog.Factions.Length;i++)
        {
            var faction=_catalog.Factions[i];
            var choice=new FactionChoice {Name="Faction_"+faction.Id,Faction=faction,
                Classes=_catalog.Classes.Where(c=>c.FactionId==faction.Id).ToArray(),
                Selected=_chosenFaction==faction.Id};
            choice.Pressed+=()=>Choose(()=> { _chosenFaction=faction.Id; _session.SelectFaction(faction.Id,_catalog); },2);
            Place(choice,89+i*730,30,540,570);choices.Add(choice);
        }
        choices[0].FocusNeighborRight=choices[1].GetPath();
        choices[1].FocusNeighborLeft=choices[0].GetPath();
        Control[] focusOrder=[_back,_menu,choices[0],choices[1]];
        for(int i=0;i<focusOrder.Length;i++)
        {
            focusOrder[i].FocusNext=focusOrder[(i+1)%focusOrder.Length].GetPath();
            focusOrder[i].FocusPrevious=focusOrder[(i+focusOrder.Length-1)%focusOrder.Length].GetPath();
        }
        _back.FocusNeighborTop=choices[0].GetPath();_menu.FocusNeighborTop=choices[1].GetPath();
    }
    private void Classes()
    {
        var choices=new List<ClassChoice>();
        foreach (var cls in _catalog.Classes.Where(c=>c.FactionId==_session.Configuration.FactionId))
        {
            var b=new ClassChoice {Name="Class_"+cls.Id,Class=cls,Selected=_chosenClass==cls.Id};
            b.Pressed+=()=>Choose(()=>{_chosenClass=cls.Id;_session.SelectClass(cls.Id,_catalog);},3);
            Place(b,-10+choices.Count*375,50,343,540);choices.Add(b);
        }
        for(int i=0;i<choices.Count;i++)
        {
            if(i>0)choices[i].FocusNeighborLeft=choices[i-1].GetPath();
            if(i+1<choices.Count)choices[i].FocusNeighborRight=choices[i+1].GetPath();
            choices[i].FocusNeighborBottom=_back.GetPath();
        }
        Control[] order=[_back,_menu,..choices];
        for(int i=0;i<order.Length;i++){order[i].FocusNext=order[(i+1)%order.Length].GetPath();order[i].FocusPrevious=order[(i+order.Length-1)%order.Length].GetPath();}
        _back.FocusNeighborTop=choices[0].GetPath();_menu.FocusNeighborTop=choices[0].GetPath();
    }
    private void Advanced()
    {
        var cls=_catalog.Class(_session.Configuration.BaseClassId);
        var choices=new List<AdvancedClassChoice>();
        foreach (var a in cls.AdvancedClasses)
        {
            var b=new AdvancedClassChoice {Name="Advanced_"+a.Id,Advanced=a,FactionId=cls.FactionId,Selected=_chosenAdvanced==a.Id};
            b.Pressed+=()=>Choose(()=>{_chosenAdvanced=a.Id;_session.SelectAdvanced(a.Id,_catalog);},4);
            Place(b,154+choices.Count*610,45,530,550);choices.Add(b);
        }
        choices[0].FocusNeighborRight=choices[1].GetPath();choices[1].FocusNeighborLeft=choices[0].GetPath();
        foreach(var choice in choices)choice.FocusNeighborBottom=_back.GetPath();
        Control[] order=[_back,_menu,..choices];
        for(int i=0;i<order.Length;i++){order[i].FocusNext=order[(i+1)%order.Length].GetPath();order[i].FocusPrevious=order[(i+order.Length-1)%order.Length].GetPath();}
        _back.FocusNeighborTop=choices[0].GetPath();_menu.FocusNeighborTop=choices[0].GetPath();
    }
    private void Galaxy()
    {
        var map=_catalog.Map(_session.Configuration.MapId);
        var chart=new GalaxyChart { Size=new Vector2(1448,650),AvailabilityReason=()=>UnavailableReason }; _page!.AddChild(chart);
        chart.DestinationSelected+=id=>{_session.SelectMap(id,_catalog);Sound("map");};
        chart.CourseRequested+=()=>{if(_transitioning || _starting)return;Sound();StartRun();};chart.Populate(_catalog,map.Id);
        var destinations=chart.Destinations;
        Control[] order=[_back,_menu,..destinations,chart.CourseButton];
        for(int i=0;i<order.Length;i++){order[i].FocusNext=order[(i+1)%order.Length].GetPath();order[i].FocusPrevious=order[(i+order.Length-1)%order.Length].GetPath();}
        destinations[^1].FocusNeighborBottom=_back.GetPath();_back.FocusNeighborTop=destinations[^1].GetPath();_menu.FocusNeighborTop=destinations[^1].GetPath();
    }
    private void StartRun()
    {
        if (_starting) return;
        string? failure = UnavailableReason;
        if (failure == null)
        {
            _starting = true;
            EmitSignal(SignalName.ConfigurationReady,CurrentConfiguration.ToJson());
            failure = PlayRequested!(CurrentConfiguration);
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
            if (_collectionsOpen || _talentsOpen) {Sound("back");ShowStep(0);}
            else if (_settings!=null) CloseSettings();
            else if (_step>0 && !_transitioning) { Sound("back"); ShowStep(_step-1); }
            GetViewport().SetInputAsHandled();
        }
    }
}
