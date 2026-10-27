using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
namespace SwtorSurvivors;

public partial class SpecMilestonesSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool pass){if(!pass)throw new Exception(name);_checks.Add(name);GD.Print("SPEC: "+name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try
        {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
            foreach(var enemy in w.Spawner.Enemies){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
            var p=w.Progression!;p.MaximumLevel=30;w.Rewards!.Visible=false;
            foreach(var name in new[]{"role_dps","role_heal","role_tank","lightning","madness","darkness","deception","hatred"})Check("original icon "+name,ResourceLoader.Exists($"res://Assets/UI/specs/{name}.png"));
            async System.Threading.Tasks.Task Capture(string name)
            {
                if(!OS.GetCmdlineUserArgs().Contains("--spec-milestones-review"))return;
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/spec-{w.RunConfiguration.AdvancedClassId}-{name}.png"));
            }
            void AdvanceTo(int level)
            {
                if(p.Selecting && p.PresentationPhase==ProgressionPresentationPhase.Confirming)p._Process(1);
                while(p.Level<level)
                {
                    if(p.Selecting){Check("ordinary level remains three choices",p.Offers.Count==3 && !p.SpecChoice);Check("ordinary choice applies",p.Choose(0));p._Process(1);}
                    else {p.AddExperience(p.RequiredExperience);p._Process(2);}
                }
            }
            void Pick(string spec)
            {
                int index=p.Offers.ToList().FindIndex(u=>u.Spec==spec);Check("choose "+spec+" at "+p.Level,index>=0 && p.Choose(index));
            }
            if(w.RunConfiguration.AdvancedClassId=="assassin")
            {
                AdvanceTo(5);Check("Assassin shows all three specs",p.Offers.Select(u=>u.Spec).SequenceEqual(new[]{"darkness","deception","hatred"}));
                Check("Darkness uses tank role",p.Offers[0].Role=="tank");await Capture("5");Pick("darkness");p._Process(1);
                AdvanceTo(10);Pick("deception");p._Process(1);AdvanceTo(15);Pick("hatred");p._Process(1);
                Check("Assassin mixes three paths",p.Ranks.GetValueOrDefault("spec_5_darkness")==1 && p.Ranks.GetValueOrDefault("spec_10_deception")==1 && p.Ranks.GetValueOrDefault("spec_15_hatred")==1);
            }
            else
            {
                Check("Force Lightning is the sole starting attack",w.Combat.Definition.Id=="lightning" && w.Combat.Unlocked && w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
                Check("Sorcerer has no basic saber attack",!w.Abilities.Abilities.Any(a=>a.Definition.IsBasicAttack));
                Check("channel upgrades support the starting channel",p.IsEligible(p.Upgrades.Single(u=>u.Id=="tick")));
                AdvanceTo(5);Check("level five Shock or Affliction",p.Offers.Select(u=>u.AbilityId).SequenceEqual(new[]{"shock","affliction"}));
                Check("first choice unlocks Shock or Affliction",p.Offers.All(u=>u.Effect==UpgradeEffect.UnlockAbility) && !w.Abilities.Find("shock")!.Unlocked);
                await Capture("5");float before=w.Combat.DamagePerTick;Pick("madness");p._Process(1);
                Check("first choice adds Affliction alongside Force Lightning",w.Combat.Unlocked && w.Abilities.Find("affliction")!.Unlocked && !w.Abilities.Find("shock")!.Unlocked);
                AdvanceTo(10);Check("level ten Storm or Death Field",p.Offers.Select(u=>u.AbilityId).SequenceEqual(new[]{"storm","deathfield"}));await Capture("10");Pick("madness");p._Process(1);
                Check("Storm cannot coexist with Death Field",!w.Abilities.Unlock("storm") && !p.Grant("unlock_storm"));
                AdvanceTo(15);Check("level fifteen Chain or Creeping Terror",p.Offers.Select(u=>u.AbilityId).SequenceEqual(new[]{"chain","creepingterror"}));await Capture("15");Pick("madness");p._Process(1);
                Check("shared Madness DoTs available",w.Abilities.Find("affliction")!.Unlocked);
                var affliction=w.Abilities.Find("affliction")!;var terror=w.Abilities.Find("creepingterror")!;var field=w.Abilities.Find("deathfield")!;
                var targets=new List<Combatant>();
                for(int i=0;i<24;i++)
                {
                    var actor=new Combatant {Name="SpreadFixture"+i,Team=CombatTeam.Hostile};actor.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(actor);
                    actor.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(.18f+(i%6)*.01f,0,(i/6)*.012f);targets.Add(actor);
                }
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var foreign=new AutomaticAbility {Caster=targets[23]};var definition=(AbilityDefinition)affliction.Definition.Duplicate();definition.Id="foreign_dot";foreign.Configure(definition);foreign.SetUnlocked(true);w.AddChild(foreign);
                w.Periodic.Apply(foreign,targets[2]);w.Periodic.Apply(affliction,targets[0]);w.Periodic.Apply(terror,targets[1]);targets[0].Health.ApplyDamage(9999);
                w.Player.Presentation.ResetPresentation();field.ResetCombat();Check("Death Field activates",field.TryBegin());for(int i=0;i<30;i++)field.Advance(.05);
                Check("lethal seed still spreads its DoT",!targets[0].Alive && targets.Skip(1).All(t=>w.Periodic.Has(affliction,t)));
                Check("both DoTs spread to every field survivor without a target cap",targets.Skip(1).All(t=>w.Periodic.Has(affliction,t) && w.Periodic.Has(terror,t)) && field.LastDistinctHits==24);
                Check("foreign DoT is not copied",!w.Periodic.Has(foreign,targets[3]));
                Check("Creeping Terror is stronger",terror.Definition.Damage==12 && terror.Definition.PeriodicDamage==12);
                w.Periodic.Clear();foreach(var target in targets.Skip(1))w.Periodic.Apply(terror,target);
                w.PlayerActor.Health.ApplyDamage(20);before=w.PlayerActor.Health.Current;w.Periodic.Advance(6);
                Check("mass-spread Terror heals modestly once per second",Math.Abs(w.PlayerActor.Health.Current-before-6)<.01);
                w.Periodic.Clear();AdvanceTo(20);Check("Force Lightning upgrade is a Madness choice",p.Offers.Single(u=>u.AbilityId=="lightning").Spec=="madness" && p.Offers.Single(u=>u.AbilityId=="lightning").Effect==UpgradeEffect.AbilityPower);await Capture("20");Pick("lightning");
                Check("full slots open explicit replacement",p.Replacing && p.ReplacementOptions.Count==4 && !w.Abilities.Find("thunderingblast")!.Unlocked);await Capture("replace");p.CancelReplacement();
                Check("back preserves build and milestone",!p.Replacing && p.Offers.Count==2 && w.Combat.Unlocked);Pick("lightning");Check("replacement applies",p.Replace(1));p._Process(1);
                Check("hybrid keeps chosen paths with four slots",w.Combat.Unlocked && w.Abilities.Find("thunderingblast")!.Unlocked && p.Ranks.GetValueOrDefault("spec_5_madness")==1 && p.Ranks.GetValueOrDefault("spec_20_lightning")==1 && w.Abilities.Equipped(AbilityCategory.Offensive)==4);
                var channel=w.Abilities.Find("lightning")!;channel.ResetCombat();w.Player.Presentation.ResetPresentation();
                Check("Madness Force Lightning casts",channel.TryBegin());for(int tick=0;tick<40;tick++)channel.Advance(.05);Check("Madness channel deals damage",channel.DamageEvents>0);
                AdvanceTo(25);Pick("madness");Check("later unlock can replace a full slot",p.Replacing && p.Replace(2));p._Process(1);AdvanceTo(30);Check("milestones continue through level thirty",p.Offers.Count==2 && p.Offers.All(u=>u.MilestoneLevel==30));
            }
            p.ResetRun();Check("reset restores starting attacks and clears choices",p.Level==1 && !p.Replacing && p.Ranks.Count==0 && w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
            if(w.RunConfiguration.AdvancedClassId=="sorcerer")
            {Check("Storm first excludes Death Field too",p.Grant("unlock_storm") && !p.Grant("unlock_deathfield"));p.ResetRun();}
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/spec-milestones-{w.RunConfiguration.AdvancedClassId}-smoke.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=failure==null,checks=_checks,failure},new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
