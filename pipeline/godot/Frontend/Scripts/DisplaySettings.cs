using Godot;

namespace SwtorSurvivors.Frontend;

/// <summary>Display preferences shared by the menu and gameplay windows.</summary>
public partial class DisplaySettings : Node
{
    private const string SavePath = "user://display.cfg";
    public static bool Fullscreen { get; private set; }
    public static Vector2I Resolution { get; private set; } = new(1000, 800);

    public override void _Ready()
    {
        if (DisplayServer.GetName() == "headless") return;
        var config = new ConfigFile();
        if (config.Load(SavePath) != Error.Ok) return;
        Fullscreen = config.GetValue("display", "fullscreen", false).AsBool();
        int width = config.GetValue("display", "width", 1000).AsInt32();
        int height = config.GetValue("display", "height", 800).AsInt32();
        var screen = DisplayServer.ScreenGetSize();
        Resolution = new Vector2I(Mathf.Clamp(width, 640, Mathf.Max(640, screen.X)),
            Mathf.Clamp(height, 480, Mathf.Max(480, screen.Y)));
        Apply();
    }

    public static Error Set(Window window, bool fullscreen, Vector2I resolution)
    {
        Fullscreen = fullscreen;
        Resolution = resolution;
        ApplyTo(window);
        var config = new ConfigFile();
        config.SetValue("display", "fullscreen", Fullscreen);
        config.SetValue("display", "width", Resolution.X);
        config.SetValue("display", "height", Resolution.Y);
        return config.Save(SavePath);
    }

    private void Apply() => ApplyTo(GetTree().Root);

    private static void ApplyTo(Window window)
    {
        // Render at the actual window size; keep the chosen resolution as the UI design size.
        // CanvasItems scales controls before rasterization instead of enlarging a finished frame.
        window.ContentScaleSize = Resolution;
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(Resolution);
        if (Fullscreen)
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        else
        {
            var usable = DisplayServer.ScreenGetUsableRect();
            DisplayServer.WindowSetPosition(usable.Position + (usable.Size - Resolution) / 2);
        }
    }
}
