using Godot;
using System;
using System.Globalization;

namespace SwtorSurvivors.UI;

/// <summary>Run totals from resolved health changes; rates use the same clock as run status.</summary>
public partial class CombatMeter : Control
{
    public GameWorld World { get; set; } = null!;
    public double OverallDamage { get; private set; }
    public double OverallHealing { get; private set; }
    public double DamageTaken { get; private set; }
    private RunRecord _record = new();
    private double _saveIn;
    private bool _finished;
    private bool SaveEnabled => RunHistory.TestDirectory != null || !System.Linq.Enumerable.Any(OS.GetCmdlineUserArgs(), a => a.Contains("smoke") || a.Contains("validation"));
    public double Dps => World.Progression!.ElapsedSeconds > 0 ? OverallDamage / World.Progression.ElapsedSeconds : 0;
    public double Hps => World.Progression!.ElapsedSeconds > 0 ? OverallHealing / World.Progression.ElapsedSeconds : 0;
    private Label _damage=null!, _dps=null!, _healing=null!, _hps=null!;

    public override void _Ready()
    {
        Name="CombatMeter";
        Size=new Vector2(240,184);
        MouseFilter=MouseFilterEnum.Ignore;
        AddChild(new NinePatchRect { Texture=SwtorSkin.Slice("bwaui_standard_window_1"),
            Size=Size*2, Scale=Vector2.One*.5f,
            PatchMarginLeft=128, PatchMarginRight=184, PatchMarginTop=72, PatchMarginBottom=64,
            MouseFilter=MouseFilterEnum.Ignore });
        var heading=SwtorSkin.Text("COMBAT",14,true);
        heading.Position=new Vector2(15,11); heading.Size=new Vector2(210,20); AddChild(heading);
        var bold=GD.Load<FontFile>("res://Assets/UI/EurostileBold.ttf");
        Label Row(string title, float y, Color color)
        {
            var label=SwtorSkin.Text(title,14);
            label.Position=new Vector2(16,y); label.Size=new Vector2(110,22);
            label.VerticalAlignment=VerticalAlignment.Center; AddChild(label);
            var value=SwtorSkin.Text("0",16);
            value.Position=new Vector2(128,y); value.Size=new Vector2(96,22);
            value.HorizontalAlignment=HorizontalAlignment.Right; value.VerticalAlignment=VerticalAlignment.Center;
            value.AddThemeFontOverride("font",bold); value.AddThemeColorOverride("font_color",color);
            AddChild(value); return value;
        }
        // Recovered style guide: lightRed and mediumLightGreen.
        _damage=Row("Overall damage",44,new Color("df4e4e"));
        _dps=Row("DPS",70,new Color("df4e4e"));
        _healing=Row("Overall healing",96,new Color("33ff4e"));
        _hps=Row("HPS",122,new Color("33ff4e"));
        World.Events.Resolved+=Record;
    }

    private void Record(CombatEvent resolved)
    {
        if (!float.IsFinite(resolved.Change.Amount) || resolved.Change.Amount<=0) return;
        // This solo game has one friendly damage producer. Track incoming HP loss separately.
        if (resolved.Change.Kind==CombatEventKind.Damage && resolved.RecipientTeam==CombatTeam.Hostile)
            OverallDamage+=resolved.Change.Amount;
        else if (resolved.Change.Kind==CombatEventKind.Healing && resolved.RecipientId==World.PlayerActor.GetInstanceId())
            OverallHealing+=resolved.Change.Amount;
        else if (resolved.Change.Kind==CombatEventKind.Damage && resolved.RecipientId==World.PlayerActor.GetInstanceId())
            DamageTaken+=resolved.Change.Amount;
    }

    public void ResetRun()
    {
        _record=new RunRecord(); _finished=false; _saveIn=0;
        OverallDamage=OverallHealing=DamageTaken=0;
        Refresh();
    }

    public void FinishRun(string outcome)
    {
        if (_finished) return;
        Capture();
        _record.Outcome=World.Waves?.CoreComplete==true ? "Cleared" : !World.PlayerActor.Alive ? "Defeated" : outcome;
        if (SaveEnabled) RunHistory.Save(_record);
        _finished=true;
    }
    private void Capture()
    {
        _record.ClassId=World.RunConfiguration.AdvancedClassId;
        _record.MapId=World.RunConfiguration.MapId;
        _record.Difficulty=_record.MapId is "eternity_vault" or "soa_p3" ? World.RunConfiguration.OperationDifficulty : "";
        _record.Duration=World.Progression?.ElapsedSeconds ?? 0;
        _record.Level=World.Progression?.Level ?? 1;
        if (World.Waves?.CoreComplete==true) _record.Outcome="Cleared";
        _record.Damage=OverallDamage; _record.Healing=OverallHealing; _record.Taken=DamageTaken;
    }

    private static string Amount(double value) => value>=9_999_999.5
        ? (value/1_000_000).ToString("0.#",CultureInfo.InvariantCulture)+"M"
        : value.ToString("N0",CultureInfo.InvariantCulture);

    private void Refresh()
    {
        _damage.Text=Amount(OverallDamage); _dps.Text=Amount(Dps);
        _healing.Text=Amount(OverallHealing); _hps.Text=Amount(Hps);
    }
    public override void _Process(double delta)
    {
        Refresh();
        if (_finished) return;
        Capture();
        if (!World.PlayerActor.Alive) { FinishRun("Defeated"); return; }
        _saveIn-=delta;
        if (_saveIn<=0) { if (SaveEnabled) RunHistory.Save(_record); _saveIn=5; }
    }
    public override void _ExitTree()
    {
        // Cached totals remain usable even if sibling gameplay nodes have already left the tree.
        if (!_finished && SaveEnabled) RunHistory.Save(_record);
        if (GodotObject.IsInstanceValid(World.Events)) World.Events.Resolved-=Record;
    }
}
