using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace SwtorSurvivors;
public partial class NpcVocalSmoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool ok) { if (!ok) throw new Exception(name); _checks.Add(name); GD.Print(name); }
    private async Task Frames(int n) { for (int i=0;i<n;i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    public override async void _Ready()
    {
        string? failure = null;
        try
        {
            var audio = new AudioPresentation(); AddChild(audio);
            var target = new Combatant { Team = CombatTeam.Player };
            target.AddChild(new Health { Name = "Health" }); AddChild(target);
            EnemyAgent Cat()
            {
                var body = GD.Load<PackedScene>("res://Gameplay/Planets/EternityVault/IceCat.tscn").Instantiate<CharacterBody3D>();
                var cat = body.GetNode<EnemyAgent>("Actor"); cat.Target = target;
                AddChild(body); cat.SetPhysicsProcess(false); return cat;
            }
            var cat = Cat(); cat.Health.ApplyDamage(1); await Frames(6);
            var mixer = audio.GetNode<NpcVocalMixer>("NpcVocals");
            Check("Actual ice cat damage starts one hurt vocal", mixer.StartedVoices == 1 && mixer.PlayingVoices == 1);
            for(int i=0;i<5;i++)cat.Health.ApplyDamage(1);
            await Frames(6);
            Check("Rapid damage cannot chatter", mixer.StartedVoices == 1);
            cat.Health.ApplyDamage(1000);
            Check("Death replaces its hurt voice without changing death state", mixer.StartedVoices == 2 && mixer.PlayingVoices == 1 && cat.State == EnemyState.Dead);
            cat.Body.QueueFree(); await Frames(3);
            Check("Death tail survives corpse removal", mixer.PlayingVoices == 1);
            audio.Clear(); Check("Reset clears NPC voices", mixer.PlayingVoices == 0);
            cat = Cat(); cat.Health.ApplyDamage(1); cat.Health.ApplyDamage(1000); await Frames(6);
            Check("Same-frame lethal followup cancels queued hurt", mixer.StartedVoices == 3);
            audio.Clear();
            string clip = "res://Assets/NpcVocals/Manka/806132088.wav";
            var cats = new List<EnemyAgent>();
            for(int i=0;i<6;i++) { var c=Cat();cats.Add(c);mixer.Request(c,clip,true,-14);await Frames(10); }
            Check("Crowd vocals stay within three voices", mixer.PlayingVoices == 3);
            await Frames(190);
            Check("One-shots finish naturally", mixer.PlayingVoices == 0);
            audio.Clear();
            var far=Cat();far.Body.Position=Vector3.Right*2;
            Check("Distant enemies are suppressed", !mixer.Request(far,clip,false,-18));
            await Frames(190);
            Check("No stale playback after reset", mixer.PlayingVoices == 0);
        }
        catch(Exception e) {failure=e.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/manka-vocals-smoke.json"), JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
