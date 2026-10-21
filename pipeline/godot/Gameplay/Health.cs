using Godot;
using System;

namespace SwtorSurvivors;

/// <summary>Gameplay health. Effects and animations cannot deal damage.</summary>
public partial class Health : Node
{
    [Export] public float Maximum { get; set; } = 100;
    public float Current { get; private set; }
    public bool IsAlive => Current > 0;
    public int DamageEvents { get; private set; }
    public DamageType LastDamageType { get; private set; } = DamageType.Energy;
    public float ShieldRemaining { get; private set; }
    public float ShieldCapacity { get; private set; }
    public double ShieldSeconds { get; private set; }
    public bool Shielded => ShieldRemaining > 0 && ShieldSeconds > 0;
    public event Action<bool>? ShieldEnded;
    public event Action<float>? Damaged;
    public event Action<HealthChange>? Resolved;
    public event Action? Died;

    public override void _Ready() => Restore();
    public override void _PhysicsProcess(double delta)
    {
        if (!Shielded) return;
        ShieldSeconds = Math.Max(0, ShieldSeconds - delta);
        if (ShieldSeconds <= 0) ClearShield();
    }

    public bool ApplyShield(float capacity, float seconds)
    {
        if (!IsAlive || Shielded || !float.IsFinite(capacity) || capacity <= 0 || !float.IsFinite(seconds) || seconds <= 0) return false;
        ShieldCapacity = ShieldRemaining = capacity; ShieldSeconds = seconds; return true;
    }
    public void ClearShield(bool depleted = false)
    {
        bool existed = ShieldRemaining > 0 || ShieldCapacity > 0;
        ShieldRemaining = ShieldCapacity = 0; ShieldSeconds = 0;
        if (existed) ShieldEnded?.Invoke(depleted);
    }

    public float ApplyDamage(float amount, DamageType damageType = DamageType.Energy, bool isCritical = false)
    {
        if (!IsAlive || !float.IsFinite(amount) || amount <= 0) return 0;
        float absorbed = Shielded ? Math.Min(ShieldRemaining, amount) : 0;
        if (absorbed > 0)
        {
            ShieldRemaining -= absorbed; amount -= absorbed;
            Resolved?.Invoke(new HealthChange(CombatEventKind.Absorbed, absorbed, damageType));
            if (ShieldRemaining <= 0) ClearShield(true);
        }
        if (amount <= 0) return 0;
        float applied = Math.Min(Current, amount);
        LastDamageType = damageType;
        Current -= applied;
        DamageEvents++;
        // Publish before reactions/death can despawn the recipient. Keep the legacy event intact.
        Resolved?.Invoke(new HealthChange(CombatEventKind.Damage, applied, damageType, isCritical));
        Damaged?.Invoke(applied);
        if (!IsAlive) { ClearShield(); Died?.Invoke(); }
        return applied;
    }

    public void Restore()
    {
        ClearShield();
        Current = Math.Max(1, float.IsFinite(Maximum) ? Maximum : 100);
        DamageEvents = 0;
    }

    public void SetMaximum(float maximum, float heal = 0)
    {
        Maximum = Math.Max(1, maximum);
        // Upgrades never revive a dead actor.
        if (IsAlive)
        {
            Current = Math.Min(Current, Maximum);
            ApplyHealing(Math.Max(0, heal));
        }
    }

    public float ApplyHealing(float amount, bool isCritical = false)
    {
        if (!IsAlive || !float.IsFinite(amount) || amount <= 0) return 0;
        float applied = Math.Min(Math.Max(0, Maximum - Current), amount);
        if (applied <= 0) return 0;
        Current += applied;
        Resolved?.Invoke(new HealthChange(CombatEventKind.Healing, applied, IsCritical: isCritical));
        return applied;
    }
}
