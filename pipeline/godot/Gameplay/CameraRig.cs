using Godot;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Fixed yaw, damped target and swept-sphere obstruction avoidance in source units.</summary>
public partial class CameraRig : Node3D
{
    [Export] public float Height { get; set; } = 0.9f;
    [Export] public float Distance { get; set; } = 1.05f;
    [Export] public float YawDegrees { get; set; } = 0;
    [Export] public float AngleDegrees { get; set; } = 48;
    [Export] public float TargetHeight { get; set; } = 0.1f;
    [Export] public float FollowSharpness { get; set; } = 12;
    [Export] public float ObstructionRadius { get; set; } = 0.02f;
    [Export] public bool RaiseAboveObstructions { get; set; } = true;
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
    private readonly SphereShape3D _probe = new();
    private readonly Dictionary<MeshInstance3D, bool> _hidden = new();
    private readonly Godot.Collections.Array<Rid> _excluded = new();
    private bool _initialized;

    public override void _Ready()
    {
        Camera = GetNode<Camera3D>("Camera3D");
        ProcessPriority = 10;
    }
    public override void _Process(double delta) => UpdateCamera(delta);
    public void Snap() { _initialized = false; if (Camera != null && Target != null) UpdateCamera(0); }
    public void UpdateCamera(double delta)
    {
        if (Target == null) return;
        var target = Target.GetGlobalTransformInterpolated().Origin + Vector3.Up * TargetHeight;
        Focus = !_initialized ? target : Focus.Lerp(target, 1 - Mathf.Exp(-FollowSharpness * (float)delta));
        if (!_initialized) EffectiveAngleDegrees = AngleDegrees;
        _initialized = true;
        RestoreOccluders();
        _excluded.Clear();
        if (HideCloseOccluders) ClearCloseProps(OffsetAt(AngleDegrees));
        float desiredAngle = AngleDegrees;
        float bestFraction = Clearance(OffsetAt(desiredAngle));
        if (RaiseAboveObstructions && bestFraction < 0.45f)
        {
            // Keep yaw/control orientation fixed. Lift only when a normal boom would
            // collapse into the character, testing the same real environmental collision.
            foreach (float angle in new[] { 65f, 78f, MaximumAvoidanceAngle })
            {
                if (angle <= AngleDegrees || angle > MaximumAvoidanceAngle) continue;
                float fraction = Clearance(OffsetAt(angle));
                if (fraction > bestFraction + 0.04f) { bestFraction = fraction; desiredAngle = angle; }
                if (bestFraction > 0.9f) break;
            }
        }
        EffectiveAngleDegrees = desiredAngle >= EffectiveAngleDegrees ? desiredAngle :
            Mathf.Lerp(EffectiveAngleDegrees, desiredAngle, 1 - Mathf.Exp(-6 * (float)delta));
        var offset = OffsetAt(EffectiveAngleDegrees);
        DesiredPosition = Focus + offset;
        ClearFraction = Clearance(offset);
        Camera.GlobalPosition = Focus + offset * Mathf.Max(0.01f, ClearFraction - 0.01f);
        Camera.LookAt(Focus, Vector3.Up);
    }

    private Vector3 OffsetAt(float angle)
    {
        // Height and distance set the orbit envelope; angle sets elevation independently.
        return new Vector3(0, Height, Distance).Rotated(Vector3.Right,
            Mathf.Atan2(Height, Distance) - Mathf.DegToRad(angle)).Rotated(Vector3.Up, Mathf.DegToRad(YawDegrees));
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

    public override void _ExitTree() => RestoreOccluders();
}
