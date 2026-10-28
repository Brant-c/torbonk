using Godot;
using System;

namespace SwtorSurvivors;

public enum CombatEventKind { Damage, Healing, Absorbed, Resisted, Status }

/// <summary>Amounts are resolved by gameplay, never by the presentation subscriber.</summary>
public readonly record struct HealthChange(CombatEventKind Kind, float Amount,
    DamageType DamageType = DamageType.Energy, bool IsCritical = false, string? Text = null, string AbilityId = "");

/// <summary>A value snapshot: queued presentation never owns or retains the recipient node.</summary>
public readonly record struct CombatEvent(ulong RecipientId, string RecipientKey, CombatTeam RecipientTeam,
    Vector3 WorldPosition, HealthChange Change);

/// <summary>Scene-local event stream. Future shields/status producers can publish resolved events here.</summary>
public partial class CombatEvents : Node
{
    public const string GroupName = "combat_event_stream";
    public event Action<CombatEvent>? Resolved;
    public override void _EnterTree() => AddToGroup(GroupName);
    public void Publish(CombatEvent combatEvent) => Resolved?.Invoke(combatEvent);
}

