using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Brief real-physics jump/animation integration review; no historic regression suite.</summary>
public partial class JumpAnimationSmoke : Node
{
    private readonly List<string> _checks = new();
    private void Check(string name, bool pass)
    { if (!pass) throw new InvalidOperationException(name); _checks.Add(name); GD.Print("JUMP: " + name); }
    private async Task Frames(int count = 2)
    { for (int i=0; i<count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Until(Func<bool> ready)
    {
        for (int i=0; i<150 && !ready(); i++) await Frames(1);
        if (!ready()) throw new TimeoutException("Jump animation transition");
    }
    private void Space()
    {
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode=Key.Space, Keycode=Key.Space, Pressed=true }, true);
        GetViewport().PushInput(new InputEventKey { PhysicalKeycode=Key.Space, Keycode=Key.Space, Pressed=false }, true);
    }

    public async void Run(GameWorld world)
    {
        ProcessMode = ProcessModeEnum.Always;
        string? failure = null;
        try
        {
            await Frames(3);
            world.Waves!.SetPhysicsProcess(false); world.Spawner!.Running=false;
            world.Abilities.SetPhysicsProcess(false); world.Abilities.Abort();
            var player=world.Player; var actor=player.Presentation;
            player.TestInput=Vector2.Zero;
            await Until(()=>player.IsOnFloor()); await Frames(4);
            Check("five original single-saber jump clips are loaded", new[]{"Takeoff","Airborne","Falling","LandIdle","LandRun"}
                .All(name=>actor.Animator.HasAnimation("Jump/"+name)));
            Check("only airborne and falling clips loop", actor.Animator.GetAnimation("Jump/Airborne").LoopMode==Animation.LoopModeEnum.Linear
                && actor.Animator.GetAnimation("Jump/Falling").LoopMode==Animation.LoopModeEnum.Linear
                && new[]{"Takeoff","LandIdle","LandRun"}.All(name=>actor.Animator.GetAnimation("Jump/"+name).LoopMode==Animation.LoopModeEnum.None));
            Check("jump tracks bind to the existing skeleton without root motion", actor.Animator.RootMotionTrack.ToString()==""
                && Enumerable.Range(0,actor.Animator.GetAnimation("Jump/Takeoff").GetTrackCount()).All(i=>
                    actor.Skeleton.FindBone(actor.Animator.GetAnimation("Jump/Takeoff").TrackGetPath(i).GetConcatenatedSubNames())>=0));
            // One closer camera review frame; the gameplay/default camera tuning is not changed.
            world.Rig.Distance=0.35f; world.Rig.Height=0.25f; world.Rig.Snap();
            int knee=actor.Skeleton.FindBone("LeftKnee"); var idleKnee=actor.Skeleton.GetBonePoseRotation(knee);
            float floor=player.GlobalPosition.Y;
            Space(); await Until(()=>actor.JumpPhase=="Takeoff");
            Check("real Space takeoff starts the source launch clip", !player.IsOnFloor() && actor.JumpClip=="Jump/Takeoff");
            await Until(()=>actor.JumpPhase=="Airborne"); await Frames(2);
            Check("ascent blends into the source airborne pose", player.GlobalPosition.Y>floor && actor.JumpWeight>0.9f
                && actor.JumpClip=="Jump/Airborne");
            Check("original jump actually articulates the leg bones", actor.Skeleton.GetBonePoseRotation(knee).AngleTo(idleKnee)>0.02f);
            await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
            string review=ProjectSettings.GlobalizePath("res://../generated/b9-jump"); DirAccess.MakeDirRecursiveAbsolute(review);
            GetViewport().GetTexture().GetImage().SavePng(review+"/airborne-review.png");
            GetTree().Paused=true;
            var pausedAt=player.GlobalPosition; double clipAt=actor.JumpClipSeconds;
            await Frames(4);
            Check("pausing freezes both the jump body and its animation", player.GlobalPosition==pausedAt && actor.JumpClipSeconds==clipAt);
            GetTree().Paused=false;
            await Until(()=>actor.JumpPhase=="Falling");
            Check("descent selects the source falling pose", player.Velocity.Y<=0 && actor.JumpClip=="Jump/Falling");
            await Until(()=>actor.JumpPhase=="LandIdle");
            Check("actual floor contact selects standing landing", player.IsOnFloor() && actor.JumpClip=="Jump/LandIdle");
            await Until(()=>actor.JumpPhase==""); await Frames(5);
            Check("landing returns smoothly to idle", actor.JumpWeight<0.01f && actor.Locomotion=="Idle");

            player.TestInput=new Vector2(0,-1); await Frames(6); Space();
            await Until(()=>actor.JumpPhase=="Takeoff");
            var movingStart=player.GlobalPosition;
            await Until(()=>actor.JumpPhase=="LandRun");
            Check("moving jump preserves travel and uses running landing", player.IsOnFloor() && actor.JumpClip=="Jump/LandRun"
                && new Vector2(player.GlobalPosition.X-movingStart.X,player.GlobalPosition.Z-movingStart.Z).Length()>0.1f);
            await Until(()=>actor.JumpPhase=="");
            player.TestInput=Vector2.Zero; await Frames(20);

            object owner=new(); int damageEvents=world.Combat.DamageEvents;
            Check("existing Sorcerer gesture can still begin", actor.TryBeginGesture(owner,"B5/StormLoop"));
            Space(); await Until(()=>actor.JumpPhase=="Airborne"); await Frames(2);
            Check("jump legs remain underneath existing upper-body casting layers", actor.GestureOwner==owner && actor.JumpWeight>0.9f
                && (double)actor.Tree.Get("parameters/StandingCast/blend_amount")<0.01
                && (double)actor.Tree.Get("parameters/Upper/blend_amount")>0.5);
            Check("jump presentation creates no ability damage", world.Combat.DamageEvents==damageEvents);
            actor.ReleaseGesture(owner);
            world.PlayerActor.Health.ApplyDamage(10000); await Frames(2);
            Check("death supersedes and clears jump playback", actor.JumpPhase=="" && actor.JumpWeight==0 && actor.State=="Death");
            world.ResetCombatWorld(); await Frames(3);
            Check("restart resets jump playback and returns to the existing locomotion", actor.JumpPhase=="" && actor.JumpWeight==0 && world.PlayerActor.Alive);
        }
        catch(Exception exception) { failure=exception.ToString(); GD.PushError(failure); }
        GetTree().Paused=false;
        using var receipt=Godot.FileAccess.Open("res://../manifests/b9-jump-smoke.json",Godot.FileAccess.ModeFlags.Write);
        receipt.StoreString(JsonSerializer.Serialize(new {passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
