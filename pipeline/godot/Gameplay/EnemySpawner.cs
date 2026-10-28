using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Bounded placement and instance lifecycle, independent of wave timing and enemy art.</summary>
public partial class EnemySpawner : Node3D
{
    [Export] public PackedScene EnemyScene { get; set; } = null!;
    [Export] public float MinimumSpawnDistance { get; set; } = 0.55f;
    [Export] public float MaximumSpawnDistance { get; set; } = 1.05f;
    [Export] public float CorpseSeconds { get; set; } = 4;
    [Export] public int PlacementAttempts { get; set; } = 18;
    public ArenaNavigation Navigation { get; set; } = null!;
    public Combatant Player { get; set; } = null!;
    public int ActiveLimit { get; set; } = 3;
    public int HardActiveLimit { get; set; } = int.MaxValue;
    public int ReservedSlots { get; set; }
    public int ExperienceDropEvery { get; set; } = 1;
    public int ExperienceMultiplier { get; set; } = 1;
    public bool ReinforcementsEnabled { get; set; } = true;
    public RunDifficulty? Difficulty { get; set; }
    public OperationDifficultyTuning OperationMode {get;set;}=OperationDifficultyTuning.Baseline;
    public Func<int>? PlayerLevel { get; set; }
    public Dictionary<string, int> ReferenceLevels { get; } = new();
    public float HealthMultiplier { get; set; } = 1;
    public float DamageMultiplier { get; set; } = 1;
    public bool Running { get; set; } = true;
    public bool ReuseAnimations {get;set;}
    public int Defeated { get; private set; }
    public int Spawned { get; private set; }
    public event Action<Vector3>? EnemyDefeated;
    public event Action<Vector3, int>? EnemyReward;
    public event Action<EnemyAgent>? EnemyKilled;
    public string LastPlacement { get; private set; } = "Waiting to spawn";
    public IReadOnlyList<EnemyAgent> Enemies => _enemies;
    public int ActiveCount => _enemies.Count(e => GodotObject.IsInstanceValid(e) && e.Alive);
    private readonly List<EnemyAgent> _enemies = new();
    private readonly Dictionary<EnemyAgent, double> _corpses = new();
    private readonly RandomNumberGenerator _random = new();
    private readonly Dictionary<string, ArenaNavigation> _navigationProfiles = new();
    private readonly Dictionary<Vector2I, List<EnemyAgent>> _cells = new();
    private const float CellSize = 0.16f;
    private readonly List<List<EnemyAgent>> _cellLists=new();

    public override void _Ready() => _random.Randomize();

    public bool TrySpawn(PackedScene? scene = null, bool ignoreLimit = false, bool elite = false, Vector3? anchor = null)
    {
        int count = ActiveCount;
        if (!Running || !Player.Alive || count >= HardActiveLimit ||
            (!ignoreLimit && (!ReinforcementsEnabled || count >= Math.Min(ActiveLimit, Math.Max(0, HardActiveLimit - ReservedSlots))))) return false;
        var source = scene ?? EnemyScene;
        var body = source.Instantiate<CharacterBody3D>();
        var enemy = body.GetNode<EnemyAgent>("Actor");
        enemy.ExperienceReward=(int)Math.Clamp((long)enemy.ExperienceReward*Math.Max(1,ExperienceMultiplier),1,int.MaxValue);
        if(enemy.GetNodeOrNull<EnemyPresentation>("Visual") is {} visual)visual.ReuseLoopLibrary=ReuseAnimations;
        if(elite) { enemy.RewardTier=1;enemy.KnockbackResistance=0.65f;enemy.DisplayName="Elite "+enemy.DisplayName;enemy.GetNode<Health>("Health").Maximum*=2;enemy.ExperienceReward*=2; }
        string key = $"{enemy.CollisionRadius}/{enemy.CollisionHeight}";
        if (!_navigationProfiles.TryGetValue(key, out var navigation))
        {
            navigation = new ArenaNavigation { Arena = Navigation.Arena, Radius = enemy.CollisionRadius, Height = enemy.CollisionHeight };
            AddChild(navigation); _navigationProfiles[key] = navigation;
        }
        for (int i = 0; i < Math.Clamp(PlacementAttempts, 1, 64); i++)
        {
            float angle = _random.RandfRange(0, Mathf.Tau);
            float radius = _random.RandfRange(Math.Max(0.15f, MinimumSpawnDistance), Math.Max(MinimumSpawnDistance, MaximumSpawnDistance));
            if(anchor.HasValue)radius=_random.RandfRange(.2f,.5f);
            var sample = (anchor??Player.GlobalPosition) + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            if (!navigation.TryStandingPoint(sample, out var feet)) continue;
            // Conservative connectivity: spawn only on terrain reachable by a safe straight corridor.
            // Local steering can then handle props approached as the player moves.
            if (!navigation.CanWalk(anchor??Player.GlobalPosition, feet)) continue;
            if (_enemies.Any(e => GodotObject.IsInstanceValid(e) && e.Alive && e.GlobalPosition.DistanceTo(feet) < e.CollisionRadius + enemy.CollisionRadius + 0.01f)) continue;
            enemy.Navigation = navigation; enemy.Target = Player; enemy.Crowd = this;
            enemy.TargetId = $"Enemy{++Spawned:0000}"; enemy.SteeringSide = Spawned % 2 == 0 ? 1 : -1;
            var health = enemy.GetNode<Health>("Health");
            if (Difficulty is {} tuning)
            {
                enemy.SpawnLevel = Math.Max(1, PlayerLevel?.Invoke() ?? 1);
                enemy.ReferenceLevel = ReferenceLevels.GetValueOrDefault(source.ResourcePath, 1);
                int levels = Math.Max(0, enemy.SpawnLevel - enemy.ReferenceLevel);
                health.Maximum *= tuning.EnemyHealthMultiplier * (float)Math.Pow(1 + tuning.HealthPerLevel, levels);
                enemy.LevelDamageMultiplier = tuning.EndlessPressure || tuning.ExponentialDamageScaling ? (float)Math.Pow(1 + tuning.DamagePerLevel, levels) : 1 + tuning.DamagePerLevel * levels;
                enemy.LevelDamageMultiplier *= tuning.EnemyDamageMultiplier;
                enemy.AttackInterval *= OperationMode.AttackInterval;
                var pattern = enemy.GetNodeOrNull<TombAttack>("TombAttack");
                if(pattern!=null) {pattern.Windup*=OperationMode.WarningTime;pattern.Cooldown*=OperationMode.AttackInterval;}
                if(pattern != null && tuning.EndlessPressure) { pattern.Cooldown /= 1 + Math.Min(1.5f, levels * .025f); pattern.Windup = Math.Max(.5f, pattern.Windup / (1 + levels * .008f)); }
                enemy.MoveSpeed *= 1 + Math.Min(tuning.MaximumSpeedBonus, tuning.SpeedPerLevel * levels);
                enemy.AttackInterval /= 1 + Math.Min(tuning.MaximumAttackFrequencyBonus, tuning.AttackFrequencyPerLevel * levels);
            }
            else if(enemy.ScaleWithWaves) {health.Maximum *= HealthMultiplier; enemy.AttackDamage *= DamageMultiplier;}
            body.Position = ToLocal(feet); AddChild(body);
            _enemies.Add(enemy);
            enemy.Health.Died += () =>
            {
                Defeated++; _corpses[enemy] = Math.Max(0.5f, CorpseSeconds);
                EnemyDefeated?.Invoke(enemy.GlobalPosition);
                if (Defeated % Math.Max(1,ExperienceDropEvery) == 0)
                    EnemyReward?.Invoke(enemy.GlobalPosition, enemy.ExperienceReward);
                EnemyKilled?.Invoke(enemy);
            };
            enemy.TreeExiting += () => { _enemies.Remove(enemy); _corpses.Remove(enemy); };
            LastPlacement = "Spawning normally";
            return true;
        }
        LastPlacement = "Waiting for a safe spawn position";
        body.Free();
        return false;
    }

