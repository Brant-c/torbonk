using Godot;
using System;
using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public partial class LoadingScreenSmoke : Node
{
    public override async void _Ready()
    {
        var tree=GetTree();var menu=GD.Load<PackedScene>(GameNavigation.MenuScene).Instantiate<FrontendFlow>();
        tree.Root.CallDeferred(Node.MethodName.AddChild,menu);
        await ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        tree.CurrentScene=menu;
        string map="korriban";
        foreach(string arg in OS.GetCmdlineUserArgs())if(arg.StartsWith("--map="))map=arg[6..];
        var selection=new SelectionConfiguration { FactionId="empire",BaseClassId="sith_inquisitor",AdvancedClassId="sorcerer",MapId=map,Appearance=AppearanceConfiguration.ForClass("sorcerer") };
        // Use the actual profile key rather than relying on display naming.
        selection.BaseClassId=ClassProfile.Find("sorcerer")!.BaseClass;
        if(!GameNavigation.TryStart(menu,selection,out string reason)){GD.PushError(reason);tree.Quit(1);return;}
        bool sawOverlay=false,captured=false;
        for(int frame=0;frame<3600;frame++)
        {
            await ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            var overlay=tree.Root.GetNodeOrNull<WorldLoadingScreen>("WorldLoadingScreen");
            sawOverlay|=overlay!=null;
            if(overlay!=null&&!captured)
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../work/loading-"+map+".png"));captured=true;
            }
            if(tree.CurrentScene is GameWorld {ReadyForPlay:true} world && overlay==null)
            {
                bool passed=sawOverlay&&world.RunConfiguration.MapId==map;
                GD.Print("LOADING_SMOKE "+map+" "+(passed?"PASS":"FAIL"));tree.Quit(passed?0:1);return;
            }
        }
        GD.PushError("Loading transition timed out");tree.Quit(1);
    }
}
