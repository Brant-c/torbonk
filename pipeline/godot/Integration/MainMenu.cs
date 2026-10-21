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
        Input.MouseMode = Input.MouseModeEnum.Visible;
        ValidateForPlay = SelectionGameplayAdapter.UnavailableReason;
        PlayRequested += selection => GameNavigation.TryStart(this, selection, out var reason) ? null : reason;
        base._Ready();
        if (GameNavigation.LastSelection is { } saved) RestoreConfiguration(saved);
        if (OS.GetCmdlineUserArgs().Contains("--c2-smoke") && !GetTree().Root.HasNode("C2Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, new C2Smoke { Name = "C2Smoke" });
    }
}
