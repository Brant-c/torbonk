using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;
using SwtorSurvivors.UI;

namespace SwtorSurvivors.Integration;

/// <summary>One brief B9 session through actual viewport input and scene transitions.</summary>
public partial class B9Smoke : Node
{
    private readonly List<string> _checks = new();
    private Vector2 _point;
    private void Check(string name, bool pass)
    { if (!pass) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("B9: " + name); }
    private async Task Frames(int count = 2)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> ready)
    {
        for (int i = 0; i < 300 && !ready(); i++) await Frames(1);
        if (!ready()) throw new TimeoutException("B9 transition");
        await Frames();
    }
    private void Button(MouseButton button, bool pressed = true)
        => GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = button, Pressed = pressed,
            Position = _point, GlobalPosition = _point, Factor = 1 }, true);
    private void Motion(Vector2 pixels)
        => GetViewport().PushInput(new InputEventMouseMotion { Position = _point, GlobalPosition = _point,
            Relative = pixels, ScreenRelative = pixels }, true);
    private async Task Point(Vector2 at)
    {
        _point = at;
        Input.WarpMouse(at); Motion(Vector2.Zero); await Frames(3);
    }
    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren())
        { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string? failure = null;
        try
        {
            await Until(() => GetTree().CurrentScene is MainMenu);
            var menu = (MainMenu)GetTree().CurrentScene;
            var cursor = SwtorCursor.Instance;
            Check("one shared native cursor loads original gold art and recovered hotspot", cursor.Theme.Normal.GetSize() == new Vector2(32,32)
                && cursor.Theme.Hotspot == new Vector2(8,3) && cursor.Theme.Hover != null && cursor.Theme.Disabled != null
                && GetTree().Root.GetChildren().OfType<SwtorCursor>().Count() == 1 && Input.MouseMode == Input.MouseModeEnum.Visible);
            var menuButton = Descendants(menu).OfType<Godot.Button>().First(b => b.IsVisibleInTree() && !b.Disabled);
            await Point(menuButton.GetGlobalRect().GetCenter());
            Check("frontend button uses native gold hand with its own hotspot", cursor.CurrentTexture == cursor.Theme.Hover
                && cursor.CurrentHotspot == new Vector2(13,1));
            Check("menu launches existing composition", GameNavigation.TryStart(menu, new SelectionConfiguration(), out _));
            await Until(() => GetTree().CurrentScene is GameWorld { Hud: not null, Progression: not null, Rig.ControlsEnabled: true });
            var world = (GameWorld)GetTree().CurrentScene; var rig = world.Rig;
            world.Waves!.SetPhysicsProcess(false); world.Spawner!.Running = false;
            world.Abilities.SetPhysicsProcess(false); world.Abilities.Abort();
            await Frames(10);
            Check("camera starts elevated with B1 framing and auto-centering disabled", !rig.AutoCenter
                && Mathf.IsEqualApprox(rig.AngleDegrees,48) && Mathf.IsEqualApprox(rig.RequestedZoom,1.383f)
                && rig.Camera.Current && Input.MouseMode == Input.MouseModeEnum.Visible);
            Check("camera has a direct character anchor and no automatic lifting", rig.FollowSharpness == 0 && !rig.RaiseAboveObstructions);
            await Until(() => world.Player.IsOnFloor());
            float groundY = world.Player.GlobalPosition.Y;
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = true }, true);
            await Frames(3);
            Check("Space jumps from the floor", !world.Player.IsOnFloor() && world.Player.GlobalPosition.Y > groundY+0.005f);
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = false }, true);
            var jumpStart = world.Player.GlobalPosition;
            world.Player.TestInput = new Vector2(0,-1); await Frames(8); world.Player.TestInput = Vector2.Zero;
            Check("jumping retains horizontal movement and the ground-footprint guard", new Vector2(world.Player.GlobalPosition.X-jumpStart.X,
                world.Player.GlobalPosition.Z-jumpStart.Z).Length() > 0.02f);
            float verticalBefore = world.Player.Velocity.Y;
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = true }, true);
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = false }, true);
            Check("Space in the air does not add a second jump", world.Player.Velocity.Y == verticalBefore);
            await Until(() => world.Player.IsOnFloor()); await Frames(8);
            Check("jump lands normally without a queued repeat", world.Player.IsOnFloor() && Mathf.Abs(world.Player.GlobalPosition.Y-groundY) < 0.04f);
            await Point(new Vector2(450,300));
            Check("world test point is outside interactive HUD", !SwtorCursor.PointerOverUi(GetViewport()));
            var facing = world.Player.Presentation.Rotation; float yaw = rig.YawDegrees;
            Button(MouseButton.Right); Motion(new Vector2(120,-40)); await Frames();
            Check("RMB captures and rotates independently of a stationary character", rig.Orbiting && cursor.Captured
                && Input.MouseMode == Input.MouseModeEnum.Captured && rig.YawDegrees != yaw
                && world.Player.Presentation.Rotation.IsEqualApprox(facing));
            Motion(new Vector2(0,-10000));
            rig.UpdateCamera(0);
            Check("pitch reaches below the horizon directly without easing or flips", rig.AngleDegrees == rig.MinimumPitch
                && rig.MinimumPitch < 0 && rig.EffectiveAngleDegrees == rig.AngleDegrees);
            Motion(new Vector2(0,10000));
            Check("pitch clamps below vertical", rig.AngleDegrees == rig.MaximumPitch && rig.MaximumPitch < 90);
            Motion(new Vector2(0,(rig.DefaultPitch-rig.AngleDegrees)/rig.MouseSensitivity));
            rig.UpdateCamera(0);
            Check("downward orbit follows the mouse immediately", Mathf.IsEqualApprox(rig.EffectiveAngleDegrees,rig.DefaultPitch));
            float capturedZoom = rig.RequestedZoom; Button(MouseButton.WheelUp);
            Check("wheel also works while orbit is captured", rig.RequestedZoom < capturedZoom);
            Button(MouseButton.WheelDown);
            Button(MouseButton.Right,false); await Frames();
            Check("RMB release restores native cursor", !rig.Orbiting && !cursor.Captured && Input.MouseMode == Input.MouseModeEnum.Visible);
            await Point(new Vector2(450,300)); yaw = rig.YawDegrees;
            Button(MouseButton.Left); Motion(new Vector2(1,0));
            Check("LMB click waits for drag threshold", !rig.Orbiting && rig.YawDegrees == yaw);
            Motion(new Vector2(40,0)); await Frames();
            Check("LMB world drag orbits", rig.Orbiting && rig.YawDegrees != yaw);
            Button(MouseButton.Left,false); await Frames();
            yaw = rig.YawDegrees; world.Player.TestInput = new Vector2(0,-1);
            await Frames(12); world.Player.TestInput = Vector2.Zero; await Frames(12);
            Check("movement preserves orbit yaw and remains camera-relative", rig.YawDegrees == yaw
                && PlayerController.CameraDirection(new Vector2(0,-1),Mathf.DegToRad(90)).DistanceTo(Vector3.Left)<0.0001f);
            rig.UpdateCamera(0);
            Check("camera focus remains exactly anchored to character", rig.Focus.DistanceTo(world.Player.GetGlobalTransformInterpolated().Origin
                + Vector3.Up*rig.TargetHeight)<0.00001f);
            await Point(new Vector2(450,300));
            float zoom = rig.RequestedZoom; Button(MouseButton.WheelUp); await Frames(8);
            Check("wheel zoom interpolates toward its target", rig.RequestedZoom < zoom && rig.CurrentZoom < zoom
                && rig.CurrentZoom > rig.RequestedZoom);
            for (int i=0;i<40;i++) Button(MouseButton.WheelUp);
            Check("minimum zoom clamps", rig.RequestedZoom == rig.MinimumZoom);
            for (int i=0;i<40;i++) Button(MouseButton.WheelDown);
            Check("maximum zoom clamps", rig.RequestedZoom == rig.MaximumZoom);
            // Actual HUD icon uses tooltips and Stop mouse filtering, without a manual ability action.
            var icon = Descendants(world.Hud!).OfType<AbilityIcon>().First();
            await Point(icon.GetGlobalRect().GetCenter());
            Check("tooltip icon is recognized as UI", SwtorCursor.PointerOverUi(GetViewport()));
            yaw = rig.YawDegrees; zoom = rig.RequestedZoom;
            foreach (var button in new[] { MouseButton.Left, MouseButton.Right })
            { Button(button); Motion(new Vector2(80,20)); Button(button,false); }
            Button(MouseButton.WheelUp); await Frames();
            Check("HUD tooltip blocks both drag starts and wheel zoom", !rig.Orbiting && rig.YawDegrees == yaw && rig.RequestedZoom == zoom);
            // A captured release over UI must still reach the camera.
            await Point(new Vector2(450,300)); Button(MouseButton.Right);
            _point = icon.GetGlobalRect().GetCenter();
            Button(MouseButton.Right,false); await Frames();
            Check("releasing an orbit leaves no capture owner", !cursor.Captured && !rig.Orbiting);
            Button(MouseButton.Right); world.Progression!.LevelAnnouncementSeconds = 0.2f;
            world.Progression.AddExperience(world.Progression.RequiredExperience); await Frames();
            Check("upgrade pause immediately releases capture and disables orbit", GetTree().Paused
                && !cursor.Captured && !rig.ControlsEnabled && Input.MouseMode == Input.MouseModeEnum.Visible);
            float pausedY = world.Player.GlobalPosition.Y;
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = true }, true);
            GetViewport().PushInput(new InputEventKey { PhysicalKeycode = Key.Space, Keycode = Key.Space, Pressed = false }, true);
            Check("paused upgrade presentation cannot queue a jump", world.Player.GlobalPosition.Y == pausedY);
            Button(MouseButton.Right,false);
            await Until(() => world.Progression.CanChoose);
            var card = Descendants(world.Hud!).OfType<UpgradeCard>().First();
            await Point(card.GetGlobalRect().GetCenter()); yaw = rig.YawDegrees; zoom = rig.RequestedZoom;
            Button(MouseButton.Right); Motion(new Vector2(100,30)); Button(MouseButton.WheelUp); Button(MouseButton.Right,false);
            Check("upgrade cards cannot orbit or zoom", !rig.Orbiting && rig.YawDegrees == yaw && rig.RequestedZoom == zoom);
            Check("real upgrade choice applies", world.Progression.Choose(0));
            await Frames();
            Check("disabled upgrade card uses native unavailable hand and hotspot", cursor.CurrentTexture == cursor.Theme.Disabled
                && cursor.CurrentHotspot == new Vector2(13,2));
            await Until(() => !GetTree().Paused);
            Check("choice restores camera controls with cursor visible", rig.ControlsEnabled && Input.MouseMode == Input.MouseModeEnum.Visible);
            await Point(new Vector2(450,300)); Button(MouseButton.Right);
            GetWindow().EmitSignal(Window.SignalName.FocusExited); await Frames();
            Check("focus loss releases capture", !rig.Orbiting && Input.MouseMode == Input.MouseModeEnum.Visible);
            Button(MouseButton.Right,false); Button(MouseButton.Right);
            world.PlayerActor.Health.ApplyDamage(10000); await Frames();
            Check("death releases capture and disables orbit", !world.PlayerActor.Alive && !rig.ControlsEnabled && !cursor.Captured
                && Input.MouseMode == Input.MouseModeEnum.Visible);
            Button(MouseButton.Right,false); world.ResetCombatWorld(); await Frames();
            Check("restart restores controls without capture", rig.ControlsEnabled && !cursor.Captured && world.PlayerActor.Alive);
            await Point(new Vector2(450,300)); Button(MouseButton.Right);
            Check("capture works again before transition", cursor.Captured);
            world.ReturnToMainMenu(); await Until(() => GetTree().CurrentScene is MainMenu);
            Check("gameplay to frontend restores original cursor without duplicate controller", !GodotObject.IsInstanceValid(world)
                && !cursor.Captured && !GetTree().Paused && Input.MouseMode == Input.MouseModeEnum.Visible
                && GetTree().Root.GetChildren().OfType<SwtorCursor>().Count()==1);
        }
        catch (Exception exception) { failure = exception.ToString(); GD.PushError(failure); }
        using var output = Godot.FileAccess.Open("res://../manifests/b9-smoke.json", Godot.FileAccess.ModeFlags.Write);
        output.StoreString(JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure,
            scope = "One brief Godot viewport-input session; manual visual/collision review still required" },
            new JsonSerializerOptions { WriteIndented = true }));
        GetTree().Quit(failure == null ? 0 : 1);
    }
}
