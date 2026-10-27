using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class JuggernautSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool ok){if(!ok)throw new Exception(name);_checks.Add(name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var cat=TalentCatalog.Current;
            Check("24 talents, eight per tree",cat.ForClass("juggernaut").Length==24 && cat.ForClass("juggernaut").GroupBy(t=>t.Tree).All(t=>t.Count()==8));
            Check("other talent catalogs retained",cat.ForClass("sorcerer").Length==24 && cat.ForClass("assassin").Length==24);
            Check("free replaceable starting Slash",w.Combat.Definition.Id=="viciousslash" && w.Combat.Unlocked && w.Combat.Definition.Slotless && w.Abilities.Equipped(AbilityCategory.Offensive)==0);
            Check("Assault icon is source Warrior Assault",w.Abilities.Find("basicassault")!.Definition.Icon=="jg_assault");
            Check("normal offensive and utility budgets",w.Abilities.OffensiveLimit==w.SlotRules!.OffensiveSlots && w.Abilities.UtilityLimit==w.SlotRules.UtilitySlots);
            Check("durable baseline",w.PlayerActor.Health.Maximum>=155 && w.Build.IncomingMultiplier<.9f);
            var target=new Combatant {Name="JuggernautFixture",Team=CombatTeam.Hostile};target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            AutomaticAbility Cast(string id){var a=w.Abilities.Find(id)!;w.Player.Presentation.ResetPresentation();a.SetUnlocked(true);a.ResetCombat();Check(id+" activates",a.TryBegin());for(int i=0;i<90;i++)a.Advance(.05);return a;}
            Cast("impale");var ravage=Cast("ravage");Check("Ravage has three timed hits",ravage.DamageEvents==3);
            Cast("forcecrush");Check("Force Crush persists",w.Periodic.Has(w.Abilities.Find("forcecrush")!,target));var before=target.Health.Current;w.Periodic.Advance(2);Check("Force Crush ticks",target.Health.Current<before);
            var retaliate=w.Abilities.Find("retaliation")!;retaliate.SetUnlocked(true);retaliate.ResetCombat();Check("counterattack waits for damage",!retaliate.TryBegin());w.PlayerActor.Health.ApplyDamage(5,DamageType.Internal);Cast("retaliation");
            var execute=w.Abilities.Find("viciousthrow")!;execute.SetUnlocked(true);execute.ResetCombat();Check("execution waits for threshold",!execute.TryBegin());target.Health.ApplyDamage(8000,DamageType.Internal);Cast("viciousthrow");
            w.PlayerActor.Health.ApplyDamage(80,DamageType.Internal);Cast("enrageddefense");float h=w.PlayerActor.Health.Current;w.PlayerActor.Health.ApplyDamage(1,DamageType.Internal);Check("Enraged Defense reacts with bounded healing",w.PlayerActor.Health.Current>h && w.PlayerActor.Health.Current<h+5);w.Build.Pregame.Advance(7);
            Cast("saberward");Check("Saber Ward creates finite protection",w.PlayerActor.Health.Shielded);w.PlayerActor.Health.ClearShield();
            var smash=w.Abilities.Find("smash")!;Check("Smash leap preserved",smash.Definition.StartClip=="D3/SmashLeap" && smash.Definition.WindupSeconds>1);
            Cast("smash");Cast("forcescream");Cast("sunderingassault");Check("Sundering Assault weakens armor",target.Health.VulnerabilitySeconds>0);
            var ranks=cat.ForClass("juggernaut").Where(t=>t.Tier==0).ToDictionary(t=>t.Id,t=>1);ranks["vengeance.impale_bleed"]=1;ranks["juggernaut_rage.resonance"]=1;ranks["immortal.sonic"]=1;
            Check("12-point three-tree hybrid validates",cat.Validate("juggernaut",ranks).Count==12);TalentProfiles.Save("juggernaut",ranks);var runtime=new TalentRuntime(w);
            var scream=w.Abilities.Find("forcescream")!;runtime.ApplyStats(scream);Check("Scream evolves into group splash",scream.Definition.Area==AbilityArea.TargetSplash && scream.Radius>=.3f);
            runtime.Activated(w.Abilities.Find("impale")!);runtime.Activated(scream);Check("Impale empowers Scream",runtime.DirectBonus(scream,target)>=.3f);
            runtime.PeriodicTick(w.Abilities.Find("forcecrush")!);runtime.PeriodicTick(w.Abilities.Find("forcecrush")!);runtime.Activated(smash);Check("Force Crush empowers Smash",runtime.DirectBonus(smash,target)>=.24f);
            runtime.Damaged(2);runtime.Activated(retaliate);Check("Retaliation grants temporary mitigation",runtime.IncomingMultiplier<.75f);runtime.Advance(5);Check("mitigation expires",runtime.IncomingMultiplier>.75f);
            Check("all equipped clips exist",w.Abilities.Abilities.All(a=>a.Definition.StartClip=="" || w.Player.Presentation.Animator.HasAnimation(a.Definition.StartClip)));
            foreach(var a in w.Abilities.Abilities)Check(a.Definition.Id+" icon exists",ResourceLoader.Exists("res://Assets/UI/icons/"+a.Definition.Icon+".png"));
            TalentProfiles.Save("juggernaut",new Dictionary<string,int>());
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/juggernaut-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks=_checks},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
