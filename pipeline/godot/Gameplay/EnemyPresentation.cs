using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SwtorSurvivors;

/// <summary>Rig-independent adapter. Clips are mapped by each scene, never inferred from player bones.</summary>
public partial class EnemyPresentation : Node3D
{
    [Export] public string IdleClip { get; set; } = "Idle";
    [Export] public string RunClip { get; set; } = "Run";
    /// <summary>World-space stride speed at playback 1, including model scale; zero preserves existing maps.</summary>
    [Export] public float LocomotionStrideSpeed { get; set; }
    [Export] public string AttackClip { get; set; } = "Attack";
    [Export] public string HitClip { get; set; } = "Hit";
    [Export] public string DeathClip { get; set; } = "Death";
    [Export] public float ForwardOffsetDegrees { get; set; }
    [Export] public string Weapon { get; set; } = "";
    [Export] public bool RedBlade {get;set;}
    [Export] public string WeaponScene { get; set; } = "";
    public bool ReuseLoopLibrary {get;set;}
    private static readonly Dictionary<string,AnimationLibrary> LoopLibraries=new();
    public AnimationPlayer Animator { get; private set; } = null!;
    public Skeleton3D Skeleton { get; private set; } = null!;
    public string State { get; private set; } = "";
    public bool Dead { get; private set; }
    private double _hold;
    private string _locomotion = "";
    private MeshInstance3D? _blaster;
    private double _flashRemaining;
    public void FireAt(Vector3 endpoint)
    {
        // One reusable bolt per shooter. Original weapon and firing clip remain on the rig.
        if(_blaster==null)
        {
            _blaster=new MeshInstance3D {TopLevel=true,Mesh=new CylinderMesh {Height=1,TopRadius=.003f,BottomRadius=.003f,RadialSegments=6},
                MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color(1,.18f,.035f),EmissionEnabled=true,Emission=new Color(1,.1f,.01f)},CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            AddChild(_blaster);
        }
        var socket=Skeleton.FindChildren("*","BoneAttachment3D",true,false).OfType<BoneAttachment3D>().FirstOrDefault();
        var start=socket?.GlobalPosition??GlobalPosition+Vector3.Up*.12f;
        var direction=endpoint-start;if(direction.Length()<.001f)return;
        _blaster.GlobalTransform=new Transform3D(new Basis(new Quaternion(Vector3.Up,direction.Normalized())).Scaled(new Vector3(1,direction.Length(),1)),(start+endpoint)*.5f);
        _blaster.Visible=true;_flashRemaining=.12;
        State="";Attack();Animator.Advance(.06);
    }
    public override void _Ready()
    {
        Animator = FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Single();
        Skeleton = FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
        foreach (var name in Animator.GetAnimationLibraryList())
        {
            var original=Animator.GetAnimationLibrary(name);
            string key=GetNode<Node3D>("Original").SceneFilePath+"/"+name;
            AnimationLibrary copy;
            if(!ReuseLoopLibrary || !LoopLibraries.TryGetValue(key,out copy!))
            {
                copy=(AnimationLibrary)original.Duplicate(true);
                if(ReuseLoopLibrary)LoopLibraries[key]=copy;
            }
            Animator.RemoveAnimationLibrary(name); Animator.AddAnimationLibrary(name, copy);
        }
        foreach (string clip in new[] { IdleClip, RunClip }) Animator.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;
        if (Weapon != "") EquipmentAttachment.Equip(Skeleton, Weapon, RedBlade);
        if (WeaponScene != "") EquipmentAttachment.Equip(Skeleton,WeaponScene,new Equipment.AttachmentDefinition(
            "RightWeapon","res://Assets/Enemies/weapon-socket.json",[0,0,0],[0,0,0],1,false));
        _locomotion = IdleClip; Play(IdleClip);
    }
    private void Play(string clip)
    {
        if(clip!=RunClip)Animator.SpeedScale=1;
        if (State == clip || !Animator.HasAnimation(clip)) return;
        State = clip; Animator.Play(clip, 0.09);
    }
    public void Present(Vector3 velocity, Vector3 target, double delta)
    {
        if (Dead) return;
        var flat = new Vector3(velocity.X, 0, velocity.Z);
        _locomotion = flat.Length() > 0.015f ? RunClip : IdleClip;
        var facing = flat.Length() > 0.015f ? flat : target - GlobalPosition;
        if (new Vector2(facing.X, facing.Z).Length() > 0.001f)
        {
            float angle = Mathf.Atan2(-facing.X, -facing.Z) + Mathf.DegToRad(ForwardOffsetDegrees);
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, angle, 1 - Mathf.Exp(-12 * (float)delta)), 0);
        }
        if (_hold <= 0) {
            Play(_locomotion);
            if(LocomotionStrideSpeed>0)Animator.SpeedScale=_locomotion==RunClip
                ? Mathf.Clamp(flat.Length()/LocomotionStrideSpeed,.01f,2.5f):1;
        }
    }
    public override void _Process(double delta)
    {
        if(_flashRemaining>0 && (_flashRemaining-=delta)<=0 && _blaster!=null)_blaster.Visible=false;
        if (Dead || _hold <= 0) return;
        _hold -= delta; if (_hold <= 0) Play(_locomotion);
    }
    public void CarbonizePose()
    {
        if(Dead)return;
        if(!Animator.HasAnimation("PTStatus/Carbonized"))
        {
            var scene=GD.Load<PackedScene>("res://Assets/Powertech/casts.glb").Instantiate<Node3D>();
            var source=scene.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().First();
            var clip=(Animation)source.GetAnimation("Carbonized").Duplicate(true);
            int matched=0;
            for(int i=clip.GetTrackCount()-1;i>=0;i--){var bone=clip.TrackGetPath(i).GetConcatenatedSubNames();if(Skeleton.FindBone(bone)<0)clip.RemoveTrack(i);else{clip.TrackSetPath(i,new NodePath(Animator.GetNode(Animator.RootNode).GetPathTo(Skeleton)+":"+bone));matched++;}}
            if(matched>10){var library=new AnimationLibrary();library.AddAnimation("Carbonized",clip);Animator.AddAnimationLibrary("PTStatus",library);}
            scene.Free();
        }
        if(Animator.HasAnimation("PTStatus/Carbonized")){State="";Play("PTStatus/Carbonized");Animator.Advance(.1);}
    }
    public void Attack() { if (!Dead) { Play(AttackClip); _hold = Animator.GetAnimation(AttackClip).Length; } }
    public void Reaction(bool death)
    {
        if (Dead) return;
        if (death) { Dead = true; Play(DeathClip); }
        else if (_hold <= 0) { Play(HitClip); _hold = Math.Min(0.25, Animator.GetAnimation(HitClip).Length); }
    }
}
