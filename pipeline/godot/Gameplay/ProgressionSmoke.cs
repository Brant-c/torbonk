using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>One short rendered B4 smoke. No captures or historic regression runs.</summary>
public partial class ProgressionSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed)
    {
        if (!passed) throw new InvalidOperationException(name);
        _checks.Add(name);GD.Print("B4 smoke: "+name);
    }
    private async Task Frames(int count)
    {
        for (int i=0;i<count;i++) await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    public async void Run(GameWorld world)
    {
        ProcessMode=ProcessModeEnum.Always;
        string? failure=null;
        var p=world.Progression!;
        float damage=world.Combat.DamagePerTick,tick=world.Combat.TickSeconds,range=world.Targeting.AcquisitionRange,
            cooldown=world.Combat.CooldownSeconds,health=world.PlayerActor.Health.Maximum,speed=world.Player.MoveSpeed;
        try
        {
            world.Combat.CombatEnabled=false;
            ulong until=Time.GetTicksMsec()+7000;
            while (world.Spawner!.ActiveCount==0 && Time.GetTicksMsec()<until) await Frames(1);
            Check("world and original-art HUD initialize",world.Spawner!.ActiveCount>0 && world.Hud!=null && Engine.GetFramesDrawn()>0);
            if (OS.GetCmdlineUserArgs().Contains("--hud-capture"))
            {
                Check("compact reference quickbar has twelve slots",world.Hud!.ActionSlotCount==12);
                GetViewport().GetTexture().GetImage().SavePng("res://../logs/hud-layout-review.png");
            }
            world.Spawner.Enemies.First(e=>e.Alive).Health.ApplyDamage(10000);
            Check("enemy defeat creates XP pickup without instant XP",p.PickupCount==1 && p.Experience==0);
            var pickup=p.GetChildren().OfType<ExperiencePickup>().Single();
            pickup.Value=p.RequiredExperience;pickup.GlobalPosition=world.PlayerActor.GlobalPosition+Vector3.Up*0.035f;
            await Frames(3);
            Check("collecting levels and presents three distinct choices",p.Level==2 && p.Selecting && p.Offers.Select(x=>x.Id).Distinct().Count()==3 &&
                world.Hud!.SelectionVisible && world.Hud.VisibleChoiceCount==3 && p.PickupCount==0);
            double elapsed=p.ElapsedSeconds,wave=world.Waves!.WaveRemaining;var position=world.Player.GlobalPosition;
            int hits=world.PlayerActor.Health.DamageEvents;
            await Frames(12);
            Check("selection pauses time, waves, movement and damage",GetTree().Paused && p.ElapsedSeconds==elapsed &&
                world.Waves.WaveRemaining==wave && world.Player.GlobalPosition==position && world.PlayerActor.Health.DamageEvents==hits);
            var upgrade=p.Offers[0];Check("offered upgrade applies a rank and resumes",p.Choose(0) && p.Rank(upgrade)==1 && !GetTree().Paused);
            bool changed=upgrade.Effect switch
            {
                UpgradeEffect.Damage=>world.Combat.DamagePerTick>damage,
                UpgradeEffect.TickRate=>world.Combat.TickSeconds<tick,
                UpgradeEffect.Range=>world.Targeting.AcquisitionRange>range,
                UpgradeEffect.Cooldown=>world.Combat.CooldownSeconds<cooldown,
                UpgradeEffect.Health=>world.PlayerActor.Health.Maximum>health,
                _=>world.Player.MoveSpeed>speed
            };
            Check("selected configurable effect changes the gameplay stat",changed);
            int required=p.RequiredExperience;p.AddExperience(required*4);
            Check("XP requirement increases and overflow queues another choice",required>p.FirstLevelExperience && p.Selecting && p.Experience>0);
            world.ResetCombatWorld();await Frames(2);
            Check("restart from selection clears XP, ranks, pickups, pause and restores base stats",!GetTree().Paused && p.Level==1 && p.Experience==0 &&
                p.Ranks.Count==0 && p.PickupCount==0 && !world.Hud!.SelectionVisible && world.Waves.Wave==1 &&
                world.Combat.DamagePerTick==damage && world.Combat.TickSeconds==tick && world.Targeting.AcquisitionRange==range &&
                world.Combat.CooldownSeconds==cooldown && world.PlayerActor.Health.Maximum==health && world.Player.MoveSpeed==speed);
        }
        catch (Exception ex) {failure=ex.ToString();GD.PushError("B4 smoke: "+failure);}
        GetTree().Paused=false;
        using (var file=FileAccess.Open("res://../manifests/progression-smoke.json",FileAccess.ModeFlags.Write))
            file.StoreString(JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
