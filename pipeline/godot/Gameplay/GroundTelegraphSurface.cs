using Godot;

namespace SwtorSurvivors;

/// <summary>Persistent warning mesh, refreshed only when a strike locks. Samples original terrain, never adds collision.</summary>
public sealed class GroundTelegraphSurface
{
    private const int Side=17;
    private readonly Vector3[] _vertices=new Vector3[Side*Side];
    private readonly Godot.Collections.Array _arrays=new();
    public ArrayMesh Mesh {get;}=new();
    public GroundTelegraphSurface()
    {
        var uv=new Vector2[Side*Side];var indices=new int[(Side-1)*(Side-1)*6];int n=0;
        for(int z=0;z<Side;z++)for(int x=0;x<Side;x++)
        {
            uv[z*Side+x]=new Vector2((float)x/(Side-1),(float)z/(Side-1));
            if(x==Side-1 || z==Side-1)continue;int a=z*Side+x,b=a+1,c=a+Side,d=c+1;
            indices[n++]=a;indices[n++]=b;indices[n++]=c;indices[n++]=b;indices[n++]=d;indices[n++]=c;
        }
        _arrays.Resize((int)Godot.Mesh.ArrayType.Max);
        _arrays[(int)Godot.Mesh.ArrayType.TexUV]=uv;_arrays[(int)Godot.Mesh.ArrayType.Index]=indices;
    }
    public void Project(MeshInstance3D marker,ArenaBounds arena)
    {
        for(int z=0;z<Side;z++)for(int x=0;x<Side;x++)
        {
            var local=new Vector3((float)x/(Side-1)-.5f,0,(float)z/(Side-1)-.5f);
            var world=marker.ToGlobal(local);
            if(arena.GroundAt(world,out var floor,out _,1.5f,3))world.Y=floor.Y+.012f;
            _vertices[z*Side+x]=marker.ToLocal(world);
        }
        _arrays[(int)Godot.Mesh.ArrayType.Vertex]=_vertices;
        Mesh.ClearSurfaces();Mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,_arrays);
    }
}
