using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;

public partial class SorcererRelicSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();string? failure=null;
        void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);GD.Print(label);}
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            w.Player.TestInput=Vector2.Zero;
            for(int frame=0;frame<8;frame++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Player.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
            var inventory=UI.EquipmentWindows.Instance.Inventory;
            Check("eight Sorcerer relics owned",inventory.Items.Count(i=>i.Classes.SequenceEqual(new[]{"sorcerer"}) && inventory.State.Owned.ContainsKey(i.Id))==8);
            void Equip(string a,string b,int rank=0){inventory.Unequip("sorcerer","relic1");inventory.Unequip("sorcerer","relic2");inventory.State.Owned[a]=rank;inventory.State.Owned[b]=rank;Check("equip "+a+" + "+b,inventory.Equip("sorcerer","relic1",a)&&inventory.Equip("sorcerer","relic2",b));}
            Check("class restriction",!inventory.Equip("assassin","relic1","eye_of_the_tempest"));
            w.Spawner.Running=true;Check("enemy fixture spawns",w.Spawner.TrySpawn(ignoreLimit:true));w.Spawner.Running=false;
            var target=w.Spawner.Enemies.Last();target.SetPhysicsProcess(false);target.CombatEnabled=true;
            target.Body.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.22f;target.Health.Maximum=100000;target.Health.Restore();
            // Plain combatants avoid AI/scene dependencies for deterministic damage checks.
            Combatant Dummy(string name,float x){var t=new Combatant {Name=name,Team=CombatTeam.Hostile};t.AddChild(new Health {Name="Health",Maximum=100000});w.AddChild(t);t.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*x;return t;}
            var a=Dummy("RelicA",.2f);var b=Dummy("RelicB",.24f);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var ability in w.Abilities.Abilities)ability.SetUnlocked(true);
            void Cast(AutomaticAbility ability,double seconds=3){w.Player.Presentation.ResetPresentation();ability.ResetCombat();Check(ability.Definition.Id+" starts",ability.TryBegin());for(double t=0;t<seconds;t+=.05)ability.Advance(.05);}
            var chain=w.Abilities.Find("chain")!;var aff=w.Abilities.Find("affliction")!;var dark=w.Abilities.Find("crushingdarkness")!;var terror=w.Abilities.Find("creepingterror")!;var shock=w.Abilities.Find("shock")!;var storm=w.Abilities.Find("storm")!;var blast=w.Abilities.Find("thunderingblast")!;
            Equip("forked_lightning","lightning_prison",3);Cast(chain,8);Check("triple chain produces repeated hits",chain.DamageEvents>=9);Check("chain roots each enemy",target.RootRemaining==3);target.Root(1.5f);Check("roots refresh without adding durations",target.RootRemaining==3);
            Equip("grip_of_madness","accelerated_torment",3);w.Periodic.Clear();w.Periodic.Apply(aff,a);w.Periodic.Apply(dark,a);w.Periodic.Apply(terror,a);w.Periodic.Apply(aff,target);w.Periodic.Apply(dark,target);w.Periodic.Apply(terror,target);Check("three DoTs cap movement reduction at 42%",Math.Abs(target.SorcererDotMovementFactor-.58f)<.001);w.Periodic.Accelerate(shock,a);int ticks=w.Periodic.Ticks;w.Periodic.Advance(4);Check("accelerated DoTs add ticks",w.Periodic.Ticks-ticks>=9);w.Periodic.Clear();Check("clearing DoTs removes slow",target.SorcererDotMovementFactor==1);Cast(shock);Check("Shock cooldown is twenty seconds",shock.CooldownDuration==20);
            Equip("eye_of_the_tempest","thundering_dominion",3);w.Periodic.Clear();Cast(storm,1.1);Check("first storm detached",storm.ActiveStorms.Count()==1);w.Player.Presentation.ResetPresentation();Check("second storm admitted",storm.TryBegin());storm.Advance(.4);Check("two simultaneous storms",storm.ActiveStorms.Count()==2);Cast(blast);Check("Dominion deduplicates overlapping storms",blast.LastDistinctHits==3);storm.Abort();Check("abort clears storms",!storm.ActiveStorms.Any());
            for(int rank=0;rank<=3;rank++) {
                Equip("eye_of_the_tempest","thundering_dominion",rank);storm.ResetCombat();storm.ChannelSeconds=20;
                for(int i=0;i<rank+2;i++){w.Player.Presentation.ResetPresentation();Check("storm capacity admission",storm.TryBegin());storm.Advance(1.01);storm.Advance(.01);}
                Check("storm rank capacity",storm.ActiveStorms.Count()==rank+2 && !storm.TryBegin());
                storm.Abort();storm.ChannelSeconds=4;
            }
            Equip("corrupting_storm","lingering_agony",3);Cast(storm,1.2);Check("storm applies all three owned DoTs",w.Periodic.On(a).Count(x=>x.Caster==w.PlayerActor)==3);float hp=b.Health.Current;w.Periodic.Detonate(a);Check("remaining DoT damage detonates",b.Health.Current<hp);
            w.Periodic.Clear();Equip("corrupting_storm","lingering_agony",0);Cast(storm,1.2);Check("base corrupting storm applies Affliction only",w.Periodic.On(a).Select(x=>x.Definition.Id).SequenceEqual(new[]{"affliction"}));
            inventory.State.Credits=10000;Check("permanent upgrade purchased",inventory.Upgrade("corrupting_storm"));var reloaded=new PersistentInventory();Check("ownership ranks and slots survive reload",reloaded.State.Owned["corrupting_storm"]==1 && reloaded.Equipped("sorcerer","relic1")=="corrupting_storm");
            inventory.Unequip("sorcerer","relic1");inventory.Unequip("sorcerer","relic2");Check("unequipped relic inactive",SorcererRelics.Rank(storm,"corrupting_storm")==-1);
            w.Abilities.Abort();w.Periodic.Clear();
        } catch(Exception e){failure=e.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/sorcerer-relic-smoke.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
