using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;
public partial class CharacterPresentation
{
    private readonly AnimationNodeAnimation _travelForward=new(){Animation="Run"},_travelBack=new(){Animation="Run"},
        _travelLeft=new(){Animation="Run"},_travelRight=new(){Animation="Run"};
    private bool _directionalLocomotion;
    private Vector3 _movementVelocity;
    private float _locomotionReferenceSpeed=.6f;
    private readonly float[] _nativeRunSpeeds=new float[4];
    public float[] NativeRunSpeeds=>(float[])_nativeRunSpeeds.Clone();
    public bool DirectionalLocomotionEnabled=>_directionalLocomotion;
    public Vector2 LocomotionDirection {get;private set;}=Vector2.Up;
    public float LocomotionPlaybackRate {get;private set;}=1;

    private void AddDirectionalLocomotion(AnimationNodeBlendTree graph)
    {
        var directions=new AnimationNodeBlendSpace2D {SyncMode=AnimationNodeBlendSpace2D.SyncModeEnum.Independent,AutoTriangles=false};
        directions.AddBlendPoint(_travelForward,Vector2.Up);
        directions.AddBlendPoint(_travelBack,Vector2.Down);
        directions.AddBlendPoint(_travelLeft,Vector2.Left);
        directions.AddBlendPoint(_travelRight,Vector2.Right);
        directions.AddTriangle(0,2,3);directions.AddTriangle(1,3,2);
        graph.AddNode("RunDirection",directions);
        graph.AddNode("RunSelection",new AnimationNodeBlend2 {Sync=true});
        graph.ConnectNode("RunSelection",0,"Run");graph.ConnectNode("RunSelection",1,"RunDirection");
        graph.AddNode("RunSpeed",new AnimationNodeTimeScale());graph.ConnectNode("RunSpeed",0,"RunSelection");
    }

    public void LoadDirectionalLocomotion(bool dualSaber,float referenceSpeed,bool pole=false,bool legsOnly=false)
    {
        // Ranged classes keep their equipped pistol poses above the shared directional legs.
        if(legsOnly){
            var selection=(AnimationNodeBlend2)((AnimationNodeBlendTree)Tree.TreeRoot).GetNode("RunSelection");
            selection.FilterEnabled=true;
            var root=Animator.GetNode(Animator.RootNode);
            for(int bone=0;bone<Skeleton.GetBoneCount();bone++)
                if(Skeleton.GetBoneName(bone)=="Root" || IsDescendant(bone,"Pelvis"))
                    selection.SetFilterPath(new NodePath(root.GetPathTo(Skeleton)+":"+Skeleton.GetBoneName(bone)),true);
        }
        LoadAbilityLibrary(pole?"res://Assets/AssassinLocomotion/casts.glb":"res://Assets/WarriorLocomotion/casts.glb","Travel",false);
        string forward="Travel/"+(pole?"PoleForward":dualSaber?"DualForward":"SaberForward"),back="Travel/"+(pole?"PoleBack":dualSaber?"DualBack":"SaberBack");
        // Matching cycle lengths keep the feet in phase when blending adjacent directions.
        double cycle=Animator.GetAnimation(forward).Length;
        foreach(string clip in new[]{forward,back}){
            var animation=Animator.GetAnimation(clip);double factor=cycle/animation.Length;
            for(int track=0;track<animation.GetTrackCount();track++)
                for(int key=0;key<animation.TrackGetKeyCount(track);key++)animation.TrackSetKeyTime(track,key,animation.TrackGetKeyTime(track,key)*factor);
            animation.Length=cycle;animation.LoopMode=Animation.LoopModeEnum.Linear;
        }
        // mv_run_left/right contain authored travel and turning rather than an in-place
        // loop. Derive lateral legs from the weapon run cycle instead. Rotating only
        // Pelvis keeps Root/RootSpine and the attack's upper-body facing untouched.
        var library=Animator.GetAnimationLibrary("Travel");
        library.AddAnimation("StrafeLeft",MakeLateralRun(Animator.GetAnimation(forward),Mathf.Pi/2));
        library.AddAnimation("StrafeRight",MakeLateralRun(Animator.GetAnimation(forward),-Mathf.Pi/2));
        _travelForward.Animation=forward;_travelBack.Animation=back;_travelLeft.Animation="Travel/StrafeLeft";_travelRight.Animation="Travel/StrafeRight";
        _locomotionReferenceSpeed=Math.Max(.01f,referenceSpeed);_directionalLocomotion=true;
        Tree.Set("parameters/RunSelection/blend_amount",1.0);
        var points=new[]{Vector2.Up,Vector2.Down,Vector2.Left,Vector2.Right};
        for(int i=0;i<points.Length;i++)_nativeRunSpeeds[i]=MeasureGroundStrideSpeed(points[i],cycle);
    }

