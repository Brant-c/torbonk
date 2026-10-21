using Godot;
namespace SwtorSurvivors.Frontend;

/// <summary>Original galaxy textures with a simplified, explicitly schematic destination layout.</summary>
public partial class GalaxyChart : Control
{
    public Action<string>? DestinationSelected;
    private readonly List<(MapDefinition Map, Control Group)> _markers = new();
    private const float MarkerWidth = 260, PlanetDiameter = 72, LabelGap = 10, ButtonHeight = 58;
    public override void _Ready() => Resized += LayoutMarkers;
    public void Populate(FrontendCatalog catalog, string selected)
    {
        ClipContents = true;
        var galaxy = FrontendSkin.Art(FrontendSkin.Assets + "Recovered/gal_map_galaxy_core.png", Size);
        galaxy.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); galaxy.Modulate = new Color(.65f,.85f,1,.9f); AddChild(galaxy);
        AddChild(new Label { Text = "GALACTIC NAVIGATION", Position = new Vector2(24,18) });
        foreach (var map in catalog.Maps)
        {
            var group = new Control { Size = new Vector2(MarkerWidth,PlanetDiameter+LabelGap+ButtonHeight), MouseFilter = MouseFilterEnum.Ignore };
            AddChild(group);
            var globe=FrontendSkin.Planet(map.Surface,PlanetDiameter);
            globe.Position=new Vector2((MarkerWidth-PlanetDiameter)/2,0); group.AddChild(globe);
            string availability = map.GameplayImplemented ? "Playable content" : "Planned";
            var b = FrontendSkin.Button(map.Name+"\n"+availability, () => DestinationSelected?.Invoke(map.Id), map.Id == selected);
            b.Position = new Vector2(0,PlanetDiameter+LabelGap); b.Size = new Vector2(MarkerWidth,ButtonHeight);
            b.AddThemeFontSizeOverride("font_size",18);
            b.AddThemeStyleboxOverride("normal",FrontendSkin.ButtonArtwork(verticalPadding:4));
            b.AddThemeStyleboxOverride("hover",FrontendSkin.ButtonArtwork(hovered:true,selected:map.Id==selected,verticalPadding:4));
            b.AddThemeStyleboxOverride("pressed",FrontendSkin.ButtonArtwork(hovered:true,selected:true,verticalPadding:4));
            b.AddThemeStyleboxOverride("hover_pressed",FrontendSkin.ButtonArtwork(hovered:true,selected:true,verticalPadding:4));
            b.TooltipText = map.Region + " — " + (map.GameplayImplemented ? "Map content available" : "Preview only; map not implemented");
            group.AddChild(b); _markers.Add((map,group));
        }
        var note = FrontendSkin.Label("Schematic positions  •  Select a destination",16);
        note.Position = new Vector2(24,Size.Y-38); AddChild(note);
        LayoutMarkers();
    }
    private void LayoutMarkers()
    {
        // Catalog positions are schematic, with space for complete marker groups.
        var plot = new Rect2(0,52,Size.X,Mathf.Max(1,Size.Y-104));
        foreach (var (map,group) in _markers)
        {
            Vector2 center = plot.Position+new Vector2(map.X,map.Y)*plot.Size;
            group.Position = center-group.Size/2;
        }
    }
}
