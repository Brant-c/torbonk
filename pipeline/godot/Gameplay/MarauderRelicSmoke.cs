using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;
public partial class MarauderRelicSmoke : Node
{
 public async void Run(GameWorld w) {
 var checks=new List<string>();string? failure=null;
 var inv=UI.EquipmentWindows.Instance.Inventory;
 var original=System.Text.Json.JsonSerializer.Serialize(inv.State);
 void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);}
 try {
 w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
 foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
 var relic=w.Build.MarauderRelics;
 var items=inv.Items.Where(i=>i.Classes.SequenceEqual(new[]{"marauder"})).ToArray();
 Check("eleven owned relics, four tooltip ranks",items.Length==11 && items.All(i=>inv.State.Owned.ContainsKey(i.Id)&&i.RankEffects.Length==4));
 foreach(var a in items)foreach(var b in items.Where(b=>b!=a))Check("pair "+a.Id+" / "+b.Id,inv.Equip("marauder","relic1",a.Id)&&inv.Equip("marauder","relic2",b.Id));
 Check("visible to Warrior, restricted to Marauder",items.All(i=>PersistentInventory.VisibleForClass(i,"juggernaut")&&!PersistentInventory.Compatible(i,"juggernaut")));
 void Equip(string a,string b,int rank){relic.Reset();w.Periodic.Clear();inv.Unequip("marauder","relic1");inv.Unequip("marauder","relic2");inv.State.Owned[a]=inv.State.Owned[b]=rank;inv.Equip("marauder","relic1",a);inv.Equip("marauder","relic2",b);}
 Combatant Dummy(string name,float x){var t=new Combatant{Name=name,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*x;return t;}
 var t=Dummy("RelicTarget",.15f);var n=Dummy("RelicNeighbor",.2f);
 await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
 foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);
 var annihilate=w.Abilities.Find("annihilate")!;var rupture=w.Abilities.Find("rupture")!;var crush=w.Abilities.Find("forcecrush")!;var smash=w.Abilities.Find("smash")!;var massacre=w.Abilities.Find("massacre")!;var ravage=w.Abilities.Find("ravage")!;var gore=w.Abilities.Find("gore")!;var predation=w.Abilities.Find("predation")!;var berserk=w.Abilities.Find("berserk")!;var ds=w.Abilities.Find("deadlysaber")!;var saber=w.Abilities.Find("saberthrow")!;
 typeof(AutomaticAbility).GetProperty("CastTarget")!.SetValue(annihilate,t);
 for(int r=0;r<4;r++) {
 Equip("accelerated_agony","exsanguination",r);w.Periodic.Apply(rupture,t);double duration=w.Periodic.Remaining(rupture,t);float hp=t.Health.Current;relic.Resolved(annihilate,new[]{t});Check("bleed burst non-consuming "+r,t.Health.Current<hp && w.Periodic.Remaining(rupture,t)==duration);hp=t.Health.Current;relic.Resolved(annihilate,new[]{t});Check("Exsanguination ICD "+r,t.Health.Current==hp);w.Periodic.Advance(3);Check("acceleration preserves duration "+r,Math.Abs(w.Periodic.Remaining(rupture,t)-(duration-3))<.001);
 Equip("crimson_executioner","endless_berserk",r);w.Periodic.Apply(rupture,n);relic.Killed(n);hp=t.Health.Current;relic.Resolved(annihilate,new[]{t});Check("stored bleed released "+r,t.Health.Current<hp);hp=t.Health.Current;relic.Resolved(annihilate,new[]{t});Check("storage consumed "+r,t.Health.Current==hp);
 Equip("gore_master","ataru_frenzy",r);w.Build.Activated(gore);hp=t.Health.Current;relic.Resolved(massacre,new[]{t,n});Check("Gore extra hit "+r,t.Health.Current<hp);relic.Activated(massacre);Check("Ataru frequency and coverage "+r,relic.AtaruInterval<1.5 && relic.AtaruTargets==3+r);
 Equip("predatory_carnage","twin_blades",r);w.Build.Activated(predation);Check("Predation acceleration "+r,relic.Acceleration>1);relic.Resolved(saber,new[]{t,n});Check("Saber Throw owned bleed "+r,w.Periodic.Has(ds,t)&&w.Periodic.Has(ds,n));int count=w.Periodic.Count;relic.Resolved(saber,new[]{t,n});Check("no duplicate bleed ownership "+r,w.Periodic.Count==count);
 Equip("crushing_fury","shockwave",r);w.Periodic.Apply(crush,t);relic.Periodic(crush);relic.Periodic(crush);relic.Periodic(crush);relic.Activated(smash);Check("Smash empowered radius and damage "+r,relic.Radius(smash)>smash.Radius && relic.Bonus(smash,t)>0);hp=t.Health.Current;relic.Resolved(smash,new[]{t});Check("Crush burst non-consuming "+r,t.Health.Current<hp&&w.Periodic.Has(crush,t));hp=t.Health.Current;relic.Advance(.5);Check("delayed impact only at +3 "+r,(t.Health.Current<hp)==(r==3));
 Equip("obliteration","endless_berserk",r);relic.Activated(w.Abilities.Find("forcecharge")!);relic.Activated(smash);Check("engaged target Smash "+r,relic.TargetSmash&&relic.Bonus(smash,t)>0);
 }
 inv.Unequip("marauder","relic1");inv.Unequip("marauder","relic2");relic.Reset();Check("unequipped relics inert",items.All(i=>relic.Rank(i.Id)==-1)&&relic.Acceleration==1&&relic.Bonus(smash,t)==0);
 Check("baseline cleave",massacre.Definition.MaximumTargets>=3&&ravage.Definition.MaximumTargets>=4&&massacre.Definition.SplashMultiplier==1&&ravage.Definition.SplashMultiplier==1);
 inv.State.Owned["accelerated_agony"]=0;inv.State.Credits=10000;Check("Credit upgrade",inv.Upgrade("accelerated_agony")&&inv.State.Credits==9500);Check("rank persistence",new PersistentInventory().State.Owned["accelerated_agony"]==1);
 }catch(Exception e){failure=e.ToString();GD.PushError(failure);}
 finally {System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("user://equipment-v1.json"),original);}
 System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/marauder-relic-smoke.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
