using Godot;

namespace SwtorSurvivors;

public enum CombatTeam { Neutral, Player, Hostile }

/// <summary>Reusable combat identity; a visual rig is optional for gameplay targeting.</summary>
public partial class Combatant : Node3D
{
    public const string GroupName = "b2_combatants";
    [Export] public string TargetId { get; set; } = "";
    [Export] public CombatTeam Team { get; set; } = CombatTeam.Neutral;
    [Export] public bool CombatEnabled { get; set; } = true;
    [Export] public float AimHeight { get; set; } = 0.11f;
    public Health Health { get; private set; } = null!;
    public CharacterPresentation? Presentation { get; set; }
    public bool Alive => IsInsideTree() && !IsQueuedForDeletion() && CombatEnabled && Health.IsAlive;
    public Vector3 AimPosition => GlobalPosition + Vector3.Up * AimHeight;
    public string SelectionKey => string.IsNullOrEmpty(TargetId) ? GetPath().ToString() : TargetId;

    public override void _Ready()
    {
        Health = GetNode<Health>("Health");
        AddToGroup(GroupName);
        Health.Resolved += PublishResolved;
    }

    private void PublishResolved(HealthChange change)
    {
        var events = GetTree().GetFirstNodeInGroup(CombatEvents.GroupName) as CombatEvents;
        events?.Publish(new CombatEvent(GetInstanceId(), SelectionKey, Team, AimPosition, change));
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Health)) Health.Resolved -= PublishResolved;
    }

    public bool IsHostileTo(Combatant other) => this != other && Team != CombatTeam.Neutral &&
        other.Team != CombatTeam.Neutral && Team != other.Team;
}
