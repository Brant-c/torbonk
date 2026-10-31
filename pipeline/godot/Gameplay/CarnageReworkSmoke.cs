using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class CarnageReworkSmoke : Node
{
 public async void Run(GameWorld w) {
 var checks=new List<string>();string? failure=null;
 string path=TalentProfiles.SavePath;string? original=System.IO.File.Exists(path)?System.IO.File.ReadAllText(path):null;
 var originalRuntime=w.Build.Pregame;
 var inv=UI.EquipmentWindows.Instance.Inventory;
 var loadout=inv.State.Loadouts.GetValueOrDefault("marauder");inv.State.Loadouts["marauder"]=new();
 void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);}
 try {
 w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
 foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
 TalentProfiles.Save("marauder",new Dictionary<string,int>());
 var rt=new TalentRuntime(w);typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,rt);
 var p=w.Progression!;p.ResetRun();
 var catalog=TalentCatalog.Current;var talents=catalog.ForClass("marauder");
 Check("all capstones require five earlier points",talents.Where(t=>t.Tier==2).Count()==9&&talents.Where(t=>t.Tier==2).All(t=>t.RequiredPoints==5));
 foreach(var tree in talents.GroupBy(t=>t.Tree)) {
     var top=tree.Where(t=>t.Tier==0).ToDictionary(t=>t.Id,t=>1);
     Check("three point off-spec dip "+tree.Key,catalog.Validate("marauder",top).Count==3);
     foreach(var capstone in tree.Where(t=>t.Tier==2))Check("top row cannot unlock "+capstone.Name,catalog.Locked("marauder",capstone,top)!=null);
 }

 Check("three nine-point trees with twelve shared points",catalog.Budget==12&&talents.GroupBy(t=>t.Tree).Count()==3&&talents.GroupBy(t=>t.Tree).All(g=>g.Sum(t=>t.MaxRank*t.Cost)==9));
 foreach(var full in talents.GroupBy(t=>t.Tree)) {
     var other=talents.Where(t=>t.Tree!=full.Key).GroupBy(t=>t.Tree).First();
     var split=full.Concat(other.OrderBy(t=>t.Tier).ThenBy(t=>t.Id).Take(3)).ToDictionary(t=>t.Id,t=>1);
     Check("nine plus three valid "+full.Key,catalog.Spent(catalog.Validate("marauder",split))==12);
     var two=full.Concat(other).ToDictionary(t=>t.Id,t=>1);var valid=catalog.Validate("marauder",two);
     Check("two full trees rejected "+full.Key,catalog.Spent(valid)==12&&valid.Count<two.Count);
 }
 var balanced=talents.GroupBy(t=>t.Tree).SelectMany(g=>g.OrderBy(t=>t.Tier).ThenBy(t=>t.Id).Take(4)).ToDictionary(t=>t.Id,t=>1);
 Check("four four four valid",catalog.Validate("marauder",balanced).Count==12);
 var screen=new Frontend.TalentsScreen {ClassId="marauder"};w.AddChild(screen);
 await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
 var buttons=screen.FindChildren("*","",true,false).OfType<Frontend.TalentIconButton>().ToArray();
 Check("all twenty-seven talent buttons fit the frame",buttons.Length==27&&buttons.All(b=>b.Position.X>=0&&b.Position.X+b.Size.X<=410&&b.Position.Y+b.Size.Y+50<=440));
 w.RemoveChild(screen);screen.QueueFree();
 foreach(string tree in new[]{"annihilation","marauder_rage"}) {
     var trained=talents.Where(t=>t.Tree==tree).ToDictionary(t=>t.Id,t=>1);TalentProfiles.Save("marauder",trained);
     var boosted=new TalentRuntime(w);var checkAbility=w.Abilities.Find(tree=="annihilation"?"rupture":"forcecrush")!;
     float baseDamage=checkAbility.DamagePerTick;boosted.ApplyStats(checkAbility);Check("new damage talent works "+tree,Math.Abs(checkAbility.DamagePerTick/baseDamage-(tree=="annihilation"?1.08:1.1))<.001);
     w.Progression!.Recalculate();
 }
 TalentProfiles.Save("marauder",new Dictionary<string,int>());

 var drops=p.Upgrades.Where(u=>u.Spec=="carnage" && u.Mechanic.StartsWith("mr_",System.StringComparison.Ordinal)).ToArray();
 Check("nine distinct Carnage interactions",drops.Length==9&&drops.Select(u=>u.Mechanic).Distinct().Count()==9);
 Check("drops share tree descriptions and recovered icons",drops.All(u=>TalentCatalog.Current.ForClass("marauder").Any(t=>t.Tree=="carnage"&&t.Name==u.Title&&t.Description==u.Description)&&ResourceLoader.Exists("res://Assets/UI/icons/"+u.Icon+".png")));
 Check("unowned cleave and Gore improvements excluded",!p.IsEligible(drops.Single(u=>u.Mechanic=="mr_cleave"))&&!p.IsEligible(drops.Single(u=>u.Mechanic=="mr_gore")));
 foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);
 var m=w.Abilities.Find("massacre")!;var r=w.Abilities.Find("ravage")!;var rupture=w.Abilities.Find("rupture")!;var gore=w.Abilities.Find("gore")!;var berserk=w.Abilities.Find("berserk")!;
 double bleed=rt.PeriodicInterval(rupture);float baseMassacre=m.CooldownSeconds;
 Check("Combat Alacrity drop grants",p.Grant(drops.Single(u=>u.Mechanic=="mr_alacrity").Id));
 Check("run haste matches 15 percent talent",Math.Abs(m.CooldownSeconds-baseMassacre/1.15)<.001&&Math.Abs(rt.PeriodicInterval(rupture)-bleed)<.001&&!rt.MarauderAtaruActive);
 Check("cleave grant",p.Grant("marauder_cleave"));Check("full damage five and six target cleave",m.Definition.MaximumTargets==5&&r.Definition.MaximumTargets==6&&m.Definition.SplashMultiplier==1&&r.Definition.SplashMultiplier==1);
 for(int i=0;i<3;i++){Check("massacre reach rank "+i,p.Grant("marauder_target_massacre"));Check("ravage reach rank "+i,p.Grant("marauder_target_ravage"));}
 Check("reach cards add targets with no cap",m.Definition.MaximumTargets==8&&r.Definition.MaximumTargets==9&&p.Grant("marauder_target_massacre")&&m.Definition.MaximumTargets==9);
 Check("radius card improves the cone",p.Grant("marauder_radius_massacre")&&m.Radius>.55f);
 var targets=new List<Combatant>();for(int i=0;i<6;i++){var t=new Combatant{Name="CarnageTarget"+i,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=10000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*(.1f+.015f*i);targets.Add(t);}
 await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
 foreach(string tree in new[]{"annihilation","carnage","marauder_rage"}) {
     var dip=talents.Where(t=>t.Tree==tree && t.Tier==0).ToDictionary(t=>t.Id,t=>1);TalentProfiles.Save("marauder",dip);var offspec=new TalentRuntime(w);
     if(tree=="annihilation") {w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(10,DamageType.Internal);float hpBefore=w.PlayerActor.Health.Current;offspec.DirectHit(m,targets[0]);Check("Annihilation dip heals unbled Carnage attacks",w.PlayerActor.Health.Current>=hpBefore+3);}
     if(tree=="carnage") {var saber=w.Abilities.Find("annihilate")!;float cd=saber.CooldownSeconds;offspec.ApplyStats(saber);Check("Carnage dip hastens Annihilate without forcing stance",Math.Abs(saber.CooldownSeconds-cd/1.15)<.001&&!offspec.MarauderAtaruActive);w.Progression!.Recalculate();}
     if(tree=="marauder_rage") {Check("Rage dip empowers saber attacks without Crush",Math.Abs(offspec.DirectBonus(m,targets[0])-.08)<.001);var execute=w.Abilities.Find("viciousthrow")!;typeof(AutomaticAbility).GetProperty("CooldownRemaining")!.SetValue(execute,10d);offspec.MarauderKilled();offspec.MarauderKilled();Check("Rage execute recovery is bounded",Math.Abs(execute.CooldownRemaining-9)<.001);}
 }
 TalentProfiles.Save("marauder",new Dictionary<string,int>());
 Check("Ataru mastery grant",p.Grant("marauder_flurry"));rt.Activated(m);rt.DirectHit(m,targets[0]);Check("Ataru mastery hits four enemies",targets.Count(t=>t.Health.Current<t.Health.Maximum)==4);
 float hp=targets.Sum(t=>t.Health.Current);foreach(var t in targets)rt.DirectHit(m,t);Check("cleave cannot multiply Ataru procs",targets.Sum(t=>t.Health.Current)==hp);rt.Advance(1.21);rt.DirectHit(m,targets[0]);Check("Ataru returns at 1.2 seconds",targets.Sum(t=>t.Health.Current)<hp);
 foreach(var u in drops.Where(u=>u.Mechanic is not ("mr_alacrity" or "mr_cleave" or "mr_flurry")))Check("grant "+u.Title,p.Grant(u.Id));
 typeof(AutomaticAbility).GetProperty("CooldownRemaining")!.SetValue(gore,10d);rt.Activated(m);Check("Slaughter refunds Gore once per activation",Math.Abs(gore.CooldownRemaining-9.25)<.001);foreach(var t in targets)rt.DirectHit(m,t);Check("cleave does not multiply Gore refund",Math.Abs(gore.CooldownRemaining-9.25)<.001);
 rt.Activated(gore);typeof(AutomaticAbility).GetProperty("CooldownRemaining")!.SetValue(m,5d);rt.Activated(r);Check("Overwhelm recovers Massacre once per Ravage activation",Math.Abs(m.CooldownRemaining-3.5)<.001);foreach(var t in targets)rt.DirectHit(r,t);Check("Overwhelm cannot multiply on cleave hits",Math.Abs(m.CooldownRemaining-3.5)<.001);Check("Gore empowers saber cleave",rt.DirectBonus(m,targets[0])>=.8f);rt.Advance(5);Check("talented Gore lasts beyond baseline",rt.MarauderAuraActive("gore"));rt.Advance(1.1);Check("Gore expires",!rt.MarauderAuraActive("gore"));
 rt.Activated(berserk);rt.Advance(7);Check("Lasting Frenzy lengthens Berserk",rt.MarauderAuraActive("berserk"));rt.Advance(1.1);Check("Berserk expiry preserved",!rt.MarauderAuraActive("berserk"));
 var generate=typeof(RunProgression).GetMethod("GenerateOffers",BindingFlags.NonPublic|BindingFlags.Instance)!;
 for(ulong seed=1;seed<=30;seed++){p.ResetRun();m.SetUnlocked(true);r.SetUnlocked(true);gore.SetUnlocked(true);berserk.SetUnlocked(true);p.SeedOffers(seed);var offers=(List<UpgradeDefinition>)generate.Invoke(p,new object[]{10,Array.Empty<UpgradeDefinition>(),3})!;Check("useful Carnage drop and mastery seed "+seed,offers.Count==3&&offers.Any(u=>u.Spec=="carnage"&&u.Effect==UpgradeEffect.Mechanic)&&offers.Any(u=>u.Effect==UpgradeEffect.AbilityPower)&&offers.All(p.IsEligible));}
 var ranks=TalentCatalog.Current.ForClass("marauder").Where(t=>t.Tree=="carnage").ToDictionary(t=>t.Id,t=>1);Check("old IDs validate",TalentCatalog.Current.Validate("marauder",ranks).Count==9);TalentProfiles.Save("marauder",ranks);rt=new TalentRuntime(w);typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,rt);p.ResetRun();foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);Check("pregame talents suppress duplicate drops",drops.All(u=>!p.IsEligible(u)));Check("Carnage opening starts Massacre Gore Ravage",rt.MarauderEarlyAbilities.Take(3).SequenceEqual(new[]{"massacre","gore","ravage"}));
 }catch(Exception e){failure=e.ToString();GD.PushError(failure);}
 finally {typeof(RunBuildEffects).GetProperty("Pregame")!.SetValue(w.Build,originalRuntime);if(loadout!=null)inv.State.Loadouts["marauder"]=loadout;else inv.State.Loadouts.Remove("marauder");if(original!=null)System.IO.File.WriteAllText(path,original);else if(System.IO.File.Exists(path))System.IO.File.Delete(path);}
 System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/carnage-rework-smoke.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
