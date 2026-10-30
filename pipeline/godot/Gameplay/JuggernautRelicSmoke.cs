using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;
public partial class JuggernautRelicSmoke:Node
{
 public async void Run(GameWorld w) {
  var checks=new List<string>();string? failure=null;
  var inv=UI.EquipmentWindows.Instance.Inventory;string saved=JsonSerializer.Serialize(inv.State);
  void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);}
  try {
   w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
   foreach(var enemy in w.Spawner.Enemies){enemy.SetPhysicsProcess(false);enemy.CombatEnabled=false;enemy.GlobalPosition+=Vector3.Right*100;}
   var relic=w.Build.JuggernautRelics;var items=inv.Items.Where(i=>i.Classes.SequenceEqual(new[]{"juggernaut"})).ToArray();
   Check("six owned relics with four tooltip ranks",items.Length==6 && items.All(i=>inv.State.Owned.ContainsKey(i.Id)&&i.RankEffects.Length==4));
   Check("recovered icons load",items.All(i=>GD.Load<Texture2D>(UI.EquipmentWindows.IconPath(i))!=null));
   Check("Warrior bag visibility and class restriction",items.All(i=>PersistentInventory.VisibleForClass(i,"marauder")&&!PersistentInventory.Compatible(i,"marauder")));
   foreach(var a in items)foreach(var b in items.Where(b=>b!=a))Check("pair "+a.Id+" / "+b.Id,inv.Equip("juggernaut","relic1",a.Id)&&inv.Equip("juggernaut","relic2",b.Id));
   var targets=new List<Combatant>();for(int i=0;i<7;i++){var t=new Combatant{Name="RelicDummy"+i,Team=CombatTeam.Hostile};t.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*(.1f+.01f*i);targets.Add(t);}
   await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
   foreach(var a in w.Abilities.Abilities)a.SetUnlocked(true);
   void Equip(string id,int r){relic.Reset();inv.Unequip("juggernaut","relic1");inv.Unequip("juggernaut","relic2");inv.State.Owned[id]=r;Check("equip "+id+" +"+r,inv.Equip("juggernaut","relic1",id));}
   var smash=w.Abilities.Find("smash")!;var impale=w.Abilities.Find("impale")!;var crush=w.Abilities.Find("forcecrush")!;var ravage=w.Abilities.Find("ravage")!;var retaliation=w.Abilities.Find("retaliation")!;
   for(int r=0;r<4;r++) {
    foreach(var a in new[]{smash,impale}) {
     Equip(a==smash?"worldbreaker":"impaling_fury",r);int before=targets[0].Health.DamageEvents,casts=a.Casts;
     relic.Resolved(a,new[]{targets[0]});relic.Advance(1);
     Check(a.Definition.Id+" echo count +"+r,targets[0].Health.DamageEvents-before==new[]{1,2,2,3}[r] && a.Casts==casts);
     before=targets[0].Health.DamageEvents;relic.Advance(1);Check("no recursive echoes "+a.Definition.Id+r,targets[0].Health.DamageEvents==before);
    }
    foreach(var a in new[]{crush,ravage}) {
     Equip(a==crush?"endless_fury":"ravagers_wrath",r);typeof(AutomaticAbility).GetProperty("CastTarget")!.SetValue(a,targets[0]);
     Check(a.Definition.Id+" targets +"+r,relic.Targets(a,new[]{targets[0]}).Length==(a==crush?2+r:3+r));
     var counts=targets.Select(t=>t.Health.DamageEvents).ToArray();
     typeof(AutomaticAbility).GetProperty("State")!.SetValue(a,LightningState.Channeling);
     typeof(AutomaticAbility).GetMethod("Tick",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(a,null);
     int affected=a==crush?2+r:3+r;
     Check(a.Definition.Id+" single dispatch per victim +"+r,targets.Select((t,i)=>t.Health.DamageEvents-counts[i]).SequenceEqual(Enumerable.Range(0,7).Select(i=>i<affected?1:0)));
     if(a==crush)Check("normal Force Crush periodic on every victim +"+r,targets.Take(affected).All(t=>w.Periodic.Has(crush,t)));
     a.ResetCombat();w.Periodic.Clear();

    }
    Equip("undying_wrath",r);relic.DefenseStarted();float total=0;for(int i=0;i<100;i++)total+=relic.CappedHealing(4);
    Check("finite healing +"+r,total==32+8*r && Math.Abs(relic.HealInterval-1d/(2+r))<.001);
    Equip("vengeful_retribution",r);retaliation.ResetCombat();w.Player.Presentation.ResetPresentation();w.PlayerActor.Health.Restore();w.PlayerActor.Health.ApplyDamage(1);int prior=retaliation.Casts;relic.Advance(.01);
    Check("damage triggers ordinary retaliation +"+r,retaliation.Casts==prior+1 && retaliation.CooldownRemaining>0);
    relic.Advance(.01);Check("retaliation respects cooldown +"+r,retaliation.Casts==prior+1);
    Check("range scales +"+r,Math.Abs(relic.Range(retaliation)/retaliation.Range-(1.25f+.25f*r))<.001);
   }
   Equip("worldbreaker",0);relic.Resolved(smash,new[]{targets[0]});inv.Unequip("juggernaut","relic1");int events=targets[0].Health.DamageEvents;relic.Advance(1);Check("unequip cancels pending damage",targets[0].Health.DamageEvents==events);
   Check("baseline targeting and healing",!relic.OverridesTargets(crush)&&!relic.OverridesTargets(ravage)&&relic.HealInterval==1&&relic.Bonus==0);
   inv.State.Credits=10000;inv.State.Owned["worldbreaker"]=0;for(int r=0;r<3;r++)Check("Credit upgrade "+r,inv.Upgrade("worldbreaker"));Check("rank capped",!inv.Upgrade("worldbreaker"));
   var reload=new PersistentInventory();Check("ownership and upgrades persist",reload.State.Owned["worldbreaker"]==3);
  }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("user://equipment-v1.json"),saved);
  System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/juggernaut-relic-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
 }
}
