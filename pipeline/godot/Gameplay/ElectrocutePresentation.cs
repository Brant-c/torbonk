using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
// Recovered Electrocute textures/events; survivor durations and exposure are gameplay adaptations.
public partial class ElectrocutePresentation : Node3D,IAbilityPresentation,ITargetImpactPresentation
{
 public CharacterPresentation Actor {get;set;}=null!;
 public AbilityDefinition Definition {get;set;}=null!;
 public GameWorld World {get;set;}=null!;
 private JsonDocument _audio=null!;
 private readonly List<(Node3D node,double life)> _tails=new();
 private readonly List<(Combatant target,Sprite3D sprite)> _targets=new();
 private AudioStreamPlayer3D _cast=null!,_hit=null!,_loop=null!;
 private double _clock;
 public int TargetImpacts {get;private set;}
 public bool LoopPlaying=>_loop.Playing;
 public override void _Ready(){
  _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Electrocute/audio.json"));
  AudioStreamPlayer3D Voice(){var v=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-20,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};AddChild(v);AudioPresentation.Find(this)?.RegisterCombatVoice(v);return v;}
  _cast=Voice();_hit=Voice();_loop=Voice();
 }
 private void Play(string name,AudioStreamPlayer3D voice,Vector3 at,bool loop=false){
  var root=_audio.RootElement;
  if(!root.GetProperty("events").TryGetProperty(name,out var id) || !root.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
  var media=graph.GetProperty("media");if(media.GetArrayLength()==0)return;
  var stream=(AudioStreamWav)GD.Load<AudioStreamWav>($"res://Assets/Electrocute/Audio/{media[0].GetInt32()}.wav").Duplicate();
  if(loop){stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);}
  voice.Stream=stream;voice.GlobalPosition=at;voice.Play();
 }
 private Vector3 Hand {get {int bone=Actor.Skeleton.FindBone("vfx_hand_right");return bone<0?Actor.GlobalPosition+Vector3.Up*.1f:(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(bone)).Origin;}}
 public bool Begin(Combatant target,Vector3 center,float radius){Play(Definition.CastAudioEvent,_cast,Hand);return Actor.Animator.HasAnimation(Definition.StartClip);}
 public void Channel(){}
 public void Impact(Vector3 center){Play(Definition.ImpactAudioEvent,_hit,center);}
 public void TargetImpact(Combatant target){
  TargetImpacts++;
  var tex=GD.Load<Texture2D>("res://Assets/Electrocute/512_singlearcb_bw.png");
  var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Lightning.gdshader")};material.SetShaderParameter("source_texture",tex);
  var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip,material);
  var from=Hand;var to=target.AimPosition;var across=(to-from).Cross(Vector3.Up).Normalized()*.006f;if(across.IsZeroApprox())across=Vector3.Right*.006f;
  for(int i=0;i<=16;i++){float t=i/16f;var p=from.Lerp(to,t)+Vector3.Up*Mathf.Sin(i*7.3f)*.015f*Mathf.Sin(t*Mathf.Pi);mesh.SurfaceSetColor(Colors.White);mesh.SurfaceSetUV(new Vector2(0,t*3));mesh.SurfaceAddVertex(ToLocal(p-across));mesh.SurfaceSetColor(Colors.White);mesh.SurfaceSetUV(new Vector2(1,t*3));mesh.SurfaceAddVertex(ToLocal(p+across));}
  mesh.SurfaceEnd();var arc=new MeshInstance3D{Mesh=mesh,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(arc);_tails.Add((arc,.3));
  var texture=GD.Load<Texture2D>("res://Assets/Electrocute/electric_rings_bw_02.png");
  var shader=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/D3Particle.gdshader")};shader.SetShaderParameter("source_texture",texture);
  var sprite=new Sprite3D{Texture=texture,Hframes=2,Vframes=2,PixelSize=.14f/(texture.GetWidth()/2),MaterialOverride=shader,Modulate=new Color(.65f,.2f,1,.7f),CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
  foreach(var old in _targets.Where(p=>p.target==target).ToArray()){old.sprite.QueueFree();_targets.Remove(old);}
  AddChild(sprite);sprite.GlobalPosition=target.AimPosition;_targets.Add((target,sprite));
  if(target is EnemyAgent {StunRemaining:>0})Play(Definition.LoopAudioEvent,_loop,target.AimPosition,true);
 }
 public override void _Process(double delta){
  _clock+=delta;
  for(int i=_tails.Count-1;i>=0;i--){var p=_tails[i];p.life-=delta;if(p.life<=0){p.node.QueueFree();_tails.RemoveAt(i);}else _tails[i]=p;}
  foreach(var p in _targets.ToArray()){
   if(!GodotObject.IsInstanceValid(p.target) || !p.target.Alive || !World.Build.Pregame.AssassinElectrified(p.target)){p.sprite.QueueFree();_targets.Remove(p);continue;}
   p.sprite.GlobalPosition=p.target.AimPosition;p.sprite.Frame=(int)(_clock*16)%4;
  }
  var audible=_targets.FirstOrDefault(p=>p.target is EnemyAgent {StunRemaining:>0});
  if(audible.target==null || !World.PlayerActor.Alive)_loop.Stop();else _loop.GlobalPosition=audible.target.AimPosition;
 }
 public void End(bool interrupted){if(interrupted)Release();}
 public void Complete(){}
 public void Release(){foreach(var p in _tails)p.node.QueueFree();_tails.Clear();foreach(var p in _targets)p.sprite.QueueFree();_targets.Clear();_cast.Stop();_hit.Stop();_loop.Stop();}
 public override void _ExitTree(){Release();_audio?.Dispose();}
}
