using System.Linq;

namespace SwtorSurvivors.Integration;

/// <summary>Playable destinations. Each entry names an existing world composition and converted environment;
/// nothing here substitutes assets for an unavailable map.</summary>
public sealed record PlayableMap(string Id, string Name, string WorldScene, string EnvironmentScene, string? FactionId);

public static class MapRegistry
{
    public static readonly PlayableMap[] All =
    [
        new("eternity_vault", "Eternity Vault — Surface", "res://Gameplay/GameWorld_EternityVault.tscn", "res://Assets/Maps/eternity_vault/MapEnvironment.tscn", null),
        new("soa_p3", "Eternity Vault — Soa", "res://Gameplay/GameWorld_SoaP3.tscn", "res://Assets/Maps/soa_p3/MapEnvironment.tscn", null),
        new("korriban", "Korriban", "res://Gameplay/GameWorld.tscn", "res://Assets/Korriban/D4/KorribanTrials.tscn", "empire"),
        new("hutta", "Hutta", "res://Gameplay/GameWorld_Hutta.tscn", "res://Assets/Maps/hutta/MapEnvironment.tscn", "empire"),
        new("dromund", "Dromund Kaas", "res://Gameplay/GameWorld_Dromund.tscn", "res://Assets/Maps/dromund/MapEnvironment.tscn", "empire"),
        new("balmorra", "Balmorra", "res://Gameplay/GameWorld_Balmorra.tscn", "res://Assets/Maps/balmorra/MapEnvironment.tscn", null),
    ];
    public static PlayableMap? Find(string id) => All.FirstOrDefault(m => m.Id == id);
}
