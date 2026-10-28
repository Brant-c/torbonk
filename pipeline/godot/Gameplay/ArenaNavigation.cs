using Godot;
using System;

namespace SwtorSurvivors;

/// <summary>Small-arena local navigation. Uses existing terrain and prop collision, never art names.</summary>
public partial class ArenaNavigation : Node3D
{
    public ArenaBounds Arena { get; set; } = null!;
    [Export] public float Radius { get; set; } = 0.03f;
    [Export] public float Height { get; set; } = 0.185f;
    private CapsuleShape3D _capsule = null!;
    private static readonly float[] SteeringAngles = { 0f, 35f, -35f, 70f, -70f, 100f, -100f, 135f, -135f };
    public long SteeringChecks { get; private set; }

    public override void _Ready() => _capsule = new CapsuleShape3D { Radius = Radius, Height = Height, Margin = 0.0005f };

    public bool TryStandingPoint(Vector3 near, out Vector3 feet)
    {
        feet = near;
        if (!Arena.Contains(near, Radius) ||
            !Arena.GroundAt(near, out var floor, out _, 0.3f, 0.6f) || !Arena.Supported(floor, Radius)) return false;
        feet = floor + Vector3.Up * 0.002f;
        // Full-body clearance includes overhangs and props. A small sole gap avoids touching the floor.
        var query = Query(feet + Vector3.Up * 0.01f, 3);
        return GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    private PhysicsShapeQueryParameters3D Query(Vector3 feet, uint mask) => new()
    {
        Shape = _capsule, CollisionMask = mask, Margin = 0.0005f,
        Transform = new Transform3D(Basis.Identity, feet + Vector3.Up * (Height * 0.5f))
    };

    public bool CanWalk(Vector3 from, Vector3 to)
    {
        var query = Query(from + Vector3.Up * 0.003f, 2);
        query.Motion = to - from;
        if (GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count > 0) return false;
        var sweep = GetWorld3D().DirectSpaceState.CastMotion(query);
        // Reject obstructed directions before doing the more expensive terrain footprint walk.
        return sweep.Length >= 2 && sweep[0] >= 0.999f && Arena.CanTraverse(from, to, Radius);
    }

    public Vector3 Steer(Vector3 from, Vector3 toward, float lookAhead, int preferredSide)
    {
        SteeringChecks++;
        var direction = new Vector3(toward.X - from.X, 0, toward.Z - from.Z).Normalized();
        if (direction.IsZeroApprox()) return Vector3.Zero;
        // A stable handedness prevents left/right jitter at a wall. No global graph or runtime baking.
        foreach (float angle in SteeringAngles)
        {
            var candidate = direction.Rotated(Vector3.Up, Mathf.DegToRad(angle * preferredSide));
            if (CanWalk(from, from + candidate * lookAhead)) return candidate;
        }
        return Vector3.Zero;
    }

    public bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        var query = PhysicsRayQueryParameters3D.Create(from, to, 3);
        query.HitBackFaces = true;
        return GetWorld3D().DirectSpaceState.IntersectRay(query).Count == 0;
    }
}
