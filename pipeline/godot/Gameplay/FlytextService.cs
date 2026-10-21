using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SwtorSurvivors;

/// <summary>One pooled entry per resolved event, one canvas, shared font. No cap, eviction,
/// aggregation, target cooldown or queue. World snapshots survive recipient destruction.</summary>
public partial class FlytextService : Control
{
    public CombatEvents Events { get; set; } = null!;
    public Camera3D Camera { get; set; } = null!;
    public int ActiveCount => _active.Count;
    public long CreatedEntries { get; private set; }
    public long DrawnEntries { get; private set; }
    public event Action<CombatEvent>? EntryCreated;
    private sealed class Entry
    {
        public Vector3 World;
        public Vector2 Offset;
        public string Text = "";
        public Color Color;
        public float Age, Duration, Drift;
        public bool Critical;
    }
    private readonly List<Entry> _active = new();
    private readonly Stack<Entry> _pool = new();
    private Font _font = null!;
    private ulong _sequence;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ProcessPriority = 20; // Project after the existing camera rig has updated.
        // Recovered original draw-text outlines; exact native flytext face binding is unresolved.
        _font = new FontFile { Data = FileAccess.GetFileAsBytes("res://Assets/UI/Flytext/EurostileNextLTPro.ttf") };
        Events.Resolved += OnResolved;
    }

    private void OnResolved(CombatEvent combatEvent)
    {
        var change = combatEvent.Change;
        bool numeric = change.Kind is CombatEventKind.Damage or CombatEventKind.Healing;
        if (numeric && (!float.IsFinite(change.Amount) || change.Amount <= 0)) return;
        string text = change.Text ?? (change.Kind switch
        {
            CombatEventKind.Healing => "+" + FormatAmount(change.Amount),
            CombatEventKind.Damage => FormatAmount(change.Amount),
            CombatEventKind.Absorbed => "Absorbed",
            CombatEventKind.Resisted => "Resist",
            _ => ""
        });
        if (string.IsNullOrEmpty(text)) return;
        var entry = _pool.Count > 0 ? _pool.Pop() : new Entry();
        // Low-discrepancy offsets spread consecutive events without retaining target state.
        // Neither sequence nor recipient identity consumes gameplay's random generator.
        double phase = (++_sequence * 0.61803398875 + combatEvent.RecipientId * 0.013) % 1;
        double height = (_sequence * 0.41421356237) % 1;
        entry.World = combatEvent.WorldPosition;
        entry.Offset = new Vector2((float)(phase * 40 - 20), (float)(-12 - height * 18));
        entry.Drift = (float)(phase * 16 - 8);
        entry.Text = text;
        entry.Critical = change.IsCritical;
        entry.Age = 0;
        entry.Duration = 1.5f; // April manager's FLYUP preset lifetime.
        entry.Color = change.Kind switch
        {
            CombatEventKind.Healing => new Color(0, 1, 0),
            CombatEventKind.Damage when combatEvent.RecipientTeam == CombatTeam.Player => new Color(1, 0.2f, 0.2f),
            CombatEventKind.Damage => new Color(1, 1, 0),
            _ => Colors.White
        };
        _active.Add(entry);
        CreatedEntries++;
        EntryCreated?.Invoke(combatEvent);
        QueueRedraw();
    }

    // Display whole numbers only; positive fractional events still show at least 1.
    // Rounding is presentation-only: the event and Health retain their applied values.
    public static string FormatAmount(float amount) => Math.Max(1d,
        Math.Round(amount, MidpointRounding.AwayFromZero)).ToString("0", CultureInfo.InvariantCulture);

    public override void _Process(double delta)
    {
        int retained = 0;
        int previousCount = _active.Count;
        for (int i = 0; i < previousCount; i++)
        {
            var entry = _active[i];
            entry.Age += (float)delta;
            if (entry.Age < entry.Duration) _active[retained++] = entry;
            else _pool.Push(entry);
        }
        // Stable linear compaction avoids shifting the entire list once per expired event.
        if (retained < previousCount) _active.RemoveRange(retained, previousCount - retained);
        if (_active.Count > 0 || DrawnEntries > 0) QueueRedraw();
    }

    public override void _Draw()
    {
        DrawnEntries = 0;
        if (!GodotObject.IsInstanceValid(Camera)) return;
        float uiScale = Mathf.Clamp(GetViewportRect().Size.Y / 800f, 0.75f, 1.4f);
        foreach (var entry in _active)
        {
            // Projection visibility is geometric, never an event-count/clutter filter.
            if (Camera.IsPositionBehind(entry.World)) continue;
            float t = entry.Age / entry.Duration;
            float rise = 48 * (1 - Mathf.Pow(1 - t, 2));
            float pop = entry.Critical ? 1 + 0.18f * Mathf.Pow(Mathf.Max(0, 1 - entry.Age / 0.16f), 2) : 1;
            int size = Mathf.RoundToInt((entry.Critical ? 29 : 23) * pop * uiScale);
            float alpha = 1 - Mathf.SmoothStep(0.62f, 1, t);
            var color = entry.Color; color.A = alpha;
            var point = Camera.UnprojectPosition(entry.World) +
                (entry.Offset + new Vector2(entry.Drift * t, -rise)) * uiScale;
            point.X -= _font.GetStringSize(entry.Text, fontSize: size).X * 0.5f;
            DrawString(_font, point + new Vector2(1, 2) * uiScale, entry.Text,
                fontSize: size, modulate: new Color(0, 0, 0, alpha * 0.85f));
            DrawStringOutline(_font, point, entry.Text, fontSize: size, size: 3,
                modulate: new Color(0.025f, 0.012f, 0, alpha));
            DrawString(_font, point, entry.Text, fontSize: size, modulate: color);
            DrawnEntries++;
        }
    }

    public void Clear()
    {
        foreach (var entry in _active) _pool.Push(entry);
        _active.Clear();
        DrawnEntries = 0;
        _sequence = 0;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(Events)) Events.Resolved -= OnResolved;
        Clear();
        _pool.Clear();
    }
}
