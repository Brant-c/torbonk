using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

public partial class CombatMeterSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed)
    { if(!passed) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("Meter: "+name); }
    private async Task Frames(int count)
    { for(int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame); }

    public async void Run(GameWorld world)
    {
        ProcessMode=ProcessModeEnum.Always;
        string? failure=null;
        try
        {
            await Frames(3);
            world.ResetCombatWorld(); world.Waves!.Stop(); world.Spawner!.Running=false;
            world.Abilities.SetPhysicsProcess(false); world.Abilities.Abort();
            var meter=world.Hud!.Meter;
            Check("fresh run has zero totals and finite rates",meter.OverallDamage==0 && meter.OverallHealing==0 && meter.Dps==0 && meter.Hps==0);
            var target=new Combatant {Team=CombatTeam.Hostile,TargetId="MeterFixture"};
            target.AddChild(new Health {Name="Health",Maximum=100}); world.AddChild(target);
            target.Health.ApplyShield(20,30);
            target.Health.ApplyDamage(30); target.Health.ApplyDamage(500);
            target.Health.ApplyDamage(100);
            Check("damage counts actual HP loss, excluding absorption, overkill and dead hits",meter.OverallDamage==100);
            world.PlayerActor.Health.ApplyDamage(40);
            world.PlayerActor.Health.ApplyHealing(500);
            world.PlayerActor.Health.ApplyHealing(10);
            Check("healing counts restored HP and excludes incoming damage and overheal",meter.OverallDamage==100 && meter.OverallHealing==40);
            await Frames(5);
            Check("rates use elapsed run seconds",Math.Abs(meter.Dps-100/world.Progression!.ElapsedSeconds)<0.001 && Math.Abs(meter.Hps-40/world.Progression.ElapsedSeconds)<0.001);
            GetTree().Paused=true;
            double pausedTime=world.Progression.ElapsedSeconds, pausedDps=meter.Dps;
            await Frames(5);
            Check("pause freezes run clock and rates",world.Progression.ElapsedSeconds==pausedTime && meter.Dps==pausedDps);
            GetTree().Paused=false;
            world.PlayerActor.Health.ApplyDamage(10000);
            double deathTime=world.Progression.ElapsedSeconds;
            await Frames(5);
            Check("death preserves totals and freezes rates",world.Progression.ElapsedSeconds==deathTime && meter.OverallDamage==100 && meter.OverallHealing==40);
            world.ResetCombatWorld(); world.Waves.Stop(); world.Spawner.Running=false;
            Check("restart clears totals and rates",meter.OverallDamage==0 && meter.OverallHealing==0 && meter.Dps==0 && meter.Hps==0);
            // Populate a readable capture through the same resolved-health path.
            target.Health.Maximum=300000; target.Health.Restore(); target.Health.ApplyDamage(248560);
            world.PlayerActor.Health.Maximum=40000; world.PlayerActor.Health.Restore();
            world.PlayerActor.Health.ApplyDamage(32640); world.PlayerActor.Health.ApplyHealing(32640);
            await Frames(3);
            if(DisplayServer.GetName()!="headless")
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/combat-meter-preview.png"));
            }
            target.QueueFree();
        }
        catch(Exception ex) { failure=ex.ToString(); GD.PushError(failure); }
        GetTree().Paused=false;
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/combat-meter-smoke.json"),
            JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