    public void LoadRifleDirectionalLocomotion(float referenceSpeed)
    {
        string forward="Agent/RifleForward",back="Agent/RifleBack";
        double cycle=Animator.GetAnimation(forward).Length;
        foreach(string clip in new[]{forward,back}){
            var animation=Animator.GetAnimation(clip);double factor=cycle/animation.Length;
            for(int track=0;track<animation.GetTrackCount();track++)
                for(int key=0;key<animation.TrackGetKeyCount(track);key++)animation.TrackSetKeyTime(track,key,animation.TrackGetKeyTime(track,key)*factor);
            animation.Length=cycle;animation.LoopMode=Animation.LoopModeEnum.Linear;
        }
        var library=Animator.GetAnimationLibrary("Agent");
        Animator.GetAnimation("Agent/RifleAutoLoop").LoopMode=Animation.LoopModeEnum.Linear;
        Animator.GetAnimation("Agent/SniperSeries").LoopMode=Animation.LoopModeEnum.Linear;
        library.AddAnimation("StrafeLeft",MakeLateralRun(Animator.GetAnimation(forward),Mathf.Pi/2));
        library.AddAnimation("StrafeRight",MakeLateralRun(Animator.GetAnimation(forward),-Mathf.Pi/2));
        _travelForward.Animation=forward;_travelBack.Animation=back;_travelLeft.Animation="Agent/StrafeLeft";_travelRight.Animation="Agent/StrafeRight";
        _locomotionReferenceSpeed=Math.Max(.01f,referenceSpeed);_directionalLocomotion=true;
        Tree.Set("parameters/RunSelection/blend_amount",1.0);
        var points=new[]{Vector2.Up,Vector2.Down,Vector2.Left,Vector2.Right};
        for(int i=0;i<points.Length;i++)_nativeRunSpeeds[i]=MeasureGroundStrideSpeed(points[i],cycle);
    }

    private Animation MakeLateralRun(Animation source,float yaw)
    {
        int root=-1,pelvis=-1;
        for(int track=0;track<source.GetTrackCount();track++){
            if(source.TrackGetType(track)!=Animation.TrackType.Rotation3D)continue;
            string bone=source.TrackGetPath(track).GetConcatenatedSubNames();
            if(bone=="Root")root=track;if(bone=="Pelvis")pelvis=track;
        }
        if(root<0 || pelvis<0)throw new InvalidOperationException("Directional run requires Root and Pelvis rotation tracks");
        var result=(Animation)source.Duplicate(true);result.RemoveTrack(pelvis);
        int rotated=result.AddTrack(Animation.TrackType.Rotation3D);result.TrackSetPath(rotated,source.TrackGetPath(pelvis));
        var up=(Skeleton.GlobalBasis.Inverse()*GlobalBasis*Vector3.Up).Normalized();
        int samples=(int)Math.Ceiling(source.Length*60);
        for(int frame=0;frame<=samples;frame++){
            double time=Math.Min(source.Length,frame/60.0);
            var parent=new Basis(source.RotationTrackInterpolate(root,time));
            var turn=new Quaternion((parent.Inverse()*up).Normalized(),yaw);
            result.RotationTrackInsertKey(rotated,time,(turn*source.RotationTrackInterpolate(pelvis,time)).Normalized());
        }
        result.LoopMode=Animation.LoopModeEnum.Linear;return result;
    }

