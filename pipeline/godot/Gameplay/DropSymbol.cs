using Godot;
using SwtorSurvivors.UI;
namespace SwtorSurvivors;

public enum DropSymbolKind { Experience, Item, Special, Credits }

/// <summary>Original blue UI artwork reused as world markers; no reward or collection logic.</summary>
public partial class DropSymbol : Node3D
{
    public DropSymbolKind Kind {get;set;}=DropSymbolKind.Item;
    public static string SliceName(DropSymbolKind kind) => kind switch {
        DropSymbolKind.Credits => "credits",
        DropSymbolKind.Experience => "bwaui_buttons_298",
        DropSymbolKind.Special => "bwaui_buttons_242",
        _ => "bwaui_buttons_135" };
    public static Sprite3D Icon(DropSymbolKind kind,float width)
    {
        var texture=kind==DropSymbolKind.Credits
            ? GD.Load<Texture2D>("res://Assets/UI/symbols/credits.png")
            : SwtorSkin.Slice(SliceName(kind));
        return new Sprite3D {Name="DropIcon",Texture=texture,PixelSize=width/texture.GetWidth(),
            Modulate=Colors.White,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Shaded=false,
            TextureFilter=BaseMaterial3D.TextureFilterEnum.Linear,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
    }
    private Sprite3D _icon=null!;
    private float _age;
    public override void _Ready()
    {
        // Freeze with gameplay while the existing reward chooser is open.
        ProcessMode=ProcessModeEnum.Pausable;
        _icon=Icon(Kind,Kind==DropSymbolKind.Special ? .095f : .08f);AddChild(_icon);
    }
    public override void _Process(double delta)
    {
        _age+=(float)delta;
        _icon.Position=Vector3.Up*(.015f+Mathf.Sin(_age*3)*.005f);
        _icon.Modulate=new Color(1,1,1,Mathf.Clamp(3-_age,0,1));
        if(_age>=3)QueueFree();
    }
}
