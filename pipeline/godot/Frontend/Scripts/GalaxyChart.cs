using System;
using Godot;
namespace SwtorSurvivors.Frontend;

/// <summary>Destination card using the selected map's original codex illustration at its native aspect, with one button per playable destination.</summary>
public partial class GalaxyChart : Control
{
    public Action<string>? DestinationSelected;
    private TextureRect? _artwork;
    private readonly System.Collections.Generic.List<Button> _destinations = new();
    public override void _Ready() => Resized += LayoutDestination;
    public void Populate(FrontendCatalog catalog, string selected)
    {
        var map = catalog.Map(selected);
        _artwork = FrontendSkin.Art(map.Artwork,Vector2.Zero);
        AddChild(_artwork);
        foreach (var destination in System.Linq.Enumerable.Where(catalog.Maps,m=>m.GameplayImplemented))
        {
            var button = FrontendSkin.Button(destination.Name,()=>DestinationSelected?.Invoke(destination.Id),selected==destination.Id);
            _destinations.Add(button); AddChild(button);
        }
        LayoutDestination();
    }
    private void LayoutDestination()
    {
        if (_artwork == null || _destinations.Count == 0) return;
        _artwork.Position = new Vector2(24,40);
        _artwork.Size = new Vector2(Mathf.Max(1,Size.X-48),Mathf.Max(1,Size.Y-160));
        const float width = 200, gap = 10;
        float total = _destinations.Count*width+(_destinations.Count-1)*gap;
        for (int i = 0; i < _destinations.Count; i++)
        {
            _destinations[i].Size = new Vector2(width,52);
            _destinations[i].Position = new Vector2((Size.X-total)/2+i*(width+gap),Size.Y-74);
        }
    }
}
