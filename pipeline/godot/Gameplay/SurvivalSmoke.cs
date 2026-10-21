using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One opt-in B3 integration smoke, without captures, asset work or historical regressions.</summary>
public partial class SurvivalSmoke : Node
{
    private readonly List<string> _checks = new();

    private void Check(string name, bool passed)
    {
        if (!passed) throw new InvalidOperationException(name);
        _checks.Add(name); GD.Print("B3 smoke: " + name);
    }

    private async Task Until(Func<bool> condition, double seconds = 8)
    {
        ulong deadline = Time.GetTicksMsec() + (ulong)(seconds * 1000);
        while (!condition())
        {
            if (Time.GetTicksMsec() >= deadline) throw new TimeoutException("B3 smoke condition timed out");
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }
    }

    public async void Run(GameWorld world)
    {
        string? failure = null;
        try
        {
            var waves = world.Waves!; var spawner = world.Spawner!;
            float duration = waves.WaveSeconds;
            // Briefly disable outgoing damage so the initial enemy can reach and hit the player.
            world.Combat.CombatEnabled = false; world.Combat.Abort();
            waves.WaveSeconds = 3; world.ResetCombatWorld();
            await Until(() => spawner.ActiveCount > 0);
            var enemy = spawner.Enemies.First(); var start = enemy.GlobalPosition;
            Check("spawn is supported, bounded and outside player collision", enemy.Navigation.Arena.Supported(start, enemy.Navigation.Radius) &&
                start.DistanceTo(world.Player.GlobalPosition) > 0.15f);
            await Until(() => enemy.GlobalPosition.DistanceTo(start) > 0.02f);
            Check("enemy pursues over existing terrain", enemy.State == EnemyState.Chase && enemy.Health.IsAlive);
            await Until(() => world.PlayerActor.Health.Current < 100);
            Check("melee damages existing player health", enemy.Attacks > 0 && world.PlayerActor.Health.DamageEvents > 0);
            await Until(() => waves.Wave >= 2);
            Check("wave increases cap, cadence and new-enemy scaling", spawner.ActiveLimit > waves.InitialActiveLimit &&
                waves.SpawnInterval < waves.InitialSpawnInterval && spawner.HealthMultiplier > 1 && spawner.DamageMultiplier > 1 &&
                spawner.ActiveCount <= spawner.ActiveLimit);
            var body = enemy.Body;
            enemy.Health.ApplyDamage(10000);
            Check("enemy death stops AI and counts defeat", enemy.State == EnemyState.Dead && !enemy.Running && !enemy.Alive && spawner.Defeated == 1);
            await Until(() => !GodotObject.IsInstanceValid(body), 6);
            Check("corpse removed after delay", !GodotObject.IsInstanceValid(body));
            world.PlayerActor.Health.ApplyDamage(10000);
            Check("player death stops combat and all enemies", !waves.Running && !spawner.Running &&
                !world.Player.IsPhysicsProcessing() && world.Combat.State == LightningState.Disabled &&
                spawner.Enemies.All(e => !e.Running) && world.Player.Presentation.State == "Death");
            int received = world.PlayerActor.Health.DamageEvents;
            for (int i = 0; i < 15; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Check("no attacks after player death", world.PlayerActor.Health.DamageEvents == received);
            waves.WaveSeconds = duration; world.ResetCombatWorld(); world.Combat.CombatEnabled = true;
            Check("restart restores health, wave and counters", waves.Wave == 1 && waves.Running && spawner.ActiveCount == 0 &&
                spawner.Defeated == 0 && world.PlayerActor.Health.Current == 100 && world.PlayerActor.Alive && world.Player.IsPhysicsProcessing());
            await Until(() => world.Combat.DamageEvents > 0);
            Check("automatic Lightning resumes against spawned enemies", world.Combat.DamageEvents > 0 && Engine.GetFramesDrawn() > 0);
        }
        catch (Exception ex) { failure = ex.ToString(); GD.PushError("B3 smoke failed: " + failure); }
        using (var report = FileAccess.Open("res://../manifests/survival-smoke.json", FileAccess.ModeFlags.Write))
            report.StoreString(JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure }, new JsonSerializerOptions { WriteIndented = true }));
        GetTree().Quit(failure == null ? 0 : 1);
    }
}
