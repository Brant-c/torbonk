using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class WarriorMovementSmoke : Node
{
 private readonly List<string> _checks=new();
 private readonly Dictionary<string,float> _runRates=new();
 private void Check(string text,bool ok){if(!ok)throw new Exception(text);_checks.Add(text);}
 public async void Run(GameWorld w)
 {
  string? failure=null;string cls=w.RunConfiguration.AdvancedClassId;bool capture=OS.GetCmdlineUserArgs().Contains("--warrior-movement-captures");
  try{
   w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
   var actor=w.Player.Presentation;actor.SetProcess(false);w.Targeting.EnvironmentMask=0;
   foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
   foreach(var a in w.Abilities.Abilities)if(a.Presentation is D3AbilityPresentation fx)fx.SetProcess(false);
   var targets=new List<Combatant>();
   for(int i=0;i<3;i++){
    var target=new Combatant{Name="MovingFixture"+i,Team=CombatTeam.Hostile};target.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(target);
    target.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3((i-1)*.04f,0,-.2f);targets.Add(target);
   }
   await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
   var basic=w.Abilities.Abilities.Single(a=>a.Definition.IsBasicAttack);
   Check("baseline is unlocked in quickbar slot 1",basic.Unlocked && w.Abilities.QuickbarAbilities[0]==basic);
   Check("two starting attacks use no offensive slots",w.Combat.Unlocked && w.Abilities.Equipped(AbilityCategory.Offensive)==0);
   Check("baseline uses neither an offensive slot nor the GCD",basic.Definition.Slotless && !basic.Definition.UsesGlobalCooldown && !basic.Definition.TriggersGlobalCooldown);
   Check("baseline stays outside unlock offers",!w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==basic.Definition.Id));
   Check("correct baseline for class",basic.Definition.Id==(cls=="marauder"?"basicassaultdual":"viciousslash"));
   Check("Vicious Slash cooldown follows class tuning",w.Abilities.Find("viciousslash")!.Definition.CooldownSeconds==(cls=="marauder"?4.5f:2f));
   Check("directional locomotion loaded",actor.DirectionalLocomotionEnabled);
   int left=actor.Skeleton.FindBone("LeftAnkle"),right=actor.Skeleton.FindBone("RightAnkle");Check("both feet are available",left>=0 && right>=0);
   var directions=new[]{Vector3.Forward,Vector3.Back,Vector3.Left,Vector3.Right,new Vector3(1,0,-1).Normalized()};
   if(capture){
    foreach(var layer in w.GetChildren().OfType<CanvasLayer>())layer.Visible=false;
    w.Player.Rig.SetProcess(false);w.Player.Rig.SetPhysicsProcess(false);
    var camera=new Camera3D{Fov=38};w.AddChild(camera);camera.GlobalPosition=w.Player.GlobalPosition+new Vector3(.45f,.25f,.6f);camera.LookAt(w.Player.GlobalPosition+Vector3.Up*.09f);camera.Current=true;
   }
   foreach(var ability in w.Abilities.Abilities.Where(a=>a.Definition.Id!="smash"))foreach(var direction in directions){
    actor.ResetPresentation();actor.Rotation=Vector3.Zero;object owner=new();var d=ability.Definition;
    Check(d.Id+" moving gesture starts",actor.TryBeginGesture(owner,d.StartClip,d.GesturePriority,d.FullBodyGesture,d.KeepFullBodyWhileMoving));
    actor.FacingTarget=actor.GlobalPosition+Vector3.Forward;
    Vector3? previousLeft=null,previousRight=null;float leftTravel=0,rightTravel=0;
    int root=actor.Skeleton.FindBone("Root");Vector2 rootMin=new(float.PositiveInfinity,float.PositiveInfinity),rootMax=new(float.NegativeInfinity,float.NegativeInfinity);
    Quaternion? rootFacing=null;float rootTurn=0;
    for(int frame=0;frame<64;frame++){
     actor.Present(direction*w.Player.MoveSpeed,.02);actor._Process(.02);
     var l=actor.Skeleton.GetBoneGlobalPose(left).Origin;var r=actor.Skeleton.GetBoneGlobalPose(right).Origin;
     if(frame>12 && previousLeft.HasValue){leftTravel+=l.DistanceTo(previousLeft.Value);rightTravel+=r.DistanceTo(previousRight!.Value);}
     previousLeft=l;previousRight=r;
     if(frame>12){
      var pose=actor.Skeleton.GetBoneGlobalPose(root);var planar=new Vector2(pose.Origin.X,pose.Origin.Z);rootMin=rootMin.Min(planar);rootMax=rootMax.Max(planar);
      var facing=pose.Basis.GetRotationQuaternion();rootFacing??=facing;rootTurn=Math.Max(rootTurn,rootFacing.Value.AngleTo(facing));
     }
     if(capture && d.Id=="viciousslash" && direction==Vector3.Right && frame is 16 or 24 or 32 or 40){
      await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
      GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/movement-{cls}-strafe-{frame}.png"));
     }
    }
    Check(d.Id+" keeps both feet running",leftTravel>.015f && rightTravel>.015f);
    Check(d.Id+" releases the lower body while moving",(float)actor.Tree.Get("parameters/StandingCast/blend_amount")<.001f);
    Check(d.Id+" does not move the visual rig away from its body",(rootMax-rootMin).Length()<.015f);
    Check(d.Id+" locomotion does not introduce a body turn",rootTurn<.6f);
    Check(d.Id+" runs in movement direction while facing the enemy",actor.LocomotionDirection.DistanceTo(new Vector2(direction.X,direction.Z))<.03f && (-actor.GlobalBasis.Z).Dot(Vector3.Forward)>.98f);
    if(d.Id=="viciousslash")_runRates[direction.ToString()]=actor.LocomotionPlaybackRate;
    actor.ReleaseGesture(owner);
   }
   actor.ResetPresentation();actor.Rotation=Vector3.Zero;
   object paceOwner=new();actor.TryBeginGesture(paceOwner,w.Abilities.Find("viciousslash")!.Definition.StartClip);actor.FacingTarget=actor.GlobalPosition+Vector3.Forward;
   actor.Present(Vector3.Right*w.Player.MoveSpeed,.02);actor._Process(.02);float speed=actor.LocomotionPlaybackRate;
   actor.Present(Vector3.Right*w.Player.MoveSpeed*1.4f,.02);actor._Process(.02);
   Check("foot cadence follows movement speed",Math.Abs(actor.LocomotionPlaybackRate-Math.Min(6,speed*1.4f))<.01f);
   var slash=w.Abilities.Find("viciousslash")!;actor.ResetPresentation();object stationary=new();actor.TryBeginGesture(stationary,slash.Definition.StartClip);
   for(int i=0;i<16;i++){actor.Present(Vector3.Zero,.02);actor._Process(.02);}
   Check("stationary attacks retain their full pose",(float)actor.Tree.Get("parameters/StandingCast/blend_amount")>.99f);
   actor.PresentJump(false,true,.2f,.2f,.02);for(int i=0;i<8;i++)actor._Process(.02);
   Check("jump legs override ordinary attack legs",actor.JumpWeight>.99f && (float)actor.Tree.Get("parameters/StandingCast/blend_amount")<.001f);
   actor.ResetPresentation();w.Abilities.ResetCombat();var smash=w.Abilities.Find("smash")!;bool hadSmash=smash.Unlocked;smash.SetUnlocked(true);smash.Definition.NearbyTriggerCount=1;
   Check("Smash starts its airborne gesture",smash.TryBegin());
   for(int i=0;i<100;i++){smash.Advance(.02);actor.Present(Vector3.Right*w.Player.MoveSpeed,.02);actor._Process(.02);}
   Check("Smash returns the legs to running after landing",actor.GestureOwner==smash && !actor.KeepFullBodyWhileMoving && (float)actor.Tree.Get("parameters/StandingCast/blend_amount")<.001f);
   smash.Interrupt();smash.SetUnlocked(hadSmash);
   actor.ResetPresentation();w.Abilities.ResetCombat();
   w.Combat.Definition.NearbyTriggerCount=1;w.Abilities._PhysicsProcess(.01);
   Check("class attack takes priority over the baseline",w.Combat.Casts==1 && basic.Casts==0);
   w.Abilities.ResetCombat();foreach(var a in w.Abilities.Abilities)a.SetUnlocked(false);basic.SetUnlocked(true);actor.ResetPresentation();w.Abilities._PhysicsProcess(.01);
   Check("baseline fills gaps when class attacks cannot activate",basic.Casts==1);
   if(cls=="marauder"){
    w.Abilities.ResetCombat();w.Periodic.Clear();actor.ResetPresentation();var rupture=w.Abilities.Find("rupture")!;var crush=w.Abilities.Find("forcecrush")!;
    rupture.SetUnlocked(true);crush.SetUnlocked(true);
    foreach(var target in targets){w.Periodic.Apply(rupture,target);w.Periodic.Apply(crush,target);}
    w.Abilities._PhysicsProcess(.01);
    Check("Assault fills gaps when every enemy already has both DoTs",basic.Casts==1 && rupture.Casts==0 && crush.Casts==0);
    var fresh=new Combatant{Name="FreshMovingFixture",Team=CombatTeam.Hostile};fresh.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(fresh);fresh.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.14f;
    w.Abilities.ResetCombat();actor.ResetPresentation();w.Abilities._PhysicsProcess(.01);
    Check("a fresh target restores DoT priority over Assault",rupture.Casts+crush.Casts==1 && basic.Casts==0);
   }
   w.Progression.ResetRun();Check("restart restores the baseline in slot 1",basic.Unlocked && w.Abilities.QuickbarAbilities[0]==basic && w.Combat.Unlocked);
  }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/warrior-movement-{cls}.json"),JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,locomotionRates=_runRates,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
