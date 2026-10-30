using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class WarriorPresentationSmoke : Node
{
 private readonly List<string> _checks=new();
 private void Check(string label,bool ok){if(!ok)throw new Exception(label);_checks.Add(label);}
 public async void Run(GameWorld w)
 {
  string? failure=null;string cls=w.RunConfiguration.AdvancedClassId;
  bool capture=OS.GetCmdlineUserArgs().Contains("--warrior-presentation-captures");
  try {
   w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
   w.Player.SetPhysicsProcess(false);w.Player.Presentation.SetProcess(false);w.Build.SetPhysicsProcess(false);
   w.Targeting.EnvironmentMask=0; // Isolated presentation fixtures do not depend on incidental map props.
   foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
   var targets=new List<EnemyAgent>();
   for(int i=0;i<3;i++){
    var body=w.Spawner.EnemyScene.Instantiate<CharacterBody3D>();var t=body.GetNode<EnemyAgent>("Actor");
    t.Target=w.PlayerActor;t.Navigation=w.Spawner.Navigation;t.SetPhysicsProcess(false);
    w.AddChild(body);body.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3((i-1)*.08f,0,-.22f);
    t.Health.Maximum=100000;t.Health.Restore();targets.Add(t);
   }
   await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
   foreach(var a in w.Abilities.Abilities)if(a.Presentation is D3AbilityPresentation fx)fx.SetProcess(false);
   if(capture){
    foreach(var layer in w.GetChildren().OfType<CanvasLayer>())layer.Visible=false;
    w.Player.Rig.SetProcess(false);w.Player.Rig.SetPhysicsProcess(false);
    var camera=new Camera3D{Fov=42};w.AddChild(camera);camera.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(.65f,.65f,.8f);
    camera.LookAt(w.PlayerActor.GlobalPosition+new Vector3(0,.12f,-.1f));camera.Current=true;
   }
   void Reset(){
    w.Player.ResetPlayer();
    w.Abilities.ResetCombat();w.Build.Pregame.Reset();w.Build.Reset();w.Periodic.Clear();w.Player.Presentation.ResetPresentation();
    foreach(var a in w.Abilities.Abilities)a.SetUnlocked(false);
    for(int i=0;i<targets.Count;i++){var t=targets[i];t.CombatEnabled=true;t.Health.Restore();t.Body.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3((i-1)*.08f,0,-.22f);}
    w.PlayerActor.Health.ClearShield();w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(w.PlayerActor.Health.Maximum*.85f);
   }
   AutomaticAbility Start(string id){
    if(cls=="marauder" && id=="forcecharge")foreach(var t in targets)t.Body.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.7f;
    if(cls=="marauder" && id=="retaliation")w.Build.Pregame.Damaged(1);
    if(id=="viciousthrow")foreach(var t in targets)t.Health.ApplyDamage(t.Health.Current-t.Health.Maximum*.2f);
    var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();w.Player.Presentation.ResetPresentation();
    Check(id+" can start",a.TryBegin());return a;
   }
   var smash=w.Abilities.Find("smash")!;var shared=GD.Load<AbilityDefinition>("res://Gameplay/Abilities/smash.tres");
   Check("Smash shares leap and landing timing",smash.Definition.StartClip==shared.StartClip && smash.Definition.WindupSeconds==shared.WindupSeconds && smash.Definition.GestureSeconds==shared.GestureSeconds && smash.Definition.FullBodyGesture);
   Check("Smash shares cast and impact audio",smash.Definition.CastAudio==shared.CastAudio && smash.Definition.ImpactAudio==shared.ImpactAudio);
   if(cls=="marauder"){
    Check("Smash reward uses Sith icon",w.Progression!.Upgrades.Single(u=>u.Id=="marauder_unlock_smash").Icon==smash.Definition.Icon);
    Check("Carnage attacks have distinct recovered clips",new[]{"batteringassault","gore","massacre","annihilate"}.Select(id=>w.Abilities.Find(id)!.Definition.StartClip).Distinct().Count()==4);
   }
   foreach(var a in w.Abilities.Abilities.Where(a=>!a.Definition.IsBasicAttack).ToArray()){
    Reset();
    if(a.Definition.Id=="frenzy"){var b=Start("berserk");b.Interrupt();}
    var d=a.Definition;var fx=(D3AbilityPresentation)a.Presentation;
    foreach(string clip in new[]{d.StartClip,d.LoopClip}.Where(s=>s!=""))Check(d.Id+" clip loaded: "+clip,w.Player.Presentation.Animator.HasAnimation(clip));
    int before=fx.WarriorTargetImpacts;Start(d.Id);
    Check(d.Id+" respects authored gesture",d.GestureSeconds>0?w.Player.Presentation.GestureOwner==a:w.Player.Presentation.GestureOwner!=a);
    bool snapped=false,checkedAudio=false,noEarlySmash=true;float rootMin=float.PositiveInfinity,rootMax=float.NegativeInfinity;
    int root=w.Player.Presentation.Skeleton.FindBone("Root");
    double snapAt=d.Effect!=AbilityEffect.Damage?.45:d.WindupSeconds+(a.ChannelSeconds>0?a.TickSeconds:0)+.04;
    int steps=(int)Math.Ceiling((d.WindupSeconds+a.ChannelSeconds+d.GestureSeconds+d.RecoverySeconds+.3)/.02);
    for(int i=0;i<steps;i++){
     a.Advance(.02);
     // Presentation fixture advances the dash along unobstructed authored space.
     // Gameplay separately checks that a blocked charge cannot hit at range.
     if(d.Id=="forcecharge" && a.CastTarget is {} charged && charged.GlobalPosition.DistanceTo(w.PlayerActor.GlobalPosition)>.3f)
      w.Player.GlobalPosition=w.Player.GlobalPosition.MoveToward(charged.GlobalPosition,2.2f*.02f);
     w.Player.Presentation.Present(Vector3.Right*.12f,.02);
     w.Player.Presentation._Process(.02);fx._Process(.02);
     foreach(var flight in w.GetChildren().OfType<MarauderThrownSaber>().ToArray()){flight.SetProcess(false);flight._Process(.02);}
     if(d.Id=="ravage" && !checkedAudio && i*.02>=snapAt){Check("Ravage presents timed strike audio",fx.PlayingAudioVoices>0);checkedAudio=true;}
     if(d.Id=="smash"){
      if(i*.02<d.WindupSeconds-.04)noEarlySmash&=a.DamageEvents==0;
      if(root>=0){float y=w.Player.Presentation.Skeleton.GetBoneGlobalPose(root).Origin.Y;rootMin=Math.Min(rootMin,y);rootMax=Math.Max(rootMax,y);}
     }
     if(capture && !snapped && i*.02>=snapAt){
      var captureCamera=GetViewport().GetCamera3D();
      captureCamera.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(.65f,.65f,.8f);
      captureCamera.LookAt(w.PlayerActor.GlobalPosition+new Vector3(0,.12f,-.1f));
      await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
      GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/warrior-{cls}-{d.Id}.png"));snapped=true;
     }
    }
    if(d.Effect==AbilityEffect.Damage){
     Check(d.Id+" damaged enemies",a.DamageEvents>0);
     Check(d.Id+" presents every damaged enemy",fx.WarriorTargetImpacts-before==a.DamageEvents);
     Check(d.Id+" tracks all distinct contacts",fx.WarriorDistinctTargets==a.LastDistinctHits);
    }
    if(d.Id=="smash"){
     Check("Smash reaches three victims",a.LastDistinctHits==3);
     Check("Smash waits for landing",noEarlySmash);
     Check("Smash animates the leap while moving",rootMax-rootMin>.01f);
    }
    a.Abort();Check(d.Id+" releases transient effects",fx.EffectCount==0);
    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
   }
   if(cls=="juggernaut"){
    Reset();var thrown=Start("viciousthrow");
    var flight=w.GetChildren().OfType<MarauderThrownSaber>().Single(f=>!f.IsQueuedForDeletion());
    Check("Juggernaut throw leases main-hand weapon",!w.Equipment.Weapon("main_hand")!.Visible);
    thrown.Advance(.1);flight._Process(.1);Check("Juggernaut throw waits for visible contact",thrown.DamageEvents==0 && !flight.Returning);
    thrown.Advance(.11);flight._Process(.11);Check("Juggernaut throw contact matches damage",thrown.DamageEvents==1 && flight.Returning);
    thrown.Abort();Check("Juggernaut throw abort restores saber",w.Equipment.Weapon("main_hand")!.Visible);
    Reset();var endure=Start("endurepain");var endureFx=(D3AbilityPresentation)endure.Presentation;
    Check("Endure Pain uses recovered aura instead of energy shell",endureFx.GetNodeOrNull<Node3D>("EndurePainAura")!=null && endureFx.GetNodeOrNull<Node3D>("EnergyShieldBubble")==null);
    Check("Endure Pain plays second cast layer",endureFx.LastPlayedAudioPath==endure.Definition.ImpactAudio);
    int bursts=endureFx.MarauderAuraBursts;endureFx._Process(.13);Check("Endure Pain emits shield-bound smoke",endureFx.MarauderAuraBursts>bursts);
    w.PlayerActor.Health.ClearShield();endureFx._Process(1.1);Check("Endure Pain clears when shield ends",!endureFx.ShieldVisible && endureFx.EffectCount==0);endure.Abort();
    Reset();var defense=Start("enrageddefense");var defenseFx=(D3AbilityPresentation)defense.Presentation;
    Check("Enraged Defense preserves locomotion without invented attack",defense.Definition.StartClip=="" && w.Player.Presentation.GestureOwner!=defense);
    Check("Enraged Defense startup samples the recovered atlas",defenseFx.GetChildren().OfType<Sprite3D>().Any(s=>s.Texture.ResourcePath.EndsWith("fx_explosion_core_02_bw.png") && s.Hframes==4 && s.Vframes==4));
    bursts=defenseFx.MarauderAuraBursts;defenseFx._Process(.13);Check("Enraged Defense emits recovered rings",defenseFx.MarauderAuraBursts>bursts);
    int procs=defenseFx.DefensiveProcsPresented;w.PlayerActor.Health.ApplyDamage(1);
    Check("Enraged Defense heal dispatches proc visual",defenseFx.DefensiveProcsPresented==procs+1);
    Check("Enraged Defense plays recovered proc event",defenseFx.LastPlayedAudioPath.EndsWith("/622419195.wav"));
    w.Build.Pregame.Reset();defenseFx._Process(2.1);Check("Enraged Defense clears on expiry",defenseFx.EffectCount==0);defense.Abort();
   }
   if(cls=="marauder"){
    Reset();
    foreach(string id in new[]{"berserk","predation","frenzy"}){
     var a=w.Abilities.Find(id)!;w.Build.Pregame.Activated(a);a.Presentation.Begin(targets[0],targets[0].GlobalPosition,.2f);
    }
    foreach(string id in new[]{"berserk","predation","frenzy"}){
     var fx=(D3AbilityPresentation)w.Abilities.Find(id)!.Presentation;int n=fx.MarauderAuraBursts;fx._Process(.13);
     Check(id+" emits during overlapping buffs",fx.MarauderAuraBursts>n);
    }
    foreach(string id in new[]{"deadlysaber","gore","batteringassault","cloakofpain","obfuscate","forcecamouflage"}){
     Reset();var a=w.Abilities.Find(id)!;a.Presentation.Begin(targets[0],targets[0].GlobalPosition,.2f);w.Build.Pregame.Activated(a);
     var fx=(D3AbilityPresentation)a.Presentation;int n=fx.MarauderAuraBursts;fx._Process(.13);Check(id+" shows gameplay window",fx.MarauderAuraBursts>n);
     if(id=="forcecamouflage")Check("Camouflage fades equipped model",fx.CamouflageVisible);
     w.Build.Pregame.Reset();fx._Process(.7);Check(id+" expires with gameplay",!fx.CamouflageVisible && fx.EffectCount==0);
     a.Abort();
    }
    Reset();var fast=Start("ravage");fast.TickSeconds=.25f;var fastFx=(D3AbilityPresentation)fast.Presentation;
    Vector3? lastWrist=null;float lateMotion=0;int wrist=w.Player.Presentation.Skeleton.FindBone("RightWrist");
    for(int i=0;i<72;i++){
     fast.Advance(.02);w.Player.Presentation._Process(.02);fastFx._Process(.02);
     var pose=w.Player.Presentation.Skeleton.GetBoneGlobalPose(wrist).Origin;
     if(i>40 && lastWrist.HasValue)lateMotion+=pose.DistanceTo(lastWrist.Value);lastWrist=pose;
    }
    Check("Ravage playback follows accelerated hit timing",(float)w.Player.Presentation.Tree.Get("parameters/GestureRate/scale")>1.9f);
    Check("Accelerated Ravage continues animating later hits",fast.DamageEvents>3 && lateMotion>.01f);fast.Abort();
    Reset();var thrown=Start("deadlythrow");var flight=w.GetChildren().OfType<MarauderThrownSaber>().Single(f=>!f.IsQueuedForDeletion());
    thrown.Advance(.15);flight._Process(.15);Check("Deadly Throw waits for outbound flight",thrown.DamageEvents==0 && !flight.Returning);
    thrown.Advance(.16);flight._Process(.16);Check("Deadly Throw lands with flight contact",thrown.DamageEvents==1 && flight.Returning);
    thrown.Abort();Check("Interrupted throw restores held weapon immediately",w.Equipment.Weapon("main_hand")!.Visible);
    Reset();var pain=w.Abilities.Find("cloakofpain")!;pain.Presentation.Begin(targets[0],targets[0].GlobalPosition,.2f);w.Build.Pregame.Activated(pain);
    var painFx=(D3AbilityPresentation)pain.Presentation;int procBefore=painFx.WarriorProcImpacts;w.PlayerActor.Health.ApplyDamage(1);
    Check("Cloak retaliation presents its own effect",painFx.WarriorProcImpacts>procBefore);
    Reset();var lethal=Start("annihilate");var victim=lethal.CastTarget!;victim.Health.ApplyDamage(victim.Health.Current-1);lethal.Advance(.2);
    var lethalFx=(D3AbilityPresentation)lethal.Presentation;Check("Lethal contact keeps visible tail",!victim.Alive && lethalFx.EffectCount>0);lethal.Abort();
   }
  }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/warrior-presentation-{cls}.json"),JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure},new JsonSerializerOptions{WriteIndented=true}));
  GetTree().Quit(failure==null?0:1);
 }
}
