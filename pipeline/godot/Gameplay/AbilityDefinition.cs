using Godot;
namespace SwtorSurvivors;
public enum AbilityTargetRule { NearestHostile, HostileCluster }
public enum AbilityArea { SingleTarget, GroundSphere }
public enum DamageType { Energy, Kinetic, Internal }
/// <summary>Survivors balance; April source facts are separately recorded in manifests.</summary>
[GlobalClass]
public partial class AbilityDefinition : Resource
{
    [Export] public string Id { get; set; } = "lightning";
    [Export] public string Title { get; set; } = "Force Lightning";
    [Export] public string Icon { get; set; } = "sithsorcerer";
    [Export] public bool StartsUnlocked { get; set; } = true;
    [Export] public bool UsesGlobalCooldown { get; set; } = true;
    [Export] public bool TriggersGlobalCooldown { get; set; } = true;
    // Persistent ground effects validate only on activation, then outlive caster range.
    [Export] public bool PersistentArea { get; set; }
    [Export] public float GestureSeconds { get; set; } = 1.4f;
    [Export] public DamageType DamageType { get; set; } = DamageType.Energy;
    [Export] public float Damage { get; set; } = 8;
    [Export] public float CooldownSeconds { get; set; } = 1;
    [Export] public float Range { get; set; } = 0.85f;
    [Export] public float Radius { get; set; } = 0.3f;
    [Export] public AbilityTargetRule TargetRule { get; set; }
    [Export] public AbilityArea Area { get; set; }
    [Export] public float WindupSeconds { get; set; } = 1.066667f;
    [Export] public float ChannelSeconds { get; set; } = 3;
    [Export] public float TickSeconds { get; set; } = 0.5f;
    [Export] public float RecoverySeconds { get; set; } = 0.5f;
    [Export] public string StartClip { get; set; } = "";
    [Export] public string LoopClip { get; set; } = "";
    [Export] public string EndClip { get; set; } = "";
    [Export] public string SourceFx { get; set; } = "";
    [Export] public string CastAudioEvent { get; set; } = "";
    [Export] public string ImpactAudioEvent { get; set; } = "";
    [Export] public string LoopAudioEvent { get; set; } = "";
}
