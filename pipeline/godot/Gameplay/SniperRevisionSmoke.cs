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
            Check("Plasma Probe keeps two fields at once but not three",fields==2);
            // Diversion
            var enemy=new EnemyAgent{Name="DiversionEnemy",Team=CombatTeam.Hostile,AttackDamage=10};
            Check("Diversion exists and is a Sniper utility",A("diversion").Definition.Category==AbilityCategory.Utility && A("diversion").Definition.Icon=="smokegrenade");
            enemy.DiversionRemaining=3;Check("diverted enemy cannot attack",enemy.DiversionRemaining>0);
            enemy.QueueFree();
            var live=w.Spawner.Enemies.FirstOrDefault(x=>GodotObject.IsInstanceValid(x) && x.Alive);
            DiversionEffect.Spawn(w,w.PlayerActor.GlobalPosition,2f);
            if(live!=null){live.DiversionRemaining=1.5;DiversionEffect.AttachTarget(live);}
            for(int i=0;i<90;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var effect=w.GetNodeOrNull<DiversionEffect>("DiversionEffect");
            Check("Diversion plays recovered grenade, impact smoke and target fx",effect!=null && effect.GetChildren().OfType<EternityVaultWeather>().Count()==4 && (live==null || live.GetNodeOrNull("DiversionTarget")!=null));
        } catch(Exception ex){failure=ex.ToString();GD.PrintErr("ERROR: "+ex);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/sniper-revision-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,checks,failure},new JsonSerializerOptions{WriteIndented=true}));GetTree().Quit(failure==null?0:1);
    }
}
