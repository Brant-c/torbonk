using Godot;
using System.Linq;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>Game-specific host for the otherwise reusable C1 menu.</summary>
public partial class MainMenu : FrontendFlow
{
    public override void _Ready()
    {
        GetTree().Paused = false;
        SwtorCursor.Instance.RestoreVisible();
        ValidateForPlay = SelectionGameplayAdapter.UnavailableReason;
        PlayRequested += selection => GameNavigation.TryStart(this, selection, out var reason) ? null : reason;
        base._Ready();
        if(OS.GetCmdlineUserArgs().Any(a=>a is "--talents-smoke" or "--talents-persistence-check"))
        {
            var smoke=new Talents.TalentsSmoke();GetTree().Root.CallDeferred(Node.MethodName.AddChild,smoke);smoke.CallDeferred("Run",this);
        }
        if(OS.GetCmdlineUserArgs().Contains("--faction-selection-smoke")) GetTree().Root.CallDeferred(Node.MethodName.AddChild,new FactionSelectionSmoke());
        if(OS.GetCmdlineUserArgs().Contains("--drop-symbol-smoke")) GetTree().Root.CallDeferred(Node.MethodName.AddChild,new DropSymbolSmoke());
        if(OS.GetCmdlineUserArgs().Contains("--menu-style-smoke")) GetTree().Root.CallDeferred(Node.MethodName.AddChild,new MenuStyleSmoke());
        if (GameNavigation.LastSelection is { } saved) RestoreConfiguration(saved);
        if (OS.GetCmdlineUserArgs().Contains("--c2-smoke") && !GetTree().Root.HasNode("C2Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, new C2Smoke { Name = "C2Smoke" });
        if (OS.GetCmdlineUserArgs().Contains("--b101-smoke") && !GetTree().Root.HasNode("B101Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, new B101Smoke { Name = "B101Smoke" });
        if (OS.GetCmdlineUserArgs().Any(a=>a is "--d11-smoke-save" or "--d11-smoke-load") && !GetTree().Root.HasNode("D11Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild,new D11Smoke {Name="D11Smoke"});
        if (OS.GetCmdlineUserArgs().Contains("--unlock-smoke") && !GetTree().Root.HasNode("UnlockSmoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild,new UnlockSmoke {Name="UnlockSmoke"});
    }
}
