using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;
public partial class AssassinRelicSmoke : Node
{
 public async void Run(GameWorld w) {
 var checks=new List<string>();string? failure=null;
 void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);}
 try {
 w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
 foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
 var inv=UI.EquipmentWindows.Instance.Inventory;var relic=w.Build.AssassinRelics;
 var items=inv.Items.Where(i=>i.Classes.SequenceEqual(new[]{"assassin"})).ToArray();
 Check("nine owned relics and four tooltip ranks",items.Length==9 && items.All(i=>inv.State.Owned.ContainsKey(i.Id)&&i.RankEffects.Length==4));
 foreach(var a in items)foreach(var b in items.Where(b=>b!=a))Check("pair "+a.Id+" / "+b.Id,inv.Equip("assassin","relic1",a.Id)&&inv.Equip("assassin","relic2",b.Id));
 Check("class restriction",!inv.Equip("sorcerer","relic1",items[0].Id));
 void Equip(string a,string b,int rank){relic.Reset();inv.Unequip("assassin","relic1");inv.Unequip("assassin","relic2");inv.State.Owned[a]=inv.State.Owned[b]=rank;inv.Equip("assassin","relic1",a);inv.Equip("assassin","relic2",b);}
 Combatant Dummy(string name,float x){var t=new Combatant{Name=name,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*x;return t;}
 var t=Dummy("RelicTarget",.15f);var n=Dummy("RelicNeighbor",.2f);
 await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
 foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);
 var wither=w.Abilities.Find("wither")!;var field=w.Abilities.Find("deathfield")!;var maul=w.Abilities.Find("maul")!;var slash=w.Abilities.Find("voltaicslash")!;var shock=w.Abilities.Find("shock")!;var cloak=w.Abilities.Find("forcecloak")!;var terror=w.Abilities.Find("creepingterror")!;var lacerate=w.Abilities.Find("lacerate")!;var deflect=w.Abilities.Find("deflection")!;
 for(int r=0;r<4;r++){
 Equip("lingering_death_field","withering_force",r);
 Check("Wither radius rank "+r,Math.Abs(relic.Radius(wither)/wither.Radius-new[]{1.25,1.4,1.6,1.8}[r])<.001);
 Check("Wither bonus rank "+r,Math.Abs(relic.Bonus(wither,t)-new[]{.75,1.25,1.75,2.5}[r])<.001);
 w.Player.Presentation.ResetPresentation();field.ResetCombat();Check("field casts "+r,field.TryBegin());relic.Resolved(field,new[]{t,n});float hp=n.Health.Current;relic.Advance(4+2*r);Check("field ticks full duration "+r,n.Health.Current<hp);hp=n.Health.Current;relic.Advance(1);Check("field expires "+r,n.Health.Current==hp);
 Equip("voltaic_fury","phantom_blade",r);relic.Damaged(slash,t);hp=n.Health.Current;relic.Resolved(shock,new[]{t});Check("empowered Shock chains "+r,n.Health.Current<hp);hp=n.Health.Current;relic.Resolved(shock,new[]{t});Check("Shock consumes empowerment "+r,n.Health.Current==hp);relic.Resolved(maul,new[]{t});Check("spectral cleave "+r,n.Health.Current<hp);
 Equip("devouring_plague","dark_parasite",r);w.Periodic.Clear();w.Periodic.Apply(terror,t);int count=w.Periodic.Count;hp=n.Health.Current;relic.Spread(lacerate,new[]{n});Check("burst does not duplicate dots "+r,w.Periodic.Count==count && n.Health.Current<hp);
 w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(30,DamageType.Internal);hp=w.PlayerActor.Health.Current;relic.Damaged(maul,t);float healed=w.PlayerActor.Health.Current;relic.Damaged(maul,t);Check("parasite bounded healing "+r,healed>hp && w.PlayerActor.Health.Current==healed);
 Equip("shrouded_assassin","crushing_darkness",r);relic.Damaged(wither,t);relic.Damaged(wither,n);hp=n.Health.Current;relic.Damaged(maul,t);Check("marks connect "+r,n.Health.Current<hp);relic.Activated(deflect);hp=n.Health.Current;relic.Advance(1);Check("defensive aura ticks "+r,n.Health.Current<hp);
 Equip("hidden_predator","phantom_blade",r);relic.Activated(cloak);relic.Activated(maul);Check("predator haste "+r,relic.Haste==1.5f);hp=n.Health.Current;relic.Resolved(maul,new[]{t});Check("opening cleave "+r,n.Health.Current<hp);relic.Advance(2+r);Check("haste expires "+r,relic.Haste==1);
 }
 Equip("lingering_death_field","withering_force",3);w.Player.Presentation.ResetPresentation();wither.ResetCombat();float before=t.Health.Current;Check("actual Wither begins",wither.TryBegin());for(int i=0;i<40;i++)wither.Advance(.05);Check("actual Wither deals empowered damage",t.Health.Current<before-wither.DamagePerTick*3);
 w.Player.Presentation.ResetPresentation();field.ResetCombat();Check("actual Death Field begins",field.TryBegin());for(int i=0;i<25;i++)field.Advance(.05);before=t.Health.Current;relic.Advance(1);Check("field persists after normal cast completes",!field.Casting && t.Health.Current<before);Check("rank three field melee vulnerability",Math.Abs(relic.Bonus(maul,t)-.25f)<.001);
 Equip("hidden_predator","phantom_blade",3);
 inv.State.Credits=10000;inv.State.Owned["hidden_predator"]=0;Check("upgrade uses Credits",inv.Upgrade("hidden_predator")&&inv.State.Credits==9500);var reload=new PersistentInventory();Check("ranks and loadout persist",reload.State.Owned["hidden_predator"]==1&&reload.Equipped("assassin","relic1")=="hidden_predator");
 inv.Unequip("assassin","relic1");inv.Unequip("assassin","relic2");Check("unequipped inert",items.All(i=>relic.Rank(i.Id)==-1)&&relic.Radius(wither)==wither.Radius&&relic.Bonus(wither,t)==0);relic.Reset();w.Abilities.Abort();w.Periodic.Clear();
 }catch(Exception e){failure=e.ToString();GD.PushError(failure);}
 System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/assassin-relic-smoke.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}



