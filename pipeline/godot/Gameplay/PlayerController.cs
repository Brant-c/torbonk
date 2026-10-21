using Godot;

namespace SwtorSurvivors;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 0.6f;
    [Export] public float Acceleration { get; set; } = 3.0f;
    [Export] public float Deceleration { get; set; } = 4.0f;
    [Export] public float Gravity { get; set; } = 3.0f;
    [Export] public float CapsuleRadius { get; set; } = 0.03f;
    [Export] public float RecoveryFallDistance { get; set; } = 0.3f;
    public ArenaBounds Arena { get; set; } = null!;
    public CameraRig Rig { get; set; } = null!;
    public CharacterPresentation Presentation { get; private set; } = null!;
    public Vector3 SpawnPosition { get; set; }
    public Vector3 ActualVelocity { get; private set; }
    public Vector2? TestInput { get; set; }
    public int Recoveries { get; private set; }
    public int WallContacts { get; private set; }
    public long PhysicsSteps { get; private set; }
    private Vector3 _safePosition;

    public override void _Ready()
    {
        Presentation = GetNode<CharacterPresentation>("Presentation");
        FloorMaxAngle = Mathf.DegToRad(48); FloorSnapLength = 0.045f;
        FloorStopOnSlope = true; FloorConstantSpeed = true; SafeMargin = 0.0005f;
        CollisionLayer = 4; CollisionMask = 3;
        _safePosition = GlobalPosition;
    }

    public static Vector3 CameraDirection(Vector2 input, float yaw)
    {
        if (input.Length() > 1) input = input.Normalized();
        return new Basis(Vector3.Up, yaw) * new Vector3(input.X, 0, input.Y);
    }

    public override void _PhysicsProcess(double delta)
    {
        Step(delta, TestInput ?? Input.GetVector("b1_left", "b1_right", "b1_forward", "b1_back"));
    }

    public void Step(double delta, Vector2 input)
    {
        PhysicsSteps++;
        float dt = (float)delta;
        var desired = CameraDirection(input, Mathf.DegToRad(Rig.YawDegrees)) * MoveSpeed;
        var horizontal = new Vector3(Velocity.X, 0, Velocity.Z).MoveToward(desired,
            (input.IsZeroApprox() ? Deceleration : Acceleration) * dt);
        var before = GlobalPosition;
        if (!Arena.CanTraverse(before, before + horizontal * dt, CapsuleRadius))
        {
            // Slide along the valid area's edge when either independent axis remains safe.
            var x = new Vector3(horizontal.X, 0, 0); var z = new Vector3(0, 0, horizontal.Z);
            horizontal = Arena.CanTraverse(before, before + x * dt, CapsuleRadius) ? x :
                Arena.CanTraverse(before, before + z * dt, CapsuleRadius) ? z : Vector3.Zero;
        }
        Velocity = new Vector3(horizontal.X, IsOnFloor() ? 0 : Mathf.Max(Velocity.Y - Gravity * dt, -3), horizontal.Z);
        MoveAndSlide();
        if (IsOnWall()) WallContacts++;
        // Physics may slide sideways along a prop; recheck the actual swept footprint as well.
        if (!Arena.CanTraverse(before, new Vector3(GlobalPosition.X, before.Y, GlobalPosition.Z), CapsuleRadius))
        {
            GlobalPosition = before; Velocity = Vector3.Zero;
        }
        if (IsOnFloor() && Arena.Supported(GlobalPosition, CapsuleRadius)) _safePosition = GlobalPosition;
        if (!Arena.Contains(GlobalPosition, CapsuleRadius) || GlobalPosition.Y < _safePosition.Y - RecoveryFallDistance)
        {
            Recoveries++; GlobalPosition = _safePosition; Velocity = Vector3.Zero;
        }
        ActualVelocity = (GlobalPosition - before) / dt;
        Presentation.Present(ActualVelocity, delta);
    }

    public void ResetPlayer()
    {
        GlobalPosition = SpawnPosition; _safePosition = SpawnPosition; Velocity = Vector3.Zero;
        ActualVelocity = Vector3.Zero; Presentation.ResetPresentation();
        ResetPhysicsInterpolation(); Rig.Snap();
    }
}
