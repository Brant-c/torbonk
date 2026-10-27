using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class AssassinExpansionSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool ok){if(!ok)throw new Exception(name);_checks.Add(name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            TalentProfiles.Save("assassin",new Dictionary<string,int>());
            var cat=TalentCatalog.Current;
            Check("24 Assassin talents in three trees",cat.ForClass("assassin").Length==24 && cat.ForClass("assassin").GroupBy(t=>t.Tree).All(t=>t.Count()==8));
            Check("Sorcerer retains 24 separate talents",cat.ForClass("sorcerer").Length==24 && !cat.ForClass("sorcerer").Any(t=>t.Classes.Contains("assassin")));
            var ranks=new Dictionary<string,int>{["deception.thrashing_blades"]=1,["deception.duplicity"]=1,["deception.chain_shock"]=1,["assassin_madness.creeping_terror"]=1,["assassin_madness.claws_of_decay"]=1,["assassin_madness.parasitism"]=1,["assassin_madness.oppressing_force"]=1,["assassin_madness.haunted_dreams"]=1};
            Check("hybrid allocations validate",cat.Validate("assassin",ranks).Count==ranks.Count);
            Check("class profiles persist independently",TalentProfiles.Save("assassin",ranks)==null && TalentProfiles.Load("assassin").Count==ranks.Count && TalentProfiles.Load("sorcerer").Count==0);
            // Recreate snapshot after saving the fixture profile.
            var runtime=new TalentRuntime(w);
            Check("spread and early terror hooks enabled",runtime.AssassinSpread && runtime.Mechanic("early_terror")==1);
            Check("14 requested ability families loaded",new[]{"lacerate","discharge","voltaicslash","maul","spike","shock","deathfield","creepingterror","crushingdarkness","wither","lowslash","forcecloak","forcespeed","deflection","forceshroud"}.All(id=>w.Abilities.Find(id)!=null));
            Check("slot budgets preserved",w.Abilities.OffensiveLimit==6 && w.Combat.Definition.Slotless);
            var target=new Combatant {Name="AssassinFixture",Team=CombatTeam.Hostile};target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            void Cast(string id){var a=w.Abilities.Find(id)!;w.Player.Presentation.ResetPresentation();a.SetUnlocked(true);a.ResetCombat();Check(id+" begins",a.TryBegin());for(int i=0;i<80;i++)a.Advance(.05);}
            var spike=w.Abilities.Find("spike")!;spike.SetUnlocked(true);Check("Spike provides its own stealth",spike.Definition.WindupSeconds==1f);
            Cast("forcecloak");Check("cloak enables opening",w.Build.Pregame.StealthOpening);Cast("spike");
            Cast("voltaicslash");float before=target.Health.Current;Cast("shock");Check("empowered Shock deals damage",target.Health.Current<before-36);
            Cast("creepingterror");Check("Creeping Terror applies periodic effect",w.Periodic.Has(w.Abilities.Find("creepingterror")!,target));
            w.PlayerActor.Health.ApplyDamage(20,DamageType.Internal);float health=w.PlayerActor.Health.Current;w.Periodic.Advance(1);Check("Creeping Terror provides bounded life drain",w.PlayerActor.Health.Current>health && w.PlayerActor.Health.Current<=health+(w.Build.Pregame.Ranks.Count>0?3:2));
            var shock=w.Abilities.Find("shock")!;runtime.ApplyStats(shock);Check("talent evolves Shock into chain",shock.Definition.Area==AbilityArea.Chain && shock.Definition.BounceCount==2);
            if(w.Build.Pregame.AssassinSpread) {
                var other=new Combatant {Name="SpreadFixture",Team=CombatTeam.Hostile};other.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(other);other.GlobalPosition=target.GlobalPosition+Vector3.Right*.04f;
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Cast("lacerate");Check("talented Lacerate spreads owned Terror",w.Periodic.Has(w.Abilities.Find("creepingterror")!,other));
                w.Periodic.RemoveSource(w.Abilities.Find("creepingterror")!);w.Periodic.Apply(w.Abilities.Find("creepingterror")!,target);Cast("deathfield");Check("Death Field spreads owned Terror",w.Periodic.Has(w.Abilities.Find("creepingterror")!,other));
                w.Periodic.RemoveSource(w.Abilities.Find("creepingterror")!);w.Periodic.Apply(w.Abilities.Find("creepingterror")!,target);w.Periodic.Advance(2);double remaining=w.Periodic.Remaining(w.Abilities.Find("creepingterror")!,target);w.Build.PassOn(target);Check("death transfer preserves remaining duration",Math.Abs(w.Periodic.Remaining(w.Abilities.Find("creepingterror")!,other)-remaining)<.001);
            }
            Cast("deflection");Check("deflection provides finite avoidance",w.Build.IncomingMultiplier<=.5f);w.Build.Pregame.Advance(6);Check("defensive windows expire",w.Build.IncomingMultiplier==1);
            Check("all authored clips resolve",w.Abilities.Abilities.Where(a=>a.Definition.StartClip!="").All(a=>w.Player.Presentation.Animator.HasAnimation(a.Definition.StartClip)));
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/assassin-expansion-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
