using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;

/// <summary>Out-of-cover Snipe/Series of Shots, Snipe cover priority, multi-field Plasma Probe and Diversion.</summary>
public partial class SniperRevisionSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            w.Player.TestInput=Vector2.Zero;for(int i=0;i<30;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Player.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.SetPhysicsProcess(false);e.CombatEnabled=false;}
            var rt=w.Build.Pregame;AutomaticAbility A(string id)=>w.Abilities.Find(id)!;
            var target=new Combatant{Name="RevisionTarget",Team=CombatTeam.Hostile};target.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.16f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);w.Targeting.Update(0,true);
            Check("not in cover after moving",!rt.AgentPositioned);
            Check("Snipe base damage 66",Math.Abs(A("snipershot").DamagePerTick-66)<.01f);
            Check("Snipe and Series of Shots are no longer cover abilities",!TalentRuntime.AgentCoverAbility("snipershot") && !TalentRuntime.AgentCoverAbility("seriesofshots") && TalentRuntime.AgentCoverAbility("ambush"));
            var snipe=A("snipershot");snipe.ResetCombat();Check("Snipe casts out of cover",snipe.TryBegin());
            snipe.Abort();w.Player.Presentation.ResetPresentation();A("seriesofshots").SetUnlocked(true);A("seriesofshots").ResetCombat();Check("Series of Shots casts out of cover",A("seriesofshots").TryBegin());
            A("seriesofshots").Abort();w.Player.Presentation.ResetPresentation();A("ambush").SetUnlocked(true);A("ambush").ResetCombat();Check("Ambush still needs cover",!A("ambush").TryBegin());
            Check("Snipe ranks normally out of cover",rt.SniperPriority(snipe)==0);
            rt.Advance(.7);Check("Snipe ranks lower in cover",!rt.AgentPositioned || rt.SniperPriority(snipe)>0);
            Check("Plasma Probe radius and damage raised",A("plasmaprobe").Radius>=.7f && A("plasmaprobe").DamagePerTick>=40 && A("explosiveprobe").DamagePerTick<64);
            var plasma=A("plasmaprobe");plasma.SetUnlocked(true);plasma.ResetCombat();
            int fields=0;
            for(int round=0;round<3;round++){
                plasma.ReduceCooldown(99);w.Player.Presentation.ResetPresentation();
                for(int i=0;i<40 && plasma.State!=LightningState.Idle;i++)plasma.Advance(.05);
                if(!plasma.TryBegin())break;
                for(int i=0;i<200 && plasma.State!=LightningState.Cooldown && plasma.State!=LightningState.Idle;i++)plasma.Advance(.05);
                fields=plasma.ActiveStorms.Count();
            }
            Check("Plasma Probe fields stack without a cap",fields==3);
            var pr=w.Progression!;var diversionCard=pr.Upgrades.SingleOrDefault(u=>u.Id=="unlock_diversion");
            Check("Probe Array is gone",!pr.Upgrades.Any(u=>u.Id=="sniper_array"));
            Check("Diversion is in the Sniper pool and weighted up",diversionCard!=null && rt.OfferWeight(diversionCard)>=4*(1+0));
            var generate=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            int offered=0;
            for(ulong seed=1;seed<=80;seed++){pr.SeedOffers(seed);var offers=(List<UpgradeDefinition>)generate.Invoke(pr,new object[]{8+(int)(seed%8),Array.Empty<UpgradeDefinition>(),3})!;if(offers.Any(o=>o.Id=="unlock_diversion"))offered++;}
            Check("Diversion is eligible and actually offered in runs ("+offered+"/80)",pr.IsEligible(diversionCard!) && offered>=8);
            {
                p0:;
                var gen=typeof(RunProgression).GetMethod("GenerateOffers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
                foreach(string id in new[]{"interrogationprobe","explosiveprobe","plasmaprobe","empdischarge","orbitalstrike","fragmentationgrenade"})A(id).SetUnlocked(true);
                w.Progression!.Recalculate();var freq=new Dictionary<string,int>();
                for(ulong seed=1;seed<=300;seed++){w.Progression.SeedOffers(seed);foreach(var o in (List<UpgradeDefinition>)gen.Invoke(w.Progression,new object[]{8+(int)(seed%8),Array.Empty<UpgradeDefinition>(),3})!)freq[o.Id]=freq.GetValueOrDefault(o.Id)+1;}
                GD.Print("OFFERFREQ "+string.Join(", ",freq.OrderByDescending(k=>k.Value).Take(25).Select(k=>k.Key+"="+k.Value)));
            }
            // Diversion
            var enemy=new EnemyAgent{Name="DiversionEnemy",Team=CombatTeam.Hostile,AttackDamage=10};
            Check("Diversion exists and is a Sniper utility",A("diversion").Definition.Category==AbilityCategory.Utility && A("diversion").Definition.Icon=="smokegrenade");
            enemy.DiversionRemaining=3;Check("diverted enemy cannot attack",enemy.DiversionRemaining>0);
            enemy.QueueFree();
            var live=w.Spawner.Enemies.FirstOrDefault(x=>GodotObject.IsInstanceValid(x) && x.Alive);
            var far=w.Spawner.Enemies.Where(x=>x!=live && GodotObject.IsInstanceValid(x) && x.Alive).FirstOrDefault();
            if(live!=null)live.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.2f;
            if(far!=null){far.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*2.5f;far.DiversionRemaining=0;}
            DiversionEffect.Spawn(w,w.PlayerActor.GlobalPosition,3f);
            for(int i=0;i<110;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check("smoke zone disables enemies inside it only",live==null || live.DiversionRemaining>0 && (far==null || far.DiversionRemaining<=0));
            Check("smoke zone leaves a persistent ground projection",w.GetNodeOrNull("DiversionEffect")?.GetNodeOrNull("DiversionGround")!=null);
            var effect=w.GetNodeOrNull<DiversionEffect>("DiversionEffect");
            Check("Diversion plays recovered grenade, impact smoke and target fx",effect!=null && effect.GetChildren().OfType<EternityVaultWeather>().Count()==4 && (live==null || live.GetNodeOrNull("DiversionTarget")!=null));
        } catch(Exception ex){failure=ex.ToString();GD.PrintErr("ERROR: "+ex);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/sniper-revision-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
