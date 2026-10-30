using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors.Frontend;

/// <summary>Small local testing log, including unfinished attempts.</summary>
public partial class RunHistoryScreen : Control
{
    public Action? Closed { get; set; }
    private VBoxContainer _rows = null!;
    private OptionButton _class = null!, _encounter = null!, _sort = null!;
    private readonly System.Collections.Generic.IReadOnlyList<RunRecord> _records = RunHistory.Load();
    public override void _Ready()
    {
        var title = FrontendSkin.Heading("LEADERBOARDS  /  RUN HISTORY", 28);
        title.Position = new Vector2(0,-64); AddChild(title);
        var close = FrontendSkin.Button("Main Menu", () => Closed?.Invoke());
        close.Position = new Vector2(1228,-64); close.Size = new Vector2(220,52); AddChild(close);
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation",12);
        AddChild(FrontendSkin.TitledPanel("Local class testing",body,Vector2.Zero,new Vector2(1448,650)));
        body.AddChild(FrontendSkin.Label("All attempts are kept, including short and unfinished runs. Rates use the full run duration.",18));
        var filters = new HBoxContainer(); body.AddChild(filters);
        _class = new OptionButton { CustomMinimumSize = new Vector2(280,44) };
        _class.AddItem("All classes");
        foreach(var id in _records.Select(r=>r.ClassId).Distinct().OrderBy(x=>x)) _class.AddItem(ClassName(id));
        filters.AddChild(_class);
        _encounter = new OptionButton { CustomMinimumSize = new Vector2(430,44) };
        _encounter.AddItem("All encounters");
        foreach(var name in _records.Select(r=>r.Encounter).Distinct().OrderBy(x=>x)) _encounter.AddItem(name);
        filters.AddChild(_encounter);
        _sort = new OptionButton { CustomMinimumSize = new Vector2(240,44) };
        foreach(var name in new[]{"Newest first","Highest DPS","Highest healing"}) _sort.AddItem(name);
        filters.AddChild(_sort);
        var scroll = new ScrollContainer { SizeFlagsVertical=SizeFlags.ExpandFill, SizeFlagsHorizontal=SizeFlags.ExpandFill, HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled };
        body.AddChild(scroll);
        _rows = new VBoxContainer { SizeFlagsHorizontal=SizeFlags.ExpandFill };
        _rows.AddThemeConstantOverride("separation",12); scroll.AddChild(_rows);
        _class.ItemSelected += _=>Refresh(); _encounter.ItemSelected += _=>Refresh(); _sort.ItemSelected += _=>Refresh();
        Refresh(); close.GrabFocus();
    }
    private static string ClassName(string id) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_',' '));
    private void Refresh()
    {
        foreach(var child in _rows.GetChildren()) { _rows.RemoveChild(child); child.QueueFree(); }
        var records = _records.Where(r=>(_class.Selected==0 || ClassName(r.ClassId)==_class.GetItemText(_class.Selected))
            && (_encounter.Selected==0 || r.Encounter==_encounter.GetItemText(_encounter.Selected)));
        records = _sort.Selected switch { 1=>records.OrderByDescending(r=>r.Dps), 2=>records.OrderByDescending(r=>r.Healing), _=>records.OrderByDescending(r=>r.Date) };
        foreach(var r in records)
        {
            var block = new VBoxContainer();
            block.AddChild(FrontendSkin.Heading($"{ClassName(r.ClassId)}  ·  {r.Raid} / {r.Encounter}  ·  {r.Difficulty}",20));
            block.AddChild(FrontendSkin.Label($"{r.Date.ToLocalTime():yyyy-MM-dd HH:mm:ss}  ·  {r.Outcome}  ·  Level {r.Level}  ·  {TimeSpan.FromSeconds(r.Duration):hh\\:mm\\:ss}",18));
            block.AddChild(FrontendSkin.Label($"DPS  {r.Dps:N1}     Damage  {r.Damage:N0}     Healing  {r.Healing:N0}     HPS  {r.Hps:N1}     Damage taken  {r.Taken:N0}",20));
            _rows.AddChild(block);
            _rows.AddChild(new HSeparator());
        }
        if(!_rows.GetChildCount().Equals(0)) return;
        _rows.AddChild(FrontendSkin.Label("No attempts recorded yet. Start a run; restarting or leaving will keep its statistics.",20));
    }
}
