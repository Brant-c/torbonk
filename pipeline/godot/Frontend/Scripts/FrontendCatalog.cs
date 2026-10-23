using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using System.Text.Json;

namespace SwtorSurvivors.Frontend;

public sealed class FactionDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Emblem { get; set; } = "";
}
public sealed class ClassDefinition
{
    public string Id { get; set; } = "";
    public string FactionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Artwork { get; set; } = "";
    public AdvancedClassDefinition[] AdvancedClasses { get; set; } = [];
}
public sealed class AdvancedClassDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Artwork { get; set; } = "";
    private bool _implemented;
    public bool GameplayImplemented { get=>_implemented && ClassProfile.Find(Id)?.Ready==true; set=>_implemented=value; }
    public string PreviewScene { get; set; } = "";
    public string IdleAnimation { get; set; } = "Idle";
    public float FacingDegrees { get; set; } = 0;
}
public sealed class MapDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string Description { get; set; } = "";
    public string Artwork { get; set; } = "";
    public string Surface { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public bool GameplayImplemented { get; set; }
}
public sealed class FrontendCatalog
{
    public FactionDefinition[] Factions { get; set; } = [];
    public ClassDefinition[] Classes { get; set; } = [];
    public MapDefinition[] Maps { get; set; } = [];
    public static FrontendCatalog Load() => JsonSerializer.Deserialize<FrontendCatalog>(
        Godot.FileAccess.GetFileAsString("res://Frontend/Data/catalog.json"))!;
    public ClassDefinition Class(string id) => Classes.Single(c => c.Id == id);
    public AdvancedClassDefinition Advanced(string id) => Classes.SelectMany(c => c.AdvancedClasses).Single(c => c.Id == id);
    public MapDefinition Map(string id) => Maps.Single(m => m.Id == id);
}
