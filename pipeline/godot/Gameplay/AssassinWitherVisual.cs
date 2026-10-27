using Godot;
using System;
using System.Collections.Generic;
namespace SwtorSurvivors;

/// <summary>Wither's selected April ground shockwave and attached dark-wisp emitters.</summary>
public partial class AssassinWitherVisual : Node3D
{
    public Combatant? Target {get;set;}
    public Vector3 Center {get;set;}
    public float Radius {get;set;}=.28f;
    private sealed class Wisp {public Sprite3D Sprite=null!;public double Age,Life;public Vector3 Offset;public float Size;public int Frame;}
    private readonly List<Wisp> _wisps=new();
    private Sprite3D? _ring;
    private double _age,_next;
    private int _sequence;
    public bool GroundWave=>Target==null;
    public int WispCount=>_wisps.Count;
    public override void _Ready()
    {
        if(GroundWave){
            _ring=Sprite("fx_shockwave_ring",Radius*2,1,new Color(.48f,.12f,.72f,.8f),false,false);
            _ring.RotationDegrees=new Vector3(-90,0,0);_ring.GlobalPosition=Center+Vector3.Up*.006f;
            for(int i=0;i<8;i++)SpawnWisp();
        } else {for(int i=0;i<3;i++)SpawnWisp();}
    }
    private Sprite3D Sprite(string stem,float size,int atlas,Color tint,bool alpha,bool billboard=true)
    {
        var texture=GD.Load<Texture2D>($"res://Assets/Assassin/{stem}.png");
        var shader=GD.Load<Shader>(alpha?"res://Gameplay/D3ParticleAlpha.gdshader":"res://Gameplay/D3Particle.gdshader");
        var material=new ShaderMaterial {Shader=shader};material.SetShaderParameter("source_texture",texture);material.SetShaderParameter("billboard",billboard);
        var sprite=new Sprite3D {Texture=texture,Hframes=atlas,Vframes=atlas,PixelSize=size/(texture.GetWidth()/atlas),MaterialOverride=material,Modulate=tint,Shaded=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(sprite);return sprite;
    }
    private void SpawnWisp()
    {
        if(_wisps.Count>=16)return;
        float angle=_sequence++*2.39996f;
        float distance=GroundWave?Radius*(.3f+(_sequence%4)*.2f):.035f;
        var offset=new Vector3(Mathf.Cos(angle)*distance,GroundWave?.015f:.035f,Mathf.Sin(angle)*distance);
        var tint=GroundWave?new Color(.19f,.015f,.31f,.72f):new Color(.29f,.035f,.43f,.8f);
        var sprite=Sprite("2x2_smoke_whisps_02",GroundWave?.19f:.12f,2,tint,true);
        sprite.Frame=_sequence%4;
        _wisps.Add(new Wisp {Sprite=sprite,Life=GroundWave?1.2:1.6,Offset=offset,Frame=sprite.Frame,Size=GroundWave?.19f:.12f});
        sprite.GlobalPosition=(GroundWave?Center:Target!.GlobalPosition)+offset;
    }
    public override void _Process(double delta)
    {
        _age+=delta;
        if(!GroundWave && (!GodotObject.IsInstanceValid(Target) || !Target!.Alive)){QueueFree();return;}
        double life=GroundWave?1.5:4;
        if(_age>=life){QueueFree();return;}
        if(_ring!=null){float progress=Math.Clamp((float)(_age/.65),0,1);_ring.Scale=Vector3.One*Mathf.Lerp(.12f,1.05f,progress);_ring.Modulate=new Color(.48f,.12f,.72f,.8f*(1-progress));}
        _next-=delta;if(_next<=0 && _age<(GroundWave?.6:2.7)){_next=GroundWave?.12:.18;SpawnWisp();}
        foreach(var wisp in _wisps.ToArray()){
            wisp.Age+=delta;
            if(wisp.Age>=wisp.Life){wisp.Sprite.QueueFree();_wisps.Remove(wisp);continue;}
            float t=(float)(wisp.Age/wisp.Life);var at=GroundWave?Center:Target!.GlobalPosition;
            var drift=new Vector3(Mathf.Sin((float)wisp.Age*3)*.012f,t*.09f,Mathf.Cos((float)wisp.Age*3)*.012f);
            wisp.Sprite.GlobalPosition=at+wisp.Offset+drift;wisp.Sprite.Scale=Vector3.One*(.65f+t*.65f);
            // Each source atlas cell is a wisp variant, not a four-frame flash animation.
            var c=wisp.Sprite.Modulate;c.A=(GroundWave?.72f:.8f)*Mathf.Sin(t*Mathf.Pi);wisp.Sprite.Modulate=c;
        }
    }
}
