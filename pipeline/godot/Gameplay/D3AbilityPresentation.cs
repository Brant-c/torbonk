using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;
/// <summary>Engine-specific one-shot adapter using recovered April artwork and PCM. No combat authority.</summary>
public partial class D3AbilityPresentation : Node3D,IAbilityPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    private readonly List<(Node3D node,double end)> _effects=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private float _radius;
    public override void _Ready()
    {
        for(int i=0;i<3;i++)
        {
            var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-18,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
            AddChild(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);_voices.Add(voice);
        }
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        _radius=radius;Play(Definition.CastAudio,Actor.GlobalPosition);
        return Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel(){}
    public void Impact(Vector3 center)
    {
        bool radial=Definition.Area==AbilityArea.CasterRadial;
        var at=radial?Actor.GlobalPosition+Vector3.Up*.06f:center;
        Play(Definition.ImpactAudio,at);
        if(Definition.ImpactTexture!="")
        {
            var texture=GD.Load<Texture2D>(Definition.ImpactTexture);
            var sprite=new Sprite3D {Name="AprilImpactArtwork",Texture=texture,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,
                Hframes=Definition.Id is "tracer" or "flamesweep"?2:1,Vframes=Definition.Id is "tracer" or "flamesweep"?2:1,
                PixelSize=Math.Max(.08f,_radius*2)/(texture.GetWidth()/(Definition.Id is "tracer" or "flamesweep"?2:1)),NoDepthTest=false,Shaded=false};
            AddChild(sprite);sprite.GlobalPosition=at;_effects.Add((sprite,.5));
        }
        if(Definition.Area is AbilityArea.SingleTarget or AbilityArea.TargetSplash)
        {
            int bone=Actor.Skeleton.FindBone("RightWeapon");
            var start=bone>=0?(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(bone)).Origin:Actor.GlobalPosition+Vector3.Up*.12f;
            var line=new ImmediateMesh();line.SurfaceBegin(Mesh.PrimitiveType.Lines);
            line.SurfaceAddVertex(ToLocal(start));line.SurfaceAddVertex(ToLocal(center));line.SurfaceEnd();
            var bolt=new MeshInstance3D {Mesh=line,MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=Definition.PeriodicDuration>0?new Color(.4f,1,.1f):new Color(1,.25f,.08f)}};
            AddChild(bolt);_effects.Add((bolt,.1));
        }
    }
    private void Play(string path,Vector3 point)
    {
        if(path=="" || !ResourceLoader.Exists(path))return;
        var voice=_voices.FirstOrDefault(v=>!v.Playing)??_voices[0];
        voice.Stop();voice.Stream=GD.Load<AudioStream>(path);voice.GlobalPosition=point;voice.Play();
    }
    public override void _Process(double delta)
    {
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var (n,t)=_effects[i];t-=delta;
            if(t<=0){n.QueueFree();_effects.RemoveAt(i);}else{if(n is Sprite3D s){s.Modulate=new Color(1,1,1,(float)Math.Min(1,t*3));if(s.Hframes>1)s.Frame=Math.Min(3,(int)((.5-t)*8));}_effects[i]=(n,t);}
        }
    }
    public void End(bool interrupted){if(interrupted)Release();}
    public void Complete(){}
    public void Release(){foreach(var e in _effects)if(GodotObject.IsInstanceValid(e.node))e.node.QueueFree();_effects.Clear();foreach(var v in _voices)if(GodotObject.IsInstanceValid(v))v.Stop();}
    public override void _ExitTree()=>Release();
}
