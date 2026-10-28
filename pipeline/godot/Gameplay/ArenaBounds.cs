using Godot;
using System;

namespace SwtorSurvivors;

/// <summary>Authoring limits plus ground-footprint probes against the existing terrain only.
/// Missing triangles stay holes. No replacement floors or generated terrain are added.</summary>
public partial class ArenaBounds : Node3D
{
    [Export] public Vector2 Minimum { get; set; } = new(20.2f, 6.2f);
    [Export] public Vector2 Maximum { get; set; } = new(45.4f, 18.8f);
    /// <summary>Preferred start (XZ). Korriban keeps its authored point; other maps fall back to the nearest supported ground.</summary>
    [Export] public Vector2 SpawnSeed { get; set; } = new(36f, 12.6f);
    [Export] public float EdgeMargin { get; set; } = 0.06f;
    [Export] public float MaximumSlopeDegrees { get; set; } = 48;
    [Export] public float MaximumDrop { get; set; } = 0.065f;
    [Export] public float ProbeRise { get; set; } = 0.07f;
    /// <summary>Terrain by default; authored raid platforms also provide standing ground.</summary>
    [Export] public uint GroundCollisionMask { get; set; } = 1;
    public int RejectedMoves { get; private set; }
    public string LastRejection { get; private set; } = "";
    public long GroundRaycasts { get; private set; }

    public bool Contains(Vector3 p, float radius = 0) =>
        p.X >= Minimum.X + EdgeMargin + radius && p.X <= Maximum.X - EdgeMargin - radius &&
        p.Z >= Minimum.Y + EdgeMargin + radius && p.Z <= Maximum.Y - EdgeMargin - radius;

    public bool GroundAt(Vector3 point, out Vector3 ground, out Vector3 normal,
        float rise = 0.07f, float drop = 0.065f)
    {
        GroundRaycasts++;
        var query = PhysicsRayQueryParameters3D.Create(point + Vector3.Up * rise, point - Vector3.Up * drop, GroundCollisionMask);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        ground = hit.Count > 0 ? (Vector3)hit["position"] : point;
        normal = hit.Count > 0 ? (Vector3)hit["normal"] : Vector3.Zero;
        return hit.Count > 0;
    }

    public bool Supported(Vector3 feet, float radius) => Supported(feet, radius, out _);

    private bool Supported(Vector3 feet, float radius, out Vector3 ground)
    {
        ground = feet;
        if (!Contains(feet, radius)) { LastRejection = "terrain crop edge"; return false; }
        // Ring covers capsule footprint, including narrow source holes near a valid centre.
        for (int i = -1; i < 8; i++)
        {
            float angle = i * Mathf.Tau / 8;
            var sample = feet + (i < 0 ? Vector3.Zero : new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
            if (!GroundAt(sample, out var hit, out var normal, ProbeRise, MaximumDrop))
            { LastRejection = "hole or unsafe drop"; return false; }
            if (normal.Y < Mathf.Cos(Mathf.DegToRad(MaximumSlopeDegrees)))
            { LastRejection = "steep terrain"; return false; }
            if (i < 0) ground = hit;
        }
        return true;
    }

    public bool CanTraverse(Vector3 from, Vector3 to, float radius)
    {
        int steps = Math.Max(1, (int)Mathf.Ceil(from.DistanceTo(to) / (radius * 0.5f)));
        var previous = from;
        for (int i = 1; i <= steps; i++)
        {
            var sample = from.Lerp(to, (float)i / steps); sample.Y = previous.Y;
            // Supported already probes the centre; reuse that exact hit for the next step.
            if (!Supported(sample, radius, out previous))
            { RejectedMoves++; return false; }
        }
        return true;
    }
}
