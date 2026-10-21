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
    public float HealthMultiplier { get; set; } = 1;
    public float DamageMultiplier { get; set; } = 1;
    public bool Running { get; set; } = true;
    public int Defeated { get; private set; }
    public int Spawned { get; private set; }
    public event Action<Vector3>? EnemyDefeated;
    public event Action<Vector3, int>? EnemyReward;
    public string LastPlacement { get; private set; } = "Waiting to spawn";
    public IReadOnlyList<EnemyAgent> Enemies => _enemies;
    public int ActiveCount => _enemies.Count(e => GodotObject.IsInstanceValid(e) && e.Alive);
    private readonly List<EnemyAgent> _enemies = new();
    private readonly Dictionary<EnemyAgent, double> _corpses = new();
    private readonly RandomNumberGenerator _random = new();
    private readonly Dictionary<string, ArenaNavigation> _navigationProfiles = new();
    private readonly Dictionary<Vector2I, List<EnemyAgent>> _cells = new();
    private const float CellSize = 0.16f;

    public override void _Ready() => _random.Randomize();

    public bool TrySpawn(PackedScene? scene = null, bool ignoreLimit = false)
    {
        if (!Running || !Player.Alive || (!ignoreLimit && ActiveCount >= ActiveLimit)) return false;
        var body = (scene ?? EnemyScene).Instantiate<CharacterBody3D>();
        var enemy = body.GetNode<EnemyAgent>("Actor");
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
            var sample = Player.GlobalPosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            if (!navigation.TryStandingPoint(sample, out var feet)) continue;
            // Conservative connectivity: spawn only on terrain reachable by a safe straight corridor.
            // Local steering can then handle props approached as the player moves.
            if (!navigation.CanWalk(Player.GlobalPosition, feet)) continue;
            if (_enemies.Any(e => GodotObject.IsInstanceValid(e) && e.Alive && e.GlobalPosition.DistanceTo(feet) < e.CollisionRadius + enemy.CollisionRadius + 0.01f)) continue;
            enemy.Navigation = navigation; enemy.Target = Player; enemy.Crowd = this;
            enemy.TargetId = $"Enemy{++Spawned:0000}"; enemy.SteeringSide = Spawned % 2 == 0 ? 1 : -1;
            var health = enemy.GetNode<Health>("Health");
            health.Maximum *= HealthMultiplier; enemy.AttackDamage *= DamageMultiplier;
            body.Position = ToLocal(feet); AddChild(body);
            _enemies.Add(enemy);
            enemy.Health.Died += () =>
            {
                Defeated++; _corpses[enemy] = Math.Max(0.5f, CorpseSeconds);
                EnemyDefeated?.Invoke(enemy.GlobalPosition);
                EnemyReward?.Invoke(enemy.GlobalPosition, enemy.ExperienceReward);
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
        foreach (var enemy in _enemies)
        {
            if (!GodotObject.IsInstanceValid(enemy) || !enemy.Alive) continue;
            var cell = Cell(enemy.GlobalPosition);
            if (!_cells.TryGetValue(cell, out var list)) _cells[cell] = list = new();
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
    }
}
