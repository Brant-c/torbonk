using Godot;
using System;
namespace SwtorSurvivors;
/// <summary>Kolto Missile ground field: original bh_kolto_missile_aoe puddle masks and billowing steam. Presentation only.</summary>
public partial class KoltoFieldVisual : Node3D
{
    private double _remaining,_life,_steam;
    private float _radius;
    private StormAreaIndicator _ground=null!;
    private ShaderMaterial _material=null!;
    private readonly RandomNumberGenerator _random=new();
    public static KoltoFieldVisual Spawn(Node parent,Vector3 at,float radius,double seconds)
    {
        var field=new KoltoFieldVisual {_remaining=seconds,_life=seconds,_radius=radius};parent.AddChild(field);field.GlobalPosition=at;return field;
    }
    public override void _Ready()
    {
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/KoltoField.gdshader")};
        _material.SetShaderParameter("puddle_detail",GD.Load<Texture2D>("res://Assets/BountyHunter/proj_oilpuddle_wrap.png"));
        _material.SetShaderParameter("puddle_border",GD.Load<Texture2D>("res://Assets/BountyHunter/proj_oilpuddle_border.png"));
        _ground=new StormAreaIndicator {Name="KoltoTerrainProjection",ProjectionMaterial=_material};AddChild(_ground);
    }
    public override void _PhysicsProcess(double delta)
    {
        _remaining-=delta;if(_remaining<=0){QueueFree();return;}
        _ground.UpdateArea(GlobalPosition,_radius);
        _material.SetShaderParameter("fade",(float)Math.Clamp(Math.Min(_remaining,_life-_remaining)*3,0,1));
        _steam-=delta;
        while(_steam<=0){_steam+=.25;Puff();}
    }
    private void Puff()
    {
        var texture=GD.Load<Texture2D>("res://Assets/BountyHunter/8x8_billowing_steam.png");
        var sprite=new Sprite3D {Texture=texture,Hframes=8,Vframes=8,PixelSize=.12f/(texture.GetWidth()/8f),Shaded=false,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=new Color(.45f,1,.6f,.5f)};
        float angle=_random.RandfRange(0,Mathf.Tau),distance=Mathf.Sqrt(_random.Randf())*_radius*.85f;
        AddChild(sprite);sprite.Position=new Vector3(Mathf.Cos(angle)*distance,.05f,Mathf.Sin(angle)*distance);
        var tween=sprite.CreateTween();
        tween.TweenMethod(Callable.From<float>(f=>{if(GodotObject.IsInstanceValid(sprite)){sprite.Frame=Math.Min(63,(int)(f*64));sprite.Modulate=new Color(.45f,1,.6f,.5f*(1-f));sprite.Position+=Vector3.Up*.0015f;}}),0f,1f,1.2);
        tween.TweenCallback(Callable.From(sprite.QueueFree));
    }
}
