using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors.UI;

/// <summary>Survivors arrangement of recovered SWTOR controls; no original runtime dependencies.</summary>
public partial class SurvivorsHud : CanvasLayer
{
    public GameWorld World { get; set; }=null!;
    [Export] public string ArenaName { get; set; }="Korriban";
    private Control _root=null!;
    private CombatHud _combatHud=null!;
    private Label _wave=null!, _elapsed=null!, _defeated=null!, _ranks=null!, _deathText=null!;
    private Control _statusRoot=null!;
    private PanelContainer _deathFrame=null!;
    public int ActionSlotCount => _combatHud.SlotCount;
    public int UnlockedAbilityCount => _combatHud.UnlockedAbilityCount;
    private Control _modal=null!;
    private Control _selectionLayout=null!;
    private int _offerLevel = -1;
    private HBoxContainer _choices=null!;
    private Label _chooseTitle=null!, _chooseHint=null!;
    public int VisibleChoiceCount => _choices.GetChildCount();
    public bool SelectionVisible => _modal.Visible;

    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        _root=new Control { Theme=SwtorSkin.MakeTheme(),MouseFilter=Control.MouseFilterEnum.Ignore };
        AddChild(_root); _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        // The player-reference arrangement: one quickbar, with unit frames above its ends.
        _combatHud=new CombatHud {World=World};_root.AddChild(_combatHud);
        BuildStatus();
        var restart=new SwtorButton {Text="RESTART [R]",TooltipText="Start a new run and reset all progression.",CustomMinimumSize=new Vector2(104,28)};
        restart.AddThemeFontSizeOverride("font_size",12);_root.AddChild(restart);
        restart.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        restart.OffsetLeft=-120;restart.OffsetRight=-16;restart.OffsetTop=16;restart.OffsetBottom=44;
        restart.Pressed+=World.ResetCombatWorld;
        _deathFrame=ChromePanel();_root.AddChild(_deathFrame);
        _deathFrame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop);
        _deathFrame.OffsetLeft=-220;_deathFrame.OffsetRight=220;_deathFrame.OffsetTop=100;_deathFrame.OffsetBottom=198;
        var deathActions = new VBoxContainer(); _deathFrame.AddChild(deathActions);
        _deathText=SwtorSkin.Text("DEFEATED  ·  PRESS R TO BEGIN AGAIN",18,true);_deathText.HorizontalAlignment=HorizontalAlignment.Center;
        deathActions.AddChild(_deathText);
        var returnMenu = new SwtorButton { Text = "RETURN TO MAIN MENU", CustomMinimumSize = new Vector2(240,36) };
        deathActions.AddChild(returnMenu); returnMenu.Pressed += World.ReturnToMainMenu;
        _deathFrame.Visible=false;

        BuildSelection();
        World.Progression!.Changed+=RefreshChoices;
        RefreshChoices();
        _root.Resized+=LayoutDock;LayoutDock();
    }

    private static PanelContainer ChromePanel()
    {
        var panel=new PanelContainer();var box=SwtorSkin.Box("bwaui_tooltip_1",8,12,8,12);
        box.ContentMarginLeft=14;box.ContentMarginRight=14;box.ContentMarginTop=4;box.ContentMarginBottom=4;
        panel.AddThemeStyleboxOverride("panel",box);return panel;
    }

    private void BuildStatus()
    {
        // Area heading and separate counters float over the world; no stretched quickbar frame.
        _statusRoot=new Control {Size=new Vector2(380,64),MouseFilter=Control.MouseFilterEnum.Ignore};
        _root.AddChild(_statusRoot);
        Label Counter(string text,int font,Vector2 at,Vector2 size)
        {
            var label=SwtorSkin.Text(text,font);label.Position=at;label.Size=size;
            label.HorizontalAlignment=HorizontalAlignment.Center;label.VerticalAlignment=VerticalAlignment.Center;
            _statusRoot.AddChild(label);return label;
        }
        var arena=Counter(ArenaName.ToUpperInvariant(),20,Vector2.Zero,new Vector2(380,25));
        arena.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        _statusRoot.AddChild(new TextureRect {Texture=SwtorSkin.Slice("bwaui_bars_183"),
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Position=new Vector2(52,28),Size=new Vector2(276,1),
            MouseFilter=Control.MouseFilterEnum.Ignore});
        _wave=Counter("",14,new Vector2(8,34),new Vector2(112,22));
        _elapsed=Counter("",15,new Vector2(128,34),new Vector2(100,22));
        _defeated=Counter("",14,new Vector2(236,34),new Vector2(140,22));
    }

    private void LayoutDock()
    {
        float scale=Mathf.Max(0.1f,Mathf.Min(1.12f,Mathf.Min((_root.Size.X-32)/CombatHud.DesignWidth,_root.Size.Y/900)));
        _combatHud.Scale=Vector2.One*scale;
        _combatHud.Position=new Vector2((_root.Size.X-CombatHud.DesignWidth*scale)/2,_root.Size.Y-CombatHud.DesignHeight*scale-6);
        float statusScale=Mathf.Clamp((_root.Size.X-256)/380,0.5f,1);
        _statusRoot.Scale=Vector2.One*statusScale;
        _statusRoot.Position=new Vector2((_root.Size.X-380*statusScale)/2,14);
        float choiceScale = Mathf.Min(1, Mathf.Min((_root.Size.X-24)/836, (_root.Size.Y-32)/540));
        _selectionLayout.Scale = Vector2.One * Mathf.Max(0.3f, choiceScale);
        _selectionLayout.Position = (_root.Size - _selectionLayout.Size * _selectionLayout.Scale) / 2;
    }

    private void BuildSelection()
    {
        _modal=new Control {MouseFilter=Control.MouseFilterEnum.Stop};_root.AddChild(_modal);
        _modal.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade=new ColorRect {Color=new Color(0,0.015f,0.035f,0.65f),MouseFilter=Control.MouseFilterEnum.Stop};
        _modal.AddChild(shade);shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _selectionLayout=new Control {Size=new Vector2(812,500), MouseFilter=Control.MouseFilterEnum.Ignore};_modal.AddChild(_selectionLayout);
        var frame=new SwtorPanel {CustomMinimumSize=new Vector2(812,500)};_selectionLayout.AddChild(frame);
        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var selectionBox=SwtorSkin.Box("bwaui_standard_window_1",12,14,12,12);
        selectionBox.ContentMarginLeft=24;selectionBox.ContentMarginRight=24;
        selectionBox.ContentMarginTop=24;selectionBox.ContentMarginBottom=20;
        frame.AddThemeStyleboxOverride("panel",selectionBox);
        var body=new VBoxContainer();frame.AddChild(body);
        _chooseTitle=SwtorSkin.Text("CHOOSE A POWER",22);_chooseTitle.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        _chooseTitle.HorizontalAlignment=HorizontalAlignment.Center;body.AddChild(_chooseTitle);
        _chooseHint=SwtorSkin.Text("",14);_chooseHint.HorizontalAlignment=HorizontalAlignment.Center;body.AddChild(_chooseHint);
        _choices=new HBoxContainer {SizeFlagsVertical=Control.SizeFlags.ExpandFill};body.AddChild(_choices);
        _ranks=SwtorSkin.Text("",13);_ranks.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        _ranks.AddThemeColorOverride("font_color",new Color("a5bac7"));body.AddChild(_ranks);
        var footer=new HBoxContainer();body.AddChild(footer);
        var instructions=SwtorSkin.Text("Click a power or press 1 / 2 / 3.",14);
        instructions.VerticalAlignment=VerticalAlignment.Center;instructions.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;footer.AddChild(instructions);
        var reset=new SwtorButton {Text="RESTART [R]",CustomMinimumSize=new Vector2(112,30)};
        reset.AddThemeFontSizeOverride("font_size",13);footer.AddChild(reset);reset.Pressed+=World.ResetCombatWorld;
    }

    private void RefreshChoices()
    {
        _combatHud.RefreshAbilities();
        var progression=World.Progression!;
        _modal.Visible=progression.PresentationPhase is ProgressionPresentationPhase.Choosing or ProgressionPresentationPhase.Confirming;
        if (progression.Selecting && _offerLevel == progression.Level)
        {
            bool confirming = progression.PresentationPhase == ProgressionPresentationPhase.Confirming;
            _chooseTitle.Text = confirming ? "POWER APPLIED" : "CHOOSE A POWER";
            _chooseHint.Text = confirming ? progression.Offers[progression.SelectedOfferIndex].Title.ToUpperInvariant() + "  ·  " + progression.Offers[progression.SelectedOfferIndex].Rarity.ToString().ToUpperInvariant()
                : $"LEVEL {progression.Level}  ·  COMBAT PAUSED";
            for (int i=0; i<_choices.GetChildCount(); i++)
            {
                var card = _choices.GetChild<UpgradeCard>(i); card.Disabled = !progression.CanChoose;
                if (confirming) card.Confirm(i == progression.SelectedOfferIndex);
            }
            if (progression.CanChoose) _choices.GetChild<Control>(0).GrabFocus();
            return;
        }
        // Keep cards stable while ordinary XP changes; only rebuild at an offer boundary.
        foreach (var child in _choices.GetChildren()) { _choices.RemoveChild(child); child.QueueFree(); }
        if (!progression.Selecting) { _offerLevel = -1; return; }
        _offerLevel = progression.Level; _chooseTitle.Text = "CHOOSE A POWER";
        _chooseHint.Text=$"LEVEL {progression.Level}  ·  COMBAT PAUSED";
        for (int i=0;i<progression.Offers.Count;i++)
        {
            int index=i;var definition=progression.Offers[i];
            var card=new UpgradeCard(definition,progression.Rank(definition),i+1);
            card.Disabled = !progression.CanChoose;
            card.Pressed+=()=>progression.Choose(index);_choices.AddChild(card);
        }
        _ranks.Text="TRAINED  ·  "+string.Join("   /   ",progression.Upgrades.Where(u=>progression.Rank(u)>0)
            .Select(u=>u.Effect==UpgradeEffect.UnlockAbility?$"{u.Title} unlocked":$"{u.Title} {progression.Rank(u)}"));
        if (progression.Ranks.Count==0) _ranks.Text="Power bonuses last for this run.";
        if (progression.CanChoose) _choices.GetChild<Control>(0).GrabFocus();
    }

    public override void _Process(double delta)
    {
        _combatHud.UpdateHud();
        var time=TimeSpan.FromSeconds(World.Progression!.ElapsedSeconds);
        _wave.Text=$"WAVE {World.Waves!.Wave}";
        _elapsed.Text=$"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        _defeated.Text=$"{World.Spawner!.Defeated} DEFEATED";
        _deathFrame.Visible=!World.PlayerActor.Alive;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey {Pressed:true,Echo:false} key) return;
        bool handled=true;
        if (key.PhysicalKeycode==Key.R) World.ResetCombatWorld();
        else if (key.PhysicalKeycode==Key.Escape) GetTree().Quit();
        else if (World.Progression!.CanChoose && key.PhysicalKeycode is >= Key.Key1 and <= Key.Key3)
            World.Progression.Choose((int)key.PhysicalKeycode-(int)Key.Key1);
        else handled=false;
        if (handled) GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(World.Progression)) World.Progression!.Changed-=RefreshChoices;
    }
}
