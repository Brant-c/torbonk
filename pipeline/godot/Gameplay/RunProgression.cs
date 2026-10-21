using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

public enum ProgressionPresentationPhase { Idle, Announcing, Choosing, Confirming }

/// <summary>Owns XP, offers, ranks and run-local stat changes. UI only selects offered resources.</summary>
public partial class RunProgression : Node3D
{
    [Export] public int FirstLevelExperience { get; set; } = 12;
    [Export] public float ExperienceGrowth { get; set; } = 1.3f;
    [Export] public int ExperiencePerEnemy { get; set; } = 4;
    [Export] public float PickupRadius { get; set; } = 0.14f;
    [Export] public float MagnetRadius { get; set; } = 0.45f;
    [Export] public Godot.Collections.Array<UpgradeDefinition> Upgrades { get; set; } = new();
    public GameWorld World { get; set; } = null!;
    public int Level { get; private set; } = 1;
    public long Experience { get; private set; }
    public int RequiredExperience => (int)Math.Clamp(Math.Ceiling(Math.Max(1,FirstLevelExperience) * Math.Pow(Math.Max(1.01f,ExperienceGrowth),Level-1)),1,int.MaxValue);
    public bool Selecting => _offers.Count > 0;
    public bool CanChoose => Selecting && PresentationPhase == ProgressionPresentationPhase.Choosing;
    public ProgressionPresentationPhase PresentationPhase { get; private set; }
    [Export] public float LevelAnnouncementSeconds { get; set; } = 1.75f;
    public int SelectedOfferIndex { get; private set; } = -1;
    public event Action<int>? LevelReached;
    public event Action<UpgradeDefinition>? UpgradeApplied;
    private double _presentationRemaining;
    public IReadOnlyList<UpgradeDefinition> Offers => _offers;
    public IReadOnlyDictionary<string,int> Ranks => _ranks;
    public int PickupCount => _pickups.Count;
    public double ElapsedSeconds { get; private set; }
    public event Action? Changed;
    private readonly List<UpgradeDefinition> _offers = new();
    private readonly Dictionary<string,int> _ranks = new();
    private readonly List<ExperiencePickup> _pickups = new();
    private readonly RandomNumberGenerator _random = new();
    private float _damage, _tick, _range, _cooldown, _health, _speed;
    private readonly Dictionary<string,(float damage,float tick,float range,float cooldown,float radius)> _abilityBases=new();
    private readonly Dictionary<string,(float healing,float shield,float duration,float threshold)> _survivalBases=new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _random.Randomize();
        _damage=World.Combat.DamagePerTick; _tick=World.Combat.TickSeconds;
        _range=World.Targeting.AcquisitionRange; _cooldown=World.Combat.CooldownSeconds;
        _health=World.PlayerActor.Health.Maximum; _speed=World.Player.MoveSpeed;
        foreach(var ability in World.Abilities.Abilities)
        {
            _abilityBases[ability.Definition.Id]=(ability.DamagePerTick,ability.TickSeconds,ability.Range,ability.CooldownSeconds,ability.Radius);
            _survivalBases[ability.Definition.Id]=(ability.Healing,ability.ShieldCapacity,ability.ShieldDuration,ability.HealthThreshold);
        }
        if (Upgrades.Count == 0)
            foreach (string name in new[] { "damage", "tick", "range", "cooldown", "health", "speed", "unlock_shock", "unlock_storm", "shock_damage", "storm_radius",
                "unlock_deathfield", "unlock_barrier", "unlock_darkheal", "deathfield_power", "barrier_power", "darkheal_power" })
                Upgrades.Add(GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{name}.tres"));
        var rarity=GD.Load<RarityTuning>("res://Gameplay/Upgrades/rarity.tres");
        foreach(var upgrade in Upgrades) upgrade.RarityRules??=rarity;
        if (Upgrades.Count < 3 || Upgrades.Select(x=>x.Id).Distinct().Count()!=Upgrades.Count)
            throw new InvalidOperationException("Progression needs at least three unique upgrade IDs");
        World.Spawner!.EnemyReward += Drop;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!GetTree().Paused && World.PlayerActor.Alive) ElapsedSeconds+=delta;
    }

    public override void _Process(double delta)
    {
        if (PresentationPhase is not (ProgressionPresentationPhase.Announcing or ProgressionPresentationPhase.Confirming)) return;
        if (!World.PlayerActor.Alive) { CancelPresentation(); return; }
        _presentationRemaining -= delta;
        if (_presentationRemaining > 0) return;
        if (PresentationPhase == ProgressionPresentationPhase.Announcing)
            PresentationPhase = ProgressionPresentationPhase.Choosing;
        else
        {
            _offers.Clear(); SelectedOfferIndex = -1; PresentationPhase = ProgressionPresentationPhase.Idle;
            GetTree().Paused = false; TryLevel();
        }
        Changed?.Invoke();
    }

    private void Drop(Vector3 point, int reward)
    {
        if (!World.PlayerActor.Alive) return;
        // All XP remains collectible in long runs, with a bounded number of rendered pickups.
        if (_pickups.Count >= 128)
        {
            _pickups.OrderBy(p=>p.GlobalPosition.DistanceSquaredTo(point)).First().Value+=Math.Max(1,reward);
            return;
        }
        var pickup=new ExperiencePickup { Progression=this, Value=Math.Max(1,reward) };
        AddChild(pickup); pickup.GlobalPosition=point+Vector3.Up*0.035f; _pickups.Add(pickup);
    }

    public void Collect(ExperiencePickup pickup)
    {
        if (Selecting || !World.PlayerActor.Alive || !_pickups.Remove(pickup)) return;
        int value=pickup.Value;
        pickup.BeginCollection(World.PlayerActor.GlobalPosition + Vector3.Up * 0.07f);
        World.Audio.PlayCue("pickup"); AddExperience(value);
    }

    public void AddExperience(int amount)
    {
        if (amount<=0 || !World.PlayerActor.Alive) return;
        Experience+=amount;
        if (!Selecting) TryLevel();
        Changed?.Invoke();
    }

    private void TryLevel()
    {
        if (Experience<RequiredExperience) return;
        var eligible=Upgrades.Where(IsEligible).ToList();
        // Custom finite catalogs must retain three eligible choices. Do not trap the game paused.
        if (eligible.Count<3) return;
        Experience-=RequiredExperience; Level++;
        for (int i=0;i<3;i++)
        {
            int index=_random.RandiRange(0,eligible.Count-1);
            _offers.Add(eligible[index]); eligible.RemoveAt(index);
        }
        GetTree().Paused=true;
        PresentationPhase = ProgressionPresentationPhase.Announcing;
        SelectedOfferIndex = -1; _presentationRemaining = Math.Max(0.2, LevelAnnouncementSeconds);
        LevelReached?.Invoke(Level);
    }

    public int Rank(UpgradeDefinition upgrade) => _ranks.GetValueOrDefault(upgrade.Id);

    public bool IsEligible(UpgradeDefinition upgrade)
    {
        if(upgrade.MaximumRank>0 && Rank(upgrade)>=upgrade.MaximumRank) return false;
        if(upgrade.AbilityId=="") return upgrade.Effect!=UpgradeEffect.UnlockAbility;
        var ability=World.Abilities.Find(upgrade.AbilityId);
        return ability!=null && (upgrade.Effect==UpgradeEffect.UnlockAbility?!ability.Unlocked:ability.Unlocked);
    }

    public bool Choose(int index)
    {
        if (!CanChoose || index<0 || index>=_offers.Count || !World.PlayerActor.Alive) return false;
        var upgrade=_offers[index];
        if(!IsEligible(upgrade)) return false;
        if(upgrade.Effect==UpgradeEffect.UnlockAbility) World.Abilities.Find(upgrade.AbilityId)!.SetUnlocked(true);
        _ranks[upgrade.Id]=Rank(upgrade)+1;
        ApplyStats(upgrade.Effect==UpgradeEffect.Health ? upgrade.EffectiveAmount : 0);
        SelectedOfferIndex = index; PresentationPhase = ProgressionPresentationPhase.Confirming;
        _presentationRemaining = upgrade.Rarity switch { PowerRarity.Artifact => 0.65, PowerRarity.Prototype => 0.48, _ => 0.34 };
        UpgradeApplied?.Invoke(upgrade); Changed?.Invoke(); return true;
    }

    private float Bonus(UpgradeEffect effect,string ability="") => Upgrades
        .Where(u=>u.Effect==effect && (u.AbilityId=="" || u.AbilityId==ability)).Sum(u=>Rank(u)*u.EffectiveAmount);
    public int AbilityRanks(string id) => Upgrades.Where(u=>Affects(u,id)).Sum(Rank);
    private static bool Affects(UpgradeDefinition u,string id) => u.Effect is not (UpgradeEffect.Health or UpgradeEffect.Speed or UpgradeEffect.UnlockAbility)
        && (u.AbilityId=="" || u.AbilityId==id) && (u.Effect!=UpgradeEffect.TickRate || id is "lightning" or "storm")
        && (u.Effect!=UpgradeEffect.Radius || id is "storm" or "deathfield")
        && (u.Effect!=UpgradeEffect.Damage || id is not ("barrier" or "darkheal"))
        && (u.Effect!=UpgradeEffect.Range || id!="darkheal");
    public PowerRarity AbilityRarity(string id) => Upgrades.Where(u=>Rank(u)>0 && Affects(u,id))
        .Select(u=>u.Rarity).DefaultIfEmpty(PowerRarity.Premium).Max();
    private void ApplyStats(float heal=0)
    {
        foreach(var ability in World.Abilities.Abilities)
        {
            string id=ability.Definition.Id;var original=_abilityBases[id];
            var survival=_survivalBases[id];
            float Power(Func<UpgradeDefinition,float> select) => Upgrades.Where(u=>u.Effect==UpgradeEffect.AbilityPower && u.AbilityId==id)
                .Sum(u=>Rank(u)*u.RarityMultiplier*select(u));
            ability.DamagePerTick=original.damage*(1+Bonus(UpgradeEffect.Damage,id)+Power(u=>u.DamagePerRank));
            ability.TickSeconds=original.tick/(1+Bonus(UpgradeEffect.TickRate,id));
            ability.Range=original.range*(1+Bonus(UpgradeEffect.Range,id));
            ability.CooldownSeconds=original.cooldown/(1+Bonus(UpgradeEffect.Cooldown,id)+Power(u=>u.CooldownPerRank));
            ability.Radius=original.radius*(1+Bonus(UpgradeEffect.Radius,id)+Power(u=>u.RadiusPerRank));
            ability.Healing=survival.healing*(1+Power(u=>u.HealingPerRank));
            ability.ShieldCapacity=survival.shield*(1+Power(u=>u.ShieldPerRank));
            ability.ShieldDuration=survival.duration*(1+Power(u=>u.DurationPerRank));
            ability.HealthThreshold=Math.Min(0.8f,survival.threshold+Power(u=>u.ThresholdPerRank));
        }
        World.Targeting.AcquisitionRange=World.Combat.Range;
        World.PlayerActor.Health.SetMaximum(_health+Bonus(UpgradeEffect.Health),heal);
        World.Player.MoveSpeed=_speed*(1+Bonus(UpgradeEffect.Speed));
    }

    public void ResetRun()
    {
        CancelPresentation(); _ranks.Clear(); Level=1; Experience=0; ElapsedSeconds=0;
        World.Abilities.ResetUnlocks();
        foreach (var pickup in _pickups) { RemoveChild(pickup); pickup.QueueFree(); }
        _pickups.Clear(); ApplyStats(); Changed?.Invoke();
        foreach (var pickup in GetChildren().OfType<ExperiencePickup>().Where(p => p.Collecting)) pickup.QueueFree();
    }

    public void CancelPresentation()
    {
        GetTree().Paused = false; _offers.Clear(); SelectedOfferIndex = -1;
        PresentationPhase = ProgressionPresentationPhase.Idle; _presentationRemaining = 0;
        Changed?.Invoke();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(World.Spawner)) World.Spawner!.EnemyReward-=Drop;
        GetTree().Paused=false;
    }
}
