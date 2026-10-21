using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One brief rendered feature smoke. Runs only with --flytext-smoke.</summary>
public partial class FlytextSmoke : Node
{
    private readonly List<string> _checks = new();
    private readonly List<CombatEvent> _entries = new();
    private void Check(string name, bool passed)
    {
        if (!passed) throw new InvalidOperationException(name);
        _checks.Add(name); GD.Print("Flytext smoke: " + name);
    }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }

    public async void Run(GameWorld world)
    {
        ProcessMode = ProcessModeEnum.Always;
        string? failure = null;
        var enemies = new List<Combatant>();
        world.Flytext.EntryCreated += Record;
        try
        {
            world.ResetCombatWorld(); world.Waves!.Stop(); world.Spawner!.Running = false;
            world.Abilities.SetPhysicsProcess(false); world.Abilities.Abort();
            world.Player.SetPhysicsProcess(false); world.Rig.Snap();
            // Lightweight recipients use the real Combatant/Health path and actual Storm implementation.
            for (int i = 0; i < 8; i++)
            {
                var enemy = new Combatant { Name = "FlytextFixture" + i,
                    TargetId = "FlytextFixture" + i, Team = CombatTeam.Hostile };
                enemy.AddChild(new Health { Name = "Health", Maximum = 1000 });
                world.AddChild(enemy);
                enemy.GlobalPosition = world.PlayerActor.GlobalPosition + new Vector3(0.14f + i * 0.014f, 0, 0);
                enemies.Add(enemy);
            }
            await Frames(2);
            Check("Eight recipients are targetable", enemies.All(e => world.Targeting.IsValidTarget(e)));
            var storm = world.Abilities.Find("storm")!;
            storm.SetUnlocked(true);
            Check("Actual Force Storm cast starts", storm.TryBegin());
            world.Flytext.Clear(); _entries.Clear();
            long before = world.Flytext.CreatedEntries;
            storm.Advance(storm.Definition.WindupSeconds + 0.001);
            storm.Advance(storm.TickSeconds);
            Check("Storm tick produces eight separate target entries", _entries.Count == 8 &&
                _entries.Select(e => e.RecipientId).Distinct().Count() == 8 && world.Flytext.ActiveCount == 8 &&
                _entries.All(e => Mathf.IsEqualApprox(e.Change.Amount, storm.DamagePerTick)));
            storm.Advance(storm.TickSeconds);
            Check("Next Storm tick preserves eight more overlapping entries", _entries.Count == 16 &&
                world.Flytext.ActiveCount == 16 && world.Flytext.CreatedEntries - before == 16 &&
                enemies.All(e => e.Health.DamageEvents == 2));
            storm.Abort();
            await Frames(3);
            Check("All sixteen entries reach the existing camera canvas", world.Flytext.DrawnEntries == 16 && Engine.GetFramesDrawn() > 0);
            // Apply real health changes directly to isolate no-op/clipping and metadata behavior.
            var health = enemies[0].Health;
            before = world.Flytext.CreatedEntries;
            float damage = health.ApplyDamage(0.125f, DamageType.Internal, isCritical: true);
            float healing = health.ApplyHealing(10000, isCritical: true);
            Check("Applied fractional critical damage and capped healing retain metadata", damage == 0.125f &&
                healing > 0 && _entries[^2].Change == new HealthChange(CombatEventKind.Damage, damage, DamageType.Internal, true) &&
                _entries[^1].Change.Kind == CombatEventKind.Healing && _entries[^1].Change.IsCritical &&
                _entries[^1].Change.Amount == healing && world.Flytext.CreatedEntries - before == 2 &&
                FlytextService.FormatAmount(0.125f) == "1" &&
                FlytextService.FormatAmount(7.4f) == "7" && FlytextService.FormatAmount(7.5f) == "8");
            before = world.Flytext.CreatedEntries;
            health.ApplyDamage(0); health.ApplyDamage(float.NaN); health.ApplyHealing(1);
            Check("Zero damage, invalid damage and full-health healing produce no numbers", world.Flytext.CreatedEntries == before);
            float remaining = health.Current;
            health.ApplyDamage(10000);
            Check("Lethal overkill displays remaining health actually removed", _entries[^1].Change.Amount == remaining);
            before = world.Flytext.CreatedEntries;
            health.ApplyDamage(1); health.ApplyHealing(1);
            Check("Dead recipients cannot produce damage or healing", world.Flytext.CreatedEntries == before);
            int active = world.Flytext.ActiveCount;
            foreach (var enemy in enemies) enemy.QueueFree();
            await Frames(3);
            Check("Flytext survives recipient despawn using value snapshots", world.Flytext.ActiveCount == active && world.Flytext.DrawnEntries == active);
            // Allow normal expiration once; no performance or historical regression run.
            await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
            Check("Transient entries expire", world.Flytext.ActiveCount == 0);
            world.PlayerActor.Health.ApplyDamage(1);
            Check("Player incoming damage uses the same event stream", world.Flytext.ActiveCount == 1);
            world.ResetCombatWorld();
            Check("Run restart clears transient entries and preserves the single action bar", world.Flytext.ActiveCount == 0 && world.Hud!.ActionSlotCount == 12);
        }
        catch (Exception ex) { failure = ex.ToString(); GD.PushError("Flytext smoke: " + failure); }
        world.Flytext.EntryCreated -= Record;
        foreach (var enemy in enemies)
            if (GodotObject.IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion()) enemy.QueueFree();
        GetTree().Paused = false;
        using (var file = FileAccess.Open("res://../manifests/flytext-smoke.json", FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure,
                renderedFrames = Engine.GetFramesDrawn(), scope = "One brief rendered flytext smoke; actual Storm, eight targets, two ticks; no screenshots, benchmarks or historical suites." },
                new JsonSerializerOptions { WriteIndented = true }));
        GD.Print(failure == null ? "FLYTEXT_SMOKE_PASSED" : "FLYTEXT_SMOKE_FAILED");
        GetTree().Quit(failure == null ? 0 : 1);
    }
    private void Record(CombatEvent entry) => _entries.Add(entry);
}
