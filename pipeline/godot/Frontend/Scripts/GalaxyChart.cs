using System;
using Godot;
namespace SwtorSurvivors.Frontend;

/// <summary>Single destination card using the original Korriban illustration at its native aspect.</summary>
public partial class GalaxyChart : Control
{
    public Action<string>? DestinationSelected;
    private TextureRect? _artwork;
    private Button? _select;
    public override void _Ready() => Resized += LayoutDestination;
    public void Populate(FrontendCatalog catalog, string selected)
    {
        var map = catalog.Map("korriban");
        _artwork = FrontendSkin.Art(map.Artwork,Vector2.Zero);
        AddChild(_artwork);
        _select = FrontendSkin.Button(map.Name,()=>DestinationSelected?.Invoke(map.Id),selected==map.Id);
        AddChild(_select);
        LayoutDestination();
    }
    private void LayoutDestination()
    {
        if (_artwork == null || _select == null) return;
        _artwork.Position = new Vector2(24,40);
        _artwork.Size = new Vector2(Mathf.Max(1,Size.X-48),Mathf.Max(1,Size.Y-160));
        _select.Size = new Vector2(260,52);
        _select.Position = new Vector2((Size.X-_select.Size.X)/2,Size.Y-74);
    }
}
