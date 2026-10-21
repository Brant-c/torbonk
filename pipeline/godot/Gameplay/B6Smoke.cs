using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Single brief B6 session; exercises real spawned rigs and encounter lifecycle.</summary>
public partial class B6Smoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool pass)
    { if (!pass) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("B6: " + name); }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    public async void Run(GameWorld world)
    {
        string? failure = null;
        try
        {
            world.ResetCombatWorld(); world.Waves!.SetPhysicsProcess(false); world.Abilities.Abort();
            world.Abilities.SetPhysicsProcess(false); world.Progression!.ProcessMode = ProcessModeEnum.Disabled;
            foreach (var ability in world.Abilities.Abilities) ability.SetPhysicsProcess(false);
            world.Player.SetPhysicsProcess(false);
            world.PlayerActor.Health.Maximum = 10000; world.PlayerActor.Health.Restore();
            var spawner = world.Spawner!; spawner.ActiveLimit = 100;
            Check("Player saber uses original RightWeapon and initializes red blade", world.PlayerSaber.BladeInitialized && world.PlayerSaber.Socket.BoneName == "RightWeapon");
            Transform3D saberBefore = world.PlayerSaber.GlobalTransform;
            world.Player.Presentation.TryBeginGesture(this, "LightningLoop");
            await Frames(20);
            Check("Saber follows animated weapon socket", !world.PlayerSaber.GlobalTransform.IsEqualApprox(saberBefore));
            world.Player.Presentation.ReleaseGesture(this);
            foreach (string type in new[] { "Broodling", "Shyrack", "CryptLooter", "Broodmatron" })
            {
                bool spawned = false;
                for (int attempt = 0; attempt < 6 && !spawned; attempt++)
                { spawned = spawner.TrySpawn(GD.Load<PackedScene>($"res://Gameplay/Enemies/{type}.tscn")); if (!spawned) await Frames(1); }
                Check(type + " safely spawns", spawned);
                var actor = spawner.Enemies.Last(); var start = actor.GlobalPosition;
                Check(type + " loads its own complete animation adapter", actor.Visual != null && actor.Visual.Animator.HasAnimation("Attack") && actor.Visual.Animator.HasAnimation("Death"));
                await Frames(65);
                Check(type + " moves on Korriban terrain", actor.GlobalPosition.DistanceTo(start) > 0.015f);
                int eventsBefore = actor.Health.DamageEvents;
                long flytextBefore = world.Flytext.CreatedEntries;
                actor.Health.ApplyDamage(1, DamageType.Energy); await Frames(2);
                Check(type + " receives normal damage events", actor.Health.DamageEvents == eventsBefore + 1);
                Check(type + " publishes damage to SWTOR flytext", world.Flytext.CreatedEntries > flytextBefore);
                actor.Visual!.Attack(); await Frames(8);
                if (type == "CryptLooter")
                {
                    Check("Looter electroblade uses original animated socket", actor.Visual.Skeleton.GetNodeOrNull<BoneAttachment3D>("B6WeaponSocket")?.BoneName == "RightWeapon");
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../generated/b6/gameplay-review.png"));
                }
                if (type == "Broodmatron")
                {
                    var boss = actor.GetNode<BossEncounter>("BossEncounter");
                    // Move the target to existing safe spawn terrain within encounter reach.
                    world.Player.GlobalPosition = actor.GlobalPosition + Vector3.Right * 0.2f;
                    for (int tick = 0; tick < 300 && boss.Strikes == 0; tick++) await Frames(1);
                    Check("Boss enters telegraph and resolves one strike", boss.Telegraphs > 0 && boss.Strikes > 0);
                    await Frames(65);
                    Check("Boss exits recovery to pursuit", boss.Phase == BossPhase.Pursuit);
                }
                int defeatedBefore = spawner.Defeated;
                actor.Health.ApplyDamage(actor.Health.Maximum + 1); await Frames(3);
                Check(type + " dies through Health, defeat counter and original death clip", !actor.Alive && actor.State == EnemyState.Dead && actor.Visual.Dead && actor.Visual.State == "Death" && spawner.Defeated == defeatedBefore + 1);
                if (type == "Broodmatron") Check("Boss encounter ends on death", actor.GetNode<BossEncounter>("BossEncounter").Phase == BossPhase.Ended);
            }
            world.ResetCombatWorld();
            Check("Restart resets boss and wave state", !world.Waves.BossSpawned && world.Waves.Boss == null && world.Waves.Wave == 1 && spawner.ActiveCount == 0);
            Check("Density starts at 16 with configurable 75 cap", spawner.ActiveLimit == 16 && world.Waves.MaximumActiveLimit == 75);
            world.Waves.BossWave = 1; world.Waves.SetPhysicsProcess(true);
            for (int tick = 0; tick < 120 && !world.Waves.BossSpawned; tick++) await Frames(1);
            Check("WaveDirector spawns the boss at its configured milestone", world.Waves.BossSpawned && world.Waves.Boss != null);
            world.PlayerActor.Health.ApplyDamage(world.PlayerActor.Health.Maximum + 1);
            Check("Player death exits the boss encounter", world.Waves.Boss!.GetNode<BossEncounter>("BossEncounter").Phase == BossPhase.Ended && !world.Waves.Running);
        }
        catch (Exception error) { failure = error.ToString(); GD.PushError(failure); }
        string path = ProjectSettings.GlobalizePath("res://../manifests/b6-smoke.json");
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure }, new JsonSerializerOptions { WriteIndented = true }));
        GetTree().Quit(failure == null ? 0 : 1);
    }
}
