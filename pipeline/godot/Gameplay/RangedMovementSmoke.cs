using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
namespace SwtorSurvivors;

public partial class RangedMovementSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();string? failure=null;
        void Check(string label,bool ok){if(!ok)throw new Exception(label);checks.Add(label);}
        try
        {
            w.Waves!.Stop();w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);
            w.Abilities.ResetCombat();
            var actor=w.Player.Presentation;actor.SetProcess(false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check("Ranged class enables directional running",actor.DirectionalLocomotionEnabled);
            foreach(string clip in w.Abilities.Abilities.SelectMany(a=>new[]{a.Definition.StartClip,a.Definition.LoopClip,a.Definition.EndClip}).Where(c=>!string.IsNullOrEmpty(c) && actor.Animator.HasAnimation(c)).Distinct())
            foreach(var direction in new[]{Vector3.Forward,Vector3.Back,Vector3.Left,Vector3.Right,new Vector3(1,0,-1).Normalized()})
            {
                actor.ResetPresentation();actor.Rotation=Vector3.Zero;object owner=new();
                Check(clip+" starts",actor.TryBeginGesture(owner,clip));
                actor.FacingTarget=actor.GlobalPosition+Vector3.Forward;
                int left=actor.Skeleton.FindBone("LeftAnkle"),right=actor.Skeleton.FindBone("RightAnkle");
                Vector3? previousLeft=null,previousRight=null;float travelLeft=0,travelRight=0;
                for(int frame=0;frame<64;frame++)
                {
                    actor.Present(direction*w.Player.MoveSpeed,.02);actor._Process(.02);
                    var l=actor.Skeleton.GetBoneGlobalPose(left).Origin;var r=actor.Skeleton.GetBoneGlobalPose(right).Origin;
                    if(frame>12 && previousLeft.HasValue){travelLeft+=l.DistanceTo(previousLeft.Value);travelRight+=r.DistanceTo(previousRight!.Value);}
                    previousLeft=l;previousRight=r;
                }
                Check(clip+" both feet cycle "+direction,travelLeft>.015f && travelRight>.015f);
                Check(clip+" releases running legs",(float)actor.Tree.Get("parameters/StandingCast/blend_amount")<.001f);
                Check(clip+" legs follow travel while torso faces target "+direction,actor.LocomotionDirection.DistanceTo(new Vector2(direction.X,direction.Z))<.03f && (-actor.GlobalBasis.Z).Dot(Vector3.Forward)>.98f);
                float rate=actor.LocomotionPlaybackRate;
                actor.Present(direction*w.Player.MoveSpeed*1.4f,.02);actor._Process(.02);
                Check("cadence follows speed",Math.Abs(actor.LocomotionPlaybackRate-Math.Min(6,rate*1.4f))<.01f);
                for(int i=0;i<16;i++){actor.Present(Vector3.Zero,.02);actor._Process(.02);}
                Check("stopping restores standing cast",(float)actor.Tree.Get("parameters/StandingCast/blend_amount")>.99f);
            }
        }
        catch(Exception e){failure=e.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/{w.RunConfiguration.AdvancedClassId}-movement-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