    public override void _PhysicsProcess(double delta)
    {
        _cells.Clear();
        int used=0;
        foreach (var enemy in _enemies)
        {
            if (!GodotObject.IsInstanceValid(enemy) || !enemy.Alive) continue;
            var cell = Cell(enemy.GlobalPosition);
            if (!_cells.TryGetValue(cell, out var list))
            {
                if(used==_cellLists.Count)_cellLists.Add(new());
                list=_cellLists[used++];list.Clear();_cells[cell]=list;
            }
            list.Add(enemy);
        }
        foreach (var enemy in _corpses.Keys.ToArray())
        {
            _corpses[enemy] -= delta;
            if (_corpses[enemy] > 0) continue;
            _corpses.Remove(enemy);
            if (GodotObject.IsInstanceValid(enemy)) enemy.Body.QueueFree();
        }
    }
    private static Vector2I Cell(Vector3 p) => new(Mathf.FloorToInt(p.X / CellSize), Mathf.FloorToInt(p.Z / CellSize));
    public Vector3 Separation(EnemyAgent actor)
    {
        var cell = Cell(actor.GlobalPosition); var push = Vector3.Zero;
        for (int z = -1; z <= 1; z++) for (int x = -1; x <= 1; x++)
        {
            if (!_cells.TryGetValue(cell + new Vector2I(x, z), out var neighbors)) continue;
            foreach (var other in neighbors)
            {
                if (other == actor || !other.Alive) continue;
                var diff = actor.GlobalPosition - other.GlobalPosition; diff.Y = 0;
                float reach = actor.CollisionRadius + other.CollisionRadius + 0.02f, distance = diff.Length();
                if (distance >= reach) continue;
                push += distance > 0.0001f ? diff / distance * (1 - distance / reach) : Vector3.Right * actor.SteeringSide;
            }
        }
        return push.LimitLength(1);
    }

    public void Stop()
    {
        Running = false;
        foreach (var enemy in _enemies) if (GodotObject.IsInstanceValid(enemy)) enemy.Stop();
    }

    public void ClearRegularEnemies()
    {
        // Despawn without death or rewards; the encounter intro must not farm XP.
        foreach(var enemy in _enemies.ToArray())
            if(GodotObject.IsInstanceValid(enemy) && enemy.Alive && enemy.RewardTier<2)enemy.Body.QueueFree();
    }

    public void ResetSpawner()
    {
        foreach (var enemy in _enemies.ToArray())
        {
            if (!GodotObject.IsInstanceValid(enemy)) continue;
            enemy.Stop(); enemy.CombatEnabled = false;
            // Remove immediately so physics, targeting, counts and R-spawn clearance agree this frame.
            var body = enemy.Body; RemoveChild(body); body.QueueFree();
        }
        _enemies.Clear(); _corpses.Clear(); _cells.Clear(); Defeated = 0; Spawned = 0;
        LastPlacement = "Waiting to spawn"; Running = true;
        ReinforcementsEnabled = true;
    }
}
