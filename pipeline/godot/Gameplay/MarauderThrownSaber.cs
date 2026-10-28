using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;
/// <summary>Render-only outbound/return flight of the equipped recovered hilt and blade meshes.</summary>
public partial class MarauderThrownSaber : Node3D
{
 public EquipmentAttachment Source {get;set;}=null!;
 public Combatant Target {get;set;}=null!;
 public double ContactSeconds {get;set;}=.45;
 public double ReturnSeconds {get;set;}=.45;
 public double Elapsed=>_age;
 public bool Returning=>_age>=ContactSeconds;
 private static readonly Dictionary<EquipmentAttachment,(int count,bool visible)> Hidden=new();
 private Node3D _copy=null!;private Vector3 _start,_aim;private double _age;private bool _leased;
 public override void _Ready(){
  var lease=Hidden.GetValueOrDefault(Source,(0,Source.Visible));Hidden[Source]=(lease.Item1+1,lease.Item2);_leased=true;_start=Source.GlobalPosition;_aim=Target.AimPosition;
  _copy=new Node3D();AddChild(_copy);GlobalPosition=_start;
  foreach(var mesh in Source.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()){
   var clone=new MeshInstance3D{Mesh=mesh.Mesh,MaterialOverride=mesh.MaterialOverride,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};_copy.AddChild(clone);clone.Transform=Source.GlobalTransform.AffineInverse()*mesh.GlobalTransform;
  }
  _copy.Basis=Source.GlobalBasis;Source.Visible=false;
 }
 public override void _Process(double delta){
  _age+=delta;
  if(_age>=ContactSeconds+ReturnSeconds || !GodotObject.IsInstanceValid(Source)){QueueFree();return;}
  if(GodotObject.IsInstanceValid(Target) && Target.Alive && !Returning)_aim=Target.AimPosition;
  var home=Source.GlobalPosition;GlobalPosition=!Returning?_start.Lerp(_aim,(float)(_age/Math.Max(.01,ContactSeconds))):_aim.Lerp(home,(float)((_age-ContactSeconds)/Math.Max(.01,ReturnSeconds)));
  _copy.RotateY((float)delta*22);
 }
 public override void _ExitTree(){if(!_leased)return;_leased=false;if(!Hidden.TryGetValue(Source,out var lease))return;if(lease.count>1)Hidden[Source]=(lease.count-1,lease.visible);else{Hidden.Remove(Source);if(GodotObject.IsInstanceValid(Source))Source.Visible=lease.visible;}}
}
