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
        // Keep music owned by the menu so entering the world releases it with the scene.
        var musicStream = GD.Load<AudioStreamWav>("res://Frontend/Assets/Audio/menu_theme.wav");
        musicStream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        musicStream.LoopEnd = (int)(musicStream.GetLength() * musicStream.MixRate);
        var music = new AudioStreamPlayer { Name = "MenuMusic", Stream = musicStream, VolumeLinear = MenuMusicVolume };
        AddChild(music);
        MenuMusicVolumeChanged += volume => music.VolumeLinear = volume;
        music.Play();
        if (GameNavigation.LastSelection is { } saved) RestoreConfiguration(saved);
        if (OS.GetCmdlineUserArgs().Contains("--c2-smoke") && !GetTree().Root.HasNode("C2Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, new C2Smoke { Name = "C2Smoke" });
        if (OS.GetCmdlineUserArgs().Contains("--b101-smoke") && !GetTree().Root.HasNode("B101Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, new B101Smoke { Name = "B101Smoke" });
        if (OS.GetCmdlineUserArgs().Any(a=>a is "--d11-smoke-save" or "--d11-smoke-load") && !GetTree().Root.HasNode("D11Smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild,new D11Smoke {Name="D11Smoke"});
    }
}
