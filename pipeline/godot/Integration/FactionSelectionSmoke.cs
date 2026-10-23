using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public partial class FactionSelectionSmoke : Node
{
    private async Task Frames(int n=4){for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static System.Collections.Generic.IEnumerable<Node> Walk(Node n){foreach(var c in n.GetChildren()){yield return c;foreach(var d in Walk(c))yield return d;}}
    private void Check(bool ok,string name){if(!ok)throw new Exception(name);GD.Print("FACTION: "+name);}
    private async Task Click(Control c,Vector2? at=null){var point=at??c.GetGlobalRect().GetCenter();GetViewport().PushInput(new InputEventMouseMotion {Position=point,GlobalPosition=point},true);await Frames(2);foreach(bool down in new[]{true,false})GetViewport().PushInput(new InputEventMouseButton {ButtonIndex=MouseButton.Left,Pressed=down,Position=point,GlobalPosition=point},true);await Frames(30);}
    private async Task Key(Key key){foreach(bool down in new[]{true,false})GetViewport().PushInput(new InputEventKey {Keycode=key,PhysicalKeycode=key,Pressed=down},true);await Frames(30);}
    private async Task PreviewBackdrops(FactionChoice[] choices)
    {
        for(int variant=0;variant<3;variant++)
        {
            var decorations=new System.Collections.Generic.List<Control>();
            foreach(var choice in choices)
            {
                Color accent=choice.Faction.Id=="republic" ? new Color(.28f,.7f,1,.45f) : new Color(1,.25f,.22f,.45f);
                var backing=new Panel {Name="BackdropPreview",Position=new Vector2(-8,-8),Size=new Vector2(556,590),MouseFilter=Control.MouseFilterEnum.Ignore};
                var style=new StyleBoxFlat {BgColor=new Color(.01f,.025f,.045f,variant==0?.65f:.38f),CornerRadiusTopLeft=18,CornerRadiusTopRight=18,CornerRadiusBottomLeft=18,CornerRadiusBottomRight=18};
                backing.AddThemeStyleboxOverride("panel",style);choice.AddChild(backing);choice.MoveChild(backing,0);decorations.Add(backing);
                void Rule(Vector2 position,Vector2 size,Color color) {var line=new ColorRect {Position=position,Size=size,Color=color,MouseFilter=Control.MouseFilterEnum.Ignore};backing.AddChild(line);}
                if(variant==1) {Rule(new Vector2(56,0),new Vector2(444,2),accent);Rule(new Vector2(56,588),new Vector2(444,2),accent);}
                if(variant==2) {Rule(new Vector2(0,35),new Vector2(2,520),accent);style.BgColor=new Color(.01f,.025f,.045f,.25f);}
            }
            await Frames(18);GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/faction-backplate-"+(variant+1)+".png"));
            foreach(var control in decorations){control.GetParent().RemoveChild(control);control.QueueFree();}
        }
    }
    public override async void _Ready()
    {
        try{
            await Frames();var menu=(MainMenu)GetTree().CurrentScene;
            Button Find(string text)=>Walk(menu).OfType<Button>().First(b=>b.Text==text);
            var entries=Walk(menu).OfType<MainMenuChoice>().ToArray();
            Check(entries.Select(b=>b.Text).SequenceEqual(new[]{"New Run","Collections","Settings","Exit"}) && entries.All(b=>b.Size==new Vector2(580,140)),"main menu has four equal-sized entries");
            Check(entries[0].Position.Y==entries[1].Position.Y && entries[2].Position.Y==entries[3].Position.Y && entries[0].Position.X==entries[2].Position.X && entries[1].Position.X==entries[3].Position.X,"main menu uses a centered two by two grid");
            Check(entries.All(b=>b.GetNode<TextureRect>("MenuVisual/MenuIcon").Size==new Vector2(38,38)),"original menu icons use a modest display size");
            var settings=Find("Settings");var settingsPoint=settings.GetGlobalRect().GetCenter();
            GetViewport().PushInput(new InputEventMouseMotion {Position=settingsPoint,GlobalPosition=settingsPoint},true);await Frames(16);
            Check(settings.GetNode<Control>("MenuVisual").Scale.IsEqualApprox(Vector2.One*1.025f),"main menu hover expands the entire entry");
            GetViewport().PushInput(new InputEventMouseMotion {Position=new Vector2(800,780),GlobalPosition=new Vector2(800,780)},true);await Frames(16);
            Check(entries.All(b=>b.GetNode<Control>("MenuVisual").Scale.IsEqualApprox(Vector2.One)),"main menu hover clears on pointer exit");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/main-menu-refined.png"));
            await Click(settings);Check(Walk(menu).OfType<OptionButton>().Count()==2,"third main entry opens display and sound settings");
            await Key(Godot.Key.Escape);
            await Click(Find("New Run"));
            var choices=Walk(menu).OfType<FactionChoice>().ToArray();
            if(OS.GetCmdlineUserArgs().Contains("--faction-backdrop-preview")) {await PreviewBackdrops(choices);GetTree().Quit();return;}

            Check(choices.Length==2 && choices.All(c=>c.Classes.Length==4),"two faction choices each show four original class icons");
            Check(Walk(menu).OfType<TextureRect>().Where(c=>c.Name.ToString().StartsWith("ClassIcon_")).All(c=>c.Size==new Vector2(58,58)),"class icons keep their intended size");
            Check(choices.All(c=>!c.Selected),"faction screen starts with neither allegiance selected");
            async Task Point(Vector2 at) {GetViewport().PushInput(new InputEventMouseMotion {Position=at,GlobalPosition=at},true);await Frames(12);}
            await Point(choices[0].GetGlobalRect().GetCenter());Check(choices[0].Emphasized && !choices[1].Emphasized,"hover emphasizes Republic only");
            Check(choices[0].GetNode<Control>("FactionVisual").Scale.IsEqualApprox(Vector2.One*1.025f) && choices[0].Scale==Vector2.One,"hover expands the whole visual group with a fixed pointer target");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/faction-hover.png"));
            await Point(choices[1].GetGlobalRect().GetCenter());Check(choices[1].Emphasized && !choices[0].Emphasized,"hover transfers to Empire");
            Check(choices[0].GetNode<Control>("FactionVisual").Scale.IsEqualApprox(Vector2.One) && choices[1].GetNode<Control>("FactionVisual").Scale.IsEqualApprox(Vector2.One*1.025f),"expansion transfers without leaving the previous faction enlarged");
            await Point(new Vector2(800,780));Check(choices.All(c=>!c.Emphasized),"pointer exit clears hover without a sticky selection");
            var empireChoice=choices.Single(c=>c.Faction.Id=="empire");
            Check(Walk(empireChoice).OfType<Label>().Single(l=>l.Text=="Select allegiance").GetThemeColor("font_color")==new Color(1,.25f,.22f),"Empire allegiance text is red without hover");
            Check(choices.All(c=>c.GetNode<Control>("FactionVisual").Scale.IsEqualApprox(Vector2.One)),"pointer exit restores the original layout size");
            Check(Find("Back").GetThemeStylebox("normal") is StyleBoxEmpty && Find("Main Menu").GetThemeStylebox("focus") is StyleBoxEmpty,"bottom navigation has no enclosing button frames");
            var nav=Find("Main Menu");Check(nav.Icon.ResourcePath.EndsWith("bwaui_buttons_236.png"),"Main Menu icon differs from Back");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/faction-insignias.png"));
            await Key(Godot.Key.Up);Check(choices[0].HasFocus() && choices[0].Emphasized,"keyboard focus has a visible cue without selection");
            await Key(Godot.Key.Right);Check(choices[1].HasFocus(),"Right arrow moves focus to Empire");
            await Key(Godot.Key.Enter);Check(menu.CurrentConfiguration.FactionId=="empire" && !Walk(menu).OfType<FactionChoice>().Any(),"Enter selects Empire and opens class selection");
            var classCards=Walk(menu).OfType<ClassChoice>().ToArray();
            Check(classCards.Length==4 && classCards.All(c=>c.Accent.R>c.Accent.B),"Empire has four illustrated class cards with red accents");
            Check(classCards.All(c=>c.GetNode<TextureRect>("ClassVisual/ClassArtwork").Size==new Vector2(319,245)),"class artwork stays inside its card");
            Check(!Walk(menu).OfType<Button>().Any(b=>b.Text=="Continue"),"class cards replace the Continue button");
            Check(Find("Back").GetThemeStylebox("normal") is StyleBoxEmpty,"class navigation uses the clean unframed style");
            foreach(string color in new[]{"font_color","font_hover_color","font_focus_color","font_pressed_color"})
                Check(Find("Back").GetThemeColor(color)==Find("Main Menu").GetThemeColor(color) && Find("Back").GetThemeColor(color).B>=Find("Back").GetThemeColor(color).R,"Back and Main Menu share standard non-red text: "+color);
            await Point(classCards[1].GetGlobalRect().GetCenter());
            Check(classCards[1].GetNode<Control>("ClassVisual").Scale.IsEqualApprox(Vector2.One*1.025f),"class hover expands all artwork within a fixed target");
            await Point(new Vector2(800,780));
            Check(classCards.All(c=>c.GetNode<Control>("ClassVisual").Scale.IsEqualApprox(Vector2.One)),"class hover expansion clears");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/class-cards-empire.png"));
            await Key(Godot.Key.Up);await Key(Godot.Key.Right);await Key(Godot.Key.Enter);
            Check(menu.CurrentConfiguration.BaseClassId=="sith_inquisitor" && !Walk(menu).OfType<ClassChoice>().Any() && Walk(menu).OfType<AdvancedClassChoice>().Count()==2,"keyboard class selection opens two advanced class cards");
            var advancedCards=Walk(menu).OfType<AdvancedClassChoice>().ToArray();
            Check(advancedCards.All(c=>c.Accent.R>c.Accent.B && c.GetNode<TextureRect>("AdvancedVisual/AdvancedArtwork").Size==new Vector2(480,275)),"Empire advanced cards use red accents and correctly sized original artwork");
            Check(advancedCards.All(c=>Walk(c).OfType<Label>().Single(l=>l.Text=="Choose "+c.Advanced.Name).GetThemeColor("font_color")==c.Accent),"Empire advanced choice text matches the preceding screen's red");
            Check(!Walk(menu).OfType<Button>().Any(b=>b.Text=="Continue") && !Walk(menu).OfType<CharacterPreview>().Any(),"advanced cards replace Continue and the model preview");
            await Point(advancedCards[1].GetGlobalRect().GetCenter());
            Check(advancedCards[1].GetNode<Control>("AdvancedVisual").Scale.IsEqualApprox(Vector2.One*1.025f),"advanced card hover expands the group");
            await Point(new Vector2(800,780));
            Check(advancedCards.All(c=>c.GetNode<Control>("AdvancedVisual").Scale.IsEqualApprox(Vector2.One)),"advanced card hover resets");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/advanced-cards-empire.png"));
            await Key(Godot.Key.Up);await Key(Godot.Key.Right);await Key(Godot.Key.Enter);
            Check(menu.CurrentConfiguration.AdvancedClassId=="assassin" && Walk(menu).OfType<GalaxyChart>().Any(),"keyboard advanced choice opens galaxy selection directly");
            var chart=Walk(menu).OfType<GalaxyChart>().Single();
            Check(chart.Destinations.Count==4 && chart.Destinations.All(c=>c.GetNode<TextureRect>("DestinationVisual/PlanetIcon").Size==new Vector2(52,52)),"destination browser shows four properly sized planet buttons");
            Check(!Walk(menu).OfType<Button>().Any(b=>b.Text=="Continue"),"Set Course replaces destination Continue");
            foreach(var destination in chart.Destinations)
            {
                await Click(destination);
                Check(menu.CurrentConfiguration.MapId==destination.Map.Id && Walk(menu).OfType<GalaxyChart>().Single()==chart && chart.Destinations.Count(c=>c.Selected)==1 && chart.GetNode<Label>("DestinationName").Text==destination.Map.Name,"browsing "+destination.Map.Name+" updates preview and configuration in place");
                Check(chart.GetNode<TextureRect>("DestinationLandscape").Texture.ResourcePath.Contains("overlay.png"),"destination uses original landscape artwork");
            }
            await Point(new Vector2(800,800));
            Check(chart.Destinations.All(c=>c.GetNode<Control>("DestinationVisual").Scale.IsEqualApprox(Vector2.One)),"destination hover expansion resets");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/destination-browser.png"));
            var launchHandler=menu.PlayRequested;SelectionConfiguration? attempted=null;
            menu.PlayRequested=configuration=>{attempted=configuration;return "Simulated launch failure";};
            await Key(Godot.Key.Right);Check(chart.CourseButton.HasFocus(),"Right arrow moves to Set Course");await Key(Godot.Key.Enter);
            Check(attempted?.MapId=="balmorra" && Walk(menu).OfType<GalaxyChart>().Single()==chart && !Walk(menu).OfType<Button>().Any(b=>b.Text=="Play"),"Set Course calls launch directly without a Ready page");
            var dialog=Walk(menu).OfType<AcceptDialog>().Single();Check(dialog.DialogText=="Simulated launch failure","launch failures keep destination selection available for retry");dialog.QueueFree();await Frames();menu.PlayRequested=launchHandler;
            Check(chart.Destinations.Single(c=>c.Selected).Map.Id=="balmorra","launch failure preserves the selected destination");
            await Click(Find("Back"));
            Check(Walk(menu).OfType<AdvancedClassChoice>().Count()==2 && Walk(menu).OfType<AdvancedClassChoice>().All(c=>!c.Selected),"Back restores advanced choices without stale selection");
            await Click(Find("Back"));
            Check(Walk(menu).OfType<ClassChoice>().Count()==4 && Walk(menu).OfType<ClassChoice>().All(c=>!c.Selected),"Back restores the class cards without a stale selected card");
            await Click(Find("Back"));
            Check(Walk(menu).OfType<FactionChoice>().All(c=>!c.Selected && !c.GetMeta("selected").AsBool()),"Back resets both faction selections");
            var republic=Walk(menu).OfType<FactionChoice>().First(c=>c.Faction.Id=="republic");
            // Click a decorative class icon, verifying child art does not block the faction target.
            await Click(republic,republic.GlobalPosition+new Vector2(155,460)*republic.GetGlobalTransform().Scale);
            Check(menu.CurrentConfiguration.FactionId=="republic" && !Walk(menu).OfType<FactionChoice>().Any(),"clicking the class-icon region selects Republic");
            classCards=Walk(menu).OfType<ClassChoice>().ToArray();
            Check(classCards.Length==4 && classCards.All(c=>c.Accent.B>c.Accent.R),"Republic class cards retain blue accents");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/class-cards-republic.png"));
            await Click(classCards[2],classCards[2].GlobalPosition+new Vector2(170,145)*classCards[2].GetGlobalTransform().Scale);
            Check(menu.CurrentConfiguration.BaseClassId=="smuggler" && !Walk(menu).OfType<ClassChoice>().Any(),"clicking class artwork selects its class and advances");
            advancedCards=Walk(menu).OfType<AdvancedClassChoice>().ToArray();
            Check(advancedCards.Length==2 && advancedCards.All(c=>c.Accent.B>c.Accent.R),"Republic advanced cards use blue accents");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/advanced-cards-republic.png"));
            await Click(advancedCards[1],advancedCards[1].GlobalPosition+new Vector2(265,145)*advancedCards[1].GetGlobalTransform().Scale);
            Check(menu.CurrentConfiguration.AdvancedClassId=="scoundrel" && Walk(menu).OfType<GalaxyChart>().Any(),"clicking advanced artwork selects and opens the galaxy");
            await Click(Find("Back"));
            await Click(Find("Back"));
            await Key(Godot.Key.Escape);
            Check(Walk(menu).OfType<FactionChoice>().Count()==2 && Walk(menu).OfType<FactionChoice>().All(c=>!c.Selected),"Escape returns with neither faction selected");
            GetWindow().Size=new Vector2I(1280,720);await Frames(12);
            Check(Walk(menu).OfType<FactionChoice>().All(c=>GetViewport().GetVisibleRect().Encloses(c.GetGlobalRect())),"choices fit a 1280 by 720 viewport");
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/faction-insignias-720p.png"));
            await Click(Find("Main Menu"));Check(Walk(menu).OfType<Button>().Any(b=>b.Text=="New Run"),"Main Menu navigation works");
            await Click(Find("New Run"));
            await Click(Walk(menu).OfType<FactionChoice>().Single(c=>c.Faction.Id=="empire"));
            await Click(Walk(menu).OfType<ClassChoice>().Single(c=>c.Class.Id=="sith_inquisitor"));
            await Click(Walk(menu).OfType<AdvancedClassChoice>().Single(c=>c.Advanced.Id=="sorcerer"));
            chart=Walk(menu).OfType<GalaxyChart>().Single();await Click(chart.Destinations.Single(c=>c.Map.Id=="korriban"));
            Check(!chart.CourseButton.Disabled,"playable selection enables Set Course");await Click(chart.CourseButton);
            for(int frame=0;frame<1800 && GetTree().CurrentScene is not GameWorld {ReadyForPlay:true};frame++)await Frames(1);
            Check(GetTree().CurrentScene is GameWorld {ReadyForPlay:true} && GameNavigation.LastSelection is {AdvancedClassId:"sorcerer",MapId:"korriban"},"Set Course enters the configured game directly");
            GD.Print("FACTION SELECTION PASSED");GetTree().Quit();
        }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
