using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;
// Explicit adapters for recovered Warrior appearances; gameplay owns timers and hit admission.
public partial class D3AbilityPresentation
{
 private const string WarriorTexture="res://Assets/WarriorLibrary/Textures/art/fx/texture/";
 private bool MarauderVisual=>GetParent() is GameWorld w && w.RunConfiguration.AdvancedClassId=="marauder";
 private bool WarriorVisual=>GetParent() is GameWorld w && w.RunConfiguration.AdvancedClassId is "marauder" or "juggernaut";
 private double _warriorAuraClock;
 private AudioStreamPlayer3D? _warriorLoopVoice;
 public bool WarriorLoopPlaying=>_warriorLoopVoice?.Playing??false;
 private bool _warriorEnabled;
 private Combatant? _obfuscatedTarget;
 private readonly Dictionary<GeometryInstance3D,float> _camouflage=new();
 private readonly Dictionary<(MeshInstance3D mesh,int surface),Material?> _camouflageMaterials=new();
 private readonly List<MarauderThrownSaber> _throws=new();
 private readonly HashSet<ulong> _warriorTargets=new();
 public int MarauderThrowsPresented {get;private set;}
 public int MarauderAuraBursts {get;private set;}
 public int WarriorTargetImpacts {get;private set;}
 public int WarriorDistinctTargets=>_warriorTargets.Count;
 public int WarriorProcImpacts {get;private set;}
 public int DefensiveProcsPresented {get;private set;}
 public bool CamouflageVisible=>_camouflage.Count>0;
 public int AtaruImpacts {get;private set;}
 public int PredationTrailsPresented {get;private set;}
 public void AtaruImpact(Combatant target){AtaruImpacts++;WarriorFlash("trail/trail_white_hollow.png",target.AimPosition,.15f,.2,new(1,.65f,.25f,.8f),target);Play(Definition.ImpactAudio,target.AimPosition,4);}
 private bool AuraActive(GameWorld w)=>MarauderVisual?w.Build.Pregame.MarauderAuraActive(Definition.Id):w.Build.Pregame.JuggernautAuraActive(Definition.Id);
 private void AdvanceWarriorEffects(double delta)
 {
  _throws.RemoveAll(t=>!GodotObject.IsInstanceValid(t) || t.IsQueuedForDeletion());
  if(!WarriorVisual || GetParent() is not GameWorld world)return;
  if(Definition.Id=="ravage" && world.Abilities.Find("ravage") is {State:LightningState.Channeling} channel)
   Actor.FitGesture(channel,Definition.GestureSeconds*channel.TickSeconds/Math.Max(.001f,Definition.TickSeconds),true);
  bool active=_warriorEnabled && world.PlayerActor.Alive && AuraActive(world);
  if(Definition.Id=="forcecamouflage")SetCamouflage(active);
  if(!active){_warriorLoopVoice?.Stop();_marauderAura="";return;}
  if(Definition.Id=="berserk" && !WarriorLoopPlaying){
   _warriorLoopVoice??=CreateChannelVoice();
   var loop=(AudioStreamWav)GD.Load<AudioStreamWav>("res://Assets/WarriorLibrary/Audio/10244116.wav").Duplicate();
   loop.LoopMode=AudioStreamWav.LoopModeEnum.Forward;loop.LoopBegin=0;loop.LoopEnd=(int)(loop.GetLength()*loop.MixRate);
   _warriorLoopVoice.Stream=loop;_warriorLoopVoice.Play();
  }
  if(_warriorLoopVoice!=null)_warriorLoopVoice.GlobalPosition=Actor.GlobalPosition;
  _marauderAura=Definition.Id;
  _warriorAuraClock-=delta;if(_warriorAuraClock>0)return;_warriorAuraClock=.1;
  MarauderAuraBursts++;
  string id=Definition.Id;
  if(id=="endurepain"){
   foreach(string bone in new[]{"vfx_shoulder_left","vfx_shoulder_right","vfx_hip_left","vfx_hip_right","vfx_aim01"})
    WarriorFlash("smoke/2x2_crystal_smoke.png",BoneWorld(bone),.08f,1,new(.95f,.15f,.016f,.45f),bone:bone);
   return;
  }
  if(id=="enrageddefense"){
   DefenseRing("glow/halo_crescent_01.png",.24f,2,new(.95f,.11f,0,.55f));
   DefenseRing("glow/bw_blue_partial_ring.png",.22f,2,new(.95f,.11f,0,.55f));
   return;
  }
  if(id=="berserk"){
   foreach(string bone in new[]{"LeftShoulder","RightShoulder","Chest1","Neck","LeftElbow","RightElbow"}){
    WarriorFlash("smoke/2x2_whispy_smoke_a_01.png",BoneWorld(bone),.12f,.3,new(1,.22f,.08f,.45f),bone:bone);
    WarriorFlash("glow/glow_basic_alpha.png",BoneWorld(bone),.07f,.25,new(1,.15f,.04f,.4f),bone:bone);
   }return;
  }
  if(id=="predation"){
   foreach(string bone in new[]{"Chest","LeftWrist","RightWrist"})WarriorFlash("electricity/lighting_2x2_anim_01_bw.png",BoneWorld(bone),.09f,.3,new(1,.2f,.05f,.55f),bone:bone);
   if(Actor.Locomotion=="Run"){
    PredationTrailsPresented+=8;
    // World-space tails remain behind the moving actor; aura stays bone attached.
    foreach(string bone in new[]{"vfx_elbow_left","vfx_elbow_right","vfx_shoulder_left","vfx_shoulder_right","vfx_hip_left","vfx_hip_right","vfx_knee_left","vfx_knee_right"})
     WarriorFlash("trail/trail_melee_02.png",BoneWorld(bone),.09f,.3,new(1,.25f,.05f,.45f));
   }return;
  }
  string texture=id switch {
   "predation"=>"electricity/lighting_2x2_anim_01_bw.png",
   "berserk"=>"fire/5x5_flame_lick_04_bw.png",
   "frenzy"=>"glow/2x2_elec_blast_ring_alpha.png",
   "cloakofpain"=>"fire/shockwave_bw_01.png",
   "obfuscate"=>"electricity/512_singlearc_alpha_bw_tiled.png",
   "forcecamouflage"=>"smoke/2x2_smoke_whisps_02.png",
   "saberward"=>"glow/vfx_force_impact_ring.png",
   "ataruform"=>"trail/trail_white_hollow.png",
   "bloodthirst"=>"fire/shockwave_bw_01.png",
   "undyingrage"=>"smoke/2x2_whispy_smoke_a_01.png",
   "gore"=>"glow/fx_force_suppression_in.png",
   "deadlysaber"=>"fire/4x4_flame_lick_02_bw.png",
   "batteringassault"=>"spark/bw_fx_2x2_sparknglow_01.png",
   _=>"glow/bw_flare_gray.png"};
  Color tint=id switch {
   "predation"=>new(1,.18f,.04f,.55f),"frenzy"=>new(1,.7f,.15f,.65f),
   "forcecamouflage"=>new(.5f,.3f,.7f,.25f),"obfuscate"=>new(.7f,.18f,.25f,.5f),
   "saberward"=>new(1,.65f,.3f,.4f),_=>new(1,.12f,.035f,.6f)};
  if(id=="obfuscate") {
   if(GodotObject.IsInstanceValid(_obfuscatedTarget) && _obfuscatedTarget!.Alive)
    WarriorFlash(texture,_obfuscatedTarget.AimPosition,.12f,.25,tint,_obfuscatedTarget);
   return;
  }
  foreach(string bone in id is "deadlysaber" or "gore" or "batteringassault"?new[]{"LeftWeapon","RightWeapon"}:new[]{"Chest","LeftWrist","RightWrist"})
   WarriorFlash(texture,BoneWorld(bone),bone=="Chest"?.16f:.07f,.3,tint,bone:bone);
 }
 private void SetCamouflage(bool active)
 {
  if(active && _camouflage.Count==0)
   foreach(var mesh in Actor.FindChildren("*","GeometryInstance3D",true,false).OfType<GeometryInstance3D>()){
    _camouflage[mesh]=mesh.Transparency;mesh.Transparency=Math.Max(mesh.Transparency,.78f);
    if(mesh is MeshInstance3D body && body.Mesh!=null)for(int i=0;i<body.Mesh.GetSurfaceCount();i++){
     if(body.GetActiveMaterial(i) is not BaseMaterial3D original)continue;
     _camouflageMaterials[(body,i)]=body.GetSurfaceOverrideMaterial(i);
     var faded=(BaseMaterial3D)original.Duplicate();faded.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
     faded.AlbedoColor=new Color(faded.AlbedoColor,faded.AlbedoColor.A*.22f);body.SetSurfaceOverrideMaterial(i,faded);
    }
   }
  if(active)return;
  foreach(var pair in _camouflage)if(GodotObject.IsInstanceValid(pair.Key))pair.Key.Transparency=pair.Value;
  foreach(var pair in _camouflageMaterials)if(GodotObject.IsInstanceValid(pair.Key.mesh))pair.Key.mesh.SetSurfaceOverrideMaterial(pair.Key.surface,pair.Value);
  _camouflageMaterials.Clear();_camouflage.Clear();
 }
 private void BeginWarrior(Combatant target)
 {
  if(!WarriorVisual)return;
  _warriorEnabled=true;_warriorTargets.Clear();_warriorAuraClock=0;
  if(GetParent() is GameWorld gw && gw.Abilities.Find(Definition.Id) is {} abilityOwner)Actor.FitGesture(abilityOwner,abilityOwner.GestureDuration);
  if(Definition.Effect!=AbilityEffect.Damage && Definition.CastAudio=="")Play(Definition.ImpactAudio,Actor.GlobalPosition,0);
  if(Definition.Id is "saberthrow" or "deadlythrow" or "viciousthrow" && GetParent() is GameWorld world){
   var ability=world.Abilities.Find(Definition.Id);
   double tick=ability?.TickSeconds??Definition.TickSeconds;
   foreach(string slot in Definition.Id=="saberthrow"?new[]{"main_hand","off_hand"}:new[]{"main_hand"})
    if(world.Equipment.Weapon(slot) is {} weapon){
     var flight=new MarauderThrownSaber{Source=weapon,Target=target,
      ContactSeconds=Definition.WindupSeconds+(Definition.ChannelSeconds>0?tick:0),ReturnSeconds=Definition.ChannelSeconds>0?tick:.3};
     world.AddChild(flight);_throws.Add(flight);MarauderThrowsPresented++;
    }
  }
  if(Definition.Id=="obfuscate" && GetParent() is GameWorld ow)
   _obfuscatedTarget=ow.Targeting.Acquire(Definition.Range,AbilityTargetRule.NearestHostile,0);
  if(Definition.Id=="endurepain"){
   WarriorFlash("glow/2x2_elec_blast_ring_alpha.png",BoneWorld("vfx_aim01"),.18f,.4,new(1,.03f,.03f,.7f),bone:"vfx_aim01");
   WarriorFlash("glow/2x2_light_rays_07.png",BoneWorld("vfx_aim01"),.18f,.4,new(.72f,.15f,.08f,.6f),bone:"vfx_aim01");
  }else if(Definition.Id=="enrageddefense"){
   // Second random container in the recovered buff event (the first uses CastAudio).
   Play("res://Assets/WarriorLibrary/Audio/857325983.wav",Actor.GlobalPosition,3);
   DefenseRing("glow/halo_crescent_01.png",.24f,1,new(.96f,.11f,0,.7f));
   WarriorFlash("fire/fx_explosion_core_02_bw.png",BoneWorld("Chest"),.24f,.7,new(.96f,.14f,0,.6f),bone:"Chest");
  }else if(Definition.Id=="smash"){
   WarriorFlash("electricity/bw_2x2_electricity_multi_01.png",BoneWorld("LeftWrist"),.1f,Definition.WindupSeconds,new(1,.35f,.05f,.5f),bone:"LeftWrist");
   WarriorFlash("glow/bw_flare_gray.png",BoneWorld("RightWrist"),.07f,Definition.WindupSeconds,new(1,.5f,.15f,.6f),bone:"RightWrist");
  }else if(Definition.Id=="berserk"){
   Play("res://Assets/WarriorLibrary/Audio/136110332.wav",Actor.GlobalPosition,4);
   WarriorFlash("glow/bw_fx_lsblue_flare_256.png",BoneWorld("Chest"),.22f,.4,new(1,.35f,.15f,.65f),bone:"Chest");
  }else if(Definition.Id=="predation"){
   Ring(Actor.GlobalPosition,WarriorTexture+"fire/shockwave_bw_01.png",.35f);
   WarriorFlash("glow/bw_flare_gray.png",BoneWorld("Chest"),.2f,.35,new(1,.4f,.1f,.65f),bone:"Chest");
  }else if(Definition.Id is "forcecrush" or "forcechoke")
   WarriorFlash("electricity/lighting_2x2_anim_01_bw.png",BoneWorld("LeftWrist"),.08f,.3,new(.65f,.15f,.8f,.7f),bone:"LeftWrist");
  else if(Definition.PresentationKind=="saber" && Definition.Effect==AbilityEffect.Damage){
   foreach(string hand in MarauderVisual?new[]{"LeftWeapon","RightWeapon"}:new[]{"RightWeapon"})WarriorFlash("glow/bw_flare_gray.png",BoneWorld(hand),.045f,Math.Max(.15,Definition.WindupSeconds),new(1,.2f,.05f,.6f),bone:hand);
  }
  else if(Definition.Effect!=AbilityEffect.Damage)
   WarriorFlash(Definition.Id=="forcecamouflage"?"smoke/2x2_smoke_whisps_02.png":"glow/bw_flare_gray.png",BoneWorld("Chest"),.18f,.35,new(1,.25f,.05f,.5f),bone:"Chest");
 }
 // The recovered Enraged Defense buff has no animation action. Healing has its own FXSpec/event.
 public void DefensiveHealProc()
 {
  if(!WarriorVisual || Definition.Id!="enrageddefense")return;
  DefensiveProcsPresented++;
  Play(Definition.ImpactAudio,Actor.GlobalPosition,4);
  WarriorFlash("fire/4x4_flame_lick_02_bw.png",BoneWorld("Chest"),.2f,1.5,new(.95f,.01f,.01f,.8f),bone:"Chest");
 }
 private void DefenseRing(string texture,float size,double life,Color tint)
 {
  var ring=Sprite(WarriorTexture+texture,size,1,false);ring.RotationDegrees=new Vector3(-90,0,0);
  Add(ring,life,fx=>{float t=(float)(fx.Age/fx.Life);ring.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.035f;ring.RotateZ(.03f);ring.Scale=Vector3.One*(.6f+.4f*t);ring.Modulate=new Color(tint,(1-t)*tint.A);});
  ring.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.035f;ring.Modulate=tint;
 }
 private bool ImpactWarrior(Vector3 at)
 {
  if(!WarriorVisual)return false;
  Play(Definition.ImpactAudio,at,3);
  if(Definition.PresentationKind=="saber" && Definition.ChannelSeconds>0){
   Play(Definition.WeaponAudio,Actor.GlobalPosition,1);
   if(Definition.OffhandAudio!="")_delayed.Add((.06,()=>Play(Definition.OffhandAudio,Actor.GlobalPosition,2)));
  }
  if(Definition.Id=="smash"){
   Ring(at,Definition.ImpactTexture,_radius*2.2f);
   Ring(at,WarriorTexture+"glow/bw_flare_gray.png",_radius*1.7f);
   for(int i=0;i<8;i++){
    float a=i*Mathf.Tau/8;var p=at+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*_radius*.65f;
    WarriorFlash("smoke/2x2_graysmoke_alpha.png",p,.16f,.65,new(.65f,.4f,.2f,.4f));
    WarriorFlash("electricity/bw_2x2_electricity_multi_01.png",p,.12f,.3,new(1,.5f,.1f,.7f));
   }
  }else if(Definition.Id=="intimidatingroar")Ring(at,Definition.ImpactTexture,_radius*2.2f);
  else if(Definition.Id=="forcescream"){
   var from=BoneWorld("Head");var wave=Sprite(WarriorTexture+"glow/2x2_elec_blast_ring_alpha.png",.07f,2);
   Add(wave,.18,fx=>{float t=(float)(fx.Age/fx.Life);wave.GlobalPosition=from.Lerp(at,t);wave.Scale=Vector3.One*(1+t*2);wave.Frame=Math.Min(3,(int)(t*4));wave.Modulate=new(1,.65f,.35f,1-t);});wave.GlobalPosition=from;
  }
  return true; // Successful targets receive their own contact effects below.
 }
 public void TargetImpact(Combatant target)
 {
  if(!GodotObject.IsInstanceValid(target))return;
  if(AssassinTargetImpact(target))return;
  if(!WarriorVisual)return;
  WarriorTargetImpacts++;_warriorTargets.Add(target.GetInstanceId());
  string id=Definition.Id;var at=target.AimPosition;
  if(Definition.PresentationKind=="saber") {
   WarriorFlash("weapon_melee/bw_lightsaber_clash01.png",at,.07f,.18,new(1,.65f,.3f,.85f),target);
   string texture=id switch {
    "rupture" or "impale"=>"damage/bw_fx_proj_melted_hole.png",
    "massacre"=>"weapon_melee/tr_horizontal_slash.png",
    "gore"=>"glow/fx_force_suppression_in.png",
    "annihilate"=>"fire/4x4_flame_lick_02_bw.png",
    "batteringassault"=>"spark/bw_fx_2x2_sparknglow_01.png",
    "sweepingslash" or "ravage"=>"trail/trail_white_hollow.png",
    "saberthrow" or "deadlythrow"=>"glow/2x2_elec_blast_ring_alpha.png",
    _=>"glow/bw_flare_gray.png"};
   WarriorFlash(texture,at,id=="massacre"?.18f:.1f,.3,new(1,.15f,.035f,.65f),target);
  }else if(id is "forcecrush" or "forcechoke")
   WarriorFlash("glow/force_choke.png",at,.15f,.85,new(.6f,.15f,.85f,.7f),target);
  else if(id=="smash") {
   WarriorFlash("electricity/bw_2x2_electricity_multi_01.png",at,.17f,.35,new(1,.5f,.15f,.8f),target);
   WarriorFlash("glow/2x2_elec_blast_ring_alpha.png",at,.13f,.3,new(1,.65f,.2f,.6f),target);
  }else if(id=="intimidatingroar")
   WarriorFlash("glow/fx_force_suppression_in.png",at+Vector3.Up*.07f,.14f,Math.Max(1,Definition.SlowSeconds),new(1,.4f,.1f,.65f),target);
  else WarriorFlash("glow/2x2_elec_blast_ring_alpha.png",at,.14f,.3,new(1,.6f,.3f,.7f),target);
 }
 public void ProcImpact(Combatant target)
 {
  if(!GodotObject.IsInstanceValid(target))return;
  if(AssassinTargetImpact(target))return;
  if(!WarriorVisual)return;
  WarriorProcImpacts++;
  WarriorFlash(Definition.Id=="cloakofpain"?"fire/shockwave_bw_01.png":"spark/bw_fx_2x2_sparknglow_01.png",target.AimPosition,.12f,.25,new(1,.2f,.04f,.75f),target);
 }
 private void WarriorFlash(string texture,Vector3 at,float size,double life,Color tint,Combatant? target=null,string bone="")
 {
  string path=texture.StartsWith("res://")?texture:WarriorTexture+texture;
  int frames=path.Contains("5x5")?5:path.Contains("4x4") || path.EndsWith("fx_explosion_core_02_bw.png")?4:path.Contains("2x2")?2:1;
  var sprite=Sprite(path,size,frames);sprite.Modulate=tint;var offset=target!=null?at-target.AimPosition:Vector3.Zero;
  Add(sprite,life,fx=>{
   float t=(float)(fx.Age/fx.Life);
   if(bone!="")sprite.GlobalPosition=BoneWorld(bone);
   else if(GodotObject.IsInstanceValid(target) && target!.Alive)sprite.GlobalPosition=target.AimPosition+offset;
   sprite.Frame=Math.Min(frames*frames-1,(int)(t*frames*frames));sprite.Modulate=new(tint,Math.Min(1,(1-t)*3)*tint.A);
  });sprite.GlobalPosition=at;
 }
 private void ReleaseWarrior()
 {
  _warriorLoopVoice?.Stop();
  _warriorEnabled=false;_obfuscatedTarget=null;SetCamouflage(false);
  foreach(var flight in _throws)if(GodotObject.IsInstanceValid(flight)){
   if(flight.IsInsideTree())flight.GetParent().RemoveChild(flight);
   flight.QueueFree();
  }
  _throws.Clear();_warriorTargets.Clear();
 }
}
