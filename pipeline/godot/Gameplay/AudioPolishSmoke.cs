using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Brief audio lifecycle check: actual Shock/Storm presentation and unchanged damage path.</summary>
public partial class AudioPolishSmoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool ok)
    { if (!ok) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("Audio polish: " + name); }
    private async Task Frames(int n)
    { for (int i=0; i<n; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    public async void Run(GameWorld world)
    {
        ProcessMode = ProcessModeEnum.Always; string? failure = null;
        try
        {
            world.ResetCombatWorld(); world.Waves!.SetPhysicsProcess(false); world.Spawner!.Running = false;
            world.Abilities.Abort(); world.Abilities.SetPhysicsProcess(false); world.Player.SetPhysicsProcess(false);
            var target = GD.Load<PackedScene>("res://Gameplay/StationaryEnemy.tscn").Instantiate<StationaryEnemy>();
            target.GetNode<Health>("Health").Maximum = 10000; world.AddChild(target);
            target.GlobalPosition = world.PlayerActor.GlobalPosition + Vector3.Right * 0.16f;
            await Frames(3);
            var shock = world.Abilities.Find("shock")!; var storm = world.Abilities.Find("storm")!;
            var shockFx = (OriginalAbilityPresentation)shock.Presentation;
            var stormFx = (OriginalAbilityPresentation)storm.Presentation;
            shock.SetUnlocked(true); storm.SetUnlocked(true);
            Check("Soft XP chime initializes and remains rate limited", world.Audio.PlayCue("pickup") && !world.Audio.PlayCue("pickup"));
            Check("Shock starts through the actual ability", shock.TryBegin());
            shock.Advance(0.13);
            Check("Shock plays both original buildup/impact layers and cast", shockFx.AudioVoicesStarted == 5 && shock.DamageEvents == 1);
            Check("Shock damage amount is unchanged", Mathf.IsEqualApprox(target.Health.Maximum - target.Health.Current, shock.DamagePerTick));
            shock.Advance(0.30); await Frames(22);
            Check("Shock impact survives the short casting recovery", shock.State == LightningState.Cooldown && shockFx.PlayingAudioVoices > 0);
            await Frames(80);
            Check("Shock one-shots expire naturally", shockFx.PlayingAudioVoices == 0);
            Check("Storm starts through the actual ability", storm.TryBegin()); storm.Advance(0.31);
            Check("Storm keeps its sustained bed and electrical layer", stormFx.LoopingAudioVoices == 2);
            // Exercise slot reuse faster than the real 0.8-second tick, without adding damage.
            for (int i=0; i<10; i++) { stormFx.Impact(storm.GroundCenter); await Frames(6); }
            Check("Repeated storm impacts never replace either channel loop", stormFx.LoopingAudioVoices == 2);
            storm.Advance(4.01);
            Check("Storm finishes channel with normal gameplay ticks and no stale loops", storm.DamageEvents > 0 && stormFx.LoopingAudioVoices == 0);
            storm.Advance(0.2); await Frames(3);
            Check("Storm retains finite thunder tails after visual completion", stormFx.PlayingAudioVoices > 0);
            world.PlayerActor.Health.ApplyDamage(world.PlayerActor.Health.Maximum + 1); await Frames(2);
            Check("Death clears pickup, completed tails and all channel audio", world.Audio.PlayingVoices == 0 && shockFx.PlayingAudioVoices == 0 && stormFx.PlayingAudioVoices == 0);
            world.ResetCombatWorld(); world.Waves.SetPhysicsProcess(false); await Frames(2);
            Check("Restart leaves no stale audio", world.Audio.PlayingVoices == 0 && stormFx.LoopingAudioVoices == 0);
            target.QueueFree();
        }
        catch (Exception e) { failure = e.ToString(); GD.PushError(failure); }
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/audio-polish-smoke.json"),
            JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure }, new JsonSerializerOptions { WriteIndented = true }));
        GetTree().Paused = false; GetTree().Quit(failure == null ? 0 : 1);
    }
}
