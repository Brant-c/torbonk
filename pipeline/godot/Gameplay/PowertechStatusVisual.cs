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
        node._sprite=new Sprite3D {Texture=GD.Load<Texture2D>("res://Assets/Powertech/"+texture+".png"),Hframes=(int)Math.Sqrt(frames),Vframes=(int)Math.Sqrt(frames),PixelSize=.0008f,Shaded=false,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled};
        node.AddChild(node._sprite);node.Position=Vector3.Up*.09f;return node;
    }
    public static void Flash(Combatant target,string texture,int frames){double until=Time.GetTicksMsec()+450;Attach(target,texture,frames,()=>Time.GetTicksMsec()<until);}
    public override void _Process(double delta){if(!_active()){QueueFree();return;}_age+=delta;_sprite.Frame=(int)(_age*18)%_frames;}
}
