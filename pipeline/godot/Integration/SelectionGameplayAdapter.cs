using System;
using System.Linq;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

/// <summary>Explicit frontend-key to existing gameplay composition binding. These are not GOM IDs.</summary>
public sealed record GameplayConfiguration(
    string FactionId, string BaseClassId, string AdvancedClassId, string MapId,
    string AppearancePresetId, string WorldScene, string EnvironmentScene, string CharacterScene);

public static class SelectionGameplayAdapter
{
    // Register future compositions here; the menu does not need class/map-specific launch branches.
    private static readonly GameplayConfiguration[] Supported =
    [
        new("empire", "sith_inquisitor", "sorcerer", "korriban", "sorcerer_april2012_bmn",
            "res://Gameplay/GameWorld.tscn", "res://Assets/Korriban/KorribanEnvironment.tscn",
            "res://Assets/sorcerer.glb")
    ];

    public static GameplayConfiguration DevelopmentDefault => Supported[0];

    public static string? UnavailableReason(SelectionConfiguration selection)
        => TryResolve(selection, out _, out string reason) ? null : reason;

    public static bool TryResolve(SelectionConfiguration? selection,
        out GameplayConfiguration configuration, out string reason)
    {
        configuration = null!;
        reason = "Preview only. Choose Sith Empire / Sith Inquisitor / Sorcerer / Korriban with the original appearance.";
        if (selection == null || selection.SchemaVersion != 1) return false;
        // Validate every frozen appearance field, not just the preset name or serialized Playable flag.
        if (selection.Appearance is not { IsSupportedPreset: true })
        { reason = "This appearance is unavailable. Only the original Sorcerer preset is supported."; return false; }
        var match = Supported.SingleOrDefault(content =>
            content.FactionId == selection.FactionId && content.BaseClassId == selection.BaseClassId
            && content.AdvancedClassId == selection.AdvancedClassId && content.MapId == selection.MapId
            && content.AppearancePresetId == selection.Appearance.PresetId);
        if (match == null) return false;
        configuration = match; reason = ""; return true;
    }
}