    private float MeasureGroundStrideSpeed(Vector2 direction,double cycle)
    {
        int[] feet={Skeleton.FindBone("LeftAnkle"),Skeleton.FindBone("RightAnkle")};
        if(feet.Any(f=>f<0))return _locomotionReferenceSpeed;
        string clip=direction==Vector2.Up?_travelForward.Animation:direction==Vector2.Down?_travelBack.Animation:direction==Vector2.Left?_travelLeft.Animation:_travelRight.Animation;
        var animation=Animator.GetAnimation(clip);
        var ancestors=new HashSet<int>();
        foreach(int foot in feet)for(int bone=foot;bone>=0;bone=Skeleton.GetBoneParent(bone))ancestors.Add(bone);
        var tracks=new List<(int track,int bone,Animation.TrackType type)>();
        for(int track=0;track<animation.GetTrackCount();track++){
            int bone=Skeleton.FindBone(animation.TrackGetPath(track).GetConcatenatedSubNames());
            if(ancestors.Contains(bone))tracks.Add((track,bone,animation.TrackGetType(track)));
        }
        var samples=new List<Vector3[]>();double step=cycle/120;
        for(int i=0;i<=120;i++){
            // Evaluate only foot ancestors from the recovered tracks. Never advance a live
            // cast or rely on deferred animation-tree caches during character initialization.
            var local=new Transform3D[Skeleton.GetBoneCount()];var world=new Transform3D[local.Length];var ready=new bool[local.Length];
            foreach(int bone in ancestors)local[bone]=Skeleton.GetBoneRest(bone);
            foreach(var track in tracks){
                var pose=local[track.bone];
                if(track.type==Animation.TrackType.Position3D)pose.Origin=animation.PositionTrackInterpolate(track.track,i*step);
                else if(track.type==Animation.TrackType.Rotation3D)pose.Basis=new Basis(animation.RotationTrackInterpolate(track.track,i*step)).Scaled(pose.Basis.Scale);
                else if(track.type==Animation.TrackType.Scale3D)pose.Basis=pose.Basis.Orthonormalized().Scaled(animation.ScaleTrackInterpolate(track.track,i*step));
                local[track.bone]=pose;
            }
            Transform3D Global(int bone){
                if(ready[bone])return world[bone];
                int parent=Skeleton.GetBoneParent(bone);world[bone]=parent<0?local[bone]:Global(parent)*local[bone];ready[bone]=true;return world[bone];
            }
            samples.Add(feet.Select(f=>Skeleton.GlobalBasis*Global(f).Origin).ToArray());
        }
        var speeds=new List<float>();
        for(int foot=0;foot<2;foot++){
            float min=samples.Min(p=>p[foot].Y),max=samples.Max(p=>p[foot].Y);
            for(int i=1;i<samples.Count;i++){
                if(samples[i][foot].Y>min+(max-min)*.25f)continue;
                var velocity=new Basis(Vector3.Up,Mathf.DegToRad(ForwardOffsetDegrees))*GlobalBasis.Inverse()*((samples[i][foot]-samples[i-1][foot])/(float)step);
                float speed=-new Vector2(velocity.X,velocity.Z).Dot(direction);
                if(speed>.02f && speed<5)speeds.Add(speed);
            }
        }
        if(speeds.Count<4)return _locomotionReferenceSpeed;
        speeds.Sort();return speeds[speeds.Count/2];
    }

    private void UpdateDirectionalLocomotion()
    {
        if(!_directionalLocomotion)return;
        // Keep attack facing. Express actual world movement in that facing's local axes.
        var local=new Basis(Vector3.Up,Mathf.DegToRad(ForwardOffsetDegrees))*GlobalBasis.Inverse()*_movementVelocity;
        if(new Vector2(local.X,local.Z).LengthSquared()>.000001f)LocomotionDirection=new Vector2(local.X,local.Z).Normalized();
        Tree.Set("parameters/RunDirection/blend_position",LocomotionDirection);
        float x=Math.Abs(LocomotionDirection.X),z=Math.Abs(LocomotionDirection.Y);
        float native=(x*_nativeRunSpeeds[LocomotionDirection.X<0?2:3]+z*_nativeRunSpeeds[LocomotionDirection.Y<0?0:1])/Math.Max(.001f,x+z);
        LocomotionPlaybackRate=Mathf.Clamp(_speed/Math.Max(.02f,native),.15f,6);
        Tree.Set("parameters/RunSpeed/scale",LocomotionPlaybackRate);
    }
}
