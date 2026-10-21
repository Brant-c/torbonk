using Godot;
using System;

namespace SwtorSurvivors.UI;

/// <summary>Dedicated boss composition from original SWTOR window chrome and clipped health pixels.</summary>
public partial class BossHealthDisplay : Control
{
    public BossEncounterController Encounter { get; set; } = null!;
    private SwtorBar _health = null!, _loss = null!;
    private Label _name = null!, _attack = null!;
    private float _shown = 1, _delayed = 1, _last = 1;
    private double _reveal, _lossHold;
    private bool _wasVisible;
    public float DisplayedFraction => _shown;
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        var chrome = new NinePatchRect { Texture = SwtorSkin.Slice("bwaui_tooltip_1"),
            PatchMarginLeft = 8, PatchMarginRight = 8, PatchMarginTop = 12, PatchMarginBottom = 12, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(chrome); chrome.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _name = SwtorSkin.Text("", 20, true); _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.Position = new Vector2(16, 6); _name.Size = new Vector2(568, 28); AddChild(_name);
        _loss = new SwtorBar { DrawFrame = false, FillHeight = 17, MinimumHeight = 28, FillSlice = "bwaui_bars_21", Modulate = new Color(1, 0.72f, 0.4f, 0.8f) };
        _loss.Position = new Vector2(18, 34); _loss.Size = new Vector2(564, 28); AddChild(_loss);
        _health = new SwtorBar { DrawFrame = true, FillHeight = 17, MinimumHeight = 28, LabelSize = 15, FillSlice = "bwaui_bars_21" };
        _health.Position = _loss.Position; _health.Size = _loss.Size; AddChild(_health);
        _attack = SwtorSkin.Text("", 13, true); _attack.HorizontalAlignment = HorizontalAlignment.Center;
        _attack.Position = new Vector2(12, 63); _attack.Size = new Vector2(576, 22); AddChild(_attack);
        _attack.AddThemeColorOverride("font_color", SwtorSkin.TextColor); Visible = false;
    }
    public override void _Process(double delta)
    {
        bool active = Encounter.InEncounter && GodotObject.IsInstanceValid(Encounter.Boss) && Encounter.Boss!.Alive;
        Visible = active;
        if (!active) { _wasVisible = false; return; }
        var boss = Encounter.Boss!;
        float value = Math.Clamp(boss.Health.Current / boss.Health.Maximum, 0, 1);
        if (!_wasVisible) { _shown = _delayed = _last = value; _reveal = 0; _wasVisible = true; }
        _reveal += delta;
        if (value < _last) _lossHold = 0.3;
        _last = value; _lossHold -= delta;
        _shown = Mathf.Lerp(_shown, value, 1 - Mathf.Exp(-(float)delta * 14));
        if (_lossHold <= 0) _delayed = Mathf.MoveToward(_delayed, value, (float)delta * 0.6f);
        _delayed = Math.Max(_shown, _delayed);
        Modulate = new Color(1, 1, 1, Mathf.Clamp((float)_reveal / 0.25f, 0, 1));
        _health.UpdateValue(_shown, 1, $"{boss.Health.Current:0.##} / {boss.Health.Maximum:0.##}   ·   {value * 100:0.0}%");
        _loss.UpdateValue(_delayed, 1, ""); _name.Text = Encounter.Definition.Title.ToUpperInvariant();
        _attack.Text = boss.GetNodeOrNull<BossEncounter>("BossEncounter")?.Phase == BossPhase.Telegraph ? "BURROW ERUPTION  ·  MOVE OUT OF THE AMBER RING" : "CHAMPION";
    }
}
