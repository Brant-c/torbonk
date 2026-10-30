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
    [Export] public int MaximumLevel { get; set; }
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
    public int BanishesRemaining { get; private set; } = 3;
    public int RerollsRemaining { get; private set; } = 3;
    public int SkipsRemaining { get; private set; } = 2;
    public int OfferRevision { get; private set; }
    internal void SeedOffers(ulong seed)=>_random.Seed=seed;
    private int _lastAbilityLevel=1;
    private int _offerLevel;
    private bool _abilityOffer;
    public float AbilityOfferChance(int level)=>Math.Min(.65f,.18f+.095f*Math.Max(0,level-_lastAbilityLevel-1));
    public bool AbilityChoice { get; private set; }
    public int SkipExperience => Math.Max(1,RequiredExperience/10);
    private readonly HashSet<string> _banished = new();
    private readonly HashSet<string> _banishedAbilities = new();
    public bool SpecChoice=>_offers.Any(u=>u.MilestoneLevel>0);
    private UpgradeDefinition? _replacement;
    public bool Replacing=>_replacement!=null;
    public string ReplacementTitle=>_replacement?.Title??"";
    public IReadOnlyList<AutomaticAbility> ReplacementOptions=>_replacement==null?Array.Empty<AutomaticAbility>():World.Abilities.Abilities
        .Where(a=>CanReplace(a) && a.Definition.Category==World.Abilities.Find(_replacement.AbilityId)!.Definition.Category).ToArray();
    public IReadOnlyDictionary<string,int> Ranks => _ranks;
    public int PickupCount => _pickups.Count;
    public double ElapsedSeconds { get; private set; }
    public event Action? Changed;
    private readonly List<UpgradeDefinition> _offers = new();
    private readonly Dictionary<string,int> _ranks = new();
    private readonly HashSet<string> _talentAdmissions=new();
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
        {
            foreach (string name in new[] { "damage", "tick", "range", "cooldown", "health", "speed", "unlock_shock", "unlock_storm", "shock_damage", "storm_radius",
                "unlock_thunderingblast", "unlock_affliction", "unlock_lightning", "unlock_creepingterror", "unlock_deathfield", "unlock_darkheal" })
                Upgrades.Add(GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{name}.tres"));
            foreach(string name in new[]{"unlock_chain","unlock_overload","chain_reach","fork","storm_duration","storm_slow","haste","bloodlust"})
                Upgrades.Add(GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{name}.tres"));
        }
        if(World.RunConfiguration.Profile.TalentPool.Length>0)
        {
            Upgrades.Clear();
            foreach(string id in World.RunConfiguration.Profile.TalentPool)
            {
                var u=(UpgradeDefinition)GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{id}.tres").Duplicate();
                if(u.Effect is UpgradeEffect.Range or UpgradeEffect.Cooldown)u.AbilityId=World.RunConfiguration.AdvancedClassId=="marauder"?"":World.Combat.Definition.Id;
                // Class kits share one damage track so unlocked abilities stay relevant.
                if(u.Effect==UpgradeEffect.Damage && World.RunConfiguration.Profile.Kit.Length>0)u.AbilityId="";
                else if(u.Effect==UpgradeEffect.Damage)u.AbilityId=World.Combat.Definition.Id;
                if(u.Effect==UpgradeEffect.Damage)u.Title="Combat Training";
                if(u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown or UpgradeEffect.Radius)u.Icon=World.Combat.Definition.Icon;
                Upgrades.Add(u);
            }
        }
        if(World.RunConfiguration.AdvancedClassId=="sorcerer")
        {
            foreach(string id in new[]{"unlock_lightningstrike","unlock_crushingdarkness","unlock_forcespeed","shock_cleave","strike_cleave","deathfield_cooldown"})
                Upgrades.Add(GD.Load<UpgradeDefinition>($"res://Gameplay/Upgrades/{id}.tres"));
            // Scoped single-axis cards replace the flat channel/range/recovery cards. Flat damage and speed stay but weaker, and nothing is rank-capped.
            foreach(var flat in Upgrades.Where(u=>u.Id is "tick" or "range" or "cooldown" or "damage" or "speed").ToArray())
            {
                Upgrades.Remove(flat);
                if(flat.Id is not ("damage" or "speed"))continue;
                var tuned=(UpgradeDefinition)flat.Duplicate();tuned.AmountPerRank=flat.Id=="damage"?.1f:.02f;tuned.MaximumRank=0;Upgrades.Add(tuned);
            }
            Upgrades.AddRange(World.Build.Pregame.SorcererRunPowers());Upgrades.AddRange(World.Build.Pregame.SorcererMechanics());Upgrades.AddRange(World.Build.Pregame.SorcererDrops());
        }
        if(World.RunConfiguration.AdvancedClassId=="assassin") {
            foreach(var old in Upgrades.Where(u=>u.Effect==UpgradeEffect.Damage || u.Id is "deathfield_power" or "d3_area_lacerate").ToArray())Upgrades.Remove(old);
            foreach(var utility in Upgrades.Where(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed or UpgradeEffect.Cooldown))utility.MaximumRank=2;
            Upgrades.AddRange(World.Build.Pregame.AssassinRunPowers());
            Upgrades.Add(new UpgradeDefinition {Id="unlock_lacerate",Title="Lacerate",Icon="spinningstrike",AbilityId="lacerate",Effect=UpgradeEffect.UnlockAbility,MaximumRank=1});
            Upgrades.Add(new UpgradeDefinition {Id="unlock_electrocute",Title="Electrocute",Icon="electrocute",AbilityId="electrocute",Effect=UpgradeEffect.UnlockAbility,MaximumRank=1});
        }
        if(World.RunConfiguration.AdvancedClassId=="sniper") {
            foreach(var old in Upgrades.Where(u=>u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown).ToArray())Upgrades.Remove(old);
            foreach(var utility in Upgrades.Where(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed))utility.MaximumRank=2;
            Upgrades.AddRange(World.Build.Pregame.SniperRunPowers());
            Upgrades.AddRange(World.Build.Pregame.SniperExtraMechanics());
        }
        if(World.RunConfiguration.AdvancedClassId=="operative") {
            foreach(var old in Upgrades.Where(u=>u.Effect==UpgradeEffect.Damage).ToArray())Upgrades.Remove(old);
            Upgrades.AddRange(World.Build.Pregame.OperativeRunPowers());
        }
        if(World.RunConfiguration.AdvancedClassId is "powertech" or "mercenary") {
            bool powertech=World.RunConfiguration.AdvancedClassId=="powertech";
            // Scoped power, reach and mechanic cards replace the generic damage/range/cooldown/health/speed spam.
            foreach(var old in Upgrades.Where(u=>u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown or UpgradeEffect.Health or UpgradeEffect.Speed || u.Id=="d3_tracer").ToArray())Upgrades.Remove(old);
            foreach(string id in World.RunConfiguration.Profile.Kit)
                if(!Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==id) && World.Abilities.Find(id) is {} kit)
                    Upgrades.Add(new UpgradeDefinition {Id="unlock_"+id,Title=kit.Definition.Title,Icon=kit.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.UnlockAbility,MaximumRank=1});
            Upgrades.AddRange(powertech?World.Build.Pregame.PowertechRunPowers():World.Build.Pregame.MercenaryRunPowers());
            Upgrades.AddRange(powertech?World.Build.Pregame.PowertechDrops():World.Build.Pregame.MercenaryDrops());
            Upgrades.Add(new UpgradeDefinition {Id=World.RunConfiguration.AdvancedClassId+"_plating",Title="Reinforced Plating",Icon=powertech?"heavyarmors":"armortech",
                Effect=UpgradeEffect.Health,AmountPerRank=8,MaximumRank=3});
        }
        if(World.RunConfiguration.AdvancedClassId=="marauder")
        {
            foreach(string id in new[]{"viciousslash","rupture","deadlysaber","annihilate","ravage","sweepingslash","saberthrow","deadlythrow","forcecrush","smash","batteringassault","massacre","gore","forcescream","intimidatingroar","cripplingslash","retaliation","viciousthrow","forcecharge","forcechoke"})
            {
                var ability=World.Abilities.Find(id);if(ability==null)continue;
                Upgrades.Add(new UpgradeDefinition {Id=$"marauder_power_{id}",Title=$"{ability.Definition.Title} Mastery",
                    Icon=ability.Definition.Icon,AbilityId=id,Effect=UpgradeEffect.AbilityPower,DamagePerRank=.7f,MaximumRank=0});
            }
            var carnageDrops=World.Build.Pregame.MarauderCarnageDrops().ToArray();
            foreach(var old in Upgrades.Where(u=>carnageDrops.Any(c=>c.Mechanic==u.Mechanic)).ToArray())Upgrades.Remove(old);
            Upgrades.AddRange(carnageDrops);
            Upgrades.Add(new UpgradeDefinition {Id="marauder_blademaster",Title="Blademaster's Reach",Icon="mr_talent_bladerush",Effect=UpgradeEffect.Mechanic,Mechanic="mr_blademaster",Spec="carnage",MaximumRank=3,MechanicalDescription="Massacre and Ravage gain one cleave target and 8% radius per rank, up to three ranks. Secondary hits retain full damage."});
            // Flat all-ability damage is replaced by the scoped power cards above.
            foreach(var old in Upgrades.Where(u=>u.Effect==UpgradeEffect.Damage).ToArray())Upgrades.Remove(old);
            foreach(var recovery in Upgrades.Where(u=>u.Effect==UpgradeEffect.Cooldown))recovery.MaximumRank=2;
        }
        if(World.RunConfiguration.AdvancedClassId=="juggernaut") {
            var drops=World.Build.Pregame.JuggernautDrops().ToArray();
            foreach(var old in Upgrades.Where(u=>drops.Any(d=>d.Mechanic==u.Mechanic)).ToArray())Upgrades.Remove(old);
            Upgrades.AddRange(drops);
            foreach(var old in Upgrades.Where(u=>u.Effect==UpgradeEffect.Damage || u.Id=="d3_smash").ToArray())Upgrades.Remove(old);
            Upgrades.AddRange(World.Build.Pregame.JuggernautRunPowers());
        }
        if(World.RunConfiguration.AdvancedClassId=="operative" && !Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId=="blowforblow"))
            Upgrades.Add(new UpgradeDefinition {Id="unlock_blowforblow",Title="Blow for Blow",Icon="shieldprobe",AbilityId="blowforblow",Effect=UpgradeEffect.UnlockAbility,MaximumRank=1});
        foreach(var milestone in SpecMilestones.Catalog(World))Upgrades.Add(milestone);
        foreach(var training in World.Build.Pregame.RunUpgrades())Upgrades.Add(training);
        if(World.RunConfiguration.AdvancedClassId=="sorcerer")foreach(var training in Upgrades.Where(u=>u.Id.StartsWith("talent_training_",StringComparison.Ordinal)))training.MaximumRank=0;
        var rarity=GD.Load<RarityTuning>("res://Gameplay/Upgrades/rarity.tres");
        foreach(var upgrade in Upgrades) {upgrade.RarityRules??=rarity;upgrade.AbilityTitle=World.Abilities.Find(upgrade.AbilityId)?.Definition.Title??"";}
        if (Upgrades.Count < 3 || Upgrades.Select(x=>x.Id).Distinct().Count()!=Upgrades.Count)
            throw new InvalidOperationException("Progression needs at least three unique upgrade IDs");
        World.Spawner!.EnemyReward += Drop;
        ApplyStats();
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
        if (!World.PlayerActor.Alive || MaximumLevel > 0 && Level >= MaximumLevel) return;
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
        if (amount<=0 || !World.PlayerActor.Alive || MaximumLevel > 0 && Level >= MaximumLevel) return;
        Experience+=amount;
        if (!Selecting) TryLevel();
        Changed?.Invoke();
    }

    private void TryLevel()
    {
        if(MaximumLevel>0 && Level>=MaximumLevel)return;
        if(World.Artifacts?.Pending==true)return;
        if (Experience<RequiredExperience) return;
        int nextLevel=Level+1;
        var choices=GenerateOffers(nextLevel,Array.Empty<UpgradeDefinition>());
        if(choices.Count==0)return;
        Experience-=RequiredExperience;Level++;
        if(MaximumLevel > 0 && Level >= MaximumLevel) {
            Experience=0;
            foreach(var pickup in _pickups)pickup.QueueFree();
            _pickups.Clear();
        }
        _offers.AddRange(choices);OfferRevision++;
        GetTree().Paused=true;
        PresentationPhase = ProgressionPresentationPhase.Announcing;
        SelectedOfferIndex = -1; _presentationRemaining = Math.Max(0.2, LevelAnnouncementSeconds);
        LevelReached?.Invoke(Level);
    }

    private List<UpgradeDefinition> GenerateOffers(int level,IEnumerable<UpgradeDefinition> excluded,int count=3)
    {
        foreach(var u in Upgrades.Where(u=>u.MilestoneLevel==level))
            if(World.Abilities.Find(u.AbilityId) is { } ability)SpecMilestones.Prepare(u,ability);
        var excludedIds=excluded.Select(u=>u.Id).ToHashSet();
        var excludedAbilities=excluded.Where(u=>u.Effect==UpgradeEffect.UnlockAbility).Select(u=>u.AbilityId).ToHashSet();
        var eligible=Upgrades.Where(u=>(u.MilestoneLevel==0 && !SpecMilestones.Reserved(World,u) || u.MilestoneLevel==level) && IsEligible(u)).ToList();
        eligible.RemoveAll(u=>excludedIds.Contains(u.Id) || u.Effect==UpgradeEffect.UnlockAbility && excludedAbilities.Contains(u.AbilityId));
        // Survival remains a periodic option; the growing owned kit supplies the normal choices.
        if(World.RunConfiguration.AdvancedClassId is "assassin" or "sniper" or "powertech" or "mercenary" && level%4!=0)
            eligible.RemoveAll(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed);
        if(World.RunConfiguration.AdvancedClassId is "operative" or "sniper" && level<7)eligible.RemoveAll(u=>u.Effect==UpgradeEffect.UnlockAbility && World.Abilities.Find(u.AbilityId)?.Definition.Category==AbilityCategory.Utility);
        var abilities=eligible.Where(u=>u.Effect==UpgradeEffect.UnlockAbility).GroupBy(u=>u.AbilityId).Select(g=>g.First()).ToList();
        // The level roll is stable across rerolls/banishes; pity tracks actual acquisition.
        if(_offerLevel!=level){_offerLevel=level;_abilityOffer=_random.Randf()<AbilityOfferChance(level);}
        if(count==1 && excluded.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && !_banished.Contains(u.Id)) &&
            !(World.RunConfiguration.AdvancedClassId=="sniper" && !eligible.Any(u=>u.Effect!=UpgradeEffect.UnlockAbility)))abilities.Clear();
        AbilityChoice=false;
        eligible.RemoveAll(u=>u.Effect==UpgradeEffect.UnlockAbility);
        UpgradeDefinition? offeredAbility=null;
        if(abilities.Count>0 && (_abilityOffer || eligible.Count==0 || World.RunConfiguration.AdvancedClassId is "assassin" or "sniper" && eligible.Count<count))
        {
            float roll=_random.Randf()*abilities.Sum(a=>World.Build.Pregame.OfferWeight(a));
            offeredAbility=abilities.Last();
            foreach(var a in abilities){roll-=World.Build.Pregame.OfferWeight(a);if(roll<=0){offeredAbility=a;break;}}
        }
        var result=new List<UpgradeDefinition>();
        if(World.RunConfiguration.AdvancedClassId=="assassin" && level<=6 && count>1) {
            foreach(string id in World.Build.Pregame.EarlyAbilityOrder) {
                var core=abilities.FirstOrDefault(a=>a.AbilityId==id);
                if(core==null)continue;
                result.Add(core);if(result.Count==2)break;
            }
        }
        if(World.RunConfiguration.AdvancedClassId=="marauder" && level<=7 &&
            (count>1 || !excluded.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && !_banished.Contains(u.Id)))) {
            foreach(string id in World.Build.Pregame.MarauderEarlyAbilities) {
                var core=abilities.FirstOrDefault(a=>a.AbilityId==id);
                if(core==null)continue;
                result.Add(core);if(result.Count==Math.Min(3,count))break;
            }
        }
        // Force Storm anchors the Sorcerer's early kit, so it is guaranteed once in the opening levels.
        if(World.RunConfiguration.AdvancedClassId=="sorcerer" && level<=5 && count>1 && abilities.FirstOrDefault(a=>a.AbilityId=="storm") is {} storm && !result.Contains(storm))
            result.Add(storm);
        // Bounty Hunters get one tree-weighted core unlock early, then the normal class-power ordering.
        if(World.RunConfiguration.AdvancedClassId is "powertech" or "mercenary" && level<=5 && count>1)
            foreach(string id in World.Build.Pregame.BountyEarlyAbilities)
                if(abilities.FirstOrDefault(a=>a.AbilityId==id) is {} core){result.Add(core);break;}
        // Juggernaut needs dependable group damage before random utility unlocks.
        // Work from eligible candidates so banish, ownership and replacement rules still apply.
        if(World.RunConfiguration.AdvancedClassId=="juggernaut" && level<=6 &&
            (count>1 || !excluded.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && !_banished.Contains(u.Id)))) {
            foreach(string id in World.Build.Pregame.JuggernautEarlyAbilities) {
                var core=abilities.FirstOrDefault(a=>a.AbilityId==id);
                if(core==null)continue;
                result.Add(core);if(result.Count==Math.Min(level<=3?2:1,count))break;
            }
        }
        if(World.RunConfiguration.AdvancedClassId is "operative" or "sniper" && level<=6 && count>1) {
            int coreCount=Math.Min(level<=3?2:1,count);
            var early=World.Build.Pregame.AgentEarlyAbilities;
            // A blank talent profile still gets an opening kit without being forced into Marksmanship.
            if(early.Length==0)early=abilities.Where(u=>World.Abilities.Find(u.AbilityId)?.Definition.Category==AbilityCategory.Offensive).OrderBy(_=>_random.Randf()).Select(u=>u.AbilityId).ToArray();
            foreach(string id in early) {
                var core=abilities.FirstOrDefault(a=>a.AbilityId==id);
                if(core!=null && !result.Contains(core))result.Add(core);
                if(result.Count>=coreCount)break;
            }
        }
        if(result.Count==0 && offeredAbility!=null)result.Add(offeredAbility);
        if(World.RunConfiguration.AdvancedClassId=="juggernaut" && level<=6 && count>1 && result.Count<count) {
            while(result.Count<count) {
                var talents=eligible.Where(u=>u.Mechanic.StartsWith("jg_",StringComparison.Ordinal)).ToArray();
                if(talents.Length==0)break;
                int lowest=talents.Min(u=>Rank(u));var fresh=talents.Where(u=>Rank(u)==lowest).ToArray();
                var talent=fresh[_random.RandiRange(0,fresh.Length-1)];
                result.Add(talent);eligible.RemoveAll(u=>u.Id==talent.Id);
            }
        }
        while(result.Count<count && eligible.Count>0)
        {
            // Choose class scope before rarity, reserving two slots and one damage upgrade.
            bool ClassUpgrade(UpgradeDefinition u)=>u.Id.StartsWith("marauder_",StringComparison.Ordinal) || u.Mechanic.StartsWith("mr_",StringComparison.Ordinal);
            var pool=eligible.ToArray();
            if(World.RunConfiguration.AdvancedClassId is "assassin" or "sniper" or "sorcerer" or "operative" or "powertech" or "mercenary") {
                bool ClassPower(UpgradeDefinition u)=>u.Id.StartsWith("assassin_",StringComparison.Ordinal) || u.Id.StartsWith("operative_",StringComparison.Ordinal) || u.Id.StartsWith("sniper_",StringComparison.Ordinal) || u.Id.StartsWith("sorcerer_",StringComparison.Ordinal)
                    || u.Id.StartsWith("powertech_",StringComparison.Ordinal) || u.Id.StartsWith("mercenary_",StringComparison.Ordinal);
                bool Interaction(UpgradeDefinition u)=>u.Mechanic.StartsWith("as_",StringComparison.Ordinal) || u.Mechanic.StartsWith("op_",StringComparison.Ordinal) || u.Mechanic.StartsWith("sn_",StringComparison.Ordinal) || u.Mechanic.StartsWith("so_",StringComparison.Ordinal)
                    || u.Effect==UpgradeEffect.Mechanic && (u.Mechanic.StartsWith("pt_",StringComparison.Ordinal) || u.Mechanic.StartsWith("mc_",StringComparison.Ordinal));
                var preferred=pool.Where(ClassPower).ToArray();
                var interactions=preferred.Where(Interaction).ToArray();
                bool Power(UpgradeDefinition u)=>u.Effect==UpgradeEffect.AbilityPower && (World.RunConfiguration.AdvancedClassId!="sorcerer" || u.DamagePerRank>0);
                var damage=preferred.Where(Power).ToArray();
                if(interactions.Length>0 && !result.Any(Interaction))pool=interactions;
                else if(damage.Length>0 && !result.Any(Power))pool=damage;
                else if(preferred.Length>0 && result.Count(ClassPower)<Math.Min(2,count))pool=preferred;
                // Prefer a different equipped ability on each card when the kit permits it.
                var distinct=pool.Where(u=>!result.Any(r=>r.AbilityId==u.AbilityId)).ToArray();
                if(distinct.Length>0)pool=distinct;
            }
            if(World.RunConfiguration.AdvancedClassId=="marauder")
            {
                var preferred=pool.Where(ClassUpgrade).ToArray();
                var damage=preferred.Where(u=>u.Effect==UpgradeEffect.AbilityPower && u.DamagePerRank>0).ToArray();
                var interactions=preferred.Where(u=>u.Spec=="carnage" && u.Effect==UpgradeEffect.Mechanic).ToArray();
                if(World.Build.Pregame.MarauderCarnagePreferred && interactions.Length>0 && !result.Any(u=>u.Spec=="carnage" && u.Effect==UpgradeEffect.Mechanic))pool=interactions;
                else if(damage.Length>0 && !result.Any(u=>u.Effect==UpgradeEffect.AbilityPower && u.DamagePerRank>0))pool=damage;
                else if(preferred.Length>0 && result.Count(ClassUpgrade)<Math.Min(2,count))pool=preferred;
            }
            var choices=pool;
            float Weight(UpgradeDefinition u)=>(u.Effect==UpgradeEffect.UnlockAbility && World.RunConfiguration.AdvancedClassId=="sorcerer"?6:u.Effect is UpgradeEffect.Mechanic or UpgradeEffect.UnlockAbility or UpgradeEffect.AbilityPower?3:1)*World.Build.Pregame.OfferWeight(u)
                /(World.RunConfiguration.AdvancedClassId is "assassin" or "sniper" or "powertech" or "mercenary"?1+.75f*Rank(u):1);
            float roll=_random.Randf()*choices.Sum(Weight);var chosen=choices.Last();
            foreach(var choice in choices){roll-=Weight(choice);if(roll<=0){chosen=choice;break;}}
            result.Add(chosen);
            eligible.RemoveAll(u=>u.Id==chosen.Id || chosen.Effect==UpgradeEffect.UnlockAbility && u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==chosen.AbilityId);
            if(World.RunConfiguration.AdvancedClassId is "assassin" or "sniper" && chosen.Effect is UpgradeEffect.Health or UpgradeEffect.Speed)
                eligible.RemoveAll(u=>u.Effect is UpgradeEffect.Health or UpgradeEffect.Speed);
        }
        foreach(var rolled in result.Where(Scalable))rolled.Rarity=GD.Load<RarityTuning>("res://Gameplay/Upgrades/rarity.tres").Roll(_random.Randf());
        if(World.RunConfiguration.AdvancedClassId is "assassin" or "sniper")
            while(result.Count<count) {
                var fallback=abilities.Where(u=>!result.Any(r=>r.AbilityId==u.AbilityId)).ToArray();
                if(fallback.Length==0)break;
                result.Add(fallback[_random.RandiRange(0,fallback.Length-1)]);
            }
        return result;
    }

    public bool Banish(int index)
    {
        if(!CanChoose || Replacing || BanishesRemaining<=0 || !World.PlayerActor.Alive || index<0 || index>=_offers.Count)return false;
        bool abilityChoice=AbilityChoice;
        var option=_offers[index];_banished.Add(option.Id);
        bool banAbility=option.Effect==UpgradeEffect.UnlockAbility && World.Abilities.Find(option.AbilityId)?.Unlocked!=true;
        if(banAbility)_banishedAbilities.Add(option.AbilityId);
        var refill=GenerateOffers(Level,_offers,1);
        if(refill.Count==0)
        {
            _banished.Remove(option.Id);if(banAbility)_banishedAbilities.Remove(option.AbilityId);AbilityChoice=abilityChoice;return false;
        }
        _offers.RemoveAt(index);_offers.InsertRange(index,refill);
        BanishesRemaining--;OfferRevision++;Changed?.Invoke();return true;
    }

    public bool Reroll()
    {
        if(!CanChoose || Replacing || RerollsRemaining<=0 || !World.PlayerActor.Alive)return false;
        var excluded=World.RunConfiguration.AdvancedClassId=="juggernaut" && Level<=6
            ?_offers.Where(u=>!World.Build.Pregame.JuggernautEarlyAbilities.Contains(u.AbilityId)):_offers;
        var choices=GenerateOffers(Level,excluded);
        if(choices.Count<_offers.Count)return false;
        _offers.Clear();_offers.AddRange(choices);RerollsRemaining--;OfferRevision++;Changed?.Invoke();return true;
    }

    public bool Skip()
    {
        if(!CanChoose || Replacing || SkipsRemaining<=0 || !World.PlayerActor.Alive)return false;
        SkipsRemaining--;Experience+=SkipExperience;
        _offers.Clear();SelectedOfferIndex=-1;PresentationPhase=ProgressionPresentationPhase.Idle;
        GetTree().Paused=false;TryLevel();Changed?.Invoke();return true;
    }
    /// <summary>Applies a rank directly (smoke tests and debug); same eligibility and effects as a level-up choice.</summary>
    public bool Grant(string id)
    {
        var upgrade=Upgrades.FirstOrDefault(u=>u.Id==id);
        if(upgrade==null || !IsEligible(upgrade))return false;
        if(upgrade.Effect==UpgradeEffect.UnlockAbility && !World.Abilities.Unlock(upgrade.AbilityId))return false;
        if(upgrade.Effect==UpgradeEffect.UnlockAbility)_lastAbilityLevel=Level;
        _ranks[upgrade.Id]=Rank(upgrade)+1;ApplyStats();Changed?.Invoke();return true;
    }
    public int Rank(UpgradeDefinition upgrade) => _ranks.GetValueOrDefault(upgrade.Id);
    // Repeatable numeric rewards roll a tier on each offer; each taken rank keeps the strength it was taken at.
    private static bool Scalable(UpgradeDefinition u)=>u.MilestoneLevel==0 && u.Effect is not (UpgradeEffect.Mechanic or UpgradeEffect.UnlockAbility) && u.MaximumRank!=1;
    private readonly Dictionary<string,float> _rankPower=new();
    private float Pow(UpgradeDefinition u)=>_rankPower.TryGetValue(u.Id,out var p)?p:Rank(u)*(u.MilestoneLevel>0?1:u.RarityMultiplier);

    public bool IsEligible(UpgradeDefinition upgrade)
    {
        if(World.Build.Pregame.AgentConcealment && upgrade.AbilityId=="carbineburst")return false;
        if(upgrade.AbilityId=="electrocute" && !World.Build.Pregame.AssassinDeception)return false;
        if(upgrade.AbilityId=="blowforblow" && !World.Build.Pregame.AgentCounterstrikeReady)return false;
        if(World.RunConfiguration.AdvancedClassId=="assassin" && !World.Build.Pregame.AssassinUpgradeUsable(upgrade))return false;
        if(World.RunConfiguration.AdvancedClassId=="sniper" && !World.Build.Pregame.SniperUpgradeUsable(upgrade))return false;
        if(World.RunConfiguration.AdvancedClassId=="powertech" && !World.Build.Pregame.PowertechUpgradeUsable(upgrade))return false;
        if(World.RunConfiguration.AdvancedClassId=="mercenary" && !World.Build.Pregame.MercenaryUpgradeUsable(upgrade))return false;
        if(upgrade.Mechanic.StartsWith("so_",StringComparison.Ordinal) && (!World.Build.Pregame.SorcererUpgradeUsable(upgrade) || World.Build.Pregame.SorcererTalentRank(upgrade.Mechanic)>0))return false;
        if(World.RunConfiguration.AdvancedClassId=="sorcerer" && upgrade.Id.StartsWith("sorcerer_",StringComparison.Ordinal) && !World.Build.Pregame.SorcererUpgradeUsable(upgrade))return false;
        if(World.Build.Pregame.AgentVenomousBlades && upgrade.AbilityId=="dart")return false;
        if(_banished.Contains(upgrade.Id) || _banishedAbilities.Contains(upgrade.AbilityId))return false;
        if(upgrade.Effect==UpgradeEffect.TickRate && !World.Abilities.Abilities.Any(a=>a.Unlocked && (a.ChannelSeconds>0 || a.Definition.PeriodicDuration>0) && (upgrade.AbilityId=="" || upgrade.AbilityId==a.Definition.Id)))return false;
        if(World.RunConfiguration.AdvancedClassId=="marauder" && upgrade.Mechanic.StartsWith("mr_",StringComparison.Ordinal) && upgrade.Mechanic!="mr_blademaster" && World.Build.Pregame.MarauderTalentRank(upgrade.Mechanic)+Rank(upgrade)>=1)return false;
        if(World.RunConfiguration.AdvancedClassId=="marauder" && !World.Build.Pregame.MarauderUpgradeUsable(upgrade.Mechanic))return false;
        if(World.RunConfiguration.AdvancedClassId=="juggernaut" && !World.Build.Pregame.JuggernautUpgradeUsable(upgrade))return false;
        if(upgrade.Mechanic=="jg_convergence" && World.Abilities.Find("forcecrush")?.Unlocked!=true)return false;
        if(upgrade.Mechanic=="haste" && World.Abilities.Equipped(AbilityCategory.Utility)==0)return false;
        if(upgrade.RequiresUpgrade!="" && _ranks.GetValueOrDefault(upgrade.RequiresUpgrade)==0)return false;
        if(World.RunConfiguration.AdvancedClassId=="juggernaut" && upgrade.Mechanic.StartsWith("jg_",StringComparison.Ordinal) && upgrade.MaximumRank>0 &&
            Rank(upgrade)+World.Build.Pregame.JuggernautTalentRank(upgrade.Mechanic)>=upgrade.MaximumRank)return false;
        if(upgrade.MaximumRank>0 && Rank(upgrade)>=upgrade.MaximumRank) return false;
        if(upgrade.Mechanic=="polarity")return !World.Build.Pregame.PolarityReady && World.Abilities.Find(upgrade.AbilityId)!=null;
        if(upgrade.AbilityId=="") return upgrade.Effect!=UpgradeEffect.UnlockAbility;
        var ability=World.Abilities.Find(upgrade.AbilityId);
        return ability!=null && (upgrade.Effect==UpgradeEffect.UnlockAbility?!ability.Unlocked && !World.Abilities.Conflicts(ability) && (World.Abilities.CanUnlock(ability) || ReplacementCandidates(ability)):ability.Unlocked);
    }

    private static bool CanReplace(AutomaticAbility ability)=>ability.Unlocked && !ability.Definition.IsBasicAttack && (!ability.Definition.Slotless || ability.Definition.ReplaceableStarter || ability.Definition.Id is "affliction" or "lacerate" or "viciousslash");
    private bool ReplacementCandidates(AutomaticAbility ability)=>World.Abilities.Abilities.Any(a=>CanReplace(a) && a.Definition.Category==ability.Definition.Category);

    public bool Choose(int index)
    {
        if (!CanChoose || Replacing || index<0 || index>=_offers.Count || !World.PlayerActor.Alive) return false;
        var upgrade=_offers[index];
        if(!IsEligible(upgrade)) return false;
        if(upgrade.Effect==UpgradeEffect.UnlockAbility && !World.Abilities.CanUnlock(World.Abilities.Find(upgrade.AbilityId)!))
        { _replacement=upgrade;Changed?.Invoke();return true; }
        return ApplyChoice(upgrade,index);
    }
    public void CancelReplacement(){_replacement=null;Changed?.Invoke();}
    public bool Replace(int index)
    {
        if(!Replacing || !CanChoose || index<0 || index>=ReplacementOptions.Count || !World.PlayerActor.Alive)return false;
        var old=ReplacementOptions[index];var chosen=_replacement!;
        var incoming=World.Abilities.Find(chosen.AbilityId)!;
        bool oldFree=old.InheritsFreeSlot,incomingFree=incoming.InheritsFreeSlot,oldSlotless=old.Definition.Slotless;
        old.SetUnlocked(false);old.InheritsFreeSlot=false;
        incoming.InheritsFreeSlot=oldFree || (old.Definition.ReplaceableStarter || old.Definition.Id is "affliction" or "lacerate" or "viciousslash") && oldSlotless;
        if(old.Definition.ReplaceableStarter && oldSlotless)old.Definition.Slotless=false;
        _replacement=null;
        if(ApplyChoice(chosen,_offers.IndexOf(chosen)))
        {
            World.Periodic.RemoveSource(old);
            // Baseline Affliction has no learned rank yet. Trading it consumes its
            // one acquisition, just like replacing any trained ability.
            if(old.Definition.Id=="affliction")
                foreach(var unlock in Upgrades.Where(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId=="affliction"))
                    _ranks[unlock.Id]=Math.Max(1,Rank(unlock));
            return true;
        }
        incoming.InheritsFreeSlot=incomingFree;old.InheritsFreeSlot=oldFree;old.Definition.Slotless=oldSlotless;old.SetUnlocked(true);return false;
    }
    private bool ApplyChoice(UpgradeDefinition upgrade,int index)
    {
        if(upgrade.Effect==UpgradeEffect.UnlockAbility && !World.Abilities.Unlock(upgrade.AbilityId))return false;
        if(upgrade.Effect==UpgradeEffect.UnlockAbility)_lastAbilityLevel=Level;
        _rankPower[upgrade.Id]=Pow(upgrade)+(upgrade.MilestoneLevel>0?1:upgrade.RarityMultiplier);
        _ranks[upgrade.Id]=Rank(upgrade)+1;
        ApplyStats(upgrade.Effect==UpgradeEffect.Health ? upgrade.EffectiveAmount : 0);
        SelectedOfferIndex = index; PresentationPhase = ProgressionPresentationPhase.Confirming;
        _presentationRemaining = upgrade.Rarity switch { PowerRarity.Artifact => 0.65, PowerRarity.Prototype => 0.48, _ => 0.34 };
        UpgradeApplied?.Invoke(upgrade); Changed?.Invoke(); return true;
    }

    private float Bonus(UpgradeEffect effect,string ability="") => Upgrades
        .Where(u=>u.Effect==effect && (u.AbilityId=="" || u.AbilityId==ability)).Sum(u=>Pow(u)*u.AmountPerRank);
    public int AbilityRanks(string id) => Upgrades.Where(u=>Affects(u,id)).Sum(Rank);
    private static bool Affects(UpgradeDefinition u,string id) => u.Effect is not (UpgradeEffect.Health or UpgradeEffect.Speed or UpgradeEffect.UnlockAbility)
        && (u.AbilityId=="" || u.AbilityId==id) && (u.Effect!=UpgradeEffect.TickRate || id is "lightning" or "storm" or "affliction" or "creepingterror" or "crushingdarkness")
        && (u.Effect!=UpgradeEffect.Radius || id is "storm" or "deathfield" || ClassProfile.All.Any(p=>p.StartingAbility==id))
        && (u.Effect!=UpgradeEffect.Damage || id is not ("barrier" or "darkheal"))
        && (u.Effect!=UpgradeEffect.Range || id!="darkheal");
    public PowerRarity AbilityRarity(string id) => Upgrades.Where(u=>Rank(u)>0 && Affects(u,id))
        .Select(u=>u.Rarity).DefaultIfEmpty(PowerRarity.Premium).Max();
    private void ApplyStats(float heal=0)
    {
        World.Abilities.Find("polarityshift")?.SetUnlocked(World.Build.Pregame.PolarityReady);
        AdmitTalentAbility("blowforblow",World.Build.Pregame.AgentCounterstrikeReady);
        if(World.Build.Pregame.AgentConcealment)World.Abilities.Find("carbineburst")?.SetUnlocked(false);
        World.Build.Pregame.ReplaceAgentDart();
        foreach(var ability in World.Abilities.Abilities)
        {
            string id=ability.Definition.Id;var original=_abilityBases[id];
            var survival=_survivalBases[id];
            float Power(Func<UpgradeDefinition,float> select) => Upgrades.Where(u=>u.Effect==UpgradeEffect.AbilityPower && u.AbilityId==id)
                .Sum(u=>Pow(u)*select(u));
            ability.DamagePerTick=original.damage*(1+Bonus(UpgradeEffect.Damage,id)+Power(u=>u.DamagePerRank));
            ability.TickSeconds=original.tick/(1+Bonus(UpgradeEffect.TickRate,id));
            ability.Range=original.range*(1+Bonus(UpgradeEffect.Range,id));
            World.Abilities.CooldownRate=1+(World.Artifacts?.PermanentHaste??0)+Bonus(UpgradeEffect.Cooldown,World.Combat.Definition.Id);
            ability.CooldownSeconds=original.cooldown/(1+(World.Artifacts?.PermanentHaste??0)+Bonus(UpgradeEffect.Cooldown,id)+Power(u=>u.CooldownPerRank));
            ability.Radius=original.radius*(1+Bonus(UpgradeEffect.Radius,id)+Power(u=>u.RadiusPerRank));
            ability.Healing=survival.healing*(1+Power(u=>u.HealingPerRank));
            ability.ShieldCapacity=survival.shield*(1+Power(u=>u.ShieldPerRank));
            ability.ShieldDuration=survival.duration*(1+Power(u=>u.DurationPerRank));
            ability.HealthThreshold=Math.Min(0.8f,survival.threshold+Power(u=>u.ThresholdPerRank));
            ability.ExtraBounces=World.Build.Talent("chain_reach")+(int)World.Build.Item("bounce");
            ability.ChannelSeconds=ability.Definition.ChannelSeconds+(id=="storm"?World.Build.Talent("storm_duration")*2+World.Build.Item("storm_duration"):0);
            if(id=="lightningstrike" || id=="shock" && World.RunConfiguration.AdvancedClassId=="sorcerer")
                ability.Definition.Area=World.Build.Talent(id=="shock"?"shock_cleave":"strike_cleave")>0?AbilityArea.TargetSplash:AbilityArea.SingleTarget;
            World.Build.Pregame.ApplyStats(ability);
        }
        World.Targeting.AcquisitionRange=World.Combat.Range;
        World.PlayerActor.Health.SetMaximum((_health+Bonus(UpgradeEffect.Health))*(1-World.Build.Item("health_cost")),heal);
        World.Player.MoveSpeed=_speed*(1+Bonus(UpgradeEffect.Speed))*World.Build.SpeedMultiplier;
    }
    public void Recalculate() => ApplyStats();
    private void AdmitTalentAbility(string id,bool ready)
    {
        var ability=World.Abilities.Find(id);if(ability==null)return;
        if(!ready){ability.SetUnlocked(false);_talentAdmissions.Remove(id);return;}
        if(_talentAdmissions.Contains(id))return;
        if(World.Abilities.Unlock(id))_talentAdmissions.Add(id);
    }
    public void ResumePending() { if(!Selecting)TryLevel();Changed?.Invoke(); }

    public void ResetRun()
    {
        CancelPresentation(); _ranks.Clear();_rankPower.Clear(); _banished.Clear(); _banishedAbilities.Clear();_talentAdmissions.Clear();
        BanishesRemaining=3; RerollsRemaining=3; SkipsRemaining=2; AbilityChoice=false; _lastAbilityLevel=1; _offerLevel=0; Level=1; Experience=0; ElapsedSeconds=0;
        World.Artifacts?.Clear();World.Build.Reset();
        World.Build.Pregame.Reset();
        World.Abilities.ResetUnlocks();
        foreach (var pickup in _pickups) { RemoveChild(pickup); pickup.QueueFree(); }
        _pickups.Clear(); ApplyStats(); Changed?.Invoke();
        foreach (var pickup in GetChildren().OfType<ExperiencePickup>().Where(p => p.Collecting)) pickup.QueueFree();
    }

    public void CancelPresentation()
    {
        GetTree().Paused = false; _offers.Clear(); _replacement=null; SelectedOfferIndex = -1;
        PresentationPhase = ProgressionPresentationPhase.Idle; _presentationRemaining = 0;
        Changed?.Invoke();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(World.Spawner)) World.Spawner!.EnemyReward-=Drop;
        GetTree().Paused=false;
    }
}
