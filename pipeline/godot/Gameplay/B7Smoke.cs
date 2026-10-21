using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One brief B7 session, exercising real progression and encounter lifecycle integration.</summary>
public partial class B7Smoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string label, bool pass)
    { if (!pass) throw new InvalidOperationException(label); _checks.Add(label); GD.Print("B7: " + label); }
    private async Task Frames(int count)
    { for (int i=0; i<count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> condition, int frames=240)
    { for (int i=0; i<frames && !condition(); i++) await Frames(1); if (!condition()) throw new TimeoutException("B7 presentation transition timed out"); await Frames(2); }
    private async Task Spawn(GameWorld world, string type)
    {
        bool success = false;
        for (int i=0; i<8 && !success; i++) { success = world.Spawner!.TrySpawn(GD.Load<PackedScene>($"res://Gameplay/Enemies/{type}.tscn")); if (!success) await Frames(1); }
        Check(type + " safely spawns for the focused presentation check", success);
    }
    public async void Run(GameWorld world)
    {
        ProcessMode = ProcessModeEnum.Always;
        string? failure = null;
        try
        {
            world.ResetCombatWorld(); world.Waves!.SetPhysicsProcess(false); world.Abilities.Abort(); world.Abilities.SetPhysicsProcess(false);
            foreach (var ability in world.Abilities.Abilities) ability.SetPhysicsProcess(false);
            world.Player.SetPhysicsProcess(false); world.PlayerActor.Health.Maximum = 10000; world.PlayerActor.Health.Restore();
            var progression = world.Progression!; var encounter = world.Waves.Encounter;
            await Frames(3);
            Check("Boss display initializes hidden before an encounter", !world.Rewards!.BossDisplay.Visible && encounter.State == EncounterState.Scheduled);
            await Spawn(world, "Broodling"); var enemy = world.Spawner!.Enemies.Last();
            enemy.Health.ApplyDamage(enemy.Health.Maximum + 1); await Frames(2);
            var pickup = progression.GetChildren().OfType<ExperiencePickup>().Single(); pickup.SetPhysicsProcess(false);
            Check("Enemy XP becomes a luminous 3D world mote", pickup.VisualInitialized && pickup.Value == 2 && pickup is Node3D);
            progression.Collect(pickup); progression.Collect(pickup);
            Check("Collection starts its trail and awards XP exactly once", pickup.Collecting && progression.Experience == 2 && progression.PickupCount == 0);
            await Frames(18);
            Check("Collected mote cleans up after its short effect", !GodotObject.IsInstanceValid(pickup));

            progression.AddExperience(26); await Frames(2);
            Check("Level banner precedes choices and pauses combat", progression.Level == 2 && GetTree().Paused && world.Rewards.LevelBannerVisible && !world.Hud!.SelectionVisible && !progression.Choose(0));
            await Until(() => progression.CanChoose);
            Check("Three unchanged upgrade choices follow the banner", world.Hud!.SelectionVisible && world.Hud.VisibleChoiceCount == 3 && !world.Rewards.LevelBannerVisible);
            Check("Selection applies once and shows a brief confirmation", progression.Choose(0) && !progression.Choose(0) && progression.PresentationPhase == ProgressionPresentationPhase.Confirming && GetTree().Paused);
            await Until(() => progression.Level == 3);
            Check("Overflow XP serializes a second banner without an overlapping modal", world.Rewards.BannersShown == 2 && world.Rewards.LevelBannerVisible && !world.Hud.SelectionVisible);
            await Until(() => progression.CanChoose); progression.Choose(0);
            await Until(() => !progression.Selecting);
            Check("Queued upgrades resume combat and preserve XP remainder", !GetTree().Paused && progression.Experience == 0 && progression.Ranks.Values.Sum() == 2);
            progression.AddExperience(progression.RequiredExperience); await Frames(2); world.ResetCombatWorld(); await Frames(3);
            Check("Restart cancels pending banners and choices", progression.Level == 1 && !progression.Selecting && !GetTree().Paused && !world.Rewards.LevelBannerVisible);
            world.Waves.SetPhysicsProcess(false); world.Player.SetPhysicsProcess(false);

            await Spawn(world, "Broodling"); var existing = world.Spawner.Enemies.Last(); existing.SetPhysicsProcess(false);
            world.Waves.BossWave = 1; world.Waves.SetPhysicsProcess(true);
            await Until(() => encounter.State == EncounterState.Introducing);
            Check("Scheduled boss introduces with health UI and retains existing enemies", world.Rewards.BossDisplay.Visible && existing.Alive && world.Spawner.Enemies.Contains(existing));
            int bossSpawnCount = world.Spawner.Spawned;
            encounter.Tick(0, true);
            Check("Repeated activation cannot duplicate the boss", world.Spawner.Spawned == bossSpawnCount);
            await Until(() => encounter.State == EncounterState.Active);
            var boss = encounter.Boss!; boss.SetPhysicsProcess(false); world.Waves.SetPhysicsProcess(false);
            boss.Health.ApplyDamage(100); await Frames(6);
            Check("Dedicated boss bar follows real health with smooth depletion", world.Rewards.BossDisplay.DisplayedFraction < 1 && boss.Health.Current == boss.Health.Maximum - 100);
            GetWindow().Size = new Vector2I(1280, 720); await Frames(3);
            Check("Boss UI stays within the smaller viewport", world.Rewards.BossDisplay.GetGlobalRect().End.X <= GetViewport().GetVisibleRect().Size.X && world.Rewards.BossDisplay.GetGlobalRect().Position.X >= 0);
            GetWindow().Size = new Vector2I(1920, 1080); await Frames(3);
            Check("Boss UI scales within the 1080p viewport", world.Rewards.BossDisplay.GetGlobalRect().End.X <= GetViewport().GetVisibleRect().Size.X);
            GetWindow().Size = new Vector2I(1000, 800); await Frames(3);
            world.Player.GlobalPosition = boss.GlobalPosition + Vector3.Right * 0.15f;
            var attack = boss.GetNode<BossEncounter>("BossEncounter"); attack.Tick(3.1);
            Check("Burrow Eruption retains radius, damage and windup", attack.Phase == BossPhase.Telegraph && attack.Radius == 0.30f && attack.Damage == 18 && attack.WindupSeconds == 1.35f);
            int strikes = attack.Strikes; attack.Tick(1.34);
            Check("Warning does not strike early", attack.Strikes == strikes);
            attack.Tick(0.02); await Frames(3);
            Check("Impact resolves exactly once after the warning", attack.Strikes == strikes+1 && attack.Phase == BossPhase.Recovery);
            int rewards = progression.PickupCount;
            boss.Health.ApplyDamage(boss.Health.Maximum + 1);
            // Inspect the actual drop before the nearby player's magnet can collect it.
            foreach (var drop in progression.GetChildren().OfType<ExperiencePickup>()) drop.SetPhysicsProcess(false);
            await Frames(3);
            Check("Boss defeat ends attack and hides its bar", encounter.State == EncounterState.Defeated && attack.Phase == BossPhase.Ended && !world.Rewards.BossDisplay.Visible);
            Check("Boss reward uses the existing single 80 XP drop path", progression.PickupCount == rewards+1 && progression.GetChildren().OfType<ExperiencePickup>().Count(p => p.Value == 80) == 1);
            encounter.Tick(100, true);
            Check("Defeated encounter cannot spawn another boss", encounter.State == EncounterState.Defeated && encounter.Boss == boss);

            world.ResetCombatWorld(); world.Player.SetPhysicsProcess(false); world.Waves.SetPhysicsProcess(true);
            await Until(() => encounter.InEncounter);
            for (int i=0; i<35; i++) world.Audio.PlayCue("pickup");
            Check("Pickup audio is rate limited and priority cues retain a voice", world.Audio.SuppressedCues > 0 && world.Audio.PlayCue("upgrade_artifact") && world.Audio.PlayingVoices <= 12);
            Check("All rarity confirmation cues initialize", world.Audio.PlayCue("upgrade_premium") && world.Audio.PlayCue("upgrade_prototype"));
            progression.AddExperience(progression.RequiredExperience); await Frames(2);
            world.PlayerActor.Health.ApplyDamage(world.PlayerActor.Health.Maximum + 1); await Frames(3);
            Check("Player death fails encounter and clears paused reward presentation and audio", encounter.State == EncounterState.Failed && !world.Rewards.BossDisplay.Visible && !world.Rewards.LevelBannerVisible && !progression.Selecting && !GetTree().Paused && world.Audio.PlayingVoices == 0);
            world.ResetCombatWorld(); world.Waves.SetPhysicsProcess(false); await Frames(3);
            Check("Restart clears all encounter state and pickups", encounter.State == EncounterState.Scheduled && encounter.Boss == null && !encounter.Spawned && world.Spawner.ActiveCount == 0 && progression.PickupCount == 0);
        }
        catch (Exception error) { failure = error.ToString(); GD.PushError(failure); }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/b7-smoke.json"), JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure }, new JsonSerializerOptions { WriteIndented = true }));
        GetTree().Paused = false; GetTree().Quit(failure == null ? 0 : 1);
    }
}
