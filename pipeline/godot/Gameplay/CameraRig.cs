using Godot;
using System.Collections.Generic;
using System.Linq;
using SwtorSurvivors.Integration;

namespace SwtorSurvivors;

/// <summary>Independent orbit using the B1 damped focus and swept-sphere obstruction probes.</summary>
public partial class CameraRig : Node3D
{
    [ExportGroup("Orbit (degrees / physical mouse pixels)")]
    [Export] public float YawDegrees { get; set; } = 0;
    [Export] public float MouseSensitivity { get; set; } = 0.18f;
    [Export] public float MinimumPitch { get; set; } = -80;
    [Export] public float MaximumPitch { get; set; } = 85;
    [Export] public float DefaultPitch { get; set; } = 48;
    [Export] public bool InvertMouseY { get; set; }
    [Export] public float LeftDragThreshold { get; set; } = 4;
    [Export] public bool AutoCenter { get; set; } = false;
    [Export] public float AutoCenterDelay { get; set; } = 3;
    [Export] public float AutoCenterSharpness { get; set; } = 1.5f;
    [ExportGroup("Zoom and follow (preserved SWTOR source units)")]
    // Same 48-degree, sqrt(0.9^2 + 1.05^2) B1 envelope at startup.
    [Export] public float DefaultZoom { get; set; } = 1.383f;
    [Export] public float MinimumZoom { get; set; } = 0.35f;
    [Export] public float MaximumZoom { get; set; } = 2.5f;
    [Export] public float ZoomStep { get; set; } = 0.12f;
    [Export] public float ZoomSharpness { get; set; } = 10;
    [Export] public float TargetHeight { get; set; } = 0.1f;
    // Zero anchors directly to the interpolated character, without a second follow delay.
    [Export] public float FollowSharpness { get; set; } = 0;
    [ExportGroup("B1 obstruction handling (source units)")]
    [Export] public float ObstructionRadius { get; set; } = 0.02f;
    [Export] public float ObstructionRecoverySharpness { get; set; } = 8;
    [Export] public bool RaiseAboveObstructions { get; set; } = false;
    [Export] public float MaximumAvoidanceAngle { get; set; } = 88;
    [Export] public bool HideCloseOccluders { get; set; } = true;
    [Export] public float MinimumSubjectDistance { get; set; } = 0.45f;
    public PlayerController Target { get; set; } = null!;
    public Camera3D Camera { get; private set; } = null!;
    public float ClearFraction { get; private set; } = 1;
    public Vector3 Focus { get; private set; }
    public Vector3 DesiredPosition { get; private set; }
    public float EffectiveAngleDegrees { get; private set; }
    public int HiddenPropMeshes => _hidden.Count;
    public float AngleDegrees { get; set; } = 48;
    // Compatibility for the opt-in B1 fixture's authored obstruction envelope.
    // Gameplay uses DefaultZoom and the live orbit radius instead.
    private float _legacyHeight = 0.9f, _legacyDistance = 1.05f;
    public float Height { get => _legacyHeight; set { _legacyHeight = value; SetLegacyEnvelope(); } }
    public float Distance { get => _legacyDistance; set { _legacyDistance = value; SetLegacyEnvelope(); } }
    private void SetLegacyEnvelope() => RequestedZoom = CurrentZoom = Mathf.Sqrt(Height * Height + Distance * Distance);
    public float RequestedZoom { get; private set; }
    public float CurrentZoom { get; private set; }
    public bool ControlsEnabled { get; private set; }
    public bool Orbiting => _orbitButton != MouseButton.None;
    public bool Steering => CanOrbit && _rightHeld;
    public bool RunForward => CanOrbit && _leftHeld && _rightHeld;
    private bool _leftHeld, _rightHeld;
    private readonly SphereShape3D _probe = new();
    private readonly Dictionary<MeshInstance3D, bool> _hidden = new();
    private readonly Godot.Collections.Array<Rid> _excluded = new();
    private bool _initialized;
    private bool _leftPending;
    private MouseButton _orbitButton;
    private Vector2 _leftTravel;
    private float _resolvedDistance;
    private double _sinceOrbit;

