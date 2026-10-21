using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors.Integration;

/// <summary>One application-wide native cursor and temporary orbit capture owner.</summary>
public partial class SwtorCursor : Node
{
    public static SwtorCursor Instance { get; private set; } = null!;
    public bool Captured => GodotObject.IsInstanceValid(_owner);
    public SwtorCursorTheme Theme { get; private set; } = null!;
    private Node? _owner;
    private Node? _scene;
    private Vector2 _returnPosition;
    private Texture2D? _shown;
    private Vector2 _shownHotspot;
    public Vector2 CurrentHotspot => _shownHotspot;
    public Texture2D? CurrentTexture => _shown;
    private bool _pressed;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        Theme = GD.Load<SwtorCursorTheme>("res://Assets/UI/Cursor/cursor_theme.tres");
        ShowTexture(Theme.Normal, Theme.Hotspot);
        RestoreVisible();
        GetWindow().FocusExited += OnFocusExited;
        GetWindow().MouseExited += OnMouseExited;
        if (OS.GetCmdlineUserArgs().Contains("--b9-smoke"))
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, new B9Smoke { Name = "B9Smoke" });
    }

    public static bool PointerOverUi(Viewport viewport) => viewport.GuiGetHoveredControl() != null;

    public bool Capture(Node owner)
    {
        if (GetTree().Paused || (Captured && _owner != owner)) return false;
        if (!Captured) _returnPosition = GetViewport().GetMousePosition();
        _owner = owner;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        return true;
    }

    public void Release(Node owner, bool warp = true)
    {
        if (_owner == owner) RestoreVisible(warp);
    }

    public void RestoreVisible(bool warp = false)
    {
        bool wasCaptured = Captured;
        _owner = null; _pressed = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (warp && wasCaptured) Input.WarpMouse(_returnPosition);
    }

    private void OnFocusExited()
    {
        if (_owner is CameraRig rig) rig.CancelOrbit(false);
        RestoreVisible();
    }
    private void OnMouseExited() => _pressed = false;

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } b) _pressed = b.Pressed;
    }

    public override void _Process(double delta)
    {
        var scene = GetTree().CurrentScene;
        if (_scene != scene || (GetTree().Paused && Captured) || (_owner != null && (!Captured || !_owner.CanProcess())))
        {
            if (_owner is CameraRig rig && GodotObject.IsInstanceValid(rig)) rig.CancelOrbit(false);
            RestoreVisible(); _scene = scene;
        }
        if (Captured) return;
        var hovered = GetViewport().GuiGetHoveredControl();
        BaseButton? button = null;
        for (Node? node = hovered; node != null; node = node.GetParent())
            if (node is BaseButton candidate) { button = candidate; break; }
        if (button?.Disabled == true && Theme.Disabled != null) ShowTexture(Theme.Disabled, Theme.DisabledHotspot);
        else if (_pressed && Theme.Pressed != null) ShowTexture(Theme.Pressed, Theme.PressedHotspot);
        else if (button != null && Theme.Hover != null) ShowTexture(Theme.Hover, Theme.HoverHotspot);
        else ShowTexture(Theme.Normal, Theme.Hotspot);
    }

    private void ShowTexture(Texture2D texture, Vector2 hotspot)
    {
        if (_shown == texture && _shownHotspot == hotspot) return;
        _shown = texture; _shownHotspot = hotspot;
        // Godot UI may select any shape (including pointing-hand/text/forbidden).
        // Keep the currently selected original artwork/hotspot across native UI shapes.
        foreach (var shape in Enum.GetValues<Input.CursorShape>())
            Input.SetCustomMouseCursor(texture, shape, hotspot);
    }

    public override void _ExitTree()
    {
        GetWindow().FocusExited -= OnFocusExited;
        GetWindow().MouseExited -= OnMouseExited;
        RestoreVisible();
        foreach (var shape in Enum.GetValues<Input.CursorShape>()) Input.SetCustomMouseCursor(null, shape);
        // Release the managed autoload's texture references before RenderingServer shuts down.
        _shown = null;
        Theme.Normal = null!; Theme.Hover = null; Theme.Pressed = null; Theme.Disabled = null;
        Theme = null!; Instance = null!;
    }
}
