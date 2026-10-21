using Godot;

namespace SwtorSurvivors.Integration;

/// <summary>Original native SWTOR cursor artwork and each image's recovered hotspot.</summary>
[GlobalClass]
public partial class SwtorCursorTheme : Resource
{
    [Export] public Texture2D Normal { get; set; } = null!;
    [Export] public Texture2D? Hover { get; set; }
    [Export] public Texture2D? Pressed { get; set; }
    [Export] public Texture2D? Disabled { get; set; }
    [Export] public Vector2 Hotspot { get; set; } = new(8, 3);
    [Export] public Vector2 HoverHotspot { get; set; } = new(13, 1);
    [Export] public Vector2 PressedHotspot { get; set; } = new(8, 3);
    [Export] public Vector2 DisabledHotspot { get; set; } = new(13, 2);
}
