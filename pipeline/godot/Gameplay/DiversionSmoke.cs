using Godot;
namespace SwtorSurvivors;
/// <summary>Ground smoke cloud for Diversion, built from the recovered SWTOR smoke flipbook.</summary>
public partial class DiversionSmoke : Node3D
{
    private double _age,_life;
    private Sprite3D[] _puffs=System.Array.Empty<Sprite3D>();
    public static void Spawn(GameWorld world,Vector3 at,float seconds)
    {
        var cloud=new DiversionSmoke {Name="DiversionSmoke",_life=seconds};world.AddChild(cloud);cloud.GlobalPosition=at;
        var image=GD.Load<Texture2D>("res://Assets/Agent/fx_smoke_2x2_alpha.png");
        cloud._puffs=new Sprite3D[6];
        for(int i=0;i<6;i++){
            var puff=new Sprite3D {Texture=image,Hframes=2,Vframes=2,Frame=i%4,PixelSize=.5f/(image.GetWidth()/2),Shaded=false,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=new Color(.72f,.74f,.78f,.0f)};
            cloud.AddChild(puff);float a=i*Mathf.Tau/6;puff.Position=new Vector3(Mathf.Cos(a)*.45f,.1f+.02f*i,Mathf.Sin(a)*.45f);cloud._puffs[i]=puff;
        }
    }
    public override void _Process(double delta)
    {
        _age+=delta;
        if(_age>=_life){QueueFree();return;}
        float fade=(float)Mathf.Min(Mathf.Min(_age/.4,1),(_life-_age)/.8);
        for(int i=0;i<_puffs.Length;i++){var c=_puffs[i].Modulate;c.A=.45f*Mathf.Clamp(fade,0,1);_puffs[i].Modulate=c;_puffs[i].Rotation=new Vector3(0,0,(float)_age*.15f*(i%2==0?1:-1));}
    }
}
