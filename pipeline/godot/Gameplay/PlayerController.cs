using Godot;

namespace SwtorSurvivors;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 0.6f;
    [Export] public float Acceleration { get; set; } = 3.0f;
    [Export] public float Deceleration { get; set; } = 4.0f;
    [Export] public float Gravity { get; set; } = 3.0f;
    [Export] public float JumpHeight { get; set; } = 0.14f;
    [Export] public float CapsuleRadius { get; set; } = 0.03f;
    [Export] public float RecoveryFallDistance { get; set; } = 0.3f;
    public ArenaBounds Arena { get; set; } = null!;
    public CameraRig Rig { get; set; } = null!;
    public CharacterPresentation Presentation { get; private set; } = null!;
    public Vector3 SpawnPosition { get; set; }
    public Vector3 ActualVelocity { get; private set; }
    public bool StationaryForAbility=>ActualVelocity.LengthSquared()<.0009f && Velocity.LengthSquared()<.0009f && IsOnFloor()
        && (TestInput??Input.GetVector("b1_left","b1_right","b1_forward","b1_back")).IsZeroApprox() && !Rig.RunForward;
    public Vector2? TestInput { get; set; }
    public int Recoveries { get; private set; }
    public int WallContacts { get; private set; }
    public long PhysicsSteps { get; private set; }
    private Vector3 _safePosition;
    private Vector3 _groundFootprint;
    private bool _jumpQueued;
    private Vector3 _abilityDash;
    private float _dashRemaining;
    private float? _facingYaw;
    public void BeginAbilityDash(Vector3 direction,float distance){_abilityDash=direction.Normalized();_dashRemaining=Mathf.Clamp(distance,0,1.1f);}

    public override void _Ready()
    {
        Presentation = GetNode<CharacterPresentation>("Presentation");
        Presentation.LoadJumpAnimations();
        FloorMaxAngle = Mathf.DegToRad(48); FloorSnapLength = 0.045f;
        FloorStopOnSlope = true; FloorConstantSpeed = true; SafeMargin = 0.0005f;
        CollisionLayer = 4; CollisionMask = 3;
        _safePosition = _groundFootprint = GlobalPosition;
    }

    public override void _Input(InputEvent e)
    {
        if (Rig == null || !Rig.ControlsEnabled || GetTree().Paused || !e.IsAction("b9_jump")) return;
        // Gameplay Space must not activate a HUD button left focused after a mouse click.
        // Paused upgrade/menu controls retain their own normal keyboard handling.
        if (GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;
        if (e.IsActionPressed("b9_jump", false) && IsOnFloor()) _jumpQueued = true;
        GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused) _jumpQueued = false;
    }

    public static Vector3 CameraDirection(Vector2 input, float yaw)
    {
        if (input.Length() > 1) input = input.Normalized();
        return new Basis(Vector3.Up, yaw) * new Vector3(input.X, 0, input.Y);
    }

    public override void _PhysicsProcess(double delta)
    {
        _facingYaw ??= Rig.YawDegrees;
        if (Rig.Steering) _facingYaw = Rig.YawDegrees;
        var input = TestInput ?? (Rig.ControlsEnabled
            ? Input.GetVector("b1_left", "b1_right", "b1_forward", "b1_back") : Vector2.Zero);
        if (TestInput == null && Rig.RunForward) input.Y = -1;
        Step(delta, input);
    }

    public void Step(double delta, Vector2 input)
    {
        PhysicsSteps++;
        float dt = (float)delta;
        // Live keyboard movement follows character facing, independent of left-drag orbit.
        // Explicit fixture input retains its authored camera-relative coordinates.
        var desired = CameraDirection(input, Mathf.DegToRad(TestInput.HasValue ? Rig.YawDegrees : _facingYaw ?? Rig.YawDegrees)) * MoveSpeed;
        var horizontal = new Vector3(Velocity.X, 0, Velocity.Z).MoveToward(desired,
            (input.IsZeroApprox() ? Deceleration : Acceleration) * dt);
        if(_dashRemaining>0){float step=Mathf.Min(_dashRemaining,2.2f*dt);horizontal=_abilityDash*(step/Mathf.Max(dt,.001f));_dashRemaining-=step;}
        var before = GlobalPosition;
        // Ground-footprint safety remains active in the air. Probe from the last
        // traversable ground height, rather than treating jump height as a terrain hole.
        var footprint = IsOnFloor() ? before : new Vector3(before.X, _groundFootprint.Y, before.Z);
        bool jump = _jumpQueued && IsOnFloor(); _jumpQueued = false;
        if (!Arena.CanTraverse(footprint, footprint + horizontal * dt, CapsuleRadius))
        {
            // Slide along the valid area's edge when either independent axis remains safe.
            var x = new Vector3(horizontal.X, 0, 0); var z = new Vector3(0, 0, horizontal.Z);
            horizontal = Arena.CanTraverse(footprint, footprint + x * dt, CapsuleRadius) ? x :
                Arena.CanTraverse(footprint, footprint + z * dt, CapsuleRadius) ? z : Vector3.Zero;
        }
        float vertical = jump ? Mathf.Sqrt(2 * Mathf.Max(0, Gravity) * Mathf.Max(0, JumpHeight)) :
            IsOnFloor() ? 0 : Mathf.Max(Velocity.Y - Gravity * dt, -3);
        Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        MoveAndSlide();
        if (IsOnWall()) WallContacts++;
        // Physics may slide sideways along a prop; recheck the actual swept footprint as well.
        var actualFootprint = new Vector3(GlobalPosition.X, footprint.Y, GlobalPosition.Z);
        if (!Arena.CanTraverse(footprint, actualFootprint, CapsuleRadius))
        {
            // Keep the part of the physics slide that is still safe terrain, so a prop edge next to a drop slides
            // along it instead of freezing the player in place.
            var slide = actualFootprint - footprint;
            var alongX = new Vector3(slide.X, 0, 0); var alongZ = new Vector3(0, 0, slide.Z);
            if (Arena.CanTraverse(footprint, footprint + alongX, CapsuleRadius))
            { GlobalPosition = new Vector3(before.X + slide.X, before.Y, before.Z); Velocity = new Vector3(Velocity.X, 0, 0); }
            else if (Arena.CanTraverse(footprint, footprint + alongZ, CapsuleRadius))
            { GlobalPosition = new Vector3(before.X, before.Y, before.Z + slide.Z); Velocity = new Vector3(0, 0, Velocity.Z); }
            else { GlobalPosition = before; Velocity = Vector3.Zero; }
        }
        else if (Arena.GroundAt(actualFootprint, out var ground, out _, Arena.ProbeRise, Arena.MaximumDrop))
            _groundFootprint = ground;
        if (IsOnFloor() && Arena.Supported(GlobalPosition, CapsuleRadius)) _safePosition = GlobalPosition;
        if (!Arena.Contains(GlobalPosition, CapsuleRadius) || GlobalPosition.Y < _safePosition.Y - RecoveryFallDistance)
        {
            Recoveries++; GlobalPosition = _safePosition; _groundFootprint = _safePosition; Velocity = Vector3.Zero;
        }
        ActualVelocity = (GlobalPosition - before) / dt;
        // Keep the movement basis independent of the visual model: strafing should
        // turn the running model toward travel without redirecting keyboard input.
        // Only stationary right-drag needs an explicit camera-facing pose.
        bool stationarySteering = !TestInput.HasValue && Rig.Steering
            && new Vector2(ActualVelocity.X, ActualVelocity.Z).Length() <= 0.025f;
        Presentation.Present(ActualVelocity, delta, stationarySteering ? _facingYaw : null);
        Presentation.PresentJump(IsOnFloor(), jump && !IsOnFloor(), Velocity.Y,
            Mathf.Sqrt(2 * Mathf.Max(0, JumpHeight) / Mathf.Max(0.01f, Gravity)), delta);
    }

    public void ResetPlayer()
    {
        GlobalPosition = SpawnPosition; _safePosition = _groundFootprint = SpawnPosition; Velocity = Vector3.Zero;
        _jumpQueued = false;_dashRemaining=0;
        _facingYaw = Rig.YawDegrees;
        ActualVelocity = Vector3.Zero; Presentation.ResetPresentation();
        ResetPhysicsInterpolation(); Rig.Snap();
    }
}
