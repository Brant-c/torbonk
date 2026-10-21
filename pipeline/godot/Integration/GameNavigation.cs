using Godot;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>Only a one-shot run handoff and a selection snapshot; no persistent scene or autoload.</summary>
public static class GameNavigation
{
    public const string MenuScene = "res://Frontend/Scenes/MainMenu.tscn";
    private static GameplayConfiguration? _pendingRun;
    private static SelectionConfiguration? _lastSelection;
    public static SelectionConfiguration? LastSelection => _lastSelection?.Snapshot();

    public static bool TryStart(FrontendFlow menu, SelectionConfiguration selection, out string reason)
    {
        if (menu.GetTree().CurrentScene != menu)
        { reason = "A scene transition is already in progress."; return false; }
        // Revalidate at the launch boundary even if the Ready button was enabled earlier.
        if (!SelectionGameplayAdapter.TryResolve(selection, out var run, out reason)) return false;
        SwtorCursor.Instance.RestoreVisible();
        _pendingRun = run;
        var error = menu.GetTree().ChangeSceneToFile(run.WorldScene);
        if (error != Error.Ok)
        { _pendingRun = null; reason = "Unable to open gameplay: " + error; return false; }
        _lastSelection = selection.Snapshot();
        return true;
    }

    internal static GameplayConfiguration TakeRunConfiguration()
    {
        var run = _pendingRun;
        _pendingRun = null;
        // Only direct development launches have no menu handoff; their composition stays unchanged.
        return run ?? SelectionGameplayAdapter.DevelopmentDefault;
    }

    public static Error ReturnToMenu(GameWorld world)
    {
        if (world.GetTree().CurrentScene != world) return Error.Busy;
        // Load first so a missing menu resource cannot stop a still-usable run.
        var menu = GD.Load<PackedScene>(MenuScene);
        if (menu == null) return Error.CantOpen;
        world.StopForMenu();
        _pendingRun = null;
        world.GetTree().Paused = false;
        SwtorCursor.Instance.RestoreVisible();
        return world.GetTree().ChangeSceneToPacked(menu);
    }
}
