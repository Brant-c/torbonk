using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors;

public sealed class RunRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public string ClassId { get; set; } = "";
    public string MapId { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string Outcome { get; set; } = "Incomplete";
    public double Duration { get; set; }
    public int Level { get; set; }
    public double Damage { get; set; }
    public double Healing { get; set; }
    public double Taken { get; set; }
    public double Dps => Duration > 0 ? Damage / Duration : 0;
    public double Hps => Duration > 0 ? Healing / Duration : 0;
    public string Encounter => MapId switch {
        "eternity_vault" => "Annihilation Droid XRR-3", "soa_p3" => "Soa",
        _ => Integration.MapRegistry.Find(MapId)?.Name ?? MapId };
    public string Raid => MapId is "eternity_vault" or "soa_p3" ? "Eternity Vault" : "World run";
}

/// <summary>One file per attempt; periodic snapshots preserve unfinished testing runs.</summary>
public static class RunHistory
{
    internal static string? TestDirectory { get; set; }
    private static string DirectoryPath => TestDirectory ?? ProjectSettings.GlobalizePath("user://run-history");
    public static void Save(RunRecord record)
    {
        if (record.Duration <= 0) return;
        try {
            System.IO.Directory.CreateDirectory(DirectoryPath);
            string path = System.IO.Path.Combine(DirectoryPath, record.Id + ".json");
            System.IO.File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(record));
            System.IO.File.Move(path + ".tmp", path, true);
        } catch (Exception e) { GD.PushWarning("Unable to save run history: " + e.Message); }
    }
    public static IReadOnlyList<RunRecord> Load()
    {
        var records = new List<RunRecord>();
        try {
            if (!System.IO.Directory.Exists(DirectoryPath)) return records;
            foreach (var path in System.IO.Directory.EnumerateFiles(DirectoryPath, "*.json"))
                try {
                    var record = JsonSerializer.Deserialize<RunRecord>(System.IO.File.ReadAllText(path));
                    if (record != null) records.Add(record);
                } catch (Exception e) { GD.PushWarning("Unable to read run record: " + e.Message); }
        } catch (Exception e) { GD.PushWarning("Unable to read run history: " + e.Message); }
        return records.OrderByDescending(r => r.Date).ToArray();
    }
}