    public override void _Ready()
    {
        Camera = GetNode<Camera3D>("Camera3D");
        ProcessPriority = 10;
        MinimumPitch = Mathf.Clamp(MinimumPitch, -85, 85);
        MaximumPitch = Mathf.Clamp(MaximumPitch, MinimumPitch, 85);
        MaximumAvoidanceAngle = Mathf.Clamp(MaximumAvoidanceAngle, MaximumPitch, 88);
        MinimumZoom = Mathf.Max(ObstructionRadius * 3, MinimumZoom);
        MaximumZoom = Mathf.Max(MinimumZoom, MaximumZoom);
        AngleDegrees = Mathf.Clamp(DefaultPitch, MinimumPitch, MaximumPitch);
        RequestedZoom = CurrentZoom = Mathf.Clamp(DefaultZoom, MinimumZoom, MaximumZoom);
        GetWindow().FocusExited += OnFocusExited;
        GetWindow().MouseExited += OnMouseExited;
    }

    public void SetControlsEnabled(bool enabled)
    {
        ControlsEnabled = enabled;
        if (!enabled) CancelOrbit();
    }
    public void CancelOrbit(bool warp = true)
    {
        _leftPending = false; _leftTravel = Vector2.Zero; _orbitButton = MouseButton.None;
        _leftHeld = _rightHeld = false;
        SwtorCursor.Instance.Release(this, warp);
    }
    private void OnFocusExited() => CancelOrbit(false);
    private void OnMouseExited() { if (!Orbiting) CancelOrbit(false); }
    private bool CanOrbit => ControlsEnabled && !GetTree().Paused;

    // Releases and captured motion must be seen even if the pointer finishes over UI.
    public override void _Input(InputEvent e)
    {
        // Once a world drag owns the mouse, see the second press before GUI handling.
        if (CanOrbit && Orbiting && e is InputEventMouseButton { Pressed: true } press
            && press.ButtonIndex is MouseButton.Left or MouseButton.Right)
        {
            if (press.ButtonIndex == MouseButton.Left) _leftHeld = true;
            else _rightHeld = true;
            GetViewport().SetInputAsHandled(); return;
        }
        if (e is InputEventMouseButton { Pressed: false } b)
        {
            if (b.ButtonIndex is MouseButton.Left or MouseButton.Right)
            {
                bool owned = Orbiting;
                if (b.ButtonIndex == MouseButton.Left) { _leftHeld = false; _leftPending = false; }
                else _rightHeld = false;
                if (owned)
                {
                    if (!_leftHeld && !_rightHeld) CancelOrbit();
                    else _orbitButton = _rightHeld ? MouseButton.Right : MouseButton.Left;
                    GetViewport().SetInputAsHandled();
                }
            }
        }
        if (!CanOrbit) { CancelOrbit(); return; }
        if (Orbiting && e is InputEventMouseButton { Pressed: true } wheel
            && wheel.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        { Zoom(wheel); GetViewport().SetInputAsHandled(); return; }
        if (e is not InputEventMouseMotion motion) return;
        if (!Orbiting) return;
        ApplyOrbitMotion(motion);
    }

    private void ApplyOrbitMotion(InputEventMouseMotion motion)
    {
        // ScreenRelative avoids sensitivity changing with window/UI stretch.
        YawDegrees = Mathf.Wrap(YawDegrees - motion.ScreenRelative.X * MouseSensitivity, -180, 180);
        AngleDegrees = Mathf.Clamp(AngleDegrees + motion.ScreenRelative.Y * MouseSensitivity * (InvertMouseY ? -1 : 1),
            MinimumPitch, MaximumPitch);
        _sinceOrbit = 0;
        GetViewport().SetInputAsHandled();
    }

    // GUI runs before this callback. The explicit hover guard also protects Pass controls/tooltips.
    public override void _UnhandledInput(InputEvent e)
    {
        if (CanOrbit && _leftPending && !Orbiting && e is InputEventMouseMotion motion)
        {
            if (SwtorCursor.PointerOverUi(GetViewport())) { _leftPending = _leftHeld = false; return; }
            _leftTravel += motion.ScreenRelative;
            if (_leftTravel.Length() >= LeftDragThreshold && BeginOrbit(MouseButton.Left)) ApplyOrbitMotion(motion);
            return;
        }
        if (!CanOrbit || e is not InputEventMouseButton { Pressed: true } b || Orbiting
            || SwtorCursor.PointerOverUi(GetViewport())) return;
        if (b.ButtonIndex == MouseButton.Right)
        {
            if (BeginOrbit(MouseButton.Right)) _rightHeld = true;
        }
        else if (b.ButtonIndex == MouseButton.Left) { _leftHeld = true; _leftPending = true; _leftTravel = Vector2.Zero; }
        else if (b.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            Zoom(b);
        }
        else return;
        GetViewport().SetInputAsHandled();
    }

    private void Zoom(InputEventMouseButton wheel)
    {
        RequestedZoom = Mathf.Clamp(RequestedZoom + (wheel.ButtonIndex == MouseButton.WheelUp ? -1 : 1)
            * ZoomStep * Mathf.Max(0.1f, wheel.Factor), MinimumZoom, MaximumZoom);
        _sinceOrbit = 0;
    }

    private bool BeginOrbit(MouseButton button)
    {
        if (!SwtorCursor.Instance.Capture(this)) return false;
        _orbitButton = button; _leftPending = false; _sinceOrbit = 0;
        return true;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused) CancelOrbit();
    }

