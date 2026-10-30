using Godot;
using System;
using System.Linq;
namespace SwtorSurvivors;
// The owning periodic record controls lifetime, refresh and target-death cleanup.
public partial class AgentStatusVisual : Node3D
{
    private Func<bool> _active=()=>false;
    private Sprite3D _sprite=null!;
    private double _age;
    private Node3D? _interrogationProbe;
    private readonly System.Collections.Generic.List<(ImmediateMesh mesh,MeshInstance3D node,Vector3 end)> _zaps=new();
    public int InterrogationZapCount=>_zaps.Count;
    public bool InterrogationProbeVisible=>GodotObject.IsInstanceValid(_interrogationProbe) && _interrogationProbe!.Visible;
    public static Node3D Attach(Combatant target,string id,Func<bool> active)
    {
        var node=new AgentStatusVisual {Name="AgentStatus_"+id,_active=()=>GodotObject.IsInstanceValid(target) && target.Alive && active()};target.AddChild(node);node.Position=Vector3.Up*.10f;
        bool plasma=id=="plasmaprobe",probe=id=="interrogationprobe" || plasma;
        string texture=plasma?"5x5_flame_lick_04_bw":probe?"2x2_electrical_blast_alpha":"fx_smoke_2x2_alpha";
        int side=plasma?5:2;var image=GD.Load<Texture2D>("res://Assets/Agent/"+texture+".png");
        node._sprite=new Sprite3D {Texture=image,Hframes=side,Vframes=side,PixelSize=.15f/(image.GetWidth()/side),Shaded=false,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Modulate=plasma?new Color(1,.55f,.12f,.8f):probe?new Color(.4f,.7f,1,.75f):new Color(.35f,.8f,.08f,.6f)};
        node.AddChild(node._sprite);
        if(probe){
            var mesh=GD.Load<PackedScene>(plasma?"res://Assets/Agent/sp_plasma_probe_top.glb":"res://Assets/PresentationRepairs/vfx_probe_mini01.glb").Instantiate<Node3D>();node.AddChild(mesh);Fit(mesh,.045f);mesh.Position=Vector3.Up*.07f;
            if(id=="interrogationprobe"){
                node._sprite.Visible=false;node._interrogationProbe=mesh;Fit(mesh,.065f);mesh.Position=new Vector3(.065f,.05f,0);
                var mat=new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>("res://Assets/Agent/4x1_tiling_electrical_white_01.png"),AlbedoColor=new Color(.4f,.7f,1,.9f),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
                foreach(var end in new[]{new Vector3(0,-.035f,0),new Vector3(-.025f,-.09f,0),new Vector3(.025f,-.09f,0)}){
                    var ribbon=new ImmediateMesh();var zap=new MeshInstance3D {Mesh=ribbon,MaterialOverride=mat,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};node.AddChild(zap);node._zaps.Add((ribbon,zap,end));
                }
                node.UpdateInterrogation();
            }
            if(plasma){var bottom=GD.Load<PackedScene>("res://Assets/Agent/sp_plasma_probe_bottom.glb").Instantiate<Node3D>();node.AddChild(bottom);Fit(bottom,.045f);bottom.Position=Vector3.Up*.06f;}
        }
        return node;
    }
    public static void Fit(Node3D node,float size)
    {
        var meshes=node.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
        float native=meshes.Select(m=>m.GetAabb().Size.Length()).DefaultIfEmpty(1).Max();
        node.Scale=Vector3.One*(size/Math.Max(.0001f,native));
        foreach(var mesh in meshes)mesh.MaterialOverride=new StandardMaterial3D {AlbedoColor=new Color(.35f,.4f,.44f),Metallic=.7f,Roughness=.4f};
    }
    public override void _Process(double delta)
    {
        if(!_active()){QueueFree();return;}_age+=delta;
        _sprite.Frame=(int)(_age*18)%(_sprite.Hframes*_sprite.Vframes);
        UpdateInterrogation();
    }
    private void UpdateInterrogation()
    {
        if(_interrogationProbe==null)return;
        var from=new Vector3(.065f+Mathf.Sin((float)_age*2)*.012f,.05f+Mathf.Sin((float)_age*3)*.008f,0);_interrogationProbe.Position=from;_interrogationProbe.Rotation=new Vector3(0,(float)_age,0);
        Vector3 camera=GetViewport().GetCamera3D()?.GlobalBasis.Z??Vector3.Back;
        int phase=0;
        foreach(var (mesh,node,end) in _zaps){
            node.Visible=(((int)(_age*12)+phase++)%5)!=4;
            Vector3 side=(end-from).Cross(GlobalBasis.Inverse()*camera).Normalized()*.0045f;
            mesh.ClearSurfaces();mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
            void V(Vector3 p,Vector2 uv){mesh.SurfaceSetUV(uv);mesh.SurfaceAddVertex(p);}
            V(from-side,new Vector2(0,0));V(from+side,new Vector2(0,1));V(end+side,new Vector2(1,1));
            V(from-side,new Vector2(0,0));V(end+side,new Vector2(1,1));V(end-side,new Vector2(1,0));mesh.SurfaceEnd();
        }
    }
}
