using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors.UI;

/// <summary>Survivors arrangement of recovered SWTOR controls; no original runtime dependencies.</summary>
public partial class SurvivorsHud : CanvasLayer
{
    public GameWorld World { get; set; }=null!;
    public RunMenu Menu {get;private set;}=null!;
    public CombatMeter Meter {get;private set;}=null!;
    [Export] public string ArenaName { get; set; }="Korriban";
    private Control _root=null!;
    private CombatHud _combatHud=null!;
    private Label _wave=null!, _elapsed=null!, _defeated=null!, _deathText=null!;
    private Control _statusRoot=null!;
    private PanelContainer _deathFrame=null!;
    public int ActionSlotCount => _combatHud.SlotCount;
    public int UnlockedAbilityCount => _combatHud.UnlockedAbilityCount;
    private Control _modal=null!;
    private Control _selectionLayout=null!;
    private int _offerLevel = -1;
    private HBoxContainer _choices=null!;
    private Label _chooseTitle=null!;
    private bool _showingReplacement, _banishing;
    private int _offerRevision=-1;
    private HBoxContainer _choiceActions=null!;
    private SwtorButton _banish=null!, _reroll=null!, _skip=null!;
    private SwtorButton _backChoice=null!;
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
        Meter=new CombatMeter {World=World};_root.AddChild(Meter);
        Menu=new RunMenu {Name="RunMenu",World=World};AddChild(Menu);
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
        var panel=new PanelContainer();var box=SwtorSkin.MenuFrame();
        box.ContentMarginLeft=14;box.ContentMarginRight=14;box.ContentMarginTop=4;box.ContentMarginBottom=4;
        panel.AddThemeStyleboxOverride("panel",box);return panel;
    }

    private void BuildStatus()
    {
        // Compact menu-style tracker. Scale the source chrome uniformly before
        // nine-slicing so its cap and bevels fit without flattening the artwork.
        _statusRoot=new Control {Name="RunStatusPanel",Size=new Vector2(180,156),MouseFilter=Control.MouseFilterEnum.Ignore};
        _root.AddChild(_statusRoot);
        _statusRoot.AddChild(new NinePatchRect {Name="RunStatusFrame",Texture=SwtorSkin.Slice("bwaui_standard_window_1"),
            Size=_statusRoot.Size*2,Scale=Vector2.One*.5f,
            PatchMarginLeft=128,PatchMarginRight=184,PatchMarginTop=72,PatchMarginBottom=64,
            MouseFilter=Control.MouseFilterEnum.Ignore});
        Label Counter(string text,int font,Vector2 at,Vector2 size,HorizontalAlignment alignment=HorizontalAlignment.Left)
        {
            var label=SwtorSkin.Text(text,font);label.Position=at;label.Size=size;
            label.HorizontalAlignment=alignment;label.VerticalAlignment=VerticalAlignment.Center;
            _statusRoot.AddChild(label);return label;
        }
        var arena=Counter(ArenaName.ToUpperInvariant(),14,new Vector2(15,11),new Vector2(150,20));
        arena.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        Counter("Wave",14,new Vector2(16,44),new Vector2(76,22));
        Counter("Time",14,new Vector2(16,70),new Vector2(76,22));
        Counter("Defeated",14,new Vector2(16,96),new Vector2(76,22));
        _wave=Counter("",17,new Vector2(92,44),new Vector2(72,22),HorizontalAlignment.Right);
        _elapsed=Counter("",16,new Vector2(92,70),new Vector2(72,22),HorizontalAlignment.Right);
        _defeated=Counter("",17,new Vector2(92,96),new Vector2(72,22),HorizontalAlignment.Right);
    }

    private void LayoutDock()
    {
        float scale=Mathf.Max(0.1f,Mathf.Min(1.12f,Mathf.Min((_root.Size.X-32)/CombatHud.DesignWidth,_root.Size.Y/900)));
        _combatHud.Scale=Vector2.One*scale;
        _combatHud.Position=new Vector2((_root.Size.X-CombatHud.DesignWidth*scale)/2,_root.Size.Y-CombatHud.DesignHeight*scale-6);
        float statusScale=Mathf.Clamp(Mathf.Min(_root.Size.X/800,_root.Size.Y/600),0.6f,1);
        _statusRoot.Scale=Vector2.One*statusScale;
        // Match the existing boss dock's bounds; on narrow windows put the tracker below it.
        float bossScale=Mathf.Max(.35f,Mathf.Min(1.25f,Mathf.Min((_root.Size.X-24)/600,_root.Size.Y/800)));
        float bossLeft=(_root.Size.X-600*bossScale)/2;
        float statusTop=16+_statusRoot.Size.X*statusScale+8>bossLeft ? 170*bossScale+12 : 16;
        _statusRoot.Position=new Vector2(16,statusTop);
        Meter.Scale=Vector2.One*statusScale;
        Meter.Position=new Vector2(16,statusTop+(_statusRoot.Size.Y+8)*statusScale);
        float choiceScale = Mathf.Min(1, Mathf.Min((_root.Size.X-24)/(_selectionLayout.Size.X+24), (_root.Size.Y-32)/(_selectionLayout.Size.Y+40)));
        _selectionLayout.Scale = Vector2.One * Mathf.Max(0.3f, choiceScale);
        _selectionLayout.Position = (_root.Size - _selectionLayout.Size * _selectionLayout.Scale) / 2;
    }

    private void BuildSelection()
    {
        _modal=new Control {MouseFilter=Control.MouseFilterEnum.Stop};_root.AddChild(_modal);
        _modal.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade=new ColorRect {Color=new Color(0,0.015f,0.035f,0.65f),MouseFilter=Control.MouseFilterEnum.Stop};
        _modal.AddChild(shade);shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _selectionLayout=new Control {Size=new Vector2(812,530), MouseFilter=Control.MouseFilterEnum.Ignore};_modal.AddChild(_selectionLayout);
        var frame=new SwtorPanel {CustomMinimumSize=_selectionLayout.Size};_selectionLayout.AddChild(frame);
        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var selectionBox=SwtorSkin.MenuFrame();
        selectionBox.ContentMarginLeft=24;selectionBox.ContentMarginRight=24;
        selectionBox.ContentMarginTop=24;selectionBox.ContentMarginBottom=64;
        frame.AddThemeStyleboxOverride("panel",selectionBox);
        var body=new VBoxContainer();body.AddThemeConstantOverride("separation",20);frame.AddChild(body);
        _chooseTitle=SwtorSkin.Text("CHOOSE A POWER",20);_chooseTitle.AddThemeColorOverride("font_color",SwtorSkin.Blue);
        _chooseTitle.HorizontalAlignment=HorizontalAlignment.Center;body.AddChild(_chooseTitle);
        _choices=new HBoxContainer {SizeFlagsVertical=Control.SizeFlags.Fill};body.AddChild(_choices);
        _choiceActions=new HBoxContainer {Alignment=BoxContainer.AlignmentMode.Center};body.AddChild(_choiceActions);
        _banish=new SwtorButton {CustomMinimumSize=new Vector2(220,36),TooltipText="Select a card to remove it for this run and refill its choice. Requires an eligible replacement."};
        _reroll=new SwtorButton {CustomMinimumSize=new Vector2(220,36),TooltipText="Replace the current choices. No charge is spent if there are too few fresh eligible options."};
        _skip=new SwtorButton {CustomMinimumSize=new Vector2(220,36)};
        _choiceActions.AddChild(_banish);_choiceActions.AddChild(_reroll);_choiceActions.AddChild(_skip);
        _banish.Pressed+=()=>{_banishing=!_banishing;RefreshChoices();};
        _reroll.Pressed+=()=>{_banishing=false;World.Progression!.Reroll();RefreshChoices();};
        _skip.Pressed+=()=>{_banishing=false;World.Progression!.Skip();};
        _backChoice=new SwtorButton {Text="BACK",CustomMinimumSize=new Vector2(160,32),SizeFlagsHorizontal=Control.SizeFlags.ShrinkCenter};
        _backChoice.Pressed+=()=>World.Progression!.CancelReplacement();body.AddChild(_backChoice);_backChoice.Visible=false;
    }

    private void RefreshChoices()
    {
        _combatHud.RefreshAbilities();
        var progression=World.Progression!;
        _modal.Visible=progression.PresentationPhase is ProgressionPresentationPhase.Choosing or ProgressionPresentationPhase.Confirming;
        _backChoice.Visible=progression.Replacing;
        if(!progression.CanChoose || progression.Replacing || progression.BanishesRemaining==0)_banishing=false;
        _choiceActions.Visible=!progression.Replacing;
        _banish.Text=_banishing?"CANCEL BANISH":$"BANISH ({progression.BanishesRemaining}/3)";
        _reroll.Text=$"REROLL ({progression.RerollsRemaining}/3)";
        _skip.Text=$"SKIP ({progression.SkipsRemaining}/2) · +{progression.SkipExperience} XP";
        _banish.Disabled=!progression.CanChoose || progression.BanishesRemaining==0;
        _reroll.Disabled=!progression.CanChoose || progression.RerollsRemaining==0;
        _skip.Disabled=!progression.CanChoose || progression.SkipsRemaining==0;
        string heading=_banishing?"SELECT A POWER TO BANISH":progression.AbilityChoice?$"CHOOSE AN ABILITY · LEVEL {progression.Level}":progression.Replacing?$"REPLACE AN ABILITY WITH {progression.ReplacementTitle.ToUpperInvariant()}":progression.SpecChoice?$"CHOOSE A TALENT · LEVEL {progression.Level}":"CHOOSE A POWER";
        if (progression.Selecting && _offerLevel == progression.Level && _offerRevision==progression.OfferRevision && _showingReplacement==progression.Replacing)
        {
            bool confirming = progression.PresentationPhase == ProgressionPresentationPhase.Confirming;
            _chooseTitle.Text = confirming ? (progression.SpecChoice?"TALENT APPLIED":"POWER APPLIED") : heading;
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
        _offerLevel = progression.Level;_offerRevision=progression.OfferRevision;_showingReplacement=progression.Replacing;_chooseTitle.Text=heading;
        _selectionLayout.Size=new Vector2(progression.Replacing?1040:812,progression.Replacing?510:530);LayoutDock();
        if(progression.Replacing)
        {
            for(int i=0;i<progression.ReplacementOptions.Count;i++)
            {
                int index=i;var ability=progression.ReplacementOptions[i];string spec=SpecMilestones.SpecFor(World.RunConfiguration.AdvancedClassId,ability.Definition.Id);
                var definition=new UpgradeDefinition {Title=ability.Definition.Title,Icon=ability.Definition.Icon,MilestoneLevel=progression.Level,Spec=spec,
                    Rarity=progression.AbilityRarity(ability.Definition.Id),Role=spec=="darkness"?"tank":"dps",
                    ChoiceDescription=ability.Definition.PeriodicDuration>0?
                        $"{ability.DamagePerTick+ability.Definition.PeriodicDamage*(ability.DamagePerTick/ability.Definition.Damage)*Mathf.Floor(ability.Definition.PeriodicDuration/ability.Definition.PeriodicInterval):0.#} total damage\n{ability.CooldownSeconds:0.#}s cooldown":
                        $"{ability.DamagePerTick:0.#} damage\n{ability.CooldownSeconds:0.#}s cooldown"};
                var card=new UpgradeCard(definition,0,i+1,action:"REPLACE");card.Pressed+=()=>progression.Replace(index);_choices.AddChild(card);
            }
            _choices.GetChild<Control>(0).GrabFocus();return;
        }
        for (int i=0;i<progression.Offers.Count;i++)
        {
            int index=i;var definition=progression.Offers[i];
            var ability=World.Abilities.Find(definition.AbilityId);
            string action=definition.Effect==UpgradeEffect.UnlockAbility && ability!=null && !World.Abilities.CanUnlock(ability)?"REPLACE":"";
            var card=new UpgradeCard(definition,progression.Rank(definition),i+1,action:action);
            card.Disabled = !progression.CanChoose;
            card.Pressed+=()=>SelectOffer(index);_choices.AddChild(card);
        }
        if(progression.PresentationPhase==ProgressionPresentationPhase.Confirming)
        {
            _chooseTitle.Text=progression.SpecChoice?"TALENT APPLIED":"POWER APPLIED";
            for(int i=0;i<_choices.GetChildCount();i++)_choices.GetChild<UpgradeCard>(i).Confirm(i==progression.SelectedOfferIndex);
        }
        if (progression.CanChoose) _choices.GetChild<Control>(0).GrabFocus();
    }

    private void SelectOffer(int index)
    {
        if(_banishing)
        {
            _banishing=false;World.Progression!.Banish(index);RefreshChoices();
        }
        else World.Progression!.Choose(index);
    }

    public override void _Process(double delta)
    {
        _combatHud.UpdateHud();
        var time=TimeSpan.FromSeconds(World.Progression!.ElapsedSeconds);
        _wave.Text=$"{World.Waves!.Wave}";
        _elapsed.Text=$"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        _defeated.Text=$"{World.Spawner!.Defeated}";
        _deathFrame.Visible=!World.PlayerActor.Alive;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey {Pressed:true,Echo:false} key) return;
        if(Menu.IsOpen || key.PhysicalKeycode==Key.Escape)return;
        bool handled=true;
        if (key.PhysicalKeycode==Key.R) World.ResetCombatWorld();
        else if (World.Progression!.CanChoose && key.PhysicalKeycode is >= Key.Key1 and <= Key.Key4)
        {
            int index=(int)key.PhysicalKeycode-(int)Key.Key1;
            if(World.Progression.Replacing)World.Progression.Replace(index);else SelectOffer(index);
        }
        else handled=false;
        if (handled) GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(World.Progression)) World.Progression!.Changed-=RefreshChoices;
    }
}
