using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Original green player reticle projected onto Korriban terrain. Presentation only.</summary>
public partial class StormAreaIndicator : MeshInstance3D
{
    public Vector3 GroundCenter { get; private set; }
    public float EffectiveRadius { get; private set; }
    public int ProjectedVertexCount { get; private set; }
    // Other ground effects reuse the same terrain-only projection and hole/cliff guards.
    public ShaderMaterial? ProjectionMaterial { get; set; }
    public float ProjectionRidgeFraction { get; set; } = 1;
    private float _ridgeFraction;
    private const int Cells = 32;
    private const float GroundLift = 0.002f;

    public override void _Ready()
    {
        if(ProjectionMaterial!=null)
        {
            _ridgeFraction=ProjectionRidgeFraction;MaterialOverride=ProjectionMaterial;
            CastShadow=ShadowCastingSetting.Off;Visible=false;return;
        }
        using var config = JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Abilities/storm-reticle.json"));
        var root = config.RootElement;
        _ridgeFraction = root.GetProperty("ringRidgeFraction").GetSingle();
        string texture = System.IO.Path.GetFileNameWithoutExtension(root.GetProperty("texture").GetString()!);
        // Use the actual green projector's color, rather than inventing a friendly tint.
        var rgba = root.GetProperty("projector").GetProperty("_fxProjectionColor").GetString()!.TrimStart('#').Split(',');
        float Component(int i) => float.Parse(rgba[i], CultureInfo.InvariantCulture);
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Gameplay/StormAreaIndicator.gdshader") };
        material.SetShaderParameter("reticle_texture", GD.Load<Texture2D>($"res://Assets/Abilities/{texture}.png"));
        material.SetShaderParameter("imperial_texture", GD.Load<Texture2D>("res://Assets/Abilities/proj_empire_faction.png"));
        material.SetShaderParameter("player_green", new Color(Component(0), Component(1), Component(2), Component(3)));
        material.SetShaderParameter("ridge_fraction", _ridgeFraction);
        MaterialOverride = material;
        CastShadow = ShadowCastingSetting.Off;
        Visible = false;
    }

    public void UpdateArea(Vector3 center, float radius)
    {
        if (!float.IsFinite(radius) || radius <= 0) { Visible = false; EffectiveRadius = 0; return; }
        if (Visible && GroundCenter == center && EffectiveRadius == radius) return;
        GroundCenter = center; EffectiveRadius = radius; GlobalPosition = center;
        // The texture's bright outer ridge lies inside its padded square. Map that ridge
        // to the gameplay radius, keeping the original soft halo beyond it.
        float extent = radius / _ridgeFraction;
        var vertices = new Vector3[(Cells + 1) * (Cells + 1)];
        var uv = new Vector2[vertices.Length];
        var supported = new bool[vertices.Length];
        var space = GetWorld3D().DirectSpaceState;
        ProjectedVertexCount = 0;
        for (int z = 0; z <= Cells; z++)
        for (int x = 0; x <= Cells; x++)
        {
            int i = z * (Cells + 1) + x;
            var offset = new Vector3((x / (float)Cells * 2 - 1) * extent, 0, (z / (float)Cells * 2 - 1) * extent);
            var point = center + offset;
            // Terrain layer only: never project onto characters or create a floor across holes.
            var query = PhysicsRayQueryParameters3D.Create(point + Vector3.Up * 0.3f, point - Vector3.Up * 0.5f, 1);
            var hit = space.IntersectRay(query);
            uv[i] = new Vector2(offset.X / (2 * radius) + 0.5f, offset.Z / (2 * radius) + 0.5f);
            if (hit.Count == 0) continue;
            supported[i] = true; ProjectedVertexCount++;
            vertices[i] = ToLocal((Vector3)hit["position"] + Vector3.Up * GroundLift);
        }
        var indices = new List<int>(Cells * Cells * 6);
        void Triangle(int a, int b, int c)
        {
            if (!supported[a] || !supported[b] || !supported[c]) return;
            // Do not stretch a projected triangle across a cliff between disconnected floors.
            if (Mathf.Abs(vertices[a].Y - vertices[b].Y) > 0.08f ||
                Mathf.Abs(vertices[a].Y - vertices[c].Y) > 0.08f ||
                Mathf.Abs(vertices[b].Y - vertices[c].Y) > 0.08f) return;
            indices.Add(a); indices.Add(b); indices.Add(c);
        }
        for (int z = 0; z < Cells; z++)
        for (int x = 0; x < Cells; x++)
        {
            int a = z * (Cells + 1) + x, b = a + 1, c = a + Cells + 1, d = c + 1;
            Triangle(a, c, b); Triangle(b, c, d);
        }
        Visible = indices.Count > 0;
        if (!Visible) { Mesh = null; return; }
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        arrays[(int)Godot.Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Godot.Mesh.ArrayType.TexUV] = uv;
        arrays[(int)Godot.Mesh.ArrayType.Index] = indices.ToArray();
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles, arrays);
        Mesh = mesh;
    }
}
