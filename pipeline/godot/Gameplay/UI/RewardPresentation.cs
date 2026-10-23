using Godot;
using System;
using System.Collections.Generic;

namespace SwtorSurvivors.UI;

/// <summary>Serialized announcements, original level-popup pixels and authored short transitions.</summary>
public partial class RewardPresentation : CanvasLayer
{
    public GameWorld World { get; set; } = null!;
    public BossHealthDisplay BossDisplay { get; private set; } = null!;
    public bool LevelBannerVisible => _level.Visible;
    public int BannersShown { get; private set; }
    private Control _root = null!, _level = null!, _notice = null!;
    private Label _levelNumber = null!, _noticeTitle = null!, _noticeBody = null!, _noticeDetail = null!;
    private double _levelAge, _noticeAge;
    private bool _levelActive;
    private readonly Queue<(string title, string body, string detail, string cue)> _queue = new();
    private readonly List<Node3D> _bursts = new();

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; Layer = 5;
        _root = new Control { Theme = SwtorSkin.MakeTheme(), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_root); _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        BossDisplay = new BossHealthDisplay { Encounter = World.Waves!.Encounter, Size = new Vector2(600, 90) }; _root.AddChild(BossDisplay);
        _level = new Control { Size = new Vector2(520, 150), MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false }; _root.AddChild(_level);
        Art(_level, "levelup_1", new Vector2(62, 20), new Vector2(397, 105));
        // Original GFX places the flare over the frame, centered on the level icon.
        Art(_level, "levelup_4", new Vector2(-104, 9), new Vector2(442, 104));
        Art(_level, "levelup_7", new Vector2(98.35f, 52.85f), new Vector2(40, 47));
        var heading = Text(_level, "SURVIVOR Has Reached:", 18, new Vector2(151, 48), new Vector2(278, 22));
        heading.HorizontalAlignment = HorizontalAlignment.Left;
        heading.AddThemeFontOverride("font", GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));
        heading.AddThemeColorOverride("font_color", new Color("e6e0c8"));
        _levelNumber = Text(_level, "", 20, new Vector2(159, 70), new Vector2(270, 26));
        _levelNumber.HorizontalAlignment = HorizontalAlignment.Left;
        _levelNumber.AddThemeFontOverride("font", GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf"));
        _levelNumber.AddThemeColorOverride("font_color", new Color("e6e0c8"));
        _notice = new Control { Size = new Vector2(600, 110), MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false }; _root.AddChild(_notice);
        var chrome = new NinePatchRect { Texture = SwtorSkin.Slice("bwaui_tooltip_1"), PatchMarginLeft = 8, PatchMarginRight = 8,
            PatchMarginTop = 12, PatchMarginBottom = 12, Size = _notice.Size, MouseFilter = Control.MouseFilterEnum.Ignore }; _notice.AddChild(chrome);
        _noticeTitle = Text(_notice, "", 14, new Vector2(12, 10), new Vector2(576, 21));
        _noticeBody = Text(_notice, "", 22, new Vector2(12, 32), new Vector2(576, 34));
        _noticeDetail = Text(_notice, "", 15, new Vector2(12, 72), new Vector2(576, 24));
        World.Progression!.LevelReached += OnLevel;
        World.Progression.UpgradeApplied += OnUpgrade;
        World.Waves.Encounter.StateChanged += OnEncounter;
        _root.Resized += Layout; Layout();
    }
    private static void Art(Control parent, string name, Vector2 at, Vector2 size) => parent.AddChild(new TextureRect {
        Texture = GD.Load<Texture2D>($"res://Assets/Presentation/{name}.png"), Position = at, Size = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = Control.MouseFilterEnum.Ignore });
    private static Label Text(Control parent, string text, int size, Vector2 at, Vector2 bounds)
    {
        var label = SwtorSkin.Text(text, size, true); label.Position = at; label.Size = bounds;
        label.HorizontalAlignment = HorizontalAlignment.Center; label.MouseFilter = Control.MouseFilterEnum.Ignore; parent.AddChild(label); return label;
    }
    private void Layout()
    {
        float scale = Mathf.Min(1.25f, Mathf.Min((_root.Size.X - 24) / 600, _root.Size.Y / 800));
        scale = Math.Max(0.35f, scale);
        BossDisplay.Scale = Vector2.One * scale; BossDisplay.Position = new Vector2((_root.Size.X - 600*scale)/2, 80*scale);
        _level.Scale = Vector2.One * scale; _level.Position = new Vector2((_root.Size.X-520*scale)/2, _root.Size.Y * 0.27f);
        _notice.Scale = Vector2.One * scale; _notice.Position = new Vector2((_root.Size.X-600*scale)/2, 185*scale);
    }
    private void OnLevel(int level)
    {
        _levelNumber.Text = $"Level {level}"; _levelAge = 0; _levelActive = true; BannersShown++;
        _level.Visible = true; _notice.Visible = false;
        World.Audio.PlayCue("level"); Burst(World.PlayerActor.GlobalPosition, SwtorSkin.Gold);
    }
    private void OnUpgrade(UpgradeDefinition upgrade) => World.Audio.PlayCue("upgrade_" + upgrade.Rarity.ToString().ToLowerInvariant());
    private void OnEncounter(EncounterState state)
    {
        var encounter = World.Waves!.Encounter;
        if (state == EncounterState.Introducing)
            // The persistent boss health frame already introduces the encounter.
            World.Audio.PlayCue("boss_start");
        else if (state == EncounterState.Defeated)
        {
            _queue.Enqueue((encounter.Boss!.RewardTier>=2?"BOSS DEFEATED":"ELITE DEFEATED", encounter.Definition.Title, $"{encounter.Boss.ExperienceReward} XP RELEASED  ·  COLLECT YOUR REWARD", "boss_victory"));
            Burst(encounter.Boss.GlobalPosition, SwtorSkin.Gold);
        }
        else if (state is EncounterState.Failed or EncounterState.Scheduled) Clear();
    }
    private void Burst(Vector3 point, Color color)
    {
        var burst = new RewardRing { Tint = color }; World.AddChild(burst); burst.GlobalPosition = point + Vector3.Up * 0.008f;
        _bursts.RemoveAll(b => !GodotObject.IsInstanceValid(b)); _bursts.Add(burst);
    }
    public override void _Process(double delta)
    {
        if (_levelActive)
        {
            _levelAge += delta;
            double duration = Math.Max(0.2, World.Progression!.LevelAnnouncementSeconds);
            float alpha = (float)Math.Clamp(Math.Min(_levelAge/0.16, (duration-_levelAge)/0.25), 0, 1);
            _level.Modulate = new Color(1, 1, 1, alpha);
            if (_levelAge >= duration || World.Progression.PresentationPhase != ProgressionPresentationPhase.Announcing)
            { _levelActive = false; _level.Visible = false; }
        }
        if (World.Progression!.Selecting) { _notice.Visible = false; return; }
        if (_noticeAge <= 0 && _queue.TryDequeue(out var item))
        {
            _noticeTitle.Text = item.title; _noticeBody.Text = item.body; _noticeDetail.Text = item.detail;
            _noticeAge = 2.2; World.Audio.PlayCue(item.cue);
        }
        if (_noticeAge <= 0) { _notice.Visible = false; return; }
        _noticeAge -= delta; _notice.Visible = true;
        float opacity = (float)Math.Clamp(Math.Min((2.2-_noticeAge)/0.15, _noticeAge/0.3), 0, 1);
        _notice.Modulate = new Color(1, 1, 1, opacity);
    }
    public void Clear()
    {
        _queue.Clear(); _noticeAge = 0; _levelAge = 0; _levelActive = false;
        if (_level != null) _level.Visible = false;
        if (_notice != null) _notice.Visible = false;
        if (BossDisplay != null) BossDisplay.Visible = false;
        foreach (var burst in _bursts) if (GodotObject.IsInstanceValid(burst)) burst.QueueFree(); _bursts.Clear();
    }
    public void Announce(string title,string body,string detail="",string cue="") => _queue.Enqueue((title,body,detail,cue));
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(World.Progression)) { World.Progression!.LevelReached -= OnLevel; World.Progression.UpgradeApplied -= OnUpgrade; }
        if (GodotObject.IsInstanceValid(World.Waves)) World.Waves!.Encounter.StateChanged -= OnEncounter;
        Clear();
    }
}

public partial class RewardRing : Node3D
{
    public Color Tint { get; set; } = Colors.White;
    private StandardMaterial3D _material = null!;
    private double _age;
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha, DisableFog = true, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            AlbedoTexture = GD.Load<Texture2D>("res://Assets/Presentation/loot_ring.png"), AlbedoColor = Tint };
        AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = Vector2.One * 0.1f }, MaterialOverride = _material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }
    public override void _Process(double delta)
    {
        _age += delta; float t = (float)_age / 0.8f;
        if (t >= 1) { QueueFree(); return; }
        Scale = Vector3.One * (1 + 4*t); _material.AlbedoColor = new Color(Tint, 1-t);
    }
}
