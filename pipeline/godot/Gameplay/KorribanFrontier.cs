using Godot;
using System;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Authored border extension from the outer source height samples, using the same splat materials.
/// Interior source holes are preserved. The outer apron provides scenery beyond the movement boundary.</summary>
public partial class KorribanFrontier : Node3D
{
    public override void _Ready()
    {
        using var data=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Korriban/D4/environment.json"));
        foreach(var patch in data.RootElement.GetProperty("terrain").EnumerateArray())
        {
            var p=patch.GetProperty("position");var origin=new Vector3(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle());
            var samples=patch.GetProperty("vertices");int width=patch.GetProperty("width").GetInt32(),depth=patch.GetProperty("depth").GetInt32();
            Vector3 At(int x,int z) {var v=samples[z*width+x];return origin+new Vector3(v[0].GetSingle(),v[1].GetSingle(),v[2].GetSingle());}
            var st=new SurfaceTool();st.Begin(Mesh.PrimitiveType.Triangles);int count=0;
            void Triangle(Vector3 a,Vector3 b,Vector3 c) {
                foreach(var v in new[]{a,b,c}){st.SetUV(new Vector2(v.X,v.Z)*.2f);st.AddVertex(v);}count++;
            }
            void Edge(Vector3 a,Vector3 b,Vector3 outward) {
                var c=a+outward*8+Vector3.Down*.3f;var d=b+outward*8+Vector3.Down*.3f;
                Triangle(a,b,c);Triangle(b,d,c);
            }
            for(int x=0;x<width-1;x++) {
                if(At(x,0).Z<6.4f)Edge(At(x+1,0),At(x,0),Vector3.Forward);
                if(At(x,depth-1).Z>31.4f)Edge(At(x,depth-1),At(x+1,depth-1),Vector3.Back);
            }
            for(int z=0;z<depth-1;z++) {
                if(At(0,z).X<20.4f)Edge(At(0,z),At(0,z+1),Vector3.Left);
                if(At(width-1,z).X>58f)Edge(At(width-1,z+1),At(width-1,z),Vector3.Right);
            }
            if(count==0)continue;
            st.GenerateNormals();var mesh=st.Commit();
            var mat=new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>(patch.GetProperty("maps").GetProperty("albedo").GetString()!),CullMode=BaseMaterial3D.CullModeEnum.Disabled,Roughness=.95f};
            mesh.SurfaceSetMaterial(0,mat);var instance=new MeshInstance3D {Mesh=mesh};AddChild(instance);
            var body=new StaticBody3D {CollisionLayer=1,CollisionMask=4};instance.AddChild(body);body.AddChild(new CollisionShape3D {Shape=mesh.CreateTrimeshShape()});
        }
    }
}
