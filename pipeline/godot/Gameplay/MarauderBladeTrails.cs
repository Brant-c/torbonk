using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;
/// <summary>Short recovered texture ribbons follow the actual equipped blade geometry on both sockets.</summary>
public partial class MarauderBladeTrails : Node3D
{
 public CharacterPresentation Actor {get;set;}=null!;
 private readonly List<(MeshInstance3D blade,Vector3 root,Vector3 tip)> _previous=new();
 private readonly List<(MeshInstance3D mesh,double life)> _ribbons=new();
 private StandardMaterial3D _material=null!;
 public int RibbonCount=>_ribbons.Count;
 public override void _Ready(){ProcessPriority=100;_material=new StandardMaterial3D{ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,CullMode=BaseMaterial3D.CullModeEnum.Disabled,AlbedoTexture=GD.Load<Texture2D>("res://Assets/WarriorLibrary/Textures/art/fx/texture/weapon_melee/lightsaber02_trail_red_nomips.png"),AlbedoColor=new Color(1,.22f,.12f,.4f)};}
 public override void _Process(double delta){
  for(int i=_ribbons.Count-1;i>=0;i--){var r=_ribbons[i];r.life-=delta;if(r.life<=0){r.mesh.QueueFree();_ribbons.RemoveAt(i);}else{r.mesh.Transparency=(float)(1-r.life/.12);_ribbons[i]=r;}}
  bool active=Actor.GestureOwner is AutomaticAbility a && a.Definition.PresentationKind=="saber" && a.Definition.Effect==AbilityEffect.Damage;
  if(!active){_previous.Clear();return;}
  var blades=Actor.FindChildren("RedBlade","MeshInstance3D",true,false).OfType<MeshInstance3D>().Where(b=>b.IsVisibleInTree() && b.Mesh is CylinderMesh c && c.TopRadius<.002f).ToArray();
  foreach(var b in blades){var c=(CylinderMesh)b.Mesh;var root=b.ToGlobal(Vector3.Down*c.Height/2);var tip=b.ToGlobal(Vector3.Up*c.Height/2);var old=_previous.FirstOrDefault(x=>x.blade==b);
   if(old.blade!=null && tip.DistanceSquaredTo(old.tip)>.000001f && _ribbons.Count<32){
    var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);void V(Vector3 v,Vector2 uv){mesh.SurfaceSetUV(uv);mesh.SurfaceAddVertex(v);}
    V(old.root,new(0,0));V(old.tip,new(0,1));V(tip,new(1,1));V(old.root,new(0,0));V(tip,new(1,1));V(root,new(1,0));mesh.SurfaceEnd();
    var node=new MeshInstance3D{Mesh=mesh,MaterialOverride=_material,TopLevel=true,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(node);node.GlobalTransform=Transform3D.Identity;_ribbons.Add((node,.12));
   }
   _previous.RemoveAll(x=>x.blade==b);_previous.Add((b,root,tip));
  }
 }
}
