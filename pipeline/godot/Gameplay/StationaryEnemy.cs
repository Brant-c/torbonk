using Godot;

namespace SwtorSurvivors;

/// <summary>Stationary damage receiver. Replacing its presentation does not change combat rules.</summary>
public partial class StationaryEnemy : Combatant
{
    public override void _Ready()
    {
        base._Ready();
        Presentation = GetNode<CharacterPresentation>("Presentation");
        Health.Damaged += OnDamaged;
        Health.Died += OnDied;
    }

    private void OnDamaged(float amount)
    {
        if (Health.IsAlive) Presentation!.PlayReaction("Hit");
    }

    private void OnDied()
    {
        CombatEnabled = false;
        Presentation!.PlayReaction("Death");
        // Retain the final death pose for inspection; disabled actors cannot be targeted.
    }

    public void ResetEnemy()
    {
        Health.Restore(); CombatEnabled = true; Presentation!.ResetPresentation();
    }
}
