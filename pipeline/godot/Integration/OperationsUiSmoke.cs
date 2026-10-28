using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

public partial class OperationsUiSmoke : Node
{
    private async Task Frames(int count=10) {for(int i=0;i<count;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);}
    private static System.Collections.Generic.IEnumerable<Node> Walk(Node n)
    {foreach(var c in n.GetChildren()){yield return c;foreach(var d in Walk(c))yield return d;}}
    private static void Check(bool condition,string label) {if(!condition)throw new Exception(label);GD.Print("OPERATIONS_UI "+label);}
    private static void Step(FrontendFlow flow,int step)=>typeof(FrontendFlow).GetMethod("ShowStep",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(flow,new object[]{step});
    private static void Press(Button button)=>button.EmitSignal(BaseButton.SignalName.Pressed);
    private async Task Capture(string name)
    {
        await Frames(20);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../work/operations-ui-"+name+".png"));
    }
    public override async void _Ready()
    {
        try
        {
            var flow=GD.Load<PackedScene>(GameNavigation.MenuScene).Instantiate<FrontendFlow>();
            GetTree().Root.CallDeferred(Node.MethodName.AddChild,flow);await Frames();GetTree().CurrentScene=flow;
            Step(flow,4);await Frames();
            GalaxyChart Chart()=>Walk(flow).OfType<GalaxyChart>().Single();
            var chart=Chart();
            Check(chart.Destinations.Where(b=>b.Visible).All(b=>!b.Map.IsOperation) && chart.Destinations.Count(b=>b.Visible)==4,"world tab contains four planets only");
            await Capture("worlds");
            Press(chart.GetNode<Button>("OperationsTab"));await Frames();
            Check(flow.CurrentConfiguration.MapId=="eternity_vault","operations tab selects the existing operation map contract");
            Check(chart.Destinations.Count(b=>b.Visible)==1 && chart.GetNode<Button>("Encounter_xrr3").Visible,"operation exposes its nested boss");
            Check(chart.ModelPreview.HasModel && !Walk(chart.ModelPreview).OfType<EnemyAgent>().Any(),"preview loads the real model without a combat actor");
            Check(Walk(chart.ModelPreview).OfType<SubViewport>().Single().RenderTargetUpdateMode==SubViewport.UpdateMode.Always,"selected model preview renders");
            Press(chart.GetNode<Button>("Destination_eternity_vault"));
            Check(!chart.GetNode<Button>("Encounter_xrr3").Visible,"operation can collapse");
            Check(chart.NavigationButtons.All(b=>b.IsVisibleInTree()),"collapsed boss excluded from keyboard navigation");
            Press(chart.GetNode<Button>("Destination_eternity_vault"));Press(chart.GetNode<Button>("Encounter_xrr3"));
            string mode=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--test-operation-mode="))?[22..] ?? "Hard";
            Press(Walk(chart).OfType<Button>().Single(b=>b.Name=="Difficulty_"+mode));
            Check(flow.CurrentConfiguration.OperationDifficulty==mode,"difficulty saved in selection");
            Check(flow.CurrentConfiguration.Snapshot().OperationDifficulty==mode,"difficulty survives configuration snapshot");
            await Capture("boss");
            foreach(var button in chart.NavigationButtons)
                Check(button.Size.X+1>=button.GetMinimumSize().X,"control fits: "+button.Name);
            Press(chart.GetNode<Button>("WorldsTab"));
            Check(flow.CurrentConfiguration.MapId=="korriban","world tab restores prior planet");
            Check(Walk(chart.ModelPreview).OfType<SubViewport>().Single().RenderTargetUpdateMode==SubViewport.UpdateMode.Disabled,"hidden model stops rendering");
            Press(chart.GetNode<Button>("OperationsTab"));Step(flow,3);Step(flow,4);await Frames();chart=Chart();
            Check(chart.OperationsSelected && chart.Difficulty==mode && chart.ModelPreview.HasModel,"back and forward restore encounter and difficulty");
            Check(SelectionGameplayAdapter.TryResolve(flow.CurrentConfiguration,out _,out _),"existing operation launch remains valid");
            if(!GameNavigation.TryStart(flow,flow.CurrentConfiguration,out var reason))throw new Exception(reason);
            bool loading=false;
            for(int i=0;i<3600;i++)
            {
                await Frames(1);
                var overlay=GetTree().Root.GetNodeOrNull<WorldLoadingScreen>("WorldLoadingScreen");
                if(overlay!=null && !loading)
                {
                    loading=true;await Capture("loading");
                    Check(Walk(overlay).OfType<TextureRect>().Any(t=>t.Texture?.ResourcePath.EndsWith("eternity_vault_overlay.png")==true),"regular operation loading artwork retained");
                }
                if(loading && overlay==null && GetTree().CurrentScene is GameWorld {ReadyForPlay:true} world)
                {
                    Check(world.RunConfiguration.OperationDifficulty==mode,"chosen difficulty reaches gameplay");
                    if(OS.GetCmdlineUserArgs().Contains("--verify-operation-tuning"))await OperationDifficultySmoke.Verify(world);
                    GD.Print("OPERATIONS_UI PASS");GetTree().Quit();return;
                }
            }
            throw new Exception("Operation loading did not complete");
        }
        catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}
    }
}
