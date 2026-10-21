using Godot;
namespace SwtorSurvivors;
public enum AbilityTargetRule { NearestHostile, HostileCluster }
public enum AbilityArea { SingleTarget, GroundSphere, Chain, CasterRadial, TargetSplash }
public enum AbilityCategory { Offensive, Utility }
public enum DamageType { Energy, Kinetic, Internal }
public enum AbilityEffect { Damage, Shield, Heal }
/// <summary>Survivors balance; April source facts are separately recorded in manifests.</summary>
[GlobalClass]
public partial class AbilityDefinition : Resource
{
    [Export] public float PeriodicDamage {get;set;}
    [Export] public float PeriodicDuration {get;set;}
    [Export] public float PeriodicInterval {get;set;}=1;
    [Export] public float SplashMultiplier {get;set;}=.4f;
    [Export] public string Description {get;set;}="";
    [Export] public string ImpactTexture {get;set;}="";
    [Export] public string CastAudio {get;set;}="";
    [Export] public string ImpactAudio {get;set;}="";
    [Export] public bool FullBodyGesture {get;set;}
    [Export] public bool ImpactTailsOnTargetDeath {get;set;}
    [Export] public string WeaponAudio {get;set;}="";
    [Export] public string OffhandAudio {get;set;}="";
    [Export] public string PresentationKind {get;set;}="impact";
    [Export] public string MuzzleBone {get;set;}="RightWeapon";
    [Export] public Color EffectTint {get;set;}=new(1,.28f,.045f);
    [Export] public string Id { get; set; } = "lightning";
    [Export] public AbilityCategory Category { get; set; }
    [Export] public int BounceCount { get; set; } = 3;
    [Export] public float BounceRange { get; set; } = 0.55f;
    [Export] public float BounceFalloff { get; set; } = 0.82f;
    [Export] public int NearbyTriggerCount { get; set; } = 3;
    [Export] public float KnockbackDistance { get; set; } = 0.3f;
    [Export] public float StaggerSeconds { get; set; } = 0.45f; // Knocked-back enemies are slowed and cannot swing for this long.
    [Export] public AbilityEffect Effect { get; set; }
    [Export] public float Healing { get; set; }
    [Export] public float HealingCap { get; set; } = 15;
    [Export] public float HealthThreshold { get; set; } = 0.5f;
    [Export] public float ShieldCapacity { get; set; } = 30;
    [Export] public float ShieldDuration { get; set; } = 12;
    [Export] public string Title { get; set; } = "Force Lightning";
    [Export] public string Icon { get; set; } = "sithsorcerer";
    [Export] public bool StartsUnlocked { get; set; } = true;
    [Export] public bool UsesGlobalCooldown { get; set; } = true;
    [Export] public bool TriggersGlobalCooldown { get; set; } = true;
    // Persistent ground effects validate only on activation, then outlive caster range.
    [Export] public bool PersistentArea { get; set; }
    [Export] public float GestureSeconds { get; set; } = 1.4f;
    [Export] public bool MaintainChannelGesture {get;set;}
    [Export] public int GesturePriority {get;set;}
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
