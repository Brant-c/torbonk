using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One opt-in rendered feature check, using the actual Korriban world and Storm lifecycle.</summary>
public partial class StormReticleSmoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool passed)
    {
        if (!passed) throw new InvalidOperationException(name);
        _checks.Add(name); GD.Print("Storm reticle smoke: " + name);
    }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }

    private async Task<Image> Render()
    {
        await Frames(8);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        return GetViewport().GetTexture().GetImage();
    }
    private static int GreenPixels(Image image, Camera3D camera, Vector3 center, float radius)
    {
        var p = camera.UnprojectPosition(center);
        float reach = 0;
        foreach (var direction in new[] { Vector3.Right, Vector3.Left, Vector3.Forward, Vector3.Back })
            reach = Mathf.Max(reach, p.DistanceTo(camera.UnprojectPosition(center + direction * radius * 1.4f)));
        int count = 0;
        for (int y = Math.Max(0, (int)(p.Y - reach)); y < Math.Min(image.GetHeight(), p.Y + reach); y++)
        for (int x = Math.Max(0, (int)(p.X - reach)); x < Math.Min(image.GetWidth(), p.X + reach); x++)
        {
            var c = image.GetPixel(x, y);
            if (c.G > 0.2f && c.G > c.R * 1.8f && c.G > c.B * 1.8f) count++;
        }
        return count;
    }
    public async void Run(GameWorld world)
    {
        ProcessMode = ProcessModeEnum.Always;
        string? failure = null;
        Combatant? target = null;
        float baseRadius = 0, grownRadius = 0, baseWidth = 0, grownWidth = 0;
        int basePixels = 0, grownPixels = 0;
        try
        {
            world.ResetCombatWorld(); world.Waves!.Stop(); world.Spawner!.Running = false;
            world.Abilities.SetPhysicsProcess(false); world.Abilities.Abort(); world.Player.SetPhysicsProcess(false);
            world.Rig.Snap();
            var storm = world.Abilities.Find("storm")!;
            var fx = (OriginalAbilityPresentation)storm.Presentation;
            storm.SetUnlocked(true);
            Check("Original April green projector color is used", FileAccess.GetFileAsString("res://Assets/Abilities/storm-reticle.json").Contains("#.0,1.0,.0,1.0"));
            Check("Target location is real Korriban terrain", world.Arena.GroundAt(world.Player.GlobalPosition + Vector3.Right * 0.36f,
                out var ground, out _, 0.3f, 0.5f));
            target = new Combatant { Name = "StormReticleTarget", TargetId = "StormReticleTarget", Team = CombatTeam.Hostile };
            target.AddChild(new Health { Name = "Health", Maximum = 1000 }); world.AddChild(target); target.GlobalPosition = ground;
            await Frames(2);
            Check("Actual Force Storm starts", storm.TryBegin());
            baseRadius = storm.Radius;
            var indicator = fx.AreaIndicator!;
            Check("Ground reticle appears at the exact damage center", indicator.Visible && indicator.GroundCenter == storm.GroundCenter &&
                indicator.EffectiveRadius == storm.Radius && indicator.ProjectedVertexCount > 0);
            storm.Advance(storm.Definition.WindupSeconds + 0.001);
            storm.Advance(storm.TickSeconds);
            Check("Existing Storm damage path remains active", target.Health.DamageEvents == 1);
            baseWidth = indicator.GetAabb().Size.X;
            using (var image = await Render()) basePixels = GreenPixels(image, world.Rig.Camera, storm.GroundCenter, storm.Radius);
            Check("Green circle actually renders through the gameplay camera", basePixels > 30);
            var progression = world.Progression!;
            var upgrade = progression.Upgrades.Single(u => u.Id == "storm_radius");
            progression.Upgrades = new Godot.Collections.Array<UpgradeDefinition> {
                upgrade, progression.Upgrades.Single(u => u.Id == "damage"), progression.Upgrades.Single(u => u.Id == "health") };
            progression.AddExperience(progression.RequiredExperience);
            Check("Existing area upgrade can be selected", progression.Choose(progression.Offers.ToList().IndexOf(upgrade)));
            using (var image = await Render())
            {
                grownRadius = storm.Radius; grownWidth = indicator.GetAabb().Size.X;
                grownPixels = GreenPixels(image, world.Rig.Camera, storm.GroundCenter, storm.Radius);
                Check("Rarity-scaled upgrade grows the live damage radius and rendered circle", grownRadius > baseRadius &&
                    Mathf.IsEqualApprox(grownRadius, baseRadius * (1 + upgrade.EffectiveAmount)) &&
                    indicator.EffectiveRadius == grownRadius && Mathf.Abs(grownWidth / baseWidth - grownRadius / baseRadius) < 0.001f && grownPixels > basePixels);
                DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://../generated/storm-reticle"));
                image.SavePng("res://../generated/storm-reticle/active-upgraded.png");
            }
            storm.Radius = baseRadius * 0.5f;
            await Frames(3);
            Check("A smaller live gameplay radius shrinks the existing indicator", indicator.EffectiveRadius == storm.Radius &&
                Mathf.Abs(indicator.GetAabb().Size.X / baseWidth - 0.5f) < 0.001f);
            storm.Radius = grownRadius;
            // Exercise persistence beyond the 0.8s caster gesture, then natural completion.
            storm.Advance(storm.TickSeconds);
            Check("Circle persists after the caster gesture and damage still ticks", fx.AreaIndicator == indicator && target.Health.DamageEvents == 2);
            storm.Advance(storm.ChannelSeconds);
            Check("Natural end immediately removes the area indicator", fx.AreaIndicator == null && !GodotObject.IsInstanceValid(indicator));
            storm.ResetCombat(); Check("Cancellation fixture begins", storm.TryBegin());
            indicator = fx.AreaIndicator!; storm.Abort();
            Check("Cancellation frees the indicator", fx.AreaIndicator == null && !GodotObject.IsInstanceValid(indicator));
            storm.ResetCombat(); Check("Death fixture begins", storm.TryBegin());
            world.PlayerActor.Health.ApplyDamage(10000);
            Check("Player death clears the indicator", fx.AreaIndicator == null);
            world.ResetCombatWorld(); world.Waves.Stop(); world.Spawner.Running = false; world.Player.SetPhysicsProcess(false);
            storm.SetUnlocked(true); Check("Restart fixture begins", storm.TryBegin());
            world.ResetCombatWorld(); world.Waves.Stop(); world.Spawner.Running = false;
            Check("Run reset frees the circle and restores the original radius", fx.AreaIndicator == null && storm.Radius == baseRadius && !storm.Unlocked);
        }
        catch (Exception ex) { failure = ex.ToString(); GD.PushError("Storm reticle smoke: " + failure); }
        GetTree().Paused = false;
        if (GodotObject.IsInstanceValid(target)) target!.QueueFree();
        using (var file = FileAccess.Open("res://../manifests/storm-reticle-smoke.json", FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure,
                baseRadius, grownRadius, baseWidth, grownWidth, basePixels, grownPixels, renderedFrames = Engine.GetFramesDrawn(),
                scope = "One brief rendered Korriban feature smoke; one review capture; no historical suites, benchmarks or archive hashing." },
                new JsonSerializerOptions { WriteIndented = true }));
        GD.Print(failure == null ? "STORM_RETICLE_SMOKE_PASSED" : "STORM_RETICLE_SMOKE_FAILED");
        GetTree().Quit(failure == null ? 0 : 1);
    }
}
