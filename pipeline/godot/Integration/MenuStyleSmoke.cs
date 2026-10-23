using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;
using SwtorSurvivors.UI;
namespace SwtorSurvivors.Integration;
public partial class MenuStyleSmoke : Node
{
    private async Task Frames(int n=8){for(int i=0;i<n;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static System.Collections.Generic.IEnumerable<Node> Walk(Node n){foreach(var c in n.GetChildren()){yield return c;foreach(var d in Walk(c))yield return d;}}
    private void Invoke(FrontendFlow f,string method,params object[] args)=>typeof(FrontendFlow).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(f,args);
    private async Task Capture(string name){await Frames(20);GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/menu-"+name+".png"));}
    public override async void _Ready()
    {
        try{
            await Frames();var f=Walk(GetTree().Root).OfType<MainMenu>().First();
            await Capture("main");
            var settings=Walk(f).OfType<Button>().First(b=>b.Text=="Settings");settings.GrabFocus();await Capture("hover");
            Invoke(f,"ShowSettings");await Capture("settings");Invoke(f,"CloseSettings");
            for(int i=1;i<=5;i++){Invoke(f,"ShowStep",i);await Capture("step"+i);}
            f.ShowCollections();await Capture("collections");
            var buttons=Walk(f).OfType<Button>().Where(b=>b.Visible).ToArray();
            if(buttons.Any(b=>b.Size.X+1<b.GetMinimumSize().X))throw new Exception("Button content exceeds allocated width");
            var theme=FrontendSkin.MakeTheme();
            if(((StyleBoxTexture)theme.GetStylebox("hover","Button")).Texture != ((StyleBoxTexture)theme.GetStylebox("pressed","Button")).Texture)throw new Exception("hover/pressed mismatch");
            if(!GameNavigation.TryStart(f,new SelectionConfiguration(),out string reason))throw new Exception(reason);
            GameWorld? world=null;
            for(int i=0;i<600;i++){await Frames(1);world=GetTree().CurrentScene as GameWorld;if(world?.Hud?.Menu!=null)break;}
            if(world?.Hud?.Menu==null)throw new Exception("World menu unavailable");
            world.Hud.Menu.OpenPause();await Capture("pause");world.Hud.Menu.Close();
            if(GetTree().Paused)throw new Exception("Resume left game paused");
            world.Hud.Menu.OpenCollections();await Capture("run-collections");world.Hud.Menu.Close();
            world.Progression!.LevelAnnouncementSeconds=.05f;
            world.Progression.AddExperience(world.Progression.RequiredExperience);
            for(int i=0;i<180 && !world.Progression.CanChoose;i++)await Frames(1);
            if(!world.Progression.CanChoose)throw new Exception("Power choice did not open");
            await Capture("power-choice");
            if(!world.Progression.Choose(0))throw new Exception("Power choice action failed");
            await Frames(90);
            GD.Print("MENU STYLE PASSED: frontend and pause/wardrobe/power menus rendered; resume and power choice work; hover/pressed shared; button widths fit");GetTree().Quit();
        }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