    public override void _Process(double delta)
    {
        _sinceOrbit += delta;
        if (AutoCenter && CanOrbit && !Orbiting && !_leftPending && _sinceOrbit > AutoCenterDelay && Target != null)
        {
            var movement = new Vector3(Target.ActualVelocity.X, 0, Target.ActualVelocity.Z);
            if (movement.Length() > 0.055f)
                YawDegrees = Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(YawDegrees),
                    Mathf.Atan2(movement.X, movement.Z) + Mathf.Pi, 1 - Mathf.Exp(-AutoCenterSharpness * (float)delta)));
        }
        UpdateCamera(delta);
    }
    public void Snap() { _initialized = false; if (Camera != null && Target != null) UpdateCamera(0); }
    public void UpdateCamera(double delta)
    {
        if (Target == null) return;
        CurrentZoom = Mathf.Lerp(CurrentZoom, RequestedZoom, 1 - Mathf.Exp(-Mathf.Max(0, ZoomSharpness) * (float)delta));
        var target = Target.GetGlobalTransformInterpolated().Origin + Vector3.Up * TargetHeight;
        Focus = !_initialized || FollowSharpness <= 0 ? target : Focus.Lerp(target, 1 - Mathf.Exp(-FollowSharpness * (float)delta));
        bool snapping = !_initialized;
        if (snapping) EffectiveAngleDegrees = AngleDegrees;
        _initialized = true;
        RestoreOccluders();
        _excluded.Clear();
        if (HideCloseOccluders) ClearCloseProps(OffsetAt(AngleDegrees));
        float desiredAngle = AngleDegrees;
        float bestFraction = Clearance(OffsetAt(desiredAngle));
        if (RaiseAboveObstructions && bestFraction < 0.45f)
        {
            // Keep user yaw fixed. Lift only when a normal boom would
            // collapse into the character, testing the same real environmental collision.
            foreach (float angle in new[] { 65f, 78f, MaximumAvoidanceAngle })
            {
                if (angle <= AngleDegrees || angle > MaximumAvoidanceAngle) continue;
                float fraction = Clearance(OffsetAt(angle));
                if (fraction > bestFraction + 0.04f) { bestFraction = fraction; desiredAngle = angle; }
                if (bestFraction > 0.9f) break;
            }
        }
        // User pitch is direct in both directions. Never interpolate the orbit itself.
        EffectiveAngleDegrees = desiredAngle;
        var offset = OffsetAt(EffectiveAngleDegrees);
        DesiredPosition = Focus + offset;
        ClearFraction = Clearance(offset);
        float safeDistance = offset.Length() * Mathf.Max(0, ClearFraction - 0.01f);
        // Retract immediately to avoid geometry; ease outward only within the current safe sweep.
        _resolvedDistance = snapping || safeDistance < _resolvedDistance ? safeDistance :
            Mathf.Lerp(_resolvedDistance, safeDistance, 1 - Mathf.Exp(-ObstructionRecoverySharpness * (float)delta));
        Camera.GlobalPosition = Focus + offset.Normalized() * _resolvedDistance;
        // A complete overlap can collapse the boom; retain the last valid orientation.
        if (_resolvedDistance < 0.0001f) return;
        Camera.LookAt(Focus, Vector3.Up);
    }

    private Vector3 OffsetAt(float angle)
    {
        return new Vector3(0, Mathf.Sin(Mathf.DegToRad(angle)), Mathf.Cos(Mathf.DegToRad(angle)))
            .Rotated(Vector3.Up, Mathf.DegToRad(YawDegrees)) * CurrentZoom;
    }

    private float Clearance(Vector3 offset)
    {
        _probe.Radius = ObstructionRadius;
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = _probe, Transform = new Transform3D(Basis.Identity, Focus), Motion = offset,
            CollisionMask = 3, Margin = 0.0005f, Exclude = _excluded
        };
        var fractions = GetWorld3D().DirectSpaceState.CastMotion(query);
        float fraction = fractions.Length > 0 ? fractions[0] : 1;
        // Shape casts can ignore initial overlaps. A centre ray still catches a thin wall
        // when a tight corner or a lagging focus starts the sweep partly in geometry.
        var ray = PhysicsRayQueryParameters3D.Create(Focus, Focus + offset, 3); ray.Exclude = _excluded;
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
        if (hit.Count > 0)
            fraction = Mathf.Min(fraction, Mathf.Max(0, Focus.DistanceTo((Vector3)hit["position"]) - ObstructionRadius) / offset.Length());
        return fraction;
    }

    private void ClearCloseProps(Vector3 offset)
    {
        for (int i=0;i<8;i++)
        {
            float fraction=Clearance(offset);
            if (fraction*offset.Length() >= MinimumSubjectDistance) break;
            var ray=PhysicsRayQueryParameters3D.Create(Focus,Focus+offset,3); ray.Exclude=_excluded;
            var hit=GetWorld3D().DirectSpaceState.IntersectRay(ray);
            StaticBody3D? body=hit.Count>0 ? hit["collider"].AsGodotObject() as StaticBody3D : null;
            if (body == null)
            {
                // A sphere may touch a ledge that its centre ray misses.
                var query=new PhysicsShapeQueryParameters3D { Shape=_probe, CollisionMask=3, Exclude=_excluded,
                    Transform=new Transform3D(Basis.Identity,Focus+offset*Mathf.Min(1,fraction+0.015f)), Margin=0.001f };
                foreach(var contact in GetWorld3D().DirectSpaceState.IntersectShape(query))
                {
                    var candidate=contact["collider"].AsGodotObject() as StaticBody3D;
                    if(candidate != null && candidate.CollisionLayer==2) { body=candidate; break; }
                }
            }
            if (body == null || body.CollisionLayer != 2) break; // Terrain always remains visible/collidable.
            var owner=body.GetParent();
            if (!owner.HasMeta("source_model")) break;
            foreach(var mesh in owner.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
            {
                if (!_hidden.ContainsKey(mesh)) _hidden[mesh]=mesh.Visible;
                mesh.Visible=false;
            }
            // Exclusion applies only to camera probes. Player collisions stay active.
            foreach(var obstacle in owner.FindChildren("*","StaticBody3D",true,false).OfType<StaticBody3D>())
                if(!_excluded.Contains(obstacle.GetRid())) _excluded.Add(obstacle.GetRid());
        }
    }

    private void RestoreOccluders()
    {
        foreach(var item in _hidden) if(GodotObject.IsInstanceValid(item.Key)) item.Key.Visible=item.Value;
        _hidden.Clear();
    }

    public override void _ExitTree()
    {
        GetWindow().FocusExited -= OnFocusExited;
        GetWindow().MouseExited -= OnMouseExited;
        CancelOrbit(false); RestoreOccluders();
    }
}
