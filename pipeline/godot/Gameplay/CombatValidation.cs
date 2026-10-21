using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Opt-in bounded rendered smoke plus focused combat logic checks. No conversion/regression pipeline.</summary>
public partial class CombatValidation : Node
{
    private readonly List<object> _checks = new();
    private readonly List<string> _failures = new();
    private GameWorld _world = null!;
    private AutomaticLightning C => _world.Combat;
    private PlayerController P => _world.Player;
    private StationaryEnemy E => _world.Enemy;

    private void Check(string name, bool passed, object? detail = null)
    {
        _checks.Add(new { name, passed, detail });
        GD.Print($"B2 {(passed ? "PASS" : "FAIL")}: {name}");
        if (!passed) _failures.Add(name);
    }

    private async Task Frames(int count = 2)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }

    private async Task Until(Func<bool> condition, double seconds = 6)
    {
        int frames = (int)(seconds * Engine.PhysicsTicksPerSecond);
        while (!condition() && frames-- > 0) await Frames(1);
        if (!condition()) throw new Exception("Timed out waiting for combat transition");
    }

    private bool Clean()
    {
        var s = C.Visual.Snapshot();
        return new[] { "beams", "sparks", "lights", "audioPlaying" }.All(k => s[k].AsInt32() == 0);
    }

    public async void Run(GameWorld world)
    {
        _world = world;
        try { await Validate(); }
        catch (Exception e) { Check("smoke exception", false, e.ToString()); }
        var result = new
        {
            passed = _failures.Count == 0, failures = _failures, checks = _checks,
            renderer = RenderingServer.GetCurrentRenderingMethod(), renderedFrames = Engine.GetFramesDrawn(),
            engine = Engine.GetVersionInfo()["string"].AsString(),
            scope = "One rendered B2 smoke and focused combat checks; A1–A4 and full B1 suites preserved, not rerun. No captures or asset conversion."
        };
        using var file = Godot.FileAccess.Open("res://../manifests/combat-smoke.json", Godot.FileAccess.ModeFlags.Write);
        file.StoreString(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print(result.passed ? "COMBAT_SMOKE_PASSED" : "COMBAT_SMOKE_FAILED");
        GetTree().Quit(result.passed ? 0 : 1);
    }

    private async Task Validate()
    {
        C.CombatEnabled = false; C.Abort(); await Frames(4);
        // Pure scheduling checks exercise boundaries, frame partitions and catch-up without rendering loops.
        foreach (double step in new[] { 1.0 / 30, 1.0 / 60, 1.0 / 120, 0.37, 3.5 })
        {
            var clock = new ChannelClock(); clock.Reset(3, 0.5); int ticks = 0;
            while (!clock.Complete) clock.Advance(step, () => { ticks++; return true; });
            clock.Advance(0, () => { ticks++; return true; });
            Check("six unique ticks at step " + step, ticks == 6);
        }
        var health = new Health { Maximum = 10 }; AddChild(health);
        int deaths = 0; health.Died += () => deaths++;
        Check("invalid damage ignored", health.ApplyDamage(-1) == 0 && health.ApplyDamage(float.NaN) == 0);
        health.ApplyDamage(3); health.ApplyDamage(50); health.ApplyDamage(1);
        Check("health clamp and single death", health.Current == 0 && deaths == 1 && health.DamageEvents == 2);
        health.QueueFree();

        var a = new Combatant { Name = "TieA", TargetId = "A", Team = CombatTeam.Hostile };
        var z = new Combatant { Name = "TieZ", TargetId = "Z", Team = CombatTeam.Hostile };
        foreach (var actor in new[] { a, z })
        {
            actor.AddChild(new Health { Name = "Health" }); _world.AddChild(actor); actor.GlobalPosition = E.GlobalPosition;
        }
        Check("stable equal-distance identity tie", _world.Targeting.FindNearest() == a);
        a.Team = CombatTeam.Player; z.Team = CombatTeam.Neutral;
        Check("friendly and neutral filtering", _world.Targeting.FindNearest() == E);
        a.Team = CombatTeam.Hostile; a.Health.ApplyDamage(1000);
        Check("dead target excluded", !_world.Targeting.IsValidTarget(a));
        z.Team = CombatTeam.Hostile; z.GlobalPosition = P.GlobalPosition + Vector3.Right * 5;
        Check("out-of-range excluded", !_world.Targeting.IsValidTarget(z));
        z.GlobalPosition = E.GlobalPosition;
        Check("range entry recognized", _world.Targeting.IsValidTarget(z));
        a.QueueFree(); z.QueueFree(); await Frames();

        // The gameplay ray must obey both terrain and prop layers, even for back faces.
        var blocker = new StaticBody3D { CollisionLayer = 2, CollisionMask = 0 };
        blocker.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.04f, 0.4f, 0.04f) } });
        _world.AddChild(blocker); blocker.GlobalPosition = _world.PlayerActor.AimPosition.Lerp(E.AimPosition, 0.5f);
        await Frames();
        Check("prop layer blocks line of sight", !_world.Targeting.IsValidTarget(E));
        blocker.CollisionLayer = 1; await Frames();
        Check("terrain layer blocks line of sight", !_world.Targeting.IsValidTarget(E));
        blocker.QueueFree(); await Frames();
        Check("line of sight restored", _world.Targeting.IsValidTarget(E));

        P.TestInput = Vector2.Up; var start = P.GlobalPosition; await Frames(12); P.TestInput = Vector2.Zero;
        Check("B1 movement remains grounded", P.GlobalPosition.DistanceTo(start) > 0.01f && P.IsOnFloor());
        Check("B1 crop boundary preserved", !_world.Arena.CanTraverse(P.GlobalPosition, new Vector3(46, P.GlobalPosition.Y, 12), P.CapsuleRadius));
        _world.ResetCombatWorld(); await Frames(3);
        C.CombatEnabled = true;
        await Until(() => C.State == LightningState.Starting);
        Check("original start animation", P.Presentation.Animator.CurrentAnimation == "LightningStart");
        int scans = _world.Targeting.Scans;
        await Until(() => C.State == LightningState.Channeling);
        await Until(() => P.Presentation.Animator.CurrentAnimation == "LightningLoop", 0.5);
        Check("original loop animation", P.Presentation.Animator.CurrentAnimation == "LightningLoop");
        var snapshot = C.Visual.Snapshot();
        Check("rendered original VFX and audio active", snapshot["beams"].AsInt32() > 0 && snapshot["audioPlaying"].AsInt32() > 0,
            new { beams = snapshot["beams"].AsInt32(), audio = snapshot["audioPlaying"].AsInt32() });
        start = P.GlobalPosition; P.TestInput = Vector2.Right; await Frames(12); P.TestInput = Vector2.Zero;
        Check("movement during cast retains physics authority", P.GlobalPosition.DistanceTo(start) > 0.01f && P.Presentation.OverrideActive && P.IsOnFloor());
        await Until(() => C.DamageEvents == 1);
        Check("one tick and original hit", E.Health.Current == 72 && E.Presentation!.State == "Hit");
        await Until(() => E.Presentation!.State == "Idle", 2);
        Check("hit returns to idle", E.Presentation!.Animator.CurrentAnimation == "Idle");
        await Until(() => C.State == LightningState.Ending);
        Check("full channel damage total", C.DamageEvents == 6 && E.Health.Current == 32);
        Check("original cast end", P.Presentation.Animator.CurrentAnimation == "LightningEnd");
        await Until(() => C.State == LightningState.Cooldown);
        Check("cooldown clean and locomotion released", Clean() && !P.Presentation.OverrideActive && C.CooldownRemaining > 0);
        Check("periodic scans only", _world.Targeting.Scans - scans < 45, _world.Targeting.Scans - scans);
        await Until(() => !E.Health.IsAlive, 9);
        Check("death animation and no overkill ticks", E.Presentation!.State == "Death" && E.Health.Current == 0 && C.DamageEvents == 10);
        int events = C.DamageEvents;
        await Until(() => C.State == LightningState.Cooldown);
        Check("death cleanup and no damage afterwards", Clean() && C.DamageEvents == events && !E.CombatEnabled);

        // Reset, then leave range during a channel: no damage can leak through cancellation.
        _world.ResetCombatWorld(); await Until(() => C.State == LightningState.Channeling);
        _world.Targeting.AcquisitionRange = 0.01f; events = C.DamageEvents;
        await Frames(2);
        Check("range exit cancels immediately", C.State == LightningState.Ending && C.DamageEvents == events && Clean());
        _world.Targeting.AcquisitionRange = 0.85f;
        await Until(() => C.State == LightningState.Cooldown);
        Check("cancel returns to locomotion", !P.Presentation.OverrideActive && Clean());

        _world.ResetCombatWorld(); await Until(() => C.State == LightningState.Channeling);
        // Remove a real rig target mid-channel, then reattach the same authored test instance.
        _world.RemoveChild(E); events = C.DamageEvents; await Frames(2);
        Check("removed target cancels", C.State == LightningState.Ending && C.DamageEvents == events && Clean());
        _world.AddChild(E); E.GlobalPosition = P.SpawnPosition + new Vector3(0, 0, -0.45f);
        _world.ResetCombatWorld(); await Until(() => C.State == LightningState.Channeling);
        _world.PlayerActor.Health.ApplyDamage(1000); events = C.DamageEvents; await Frames(2);
        Check("caster death aborts without orphan effects", C.State == LightningState.Disabled && Clean() &&
            P.Presentation.State == "Death" && C.DamageEvents == events && !P.IsPhysicsProcessing());
        _world.ResetCombatWorld(); C.CombatEnabled = false; await Frames(3);
        Check("reset restores movement and health", P.IsPhysicsProcessing() && P.IsOnFloor() &&
            _world.PlayerActor.Health.Current == 100 && E.Health.Current == 80 && Clean());
        Check("actual renderer produced frames", Engine.GetFramesDrawn() > 10 && DisplayServer.GetName() != "headless");
    }
}
