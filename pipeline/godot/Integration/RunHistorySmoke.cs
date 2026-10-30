using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace SwtorSurvivors.Integration;

public partial class RunHistorySmoke : Node
{
    private async Task Frames(int n=8) { for(int i=0;i<n;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }
    private static void Check(bool passed,string name) { if(!passed) throw new Exception(name); GD.Print("Run history: "+name); }
    public override async void _Ready()
    {
        string folder=ProjectSettings.GlobalizePath("res://../logs/run-history-test-"+Guid.NewGuid().ToString("N"));
        RunHistory.TestDirectory=folder;
        try {
            await Frames();
            var record=new RunRecord { ClassId="sorcerer",MapId="eternity_vault",Difficulty="Hard",Duration=30,Damage=4500,Healing=400,Taken=500,Level=8 };
            RunHistory.Save(record);
            var loaded=RunHistory.Load().Single();
            Check(loaded.Outcome=="Incomplete" && loaded.Duration==30 && loaded.Dps==150 && loaded.Taken==500,"short incomplete attempt survives save/load");
            record.Duration=60;record.Damage=12000;record.Outcome="Incomplete · Restarted";RunHistory.Save(record);
            Check(RunHistory.Load().Count==1 && RunHistory.Load()[0].Dps==200,"snapshots update one attempt without duplicates");
            RunHistory.Save(new RunRecord { ClassId="juggernaut",MapId="soa_p3",Difficulty="Normal",Duration=120,Damage=18000,Taken=800,Level=12 });
            Check(RunHistory.Load().Count==2 && RunHistory.Load().Any(r=>r.Encounter=="Soa"),"separate attempts retain encounter identity");
            var menu=GetTree().CurrentScene as MainMenu ?? throw new Exception("Main menu unavailable");
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("res://../logs/run-history-menu.png");
            menu.ShowRunHistory();await Frames();
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("res://../logs/run-history-screen.png");
            GD.Print("RUN HISTORY SMOKE PASSED");GetTree().Quit();
        } catch(Exception e) { GD.PushError(e.ToString());GetTree().Quit(1); }
        finally {
            RunHistory.TestDirectory=null;
            // Delete only the two fixture records created in this unique test directory.
            if(System.IO.Directory.Exists(folder)) {
                foreach(var file in System.IO.Directory.EnumerateFiles(folder,"*.json")) System.IO.File.Delete(file);
                System.IO.Directory.Delete(folder);
            }
        }
    }
}
