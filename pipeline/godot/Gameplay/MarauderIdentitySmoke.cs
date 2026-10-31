using Godot;
using System;
using System.Linq;
namespace SwtorSurvivors;
public partial class MarauderIdentitySmoke : Node
{
 public void Run(GameWorld w){try{
  void Check(string n,bool ok){if(!ok)throw new Exception(n);GD.Print("IDENTITY: "+n);}
  w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);
  var p=w.Progression!;p.ResetRun();var runtime=w.Build.Pregame;
  Check("gap closers removed",!w.Abilities.Abilities.Any(a=>a.Definition.Id is "forcecharge" or "obliterate"));
  foreach(string id in new[]{"massacre","batteringassault","gore","forcescream","intimidatingroar"}){
   var a=w.Abilities.Find(id);Check(id+" available",a!=null && p.Upgrades.Any(u=>u.AbilityId==id && u.Effect==UpgradeEffect.UnlockAbility));
   Check(id+" animation and icon",w.Player.Presentation.Animator.HasAnimation(a!.Definition.StartClip) && ResourceLoader.Exists("res://Assets/UI/icons/"+a.Definition.Icon+".png"));
  }
  var target=new Combatant{Name="IdentityTarget",Team=CombatTeam.Hostile};target.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.1f;
  var gore=w.Abilities.Find("gore")!;runtime.Activated(gore);
  Check("Gore grants burst without bleeds",runtime.DirectBonus(w.Abilities.Find("massacre")!,target)>=.6f);
  runtime.Activated(w.Abilities.Find("berserk")!);Check("Berserk active",runtime.MarauderAuraActive("berserk"));
  var visual=(D3AbilityPresentation)w.Abilities.Find("berserk")!.Presentation;
  visual.Begin(target,target.GlobalPosition,.2f);int bursts=visual.MarauderAuraBursts;visual._Process(.11);Check("Berserk emits attached aura",visual.MarauderAuraBursts>bursts);
  runtime.Advance(3);Check("Berserk persists halfway",runtime.MarauderAuraActive("berserk"));runtime.Advance(6);Check("Berserk expires",!runtime.MarauderAuraActive("berserk"));
  bursts=visual.MarauderAuraBursts;visual._Process(.11);Check("expired Berserk stops emitting",visual.MarauderAuraBursts==bursts);
  var rupture=w.Abilities.Find("rupture")!;var sweep=w.Abilities.Find("sweepingslash")!;rupture.SetUnlocked(true);sweep.SetUnlocked(true);w.Periodic.Apply(rupture,target);
  Check("seeded Rupture waits for spreading",!runtime.CanBeginMarauder(rupture));
  w.Periodic.Clear();Check("Rupture can seed again after expiry",runtime.CanBeginMarauder(rupture));
  Check("Deadly Saber bleed exceeds Rupture",w.Abilities.Find("deadlysaber")!.Definition.PeriodicDamage>rupture.Definition.PeriodicDamage);
  w.Abilities.Find("ravage")!.SetUnlocked(true);
  for(int i=0;i<6;i++){Check("massacre reach rank "+i,p.Grant("marauder_target_massacre"));Check("ravage reach rank "+i,p.Grant("marauder_target_ravage"));}
  Check("uncapped reach grows both cleaves",w.Abilities.Find("massacre")!.Definition.MaximumTargets>=8 && w.Abilities.Find("ravage")!.Definition.MaximumTargets>=8);
  Check("full strength secondary hits",w.Abilities.Find("massacre")!.Definition.SplashMultiplier==1);
  p.ResetRun();GetTree().Quit(0);
 }catch(Exception e){GD.PushError(e.ToString());GetTree().Quit(1);}}
}
