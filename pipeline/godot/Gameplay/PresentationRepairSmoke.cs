using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class PresentationRepairSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string label,bool ok){if(!ok)throw new Exception(label);_checks.Add(label);}
    public async void Run(GameWorld w)
    {
        string? failure=null;string profile=w.RunConfiguration.AdvancedClassId;
        try
        {
            w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            w.Player.Presentation.SetProcess(false); // Sample animation and effects on the same fixture clock.
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            var target=new Combatant {Team=CombatTeam.Hostile,Name="PresentationFixture"};target.AddChild(new Health {Name="Health",Maximum=100000});w.AddChild(target);
            target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var a in w.Abilities.Abilities.Where(a=>a.Definition.Id!="lightning"))
            {
                w.Abilities.ResetCombat();w.Build.Reset();w.Periodic.Clear();w.Player.Presentation.ResetPresentation();w.PlayerActor.Health.ClearShield();target.Health.Restore();
                a.SetUnlocked(true);a.ResetCombat();var d=a.Definition;
                d.NearbyTriggerCount=1; // One isolated hostile exercises radial presentation admission.
                Check(d.Id+" clip exists",w.Player.Presentation.Animator.HasAnimation(d.StartClip));
                if(d.Effect==AbilityEffect.Heal)w.PlayerActor.Health.ApplyDamage(w.PlayerActor.Health.Maximum*.75f);
                var fx=a.Presentation as D3AbilityPresentation;fx?.SetProcess(false);
                Check(d.Id+" starts with gesture",a.TryBegin() && w.Player.Presentation.GestureOwner==a);
                int shotBefore=fx?.ShotsPresented??0;
                if(d.Id is "tracer" or "heatseeker" or "incendiarymissile")
                    Check(d.Id+" launches before damage",fx!.MissilesPresented>0 && a.DamageEvents==0 && fx.EffectCount>0);
                if(d.Id=="ambush")Check("Ambush charges with target reticle",fx!.ReticleVisible && a.DamageEvents==0);
                double elapsed=0;bool sawShock=false,sawChannelSound=false,captured=false;
                while(elapsed<d.WindupSeconds+a.ChannelSeconds+d.RecoverySeconds+.15)
                {
                    a.Advance(.02);w.Player.Presentation._Process(.02);fx?._Process(.02);
                    if(a.Presentation is OriginalAbilityPresentation original){original._Process(.02);sawShock|=original.BoltCount>0;}
                    sawChannelSound|=fx?.ChannelSoundPlaying??false;
                    elapsed+=.02;
                    if(OS.GetCmdlineUserArgs().Contains("--presentation-repair-captures"))
                    {
                        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                        double captureTime=d.Id=="ambush"?d.WindupSeconds*.5:
                            d.Id is "tracer" or "heatseeker" or "incendiarymissile"?d.WindupSeconds*.5:
                            a.ChannelSeconds>0?d.WindupSeconds+d.TickSeconds+.02:d.WindupSeconds+.02;
                        if(!captured && elapsed>=captureTime)
                        {
                            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/presentation-repairs-{profile}-{d.Id}.png"));captured=true;
                        }
                    }
                }
                if(d.Effect==AbilityEffect.Damage)Check(d.Id+" still deals damage",a.DamageEvents>0);
                if(d.Id=="shock")Check("Shock bolt survives instant cast completion",sawShock);
                if(d.Id=="unload")Check("Unload presents every paired shot",fx!.ShotsPresented-shotBefore==4);
                if(d.Id=="seriesofshots")Check("Series presents repeated shots",fx!.ShotsPresented-shotBefore>=3);
                if(d.Id=="flamethrower")Check("Flame Thrower sound starts and stops with channel",sawChannelSound && !fx!.ChannelSoundPlaying);
                if(d.Id=="creepingterror")Check("Creeping Terror has target duration effect",target.GetChildren().OfType<PresentationStatusVisual>().Any());
                if(d.Id is "shieldprobe" or "forceshroud")
                {
                    Check(d.Id+" distinct shield persists through recovery",fx!.ShieldVisible && fx.ShieldStyle!="energy");
                    w.PlayerActor.Health.ClearShield();fx._Process(.02);Check(d.Id+" expires with shield",!fx.ShieldVisible);
                }
                a.Abort();fx?._Process(.02);
                Check(d.Id+" abort releases gesture",w.Player.Presentation.GestureOwner!=a);
                if(fx!=null)Check(d.Id+" abort clears effects and channel sound",fx.EffectCount==0 && !fx.ChannelSoundPlaying && !fx.ReticleVisible);
                if(d.Id is "shock" or "unload")
                {
                    a.ResetCombat();w.Player.Presentation.ResetPresentation();target.Health.Restore();target.Health.ApplyDamage(target.Health.Current-1);
                    Check(d.Id+" lethal fixture starts",a.TryBegin());
                    for(double t=0;t<d.WindupSeconds+d.TickSeconds+.2 && a.DamageEvents==0;t+=.02)a.Advance(.02);
                    fx?._Process(.01);
                    if(a.Presentation is OriginalAbilityPresentation tail)tail._Process(.01);
                    Check(d.Id+" killing hit retains visual tail",!target.Alive && (fx?.EffectCount>0 || a.Presentation is OriginalAbilityPresentation shockTail && shockTail.BoltCount>0));
                    a.Abort();
                }
            }
            // Rejected gestures must not commit damage/cooldown/audio, then activate once free.
            var attack=w.Abilities.Abilities.First(a=>a.Definition.Effect==AbilityEffect.Damage && a.Definition.Id!="lightning");
            w.Player.Presentation.ResetPresentation();attack.SetUnlocked(true);attack.ResetCombat();target.Health.Restore();
            object owner=new();w.Player.Presentation.TryBeginGesture(owner,attack.Definition.StartClip,100);
            Check("occupied gesture defers activation",!attack.TryBegin() && attack.CooldownRemaining==0 && attack.DamageEvents==0);
            w.Player.Presentation.ResetPresentation();Check("deferred activation starts when free",attack.TryBegin());attack.Abort();
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/presentation-repairs-{profile}-smoke.json"),JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
