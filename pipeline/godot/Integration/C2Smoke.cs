using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>Opt-in, single-session integration smoke. Does not run historic gameplay suites.</summary>
public partial class C2Smoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool passed)
    { if (!passed) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("C2: " + name); }
    private async Task Frames(int count = 2)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> ready)
    {
        for (int i = 0; i < 360 && !ready(); i++) await Frames(1);
        if (!ready()) throw new TimeoutException("C2 scene transition timed out");
        await Frames();
    }
    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (var child in root.GetChildren())
        { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private Button Button(Node root, string text) => Descendants(root).OfType<Button>().Single(b => b.Text.Contains(text));
    private async Task Press(Node root, string text)
    {
        var button = Button(root, text);
        if (button.Disabled) throw new InvalidOperationException("Disabled button: " + text);
        button.EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(GetTree().CreateTimer(.4), SceneTreeTimer.SignalName.Timeout); await Frames();
    }

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string? failure = null;
        try
        {
            await Frames(5);
            var menu = (MainMenu)GetTree().CurrentScene;
            Check("Default entry loads main menu with keyboard focus", Button(menu, "New Run").HasFocus());
            var catalog = FrontendCatalog.Load();
            int supported = 0;
            foreach (var cls in catalog.Classes)
                foreach (var advanced in cls.AdvancedClasses)
                    foreach (var map in catalog.Maps)
                        if (SelectionGameplayAdapter.TryResolve(new SelectionConfiguration {
                            FactionId = cls.FactionId, BaseClassId = cls.Id, AdvancedClassId = advanced.Id,
                            MapId = map.Id }, out _, out _)) supported++;
            Check("All 16 advanced classes resolve to exactly one supported Korriban combination", supported == 1);
            var altered = new SelectionConfiguration(); altered.Appearance.HeadId = "unavailable_head";
            Check("Altered appearance and unknown schema are blocked",
                !SelectionGameplayAdapter.TryResolve(altered, out _, out _)
                && !SelectionGameplayAdapter.TryResolve(new SelectionConfiguration { SchemaVersion = 2 }, out _, out _));
            var forged = JsonSerializer.Deserialize<SelectionConfiguration>(
                "{\"MapId\":\"tython\",\"Playable\":true,\"PlayableStatus\":\"ready\"}")!;
            Check("Forged playable flag cannot cross launch boundary",
                !GameNavigation.TryStart(menu, forged, out _) && GetTree().CurrentScene == menu);

            await Press(menu, "New Run"); await Press(menu, "Sith Empire");
            await Press(menu, "Sith Inquisitor"); await Press(menu, "Continue"); await Press(menu, "Sorcerer");
            Check("Sorcerer selection loads its isolated 3D preview",
                Descendants(menu).OfType<CharacterPreview>().Count() == 1
                && Descendants(menu).OfType<Skeleton3D>().Any());
            await Press(menu, "Continue");
            Check("Destination screen offers only Korriban", catalog.Maps.Length == 1
                && catalog.Maps[0].Id == "korriban"
                && !Descendants(menu).OfType<Button>().Any(b => b.Text.Contains("Tython")));
            await Press(menu, "Korriban"); await Press(menu, "Continue");
            var valid = menu.CurrentConfiguration;
            menu.RestoreConfiguration(new SelectionConfiguration { BaseClassId = "sith_warrior", AdvancedClassId = "juggernaut" });
            await Frames();
            Check("Unsupported class remains previewable with Play disabled and no Sorcerer model",
                Button(menu, "Play").Disabled && !Descendants(menu).OfType<Skeleton3D>().Any());
            menu.RestoreConfiguration(valid); await Frames();
            Check("Supported Ready screen enables Play", !Button(menu, "Play").Disabled);
            var oldPreview = Descendants(menu).OfType<CharacterPreview>().Single();
            var menuAudio = Descendants(menu).OfType<AudioStreamPlayer>().ToArray();
            await Press(menu, "Play");
            await Until(() => GetTree().CurrentScene is GameWorld { Progression: not null, Hud: not null, Rewards: not null });
            var world = (GameWorld)GetTree().CurrentScene;
            Check("Play opens exactly one configured Korriban/Sorcerer world in the same process",
                GetTree().Root.GetChildren().OfType<GameWorld>().Count() == 1
                && world.RunConfiguration.MapId == "korriban" && world.RunConfiguration.AdvancedClassId == "sorcerer"
                && world.Initialized && world.PlayerSaber.BladeInitialized);
            Check("Menu preview and menu audio are released on Play", !GodotObject.IsInstanceValid(oldPreview)
                && menuAudio.All(a => !GodotObject.IsInstanceValid(a)) && !GodotObject.IsInstanceValid(menu));
            await Frames(90);
            Check("Normal B7 world runs with original HUD, abilities and current spawn tuning",
                world.Waves!.Running && world.Spawner!.Spawned > 0 && world.Hud!.ActionSlotCount == 12
                && world.Abilities.Abilities.Count == 3 && world.InitialActiveLimit == 8 && world.InitialSpawnInterval == 0.75f);
            world.Progression!.AddExperience(12); await Frames();
            Check("Existing level-up presentation still pauses the run", GetTree().Paused && world.Rewards!.LevelBannerVisible);
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.R, Pressed = true }); await Frames();
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.R, Pressed = false }); await Frames();
            Check("R restarts the same run without reopening selection", GetTree().CurrentScene == world
                && !GetTree().Paused && world.Progression.Level == 1 && world.Waves.Wave == 1);
            // Seed live encounter/UI/audio state solely to observe scene teardown, not boss balance.
            world.Waves.SetPhysicsProcess(false);
            for (int i = 0; i < 8 && !world.Waves.Encounter.Spawned; i++)
            { world.Waves.Encounter.Tick(1.1, true); await Frames(); }
            Check("A live boss encounter is present for cleanup", world.Waves.Encounter.Spawned);
            world.Progression.AddExperience(12); await Frames();
            world.Audio.PlayCue("level");
            var oldNodes = Descendants(world).Where(n => n is AudioStreamPlayer or AudioStreamPlayer3D
                or EnemyAgent or ExperiencePickup or UI.SurvivorsHud or UI.RewardPresentation).ToArray();
            Check("Return action clears a paused upgrade presentation", GetTree().Paused);
            world.ReturnToMainMenu();
            await Until(() => GetTree().CurrentScene is MainMenu);
            menu = (MainMenu)GetTree().CurrentScene;
            Check("Return frees world, boss, effects, HUD and audio nodes", !GodotObject.IsInstanceValid(world)
                && oldNodes.All(n => !GodotObject.IsInstanceValid(n)) && AudioPresentation.Find(menu) == null);
            Check("Menu restores input, focus and last valid selection", !GetTree().Paused
                && Input.MouseMode == Input.MouseModeEnum.Visible && Button(menu, "New Run").HasFocus()
                && menu.CurrentConfiguration.ToJson() == valid.ToJson());
            await Press(menu, "New Run"); await Press(menu, "Sith Empire");
            await Press(menu, "Sith Inquisitor"); await Press(menu, "Continue"); await Press(menu, "Sorcerer");
            await Press(menu, "Continue"); await Press(menu, "Korriban"); await Press(menu, "Continue");
            await Press(menu, "Play");
            await Until(() => GetTree().CurrentScene is GameWorld { Hud: not null, Rewards: not null });
            world = (GameWorld)GetTree().CurrentScene;
            Check("Next run starts fresh without duplicate worlds or audio services", world.Progression!.Level == 1
                && world.Spawner!.Defeated == 0 && world.Waves!.Wave == 1
                && GetTree().GetNodesInGroup(AudioPresentation.Group).Count == 1);
            world.PlayerActor.Health.ApplyDamage(10000); await Frames();
            Check("Death retains restart and exposes Return to Main Menu", !world.PlayerActor.Alive
                && Button(world.Hud!, "RETURN TO MAIN MENU").IsVisibleInTree());
            await Press(world.Hud!, "RETURN TO MAIN MENU");
            await Until(() => GetTree().CurrentScene is MainMenu);
            Check("Game-over Return button restores a clean main menu", !GodotObject.IsInstanceValid(world) && !GetTree().Paused);
        }
        catch (Exception exception) { failure = exception.ToString(); GD.PushError(failure); }
        using var output = Godot.FileAccess.Open("res://../manifests/c2-smoke.json", Godot.FileAccess.ModeFlags.Write);
        output.StoreString(JsonSerializer.Serialize(new { passed = failure == null, checks = _checks, failure },
            new JsonSerializerOptions { WriteIndented = true }));
        GetTree().Quit(failure == null ? 0 : 1);
    }
}
