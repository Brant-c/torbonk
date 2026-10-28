using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class MarauderSmoke : Node
{
 private readonly List<string> checks=new();
 private void Check(string n,bool b){if(!b)throw new Exception(n);checks.Add(n);}
 public async void Run(GameWorld w){string? failure=null;try{
  w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
  foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
  var cat=TalentCatalog.Current;
  Check("24 talents, eight per tree",cat.ForClass("marauder").Length==24 && cat.ForClass("marauder").GroupBy(t=>t.Tree).All(t=>t.Count()==8));
  Check("other classes retain catalogs",cat.ForClass("juggernaut").Length==27 && cat.ForClass("assassin").Length==24 && cat.ForClass("sorcerer").Length==24);
  Check("fast free starter and lower health",w.Combat.Definition.Id=="viciousslash" && w.Combat.Definition.Slotless && w.PlayerActor.Health.Maximum==115 && w.Abilities.GlobalCooldownSeconds<1);
  Check("no generic damage tax",!w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.Damage));
  Check("untrained saber damage raised",w.Combat.DamagePerTick>=23 && w.Abilities.Find("annihilate")!.DamagePerTick>=52);
  Check("Sith Smash icon and deliberate cooldown",w.Abilities.Find("smash")!.Definition.Icon=="juggernautsmash" && w.Abilities.Find("smash")!.CooldownSeconds>=18);
  Check("distinct talent icons",cat.ForClass("marauder").Select(t=>t.Icon).Distinct().Count()>=18 && cat.ForClass("marauder").All(t=>ResourceLoader.Exists(t.Icon)));
  Check("slot budgets preserved",w.Abilities.OffensiveLimit==w.SlotRules!.OffensiveSlots && w.Abilities.UtilityLimit==w.SlotRules.UtilitySlots);
  Check("all clips and icons load",w.Abilities.Abilities.All(a=>w.Player.Presentation.Animator.HasAnimation(a.Definition.StartClip) && ResourceLoader.Exists("res://Assets/UI/icons/"+a.Definition.Icon+".png")));
  var progression=w.Progression!;
  var mastery=progression.Upgrades.Single(u=>u.Id=="marauder_power_viciousslash");
  float starterDamage=w.Combat.DamagePerTick;
  for(int rank=0;rank<25;rank++)Check("uncapped mastery rank "+rank,progression.Grant(mastery.Id));
  Check("mastery scales additively from base",Math.Abs(w.Combat.DamagePerTick-starterDamage*6)<.05f);
  Check("unowned attacks cannot receive mastery",!progression.IsEligible(progression.Upgrades.Single(u=>u.Id=="marauder_power_ravage")));
  Check("recovery stops after two ranks",progression.Grant("cooldown") && progression.Grant("cooldown") && !progression.Grant("cooldown"));
  var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
  for(ulong seed=1;seed<=100;seed++){
   progression.ResetRun();progression.SeedOffers(seed);
   w.Abilities.Find("rupture")!.SetUnlocked(true);w.Abilities.Find("ravage")!.SetUnlocked(true);
   var offers=(List<UpgradeDefinition>)generate.Invoke(progression,new object[]{10,Array.Empty<UpgradeDefinition>(),3})!;
   Check("class offers dominate "+seed,offers.Count(u=>u.Id.StartsWith("marauder_") || u.Mechanic.StartsWith("mr_"))>=2);
   Check("damage mastery offered "+seed,offers.Any(u=>u.Id.StartsWith("marauder_power_")));
  }
  progression.ResetRun();
  for(ulong seed=1;seed<=8;seed++){
   var p=w.Progression!;p.ResetRun();p.SeedOffers(seed);p.AddExperience(p.RequiredExperience);p._Process(3);
   Check("opening damage choices "+seed,new[]{"rupture","sweepingslash","ravage"}.All(id=>p.Offers.Any(o=>o.AbilityId==id)));
   Check("banish respects ability "+seed,p.Banish(0) && !p.Offers.Any(o=>o.AbilityId=="rupture"));
   Check("reroll works "+seed,p.Reroll());Check("skip works "+seed,p.Skip());
  }
  w.Progression!.ResetRun();GetTree().Paused=false;
  var target=new Combatant{Name="MarauderFixture",Team=CombatTeam.Hostile};target.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
  await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
  if(OS.GetCmdlineUserArgs().Contains("--melee-audio-smoke")) {
  // A completed killing blow must retain both the main-hand tail and delayed offhand swing.
  var slash=w.Combat;slash.ResetCombat();w.Player.Presentation.ResetPresentation();target.Health.ApplyDamage(target.Health.Current-1);
  Check("lethal starter begins",slash.TryBegin());slash.Advance(.2);
  var slashFx=(D3AbilityPresentation)slash.Presentation;slashFx._Process(.2);
  Check("killing blow preserves melee audio",!target.Alive && slashFx.PlayingAudioVoices>0 && slashFx.PendingAudioEvents>0);
  slashFx._Process(.4);Check("offhand audio survives killing blow",slashFx.PendingAudioEvents==0 && slashFx.PlayingAudioVoices>0);
  slash.Abort();Check("reset still clears melee audio",slashFx.PlayingAudioVoices==0 && slashFx.PendingAudioEvents==0);
  target.Health.Restore();
   return;
  }
  AutomaticAbility Cast(string id){var a=w.Abilities.Find(id)!;w.Player.Presentation.ResetPresentation();a.SetUnlocked(true);a.ResetCombat();Check(id+" activates",a.TryBegin());for(int i=0;i<80;i++)a.Advance(.025);return a;}
  var actor=w.Player.Presentation;
  Check("Marauder has no separate basic Assault",w.Abilities.Find("basicassaultdual")==null && !w.Abilities.Abilities.Any(a=>a.Definition.IsBasicAttack));
  Check("starter can be replaced",w.Combat.Definition.ReplaceableStarter);
  Check("generic cooldown training no longer targets starter",w.Progression!.Upgrades.Where(u=>u.Effect==UpgradeEffect.Cooldown).All(u=>u.AbilityId==""));
  foreach(string id in new[]{"viciousslash","rupture","sweepingslash","annihilate","ravage"}){
   var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();actor.ResetPresentation();Check(id+" gesture starts",a.TryBegin());
   var left=actor.Skeleton.FindBone("LeftWrist");var right=actor.Skeleton.FindBone("RightWrist");
   Vector3? lastL=null,lastR=null;float travelL=0,travelR=0;int ribbons=0;var trails=actor.GetChildren().OfType<MarauderBladeTrails>().Single();
   for(int i=0;i<16;i++){
    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);a.Advance(.04);actor._Process(.04);actor.Skeleton.ForceUpdateAllBoneTransforms();trails._Process(.04);ribbons=Math.Max(ribbons,trails.RibbonCount);
    var l=actor.Skeleton.GetBoneGlobalPose(left).Origin;var r=actor.Skeleton.GetBoneGlobalPose(right).Origin;
    if(lastL.HasValue){travelL+=l.DistanceTo(lastL.Value);travelR+=r.DistanceTo(lastR!.Value);}lastL=l;lastR=r;
    if(i==8)Check(id+" survives first damage frame",actor.GestureOwner==a);
   }
   Check(id+" animates both wrists",travelL>.01f && travelR>.01f);Check(id+" produces blade trails",ribbons>0);
   a.Interrupt();actor.ResetPresentation();
  }
  w.Periodic.Clear();Cast("rupture");Check("Rupture persists",w.Periodic.Has(w.Abilities.Find("rupture")!,target));float hp=target.Health.Current;w.Periodic.Advance(2);Check("bleed ticks",target.Health.Current<hp);Check("bleed status visual attached",target.GetNodeOrNull<Node3D>("MarauderPeriodicStatus")!=null);
  var rupture=w.Abilities.Find("rupture")!;
  actor.ResetPresentation();rupture.ResetCombat();rupture.CooldownSeconds=.01f;
  Check("ready Rupture does not overwrite healthy bleed",!rupture.TryBegin());
  var fresh=new Combatant{Name="FreshBleedFixture",Team=CombatTeam.Hostile};fresh.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(fresh);fresh.GlobalPosition=target.GlobalPosition+Vector3.Right*.03f;
  await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
  w.Abilities.Find("sweepingslash")!.SetUnlocked(false);w.Abilities.Find("smash")!.SetUnlocked(false);actor.ResetPresentation();Check("Rupture chooses fresh target",rupture.TryBegin() && rupture.CastTarget==fresh);rupture.Interrupt();
  foreach(var a in w.Abilities.Abilities)a.SetUnlocked(false);
  var spreading=w.Abilities.Find("sweepingslash")!;spreading.SetUnlocked(true);rupture.SetUnlocked(true);spreading.Definition.SpreadsPeriodicEffects=true;
  w.Abilities.ResetCombat();actor.ResetPresentation();
  Check("spread priority precedes fresh DoT",w.Build.Pregame.MarauderPriority(spreading)<w.Build.Pregame.MarauderPriority(rupture));
  w.PlayerActor.Health.ApplyDamage(12,DamageType.Internal);float beforeSpread=w.PlayerActor.Health.Current;
  w.Abilities._PhysicsProcess(.01);
  Check("controller casts spread before ready Rupture",spreading.Casts==1 && rupture.Casts==0);
  spreading.Definition.SpreadsPeriodicEffects=true;spreading.Advance(.22);Check("sweep spreads bleed to fresh target",w.Periodic.Has(rupture,fresh));Check("successful spread pays healing",w.PlayerActor.Health.Current>beforeSpread);Check("spread rewards follow-up weapon damage",w.Build.Pregame.DirectBonus(w.Abilities.Find("annihilate")!,fresh)>=.7f);
  float oldTickHp=target.Health.Current,newTickHp=fresh.Health.Current;w.Periodic.Advance(1);Check("spread bleeds tick harder than seed",newTickHp-fresh.Health.Current>(oldTickHp-target.Health.Current)*1.25f);
  spreading.Interrupt();w.RemoveChild(fresh);fresh.QueueFree();w.Periodic.Clear();
  Cast("forcecrush");var crush=w.Abilities.Find("forcecrush")!;actor.ResetPresentation();crush.ResetCombat();
  Check("Crush waits while its DoT is healthy",!crush.TryBegin());w.Periodic.Advance(5.5);w.Abilities.Find("sweepingslash")!.SetUnlocked(false);w.Abilities.Find("smash")!.SetUnlocked(false);actor.ResetPresentation();
  Check("Crush can refresh near expiry",crush.TryBegin());crush.Interrupt();
  var ravage=Cast("ravage");Check("Ravage three hits",ravage.DamageEvents==3);
  Cast("deadlysaber");Cast("annihilate");Check("Deadly Saber weapon application",w.Periodic.Has(w.Abilities.Find("deadlysaber")!,target));
  var berserk=Cast("berserk");Check("Berserk accelerates",w.Combat.CooldownSeconds<w.Combat.Definition.CooldownSeconds);w.Build.Pregame.Advance(10);Check("Berserk expires",Math.Abs(w.Combat.CooldownSeconds-w.Combat.Definition.CooldownSeconds)<.001);
  Cast("predation");Check("Predation speed and avoidance",w.Build.SpeedMultiplier>=1.45f && w.Build.IncomingMultiplier<1);
  var predationVisual=(D3AbilityPresentation)w.Abilities.Find("predation")!.Presentation;
  predationVisual._Process(1);predationVisual._Process(.1);Check("Predation has sustained character effects",predationVisual.EffectCount>=3);
  Cast("frenzy");Check("Frenzy readies Berserk",w.Abilities.Find("berserk")!.CooldownRemaining==0);
  var frenzyVisual=(D3AbilityPresentation)w.Abilities.Find("frenzy")!.Presentation;frenzyVisual._Process(1);frenzyVisual._Process(.1);Check("Frenzy has sustained character effects",frenzyVisual.EffectCount>=3);
  var thrown=Cast("saberthrow");var throwVisual=(D3AbilityPresentation)thrown.Presentation;
  Check("Dual Throw has two combat hits",thrown.DamageEvents==2);
  Check("both sabers are thrown",throwVisual.MarauderThrowsPresented==2 && !w.Equipment.Weapon("main_hand")!.Visible && !w.Equipment.Weapon("off_hand")!.Visible);
  foreach(var flight in w.GetChildren().OfType<MarauderThrownSaber>().ToArray())flight._Process(1);
  await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
  Check("both sabers return",w.Equipment.Weapon("main_hand")!.Visible && w.Equipment.Weapon("off_hand")!.Visible);
  var overlapA=new MarauderThrownSaber{Source=w.Equipment.Weapon("main_hand")!,Target=target};var overlapB=new MarauderThrownSaber{Source=w.Equipment.Weapon("main_hand")!,Target=target};w.AddChild(overlapA);w.AddChild(overlapB);
  w.RemoveChild(overlapA);overlapA.QueueFree();Check("overlapping throw keeps weapon hidden until both return",!w.Equipment.Weapon("main_hand")!.Visible);
  w.RemoveChild(overlapB);overlapB.QueueFree();Check("overlapping throws restore original weapon visibility",w.Equipment.Weapon("main_hand")!.Visible);
  var hybrid=new[]{"annihilation.spread","annihilation.rhythm","annihilation.heal","annihilation.charges","carnage.alacrity","carnage.cleave","carnage.precision","carnage.bloodletting","marauder_rage.shockwave","marauder_rage.forceflow","marauder_rage.rageblade","marauder_rage.dominate"}.ToDictionary(x=>x,x=>1);
  Check("12 point hybrid validates",cat.Validate("marauder",hybrid).Count==12);TalentProfiles.Save("marauder",hybrid);var runtime=new TalentRuntime(w);
  var sweep=w.Abilities.Find("sweepingslash")!;runtime.ApplyStats(sweep);Check("bleed spread enabled",sweep.Definition.SpreadsPeriodicEffects);runtime.ApplyStats(ravage);Check("Ravage cleave enabled",ravage.Definition.MaximumTargets==4);
  runtime.PeriodicTick(w.Abilities.Find("forcecrush")!);runtime.Activated(w.Abilities.Find("smash")!);Check("Crush empowers Smash",runtime.DirectBonus(w.Abilities.Find("smash")!,target)>.1f);
  Check("starter has no offensive talent procs",runtime.DirectBonus(w.Combat,target)==0);
  Check("Carnage benefits owned bleeds",runtime.DirectBonus(w.Abilities.Find("annihilate")!,target)>.19f);
  var smash=w.Abilities.Find("smash")!;smash.CooldownSeconds=.1f;runtime.ApplyStats(smash);Check("Smash cannot become a spam cooldown",smash.CooldownSeconds>=10);
  w.Progression.ResetRun();Check("restart clears utility state",w.Build.SpeedMultiplier==1 && w.Build.IncomingMultiplier==1);
 }catch(Exception e){failure=e.ToString();GD.PushError(failure);}finally{TalentProfiles.Save("marauder",new Dictionary<string,int>());System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/marauder-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);}}
}




