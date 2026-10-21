using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>Opt-in B10.1 UI/navigation smoke; one bounded rendered session, no gameplay regression suite.</summary>
public partial class B101Smoke : Node
{
    private readonly List<string> _checks=new();
    private MainMenu Menu => (MainMenu)GetTree().CurrentScene;
    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (var child in root.GetChildren())
        { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private Button Find(string text) => Descendants(Menu).OfType<Button>().Single(b=>b.IsVisibleInTree() && b.Text==text);
    private string Page => Descendants(Menu).OfType<SelectionPage>().Single().StepId;
    private void Check(string name,bool passed)
    { if (!passed) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("B10.1: "+name); }
    private async Task Frames(int count=2)
    { for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> ready)
    {
        for (int i=0;i<300 && !ready();i++) await Frames(1);
        if (!ready()) throw new TimeoutException("B10.1 transition"); await Frames();
    }
    private async Task Click(string text)
    {
        var button=Find(text); if (button.Disabled) throw new InvalidOperationException("Disabled: "+text);
        var point=button.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion {Position=point,GlobalPosition=point},true);
        foreach (bool pressed in new[]{true,false})
            GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=pressed,Position=point,GlobalPosition=point},true);
        await Frames();
    }
    private async Task KeyPress(Key key)
    {
        GetViewport().PushInput(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=true},true); await Frames();
        GetViewport().PushInput(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=false},true); await Frames();
    }
    private void Selected(string name)
    {
        var page=Descendants(Menu).OfType<SelectionPage>().Single();
        var selected=Descendants(page).OfType<Button>().Where(b=>b.HasMeta("selected") && b.GetMeta("selected").AsBool()).ToArray();
        Check("Exactly one persistent selected entry: "+name,selected.Length==1 && selected[0].Text==name
            && selected[0].HasThemeStyleboxOverride("normal")
            && selected[0].GetThemeStylebox("normal") is StyleBoxTexture box && box.Texture.ResourcePath.EndsWith("_18.png"));
    }
    private async Task Capture(string name)
    {
        await Frames(12); await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        if (GetTree().CurrentScene is MainMenu && Page is ("BaseClass" or "AdvancedClass" or "Galaxy" or "Ready"))
        {
            var action=Find(Page=="Ready" ? "Play" : "Continue");
            var rect=action.GetGlobalRect();
            var back=Find("Back").GetGlobalRect();
            var menu=Find("Main Menu").GetGlobalRect();
            Check("Action aligns at bottom-right with navigation: "+Page,
                action.GetParent()==Find("Back").GetParent()
                && Mathf.IsEqualApprox(rect.Position.Y,back.Position.Y)
                && Mathf.IsEqualApprox(rect.Position.Y,menu.Position.Y)
                && Mathf.IsEqualApprox(rect.Size.Y,back.Size.Y)
                && rect.Position.X>menu.End.X
                && Mathf.IsEqualApprox(action.Position.X+action.Size.X,((Control)action.GetParent()).Size.X));
        }
        GetViewport().GetTexture().GetImage().SavePng("res://../work/b101-"+name+".png");
    }
    private async Task ReviewLevelUp()
    {
        Check("Level UI review launches the existing composition",GameNavigation.TryStart(Menu,new SelectionConfiguration(),out _));
        await Until(()=>GetTree().CurrentScene is GameWorld {Initialized:true,Hud:not null,Progression:not null,Rewards:not null});
        var world=(GameWorld)GetTree().CurrentScene;
        var progression=world.Progression!;
        progression.AddExperience(progression.RequiredExperience); await Frames(12);
        Check("Level banner appears before choices and pauses combat",world.Rewards!.LevelBannerVisible && !world.Hud!.SelectionVisible && GetTree().Paused);
        await Capture("level-banner");
        await Until(()=>progression.CanChoose);
        Check("Three existing upgrade choices replace the banner",world.Hud!.VisibleChoiceCount==3 && world.Hud.SelectionVisible && !world.Rewards.LevelBannerVisible);
        await Capture("level-choices");
        await KeyPress(Key.Key1);
        Check("Keyboard selection enters existing confirmation",progression.PresentationPhase==ProgressionPresentationPhase.Confirming && GetTree().Paused);
        await Capture("level-confirmed");
        await Until(()=>!progression.Selecting);
        Check("Choosing a power resumes the same run",!GetTree().Paused && progression.Level==2 && GetTree().CurrentScene==world);
        world.ReturnToMainMenu(); await Until(()=>GetTree().CurrentScene is MainMenu);
    }
    public override async void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always; string? failure=null;
        try
        {
            await Frames(8);
            if (OS.GetCmdlineUserArgs().Contains("--b101-level-review")) await ReviewLevelUp();
            else
            {
            Check("Main menu has New Run, Collections, Settings, Exit",Descendants(Menu).OfType<Button>().Where(b=>b.IsVisibleInTree()).Select(b=>b.Text).SequenceEqual(new[]{"New Run","Collections","Settings","Exit"}));
            await Capture("main-menu");
            await Click("Settings"); Check("Settings uses SWTOR panel and sound control",Find("Interface Sounds: On").HasFocus());
            await KeyPress(Key.Escape); await Click("New Run");
            Check("Faction has no Continue",Page=="Faction" && !Descendants(Menu).OfType<Button>().Any(b=>b.Text.Contains("Continue")));
            Check("Fresh faction screen has no selected side or logo",!Descendants(Menu).OfType<Button>().Any(b=>b.HasMeta("selected") && b.GetMeta("selected").AsBool())
                && !Descendants(Menu).OfType<TextureRect>().Any(t=>t.IsVisibleInTree() && t.Texture?.ResourcePath.EndsWith("_101.png")==true));
            await Capture("faction");
            await Click("Galactic Republic"); await Until(()=>Page=="BaseClass");
            Check("Republic saves and advances automatically",Menu.CurrentConfiguration.FactionId=="republic");
            await Click("Back"); Selected("Galactic Republic");
            await Click("Sith Empire"); await Until(()=>Page=="BaseClass");
            Check("Empire saves and advances automatically",Menu.CurrentConfiguration.FactionId=="empire");
            // Focus elsewhere so the rest-state texture, rather than focus/hover, proves selection.
            Find("Back").GrabFocus(); Selected("Sith Inquisitor"); await Capture("base-class");
            var warrior=Find("Sith Warrior"); warrior.GrabFocus();
            await KeyPress(Key.Enter);
            Check("Keyboard class choice updates preview without advancing",Page=="BaseClass" && Menu.CurrentConfiguration.BaseClassId=="sith_warrior");
            await Click("Continue"); await Until(()=>Page=="AdvancedClass");
            await Click("Back"); Selected("Sith Warrior");
            await Click("Sith Inquisitor");
            Check("Class click stays on class screen",Page=="BaseClass");
            await Click("Continue"); await Until(()=>Page=="AdvancedClass");
            Find("Back").GrabFocus(); Selected("Sorcerer"); await Capture("advanced-class");
            await Click("Assassin");
            Check("Advanced class click stays on advanced screen",Page=="AdvancedClass" && Menu.CurrentConfiguration.AdvancedClassId=="assassin"
                && Descendants(Menu).OfType<Label>().Any(l=>l.Text=="Preview Only") && !Descendants(Menu).OfType<Skeleton3D>().Any());
            await Click("Continue"); await Until(()=>Page=="Galaxy");
            await Click("Back"); Selected("Assassin");
            await Click("Sorcerer");
            Check("Sorcerer retains real preview",Page=="AdvancedClass" && Descendants(Menu).OfType<Skeleton3D>().Any());
            Check("Sorcerer viewer holds its initialized lightsaber",Descendants(Menu).OfType<EquipmentAttachment>()
                .Any(saber=>saber.BladeInitialized));
            Check("Sorcerer viewer uses the recovered saber stance",Descendants(Menu).OfType<AnimationPlayer>()
                .Any(player=>player.CurrentAnimation=="Preview/SaberIdle"));
            await Click("Continue"); await Until(()=>Page=="Galaxy");
            Check("Advanced Continue skips appearance entirely",!Descendants(Menu).OfType<Label>().Any(l=>l.Text=="Character Appearance"));
            Selected("Korriban"); await Click("Korriban");
            Check("Destination click stays on galaxy screen",Page=="Galaxy");
            await Capture("galaxy"); await Click("Continue"); await Until(()=>Page=="Ready");
            Check("Galaxy Continue opens Ready",Menu.CurrentConfiguration.MapId=="korriban" && !Find("Play").Disabled);
            await Click("Back"); Selected("Korriban");
            await Click("Continue"); await Until(()=>Page=="Ready");
            var valid=Menu.CurrentConfiguration;
            Menu.RestoreConfiguration(new SelectionConfiguration {BaseClassId="sith_warrior",AdvancedClassId="juggernaut"}); await Frames();
            Check("Unsupported composition cannot Play",Find("Play").Disabled);
            Menu.RestoreConfiguration(valid); await Frames(); await Capture("ready");
            await Click("Play");
            await Until(()=>GetTree().CurrentScene is GameWorld {Initialized:true,Hud:not null,Progression:not null,Rewards:not null});
            var world=(GameWorld)GetTree().CurrentScene; await Frames(60);
            Check("Gameplay initializes with one original action bar and cursor",world.Hud!.ActionSlotCount==12
                && world.RunConfiguration.AdvancedClassId=="sorcerer" && world.RunConfiguration.MapId=="korriban"
                && world.Waves!.Running && SwtorCursor.Instance.Theme.Normal!=null);
            await Capture("gameplay");
            if (OS.GetCmdlineUserArgs().Contains("--b101-hud-review"))
            {
                // Stage an encounter only in this opt-in visual review; production scheduling is unchanged.
                world.Waves!.SetPhysicsProcess(false);
                for (int attempt=0;attempt<8 && !world.Waves.Encounter.Spawned;attempt++)
                { world.Waves.Encounter.Tick(1.1,true); await Frames(); }
                await Frames(30);
                Check("Boss frame is visible with pale/blue text",world.Rewards!.BossDisplay.Visible
                    && Descendants(world.Rewards.BossDisplay).OfType<Label>().All(label=>
                        label.GetThemeColor("font_color")==UI.SwtorSkin.TextColor || label.GetThemeColor("font_color")==UI.SwtorSkin.Blue));
                Check("Boss introduction has no duplicate encounter banner",!Descendants(world.Rewards).OfType<Label>()
                    .Any(label=>label.IsVisibleInTree() && label.Text=="CHAMPION ENCOUNTER"));
                await Capture("boss-hud");
            }
            }
        }
        catch (Exception exception) {failure=exception.ToString(); GD.PushError(failure);}
        if (OS.GetCmdlineUserArgs().Contains("--b101-level-review"))
        {
            // Release temporary managed Godot collections while the native engine is still alive.
            GC.Collect(); GC.WaitForPendingFinalizers(); await Frames();
        }
        string receipt=OS.GetCmdlineUserArgs().Contains("--b101-level-review") ? "b101-level-smoke.json" : "b101-smoke.json";
        using var output=Godot.FileAccess.Open("res://../manifests/"+receipt,Godot.FileAccess.ModeFlags.Write);
        output.StoreString(JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
