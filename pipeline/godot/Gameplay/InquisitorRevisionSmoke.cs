using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class InquisitorRevisionSmoke : Node
{
 private readonly List<string> _checks=new();
 private void Check(string label,bool ok){if(!ok)throw new Exception(label);_checks.Add(label);}
 public async void Run(GameWorld w){string? failure=null;string cls=w.RunConfiguration.AdvancedClassId;
  try{
   w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
   w.Player.TestInput=Vector2.Zero;
   for(int i=0;i<8;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
   w.Player.SetPhysicsProcess(false);w.Targeting.EnvironmentMask=0;
   foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
   var targets=new List<EnemyAgent>();
   for(int i=0;i<3;i++){var body=w.Spawner.EnemyScene.Instantiate<CharacterBody3D>();var t=body.GetNode<EnemyAgent>("Actor");t.Target=w.PlayerActor;t.Navigation=w.Spawner.Navigation;t.SetPhysicsProcess(false);w.AddChild(body);body.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3((i-1)*.07f,0,-.17f);t.Health.Maximum=100000;t.Health.Restore();targets.Add(t);}
   async System.Threading.Tasks.Task Capture(string name){
    if(!OS.GetCmdlineUserArgs().Contains("--inquisitor-revision-captures"))return;
    foreach(var layer in w.GetChildren().OfType<CanvasLayer>())layer.Visible=false;
    w.Player.Rig.SetProcess(false);w.Player.Rig.SetPhysicsProcess(false);
    var camera=new Camera3D {Fov=38};w.AddChild(camera);camera.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(.55f,.42f,.7f);camera.LookAt(w.PlayerActor.GlobalPosition+new Vector3(0,.1f,-.08f));camera.Current=true;
    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
    GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/inquisitor-{name}.png"));camera.QueueFree();
   }
   void Reset(){w.Abilities.ResetCombat();w.Build.Pregame.Reset();w.Build.Reset();w.Periodic.Clear();w.Player.Presentation.ResetPresentation();foreach(var a in w.Abilities.Abilities)a.SetUnlocked(false);foreach(var t in targets)t.Health.Restore();w.Player.TestInput=Vector2.Zero;w.Player.Velocity=Vector3.Zero;}
   AutomaticAbility Start(string id,double time=0){w.Player.Presentation.ResetPresentation();var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();Check(id+" starts",a.TryBegin());if(time>0)a.Advance(time);return a;}
   if(cls=="sorcerer"){
    Reset();Check("grounded stationary admission",w.Player.StationaryForAbility);
    foreach(string id in new[]{"deathfield","storm"}){
     Reset();var a=w.Abilities.Find(id)!;a.SetUnlocked(true);w.Player.TestInput=Vector2.Right;int casts=a.Casts;
     Check(id+" rejects moving start",!a.TryBegin() && a.Casts==casts && a.CooldownRemaining==0);
     w.Player.TestInput=Vector2.Zero;Check(id+" starts on stopping",a.TryBegin());w.Player.TestInput=Vector2.Right;
     a.Advance(a.Definition.WindupSeconds+.85);Check(id+" survives movement after initiation",a.DamageEvents>0);
    }
    Reset();foreach(string id in new[]{"shock","deathfield","storm"})w.Abilities.Find(id)!.SetUnlocked(true);
    int before=w.Abilities.Find("deathfield")!.Casts+w.Abilities.Find("storm")!.Casts;w.Abilities._PhysicsProcess(.01);
    Check("stationary area spell wins over ready Shock",w.Abilities.Find("deathfield")!.Casts+w.Abilities.Find("storm")!.Casts==before+1 && w.Abilities.Find("shock")!.State==LightningState.Idle);
    Reset();var blast=Start("thunderingblast");var fx=(SorcererAbilityPresentation)blast.Presentation;fx.SetProcess(false);float hp=targets.Sum(t=>t.Health.Current);
    Check("Blast cast audio loops",fx.CastLoopPlaying);blast.Advance(2.01);Check("Blast release launches without damage",fx.ProjectileVisible && !fx.ChargeVisible && targets.Sum(t=>t.Health.Current)==hp);
    var target=blast.CastTarget!;target.GlobalPosition+=Vector3.Right*.02f;fx._Process(.12);Check("Blast follows moving endpoint",fx.ProjectilePosition!=Vector3.Zero);
    await Capture("blast-flight");
    int contacts=fx.TargetImpacts;blast.Advance(.25);Check("Blast arrival applies contact and damage",!fx.ProjectileVisible && blast.DamageEvents>0 && fx.TargetImpacts==contacts+blast.LastDistinctHits);
    Reset();blast=Start("thunderingblast",2.01);blast.Abort();Check("Blast abort cleans projectile and loop",!fx.ProjectileVisible && !fx.CastLoopPlaying);
    Reset();w.Abilities.Find("lightningstrike")!.SetUnlocked(true);Check("Strike cleave learns",w.Progression!.Grant("strike_cleave"));var strike=Start("lightningstrike",.8);var sf=(SorcererRefinementPresentation)strike.Presentation;
    Check("Strike cleave presents every victim",strike.LastDistinctHits==3 && sf.TargetImpacts>=3);
    Reset();var speed=Start("forcespeed");var speedFx=(SorcererRefinementPresentation)speed.Presentation;speedFx.SetProcess(false);Check("Force Speed sound loops",speedFx.BuffLoopPlaying);w.Build.Reset();speedFx._Process(.01);Check("Force Speed loop follows actual buff reset",!speedFx.BuffLoopPlaying);
    Reset();Check("Polarity training learns",w.Progression!.Grant("polarity_training"));var polarity=Start("polarityshift");var pf=(SorcererRefinementPresentation)polarity.Presentation;pf.SetProcess(false);Check("Polarity sound loops",pf.BuffLoopPlaying);w.Build.Pregame.Advance(6.1);pf._Process(.01);Check("Polarity sound expires",!pf.BuffLoopPlaying);
   }else{
    Reset();Check("Deception grants Electrocute",w.Build.Pregame.AssassinDeception && w.Abilities.Find("electrocute")!=null);
    var elect=w.Abilities.Find("electrocute")!;elect.SetUnlocked(true);Check("Electrocute waits for owned Discharge",!elect.TryBegin());
    var discharge=Start("discharge",.51);Check("Discharge remains strengthened main DoT",discharge.Definition.PeriodicDamage==6 && w.Build.Pregame.PeriodicDuration(discharge)==8 && discharge.CooldownSeconds<6 && targets.All(t=>w.Periodic.Has(discharge,t)));
    var dotRemaining=w.Periodic.Remaining(discharge,targets[0]);elect=Start("electrocute",.31);
    var ef=(ElectrocutePresentation)elect.Presentation;Check("Conductive Field reaches three enemies",elect.LastDistinctHits==3 && ef.TargetImpacts==3);
    Check("Electrocute stuns and exposes victims",targets.All(t=>t.StunRemaining>0 && w.Build.Pregame.AssassinElectrified(t)));
    Check("Electrocute retains Discharge",Math.Abs(w.Periodic.Remaining(discharge,targets[0])-dotRemaining)<.001);
    Check("Electrocute opens Maul burst window",w.Build.Pregame.StealthOpening);
    var maul=w.Abilities.Find("maul")!;Check("Maul gains exposed-target bonus",w.Build.Pregame.DirectBonus(maul,targets[0])>=.55f);
    Check("Electrocute plays recovered sustained sound",ef.LoopPlaying);
    await Capture("electrocute");
    w.Build.Pregame.Advance(6.1);ef._Process(.01);Check("Exposure expires without consuming DoT",!w.Build.Pregame.AssassinElectrified(targets[0]) && w.Periodic.Has(discharge,targets[0]));
    Reset();targets[0]._PhysicsProcess(10);targets[0].RewardTier=2;discharge=Start("discharge",.51);elect=Start("electrocute",.31);
    Check("boss resists stun but retains exposure",targets[0].StunRemaining<=0 && w.Build.Pregame.AssassinElectrified(targets[0]));targets[0].RewardTier=0;
    Reset();var terror=w.Abilities.Find("creepingterror")!;terror.SetUnlocked(true);discharge.SetUnlocked(true);var lacerate=w.Abilities.Find("lacerate")!;lacerate.SetUnlocked(true);
    Check("Razor Wake learns",w.Progression!.Grant("assassin_spread"));w.Periodic.Apply(terror,targets[0]);
    int previous=((D3AbilityPresentation)lacerate.Presentation).AssassinTargetImpacts;Start("lacerate",.51);
    Check("Razor Wake extra damage gets its own contacts",((D3AbilityPresentation)lacerate.Presentation).AssassinTargetImpacts-previous==5);
    Reset();elect.SetUnlocked(true);foreach(var a in w.Abilities.Abilities)w.Abilities.Unlock(a.Definition.Id);w.Hud!.GetChildren().SelectMany(n=>n.GetChildren()).OfType<UI.CombatHud>().Single().RefreshAbilities();
    Check("capped Deception build keeps all learned abilities visible",w.Hud.UnlockedAbilityCount==w.Abilities.Abilities.Count(a=>a.Unlocked) && w.Abilities.QuickbarAbilities.Contains(elect) && w.Abilities.Equipped(AbilityCategory.Offensive)<=4 && w.Abilities.Equipped(AbilityCategory.Utility)<=2);
    Reset();w.Player.TestInput=Vector2.Right;var field=Start("deathfield",1);Check("Assassin Death Field still starts while moving",field.DamageEvents>0);
    Reset();var cloak=Start("forcecloak");var cf=(AssassinAbilityPresentation)cloak.Presentation;cf.SetProcess(false);Check("Force Cloak starts recovered loop",cf.CloakLoopPlaying);w.Build.Pregame.RevealSpike();cf._Process(.01);Check("Force Cloak loop ends on reveal",!cf.CloakLoopPlaying);
   }
  }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/inquisitor-revision-{cls}.json"),JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
