using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace SwtorSurvivors.Frontend;

/// <summary>Original galaxy textures with a simplified, explicitly schematic destination layout.</summary>
public partial class GalaxyChart : Control
{
    public Action<string>? DestinationSelected;
    private readonly List<(MapDefinition Map, Button Button)> _markers = new();
    public void Populate(FrontendCatalog catalog, string selected)
    {
        ClipContents = true;
        var galaxy = FrontendSkin.Art(FrontendSkin.Assets + "Recovered/gal_map_galaxy_core.png", Size);
        galaxy.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); galaxy.Modulate = new Color(.65f,.85f,1,.9f); AddChild(galaxy);
        AddChild(new Label { Text = "GALACTIC NAVIGATION", Position = new Vector2(24,18) });
        foreach (var map in catalog.Maps)
        {
            var globe=FrontendSkin.Art(map.Surface,new Vector2(46,46));
            globe.Material=new ShaderMaterial { Shader=GD.Load<Shader>(FrontendSkin.Assets+"planet.gdshader") };
            globe.Position=new Vector2(map.X * (Size.X-240)+88, map.Y * (Size.Y-100)-52);
            AddChild(globe);
            var b = FrontendSkin.Button((map.Id == selected ? "◆  " : "◇  ") + map.Name + (map.GameplayImplemented ? "" : "  ·  Planned"), () => DestinationSelected?.Invoke(map.Id), map.Id == selected);
            b.Position = new Vector2(map.X * (Size.X-240), map.Y * (Size.Y-100)); b.Size = new Vector2(225,52);
            b.TooltipText = map.Region + " — " + (map.GameplayImplemented ? "Map content available" : "Preview only; map not implemented");
            AddChild(b); _markers.Add((map,b));
        }
        var note = FrontendSkin.Label("Schematic positions  •  Select a destination",16);
        note.Position = new Vector2(24,Size.Y-38); AddChild(note);
    }
    public override void _Draw()
    {
        Vector2 center = Size * new Vector2(.48f,.52f);
        for (int i=1;i<=4;i++) DrawArc(center, i*Size.Y*.105f,0,Mathf.Tau,120,new Color(.25f,.65f,.8f,.2f),1,true);
        DrawLine(new Vector2(20,center.Y),new Vector2(Size.X-20,center.Y),new Color(.25f,.65f,.8f,.16f),1,true);
    }
}
