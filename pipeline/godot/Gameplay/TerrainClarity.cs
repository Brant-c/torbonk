using Godot;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Replace only ground surface materials; retain mesh, UVs and collision.</summary>
public static class TerrainClarity
{
    public static int Apply(Node3D environment, string mapId)
    {
        using var data = JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/TerrainClarity/materials.json"));
        if (!data.RootElement.TryGetProperty(mapId, out var patches)) return 0;
        int count = 0;
        foreach (var node in environment.FindChildren("Terrain_*", "MeshInstance3D", true, false))
        {
            var mesh = (MeshInstance3D)node;
            string id = mesh.Name.ToString().Substring("Terrain_".Length);
            if (!patches.TryGetProperty(id, out var path)) continue;
            mesh.SetSurfaceOverrideMaterial(0, GD.Load<ShaderMaterial>(path.GetString()!));
            count++;
        }
        GD.Print($"TERRAIN_CLARITY {mapId}: {count} tiled patches");
        return count;
    }
}
