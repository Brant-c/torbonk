using Godot;
using System;
using System.Linq;
namespace SwtorSurvivors;
// Recovered knife geometry follows each original animation's source FX attachment.
public partial class AgentKnifeVisual : Node3D
{
    private CharacterPresentation _actor=null!;
    private object? _owner;
    private Node3D _knife=null!;
    private StandardMaterial3D? _trailMaterial;
    private MeshInstance3D? _bladeMesh;
    private Vector3 _previousRoot,_previousTip;
    private bool _hasPrevious;
    private readonly System.Collections.Generic.List<(MeshInstance3D mesh,double life)> _trails=new();
    private (EquipmentAttachment item,bool visible)[] _weapons=[];
    private double _age,_duration;
    private bool _laceration;
    public string SocketName=>_laceration?"LeftWeapon":"RightWeapon";
    public bool RifleHidden=>_weapons.All(w=>!w.item.Visible);
    public int HiddenRifleCount=>_weapons.Count(w=>!w.item.Visible);
    public static AgentKnifeVisual Create(CharacterPresentation actor,double duration,bool laceration=false)
    {
        foreach(var old in actor.FindChildren("*","Node3D",true,false).OfType<AgentKnifeVisual>().ToArray()){old.Restore();old.GetParent().RemoveChild(old);old.QueueFree();}
        var node=new AgentKnifeVisual {_actor=actor,_owner=actor.GestureOwner,_duration=duration,_laceration=laceration};actor.AddChild(node);
        node._weapons=actor.Skeleton.FindChildren("*","Node3D",true,false).OfType<EquipmentAttachment>().Where(w=>w.Socket.BoneName=="RightWeapon").Select(w=>(w,w.Visible)).ToArray();
        foreach(var w in node._weapons)w.item.Visible=false;
        node._knife=GD.Load<PackedScene>("res://Assets/Agent/spy_shiv_low01.glb").Instantiate<Node3D>();node.AddChild(node._knife);AgentStatusVisual.Fit(node._knife,.065f);node.UpdatePose();
        if(laceration){
            node._knife.RotationDegrees=new Vector3(0,180,0);node._knife.Position=new Vector3(0,0,.011f);
            // sp_knife_trail_lacerate_p: original fan artwork and green trail colour.
            var texture=GD.Load<Texture2D>("res://Assets/Agent/lightsaber02_trail_fan_white.png");
            node._trailMaterial=new StandardMaterial3D {AlbedoTexture=texture,AlbedoColor=new Color(.349f,.993f,.327f,.45f),ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
            node._bladeMesh=node._knife.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().FirstOrDefault();
        }
        return node;
    }
    private void UpdatePose()
    {
        int bone=_actor.Skeleton.FindBone(SocketName);
        var socket=_actor.Skeleton.GlobalTransform*_actor.Skeleton.GetBoneGlobalPose(bone);
        GlobalTransform=new Transform3D(socket.Basis.Orthonormalized(),socket.Origin);
        _knife.Visible=_age>=.066;
        if(_laceration && _age>=_duration/2){_knife.Visible=false;foreach(var weapon in _weapons)if(GodotObject.IsInstanceValid(weapon.item))weapon.item.Visible=weapon.visible;}
        if(_bladeMesh!=null && _knife.Visible && _age<_duration/2){
            var box=_bladeMesh.GetAabb();int axis=(int)box.Size.MaxAxisIndex();var half=Vector3.Zero;half[axis]=box.Size[axis]/2;
            Vector3 center=box.Position+box.Size/2,root=_bladeMesh.ToGlobal(center-half),tip=_bladeMesh.ToGlobal(center+half);
            if(_hasPrevious && tip.DistanceSquaredTo(_previousTip)>.0000001f && _trails.Count<12){
                var ribbon=new ImmediateMesh();ribbon.SurfaceBegin(Mesh.PrimitiveType.Triangles);
                void V(Vector3 p,Vector2 uv){ribbon.SurfaceSetUV(uv);ribbon.SurfaceAddVertex(p);}
                V(_previousRoot,new(0,0));V(_previousTip,new(0,1));V(tip,new(1,1));V(_previousRoot,new(0,0));V(tip,new(1,1));V(root,new(1,0));ribbon.SurfaceEnd();
                var trail=new MeshInstance3D {Mesh=ribbon,MaterialOverride=_trailMaterial,TopLevel=true,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(trail);trail.GlobalTransform=Transform3D.Identity;_trails.Add((trail,.08));
            }
            _hasPrevious=true;_previousRoot=root;_previousTip=tip;
        }else _hasPrevious=false;
    }
    private void Restore(){foreach(var w in _weapons)if(GodotObject.IsInstanceValid(w.item))w.item.Visible=w.visible;_weapons=[];}
    public void Stop(){Visible=false;Restore();QueueFree();}
    public override void _Process(double delta)
    {
        _age+=delta;
        for(int i=_trails.Count-1;i>=0;i--){var item=_trails[i];item.life-=delta;if(item.life<=0){item.mesh.QueueFree();_trails.RemoveAt(i);}else{item.mesh.Transparency=(float)(1-item.life/.08);_trails[i]=item;}}
        if(!GodotObject.IsInstanceValid(_actor) || _age>=_duration || (_owner!=null && _actor.GestureOwner!=_owner)){Stop();return;}
        UpdatePose();
    }
    public override void _ExitTree()=>Restore();
}
