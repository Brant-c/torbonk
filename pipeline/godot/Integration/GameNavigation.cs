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
        // Revalidate at the launch boundary even if Set Course was enabled earlier.
        if (!SelectionGameplayAdapter.TryResolve(selection, out var run, out reason)) return false;
        SwtorCursor.Instance.RestoreVisible();
        if (menu.GetTree().Root.GetNodeOrNull<WorldLoadingScreen>("WorldLoadingScreen") != null)
        { reason = "A scene transition is already in progress."; return false; }
        _pendingRun = run;
        menu.GetTree().Root.AddChild(new WorldLoadingScreen { Name="WorldLoadingScreen",Run=run });
        _lastSelection = selection.Snapshot();
        return true;
    }

    internal static void CancelPendingRun() => _pendingRun = null;

    internal static GameplayConfiguration TakeRunConfiguration()
    {
        var run = _pendingRun;
        _pendingRun = null;
        // Only direct development launches have no menu handoff; their composition stays unchanged.
        if(run!=null)return run;
        foreach(string arg in OS.GetCmdlineUserArgs())
            if(arg.StartsWith("--advanced-class="))
            {
                var profile=ClassProfile.Find(arg[17..])??throw new System.InvalidOperationException("Unknown development class: "+arg);
                var selected=new SelectionConfiguration {FactionId=profile.Faction,BaseClassId=profile.BaseClass,AdvancedClassId=profile.Id,Appearance=AppearanceConfiguration.ForClass(profile.Id)};
                foreach(string mapArg in OS.GetCmdlineUserArgs())if(mapArg.StartsWith("--map="))selected.MapId=mapArg[6..];
                if(!SelectionGameplayAdapter.TryResolve(selected,out var configured,out string reason))throw new System.InvalidOperationException(reason);
                return configured;
            }
        return SelectionGameplayAdapter.DevelopmentDefault;
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
