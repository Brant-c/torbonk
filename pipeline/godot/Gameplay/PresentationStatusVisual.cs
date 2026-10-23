using Godot;
using System;
namespace SwtorSurvivors;
// Lifetime belongs to PeriodicEffects; refresh replaces rather than stacks visuals or voices.
public partial class PresentationStatusVisual : Node3D
{
    private Func<bool> _active=null!;
    private Sprite3D _glow=null!,_smoke=null!;
    private double _age;
    public static Node3D AttachTerror(Combatant target,Func<bool> active)
    {
        var node=new PresentationStatusVisual {_active=()=>GodotObject.IsInstanceValid(target) && target.Alive && active()};
        target.AddChild(node);node.Position=Vector3.Up*.09f;
        node._glow=node.Make("2x2_electrical_blast_alpha",2,.1f,new Color(.527f,.535f,.993f,.5f));
        node._smoke=node.Make("5x5_flame_lick_04_bw",5,.12f,new Color(.3f,.15f,.45f,.4f));
        return node;
    }
    private Sprite3D Make(string stem,int frames,float size,Color tint)
    {
        var tex=GD.Load<Texture2D>("res://Assets/PresentationRepairs/"+stem+".png");
        var sprite=new Sprite3D {Texture=tex,Hframes=frames,Vframes=frames,PixelSize=size/(tex.GetWidth()/frames),Shaded=false,Modulate=tint,
            MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode=BaseMaterial3D.BlendModeEnum.Add,BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled,AlbedoTexture=tex,VertexColorUseAsAlbedo=true}};
        AddChild(sprite);return sprite;
    }
    public override void _Process(double delta)
    {
        if(!_active()){QueueFree();return;}
        _age+=delta;_glow.Frame=(int)(_age*12)%4;_smoke.Frame=(int)(_age*20)%25;
    }
}
