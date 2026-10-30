using Godot;
using System;
namespace SwtorSurvivors;
// Original April particle textures; Godot adaptation, not native particle simulation.
public partial class PowertechStatusVisual : Node3D
{
    private Func<bool> _active=()=>true;private Sprite3D _sprite=null!;private int _frames;private double _age;
    public static Node3D Attach(Combatant target,string texture,int frames,Func<bool> active)
    {
        var node=new PowertechStatusVisual {_active=()=>GodotObject.IsInstanceValid(target) && target.Alive && active(),_frames=frames};target.AddChild(node);
        // Bare names are Powertech recoveries; Bounty Hunter kit textures pass their full res:// path.
        string path=texture.StartsWith("res://",StringComparison.Ordinal)?texture:"res://Assets/Powertech/"+texture+".png";
        var image=GD.Load<Texture2D>(path);int side=(int)Math.Sqrt(frames);
        node._sprite=new Sprite3D {Texture=image,Hframes=side,Vframes=side,PixelSize=path==texture?.1f/Math.Max(1,image.GetWidth()/side):.0008f,Shaded=false,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled};
        // Original BW atlases have no alpha: draw them additively and in front of the body they belong to.
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/BountyStatusSprite.gdshader")};
        material.SetShaderParameter("source_texture",image);node._sprite.MaterialOverride=material;
        node.AddChild(node._sprite);node.Position=Vector3.Up*.09f;return node;
    }
    public static void Flash(Combatant target,string texture,int frames){double until=Time.GetTicksMsec()+450;Attach(target,texture,frames,()=>Time.GetTicksMsec()<until);}
    public override void _Process(double delta){if(!_active()){QueueFree();return;}_age+=delta;_sprite.Frame=(int)(_age*18)%_frames;}
}
