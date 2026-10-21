using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Periodic group scan, deterministic nearest selection, and immediate locked-target checks.</summary>
public partial class TargetingService : Node
{
    [Export] public float AcquisitionRange { get; set; } = 0.85f;
    [Export] public float RefreshSeconds { get; set; } = 0.2f;
    [Export(PropertyHint.Layers3DPhysics)] public uint EnvironmentMask { get; set; } = 3;
    public Combatant OwnerActor { get; set; } = null!;
    public Combatant? Current { get; private set; }
    public int Scans { get; private set; }
    private double _refreshIn;

    public bool HasLineOfSight(Combatant candidate)
    {
        var query = PhysicsRayQueryParameters3D.Create(OwnerActor.AimPosition, candidate.AimPosition, EnvironmentMask);
        query.HitBackFaces = true;
        return OwnerActor.GetWorld3D().DirectSpaceState.IntersectRay(query).Count == 0;
    }

    public bool IsValidTarget(Combatant? candidate) => IsValidTarget(candidate,AcquisitionRange);
    public bool IsValidTarget(Combatant? candidate,float range) => GodotObject.IsInstanceValid(OwnerActor) && OwnerActor.Alive &&
        candidate != null && GodotObject.IsInstanceValid(candidate) && candidate.Alive &&
        OwnerActor.IsHostileTo(candidate) && range > 0 &&
        OwnerActor.GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition) <= range * range &&
        HasLineOfSight(candidate);

    public Combatant? FindNearest()
    {
        Scans++;
        Combatant? best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (var node in GetTree().GetNodesInGroup(Combatant.GroupName))
        {
            if (node is not Combatant candidate || !IsValidTarget(candidate)) continue;
            float distance = OwnerActor.GlobalPosition.DistanceSquaredTo(candidate.GlobalPosition);
            // Equal-distance targets sort by authored identity, then unique scene path.
            int order = best == null ? -1 : string.CompareOrdinal(candidate.SelectionKey, best.SelectionKey);
            if (distance < bestDistance || (distance == bestDistance &&
                (order < 0 || (order == 0 && string.CompareOrdinal(candidate.GetPath().ToString(), best!.GetPath().ToString()) < 0))))
            { best = candidate; bestDistance = distance; }
        }
        return best;
    }

    public void Update(double delta, bool lockCurrent)
    {
        _refreshIn -= delta;
        if (!IsValidTarget(Current)) Current = null;
        if (_refreshIn > 0) return;
        _refreshIn = Math.Max(0.02, RefreshSeconds);
        if (!lockCurrent) Current = FindNearest();
    }

    public void Clear() { Current = null; _refreshIn = 0; }

    public Combatant? Acquire(float range,AbilityTargetRule rule,float radius)
    {
        var candidates=GetTree().GetNodesInGroup(Combatant.GroupName).OfType<Combatant>()
            .Where(c=>IsValidTarget(c,range)).ToArray();
        return candidates.OrderByDescending(c=>rule==AbilityTargetRule.HostileCluster
                ? candidates.Count(other=>other.GlobalPosition.DistanceSquaredTo(c.GlobalPosition)<=radius*radius):0)
            .ThenBy(c=>OwnerActor.GlobalPosition.DistanceSquaredTo(c.GlobalPosition))
            .ThenBy(c=>c.SelectionKey,StringComparer.Ordinal).ThenBy(c=>c.GetPath().ToString(),StringComparer.Ordinal).FirstOrDefault();
    }
    public IEnumerable<Combatant> InArea(Vector3 center,float radius,float range,bool persistent=false) =>
        GetTree().GetNodesInGroup(Combatant.GroupName).OfType<Combatant>()
            .Where(c=>c.Alive && OwnerActor.IsHostileTo(c) && c.GlobalPosition.DistanceSquaredTo(center)<=radius*radius &&
                (persistent?AreaLineOfSight(center,c):IsValidTarget(c,range)));

    private bool AreaLineOfSight(Vector3 center,Combatant target)
    {
        var query=PhysicsRayQueryParameters3D.Create(center+Vector3.Up*0.1f,target.AimPosition,EnvironmentMask);
        query.HitBackFaces=true;
        return OwnerActor.GetWorld3D().DirectSpaceState.IntersectRay(query).Count==0;
    }
}
