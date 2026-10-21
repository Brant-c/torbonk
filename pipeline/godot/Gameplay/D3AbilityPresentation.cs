using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;
/// <summary>Engine-specific adapter using recovered April artwork and PCM. No combat authority: every effect is finite and bounded.</summary>
public partial class D3AbilityPresentation : Node3D,IAbilityPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    private sealed class Fx { public Node3D Node=null!;public double Age,Life;public Action<Fx>? Update; }
    private readonly List<Fx> _effects=new();
    private readonly List<(double at,Action run)> _delayed=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private static ShaderMaterial? _additive;
    private readonly Dictionary<string,ShaderMaterial> _materials=new();
    private readonly RandomNumberGenerator _random=new();
    private float _radius;
    private double _flame,_flameSpawn;
    private const int MaxEffects=48;
    public int EffectCount=>_effects.Count;
    public bool AdditiveBound{get;private set;}
    public bool Emitting=>_flame>0;
    // Voices: 0 cast, 1 main-hand, 2 off-hand, 3 impact, 4 spare. Allocated once; never per attack.
    public override void _Ready()
    {
        _random.Seed=(ulong)(Definition?.Id.GetHashCode()??1);
        for(int i=0;i<5;i++)
        {
            var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-18,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
            AddChild(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);_voices.Add(voice);
        }
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        _radius=radius;Play(Definition.CastAudio,Actor.GlobalPosition,0);
        if(Definition.WeaponAudio!="")Play(Definition.WeaponAudio,Actor.GlobalPosition,1);
        if(Definition.OffhandAudio!="")_delayed.Add((.3,()=>Play(Definition.OffhandAudio,Actor.GlobalPosition,2)));
        if(Definition.Effect!=AbilityEffect.Damage)Pulse(Actor.GlobalPosition+Vector3.Up*.06f);
        return Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel(){ if(Definition.PresentationKind=="flame"){_flame=Math.Max(.1,Definition.ChannelSeconds);_flameSpawn=0;} }
    public void Impact(Vector3 center)
    {
        if(Definition.Effect!=AbilityEffect.Damage)return;
        bool radial=Definition.Area==AbilityArea.CasterRadial;
        var at=radial?Actor.GlobalPosition+Vector3.Up*.06f:center;
        Play(Definition.ImpactAudio,at,3);
        switch(Definition.PresentationKind)
        {
            case "slam": Ring(at,Definition.ImpactTexture,_radius*2.2f);break;
            case "flame": break; // Flames come from the emitter, never a central billboard.
            case "saber": break; // The real blades are the effect; audio only.
            case "rifle": MuzzleFlash();Bolt(MuzzleWorld(),center,.012f,.14);break;
            case "bracer": MuzzleFlash();Projectile(MuzzleWorld(),center);break;
            default: if(Definition.ImpactTexture!="")Burst(at,Definition.ImpactTexture,Math.Max(.08f,_radius*2));break;
        }
    }
    private Vector3 MuzzleWorld()
    {
        int bone=Actor.Skeleton.FindBone(Definition.MuzzleBone);
        return bone>=0?(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(bone)).Origin:Actor.GlobalPosition+Vector3.Up*.12f;
    }
    private ShaderMaterial Material(string texture,bool billboard=true)
    {
        string key=texture+(billboard?"|b":"|f");
        if(_materials.TryGetValue(key,out var m))return m;
        _additive??=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/D3Particle.gdshader")};
        m=(ShaderMaterial)_additive.Duplicate();m.SetShaderParameter("source_texture",GD.Load<Texture2D>(texture));m.SetShaderParameter("billboard",billboard);
        AdditiveBound=true;return _materials[key]=m;
    }
    private Fx Add(Node3D node,double life,Action<Fx>? update=null)
    {
        // Bounded: the oldest effect yields before the budget is exceeded.
        if(_effects.Count>=MaxEffects){var old=_effects[0];if(GodotObject.IsInstanceValid(old.Node))old.Node.QueueFree();_effects.RemoveAt(0);}
        AddChild(node);var fx=new Fx {Node=node,Life=life,Update=update};_effects.Add(fx);return fx;
    }
    private Sprite3D Sprite(string texture,float size,int frames=1,bool billboard=true)
    {
        var tex=GD.Load<Texture2D>(texture);
        return new Sprite3D {Texture=tex,Hframes=frames,Vframes=frames,PixelSize=size/(tex.GetWidth()/frames),Shaded=false,MaterialOverride=Material(texture,billboard)};
    }
    private void Burst(Vector3 at,string texture,float size)
    {
        int frames=texture.Contains("2x2")||texture.Contains("flames")?2:1;
        var s=Sprite(texture,size,frames);s.Modulate=Definition.EffectTint;
        Add(s,.5,fx=>{
            var sp=(Sprite3D)fx.Node;float f=(float)(fx.Age/fx.Life);sp.Modulate=new Color(Definition.EffectTint,1-f);
            if(sp.Hframes>1)sp.Frame=Math.Min(sp.Hframes*sp.Vframes-1,(int)(f*sp.Hframes*sp.Vframes));
        });
        s.GlobalPosition=at;
    }
    private void Ring(Vector3 at,string texture,float size)
    {
        if(texture=="")return;
        var s=Sprite(texture,size,1,false);s.RotationDegrees=new Vector3(-90,0,0);s.Modulate=Definition.EffectTint;
        Add(s,.55,fx=>{float f=(float)(fx.Age/fx.Life);((Sprite3D)fx.Node).Scale=Vector3.One*(.5f+f);((Sprite3D)fx.Node).Modulate=new Color(Definition.EffectTint,1-f);});
        s.GlobalPosition=at;
    }
    private void Pulse(Vector3 at)
    {
        const string tex="res://Assets/D3/glow_basic_alpha.png";
        if(!ResourceLoader.Exists(tex))return;
        var s=Sprite(tex,.2f);s.Modulate=Definition.EffectTint;
        Add(s,.6,fx=>{float f=(float)(fx.Age/fx.Life);((Sprite3D)fx.Node).Scale=Vector3.One*(1+f);((Sprite3D)fx.Node).Modulate=new Color(Definition.EffectTint,1-f);});
        s.GlobalPosition=at;
    }
    private void MuzzleFlash()
    {
        const string tex="res://Assets/D3/bw_orange_flare02.png";
        var s=Sprite(tex,.07f);s.Modulate=Definition.EffectTint;
        Add(s,.1,fx=>((Sprite3D)fx.Node).Modulate=new Color(Definition.EffectTint,1-(float)(fx.Age/fx.Life)));
        s.GlobalPosition=MuzzleWorld();
    }
    private void Bolt(Vector3 from,Vector3 to,float width,double life)
    {
        float length=from.DistanceTo(to);if(length<.001f)return;
        var mesh=new MeshInstance3D {Mesh=new BoxMesh {Size=new Vector3(width,width,length)},CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,BlendMode=BaseMaterial3D.BlendModeEnum.Add,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,AlbedoColor=Definition.EffectTint}};
        Add(mesh,life,fx=>((StandardMaterial3D)((MeshInstance3D)fx.Node).MaterialOverride).AlbedoColor=new Color(Definition.EffectTint,1-(float)(fx.Age/fx.Life)));
        mesh.GlobalPosition=(from+to)/2;mesh.LookAt(to,Vector3.Up);
    }
    private void Projectile(Vector3 from,Vector3 to)
    {
        const string tex="res://Assets/D3/glow_basic_alpha.png";
        var s=Sprite(tex,.045f);s.Modulate=Definition.EffectTint;
        Add(s,.16,fx=>fx.Node.GlobalPosition=from.Lerp(to,Math.Min(1f,(float)(fx.Age/fx.Life))));
        s.GlobalPosition=from;
        _delayed.Add((.16,()=>Burst(to,tex,.1f)));
    }
    private void EmitFlame()
    {
        const string tex="res://Assets/D3/fx_2x2_flames_emmissive_bw_01_d.png";
        var s=Sprite(tex,.06f,2);s.Frame=_random.RandiRange(0,3);
        var velocity=new Vector3(_random.RandfRange(-.12f,.12f),_random.RandfRange(.03f,.1f),_random.RandfRange(-.12f,.12f));
        var origin=MuzzleWorld();
        Add(s,.45,fx=>{float f=(float)(fx.Age/fx.Life);fx.Node.GlobalPosition=origin+velocity*(float)fx.Age;
            ((Sprite3D)fx.Node).Scale=Vector3.One*(.7f+f*.8f);((Sprite3D)fx.Node).Modulate=new Color(1,.45f+.25f*(1-f),.1f,1-f);});
        s.GlobalPosition=origin;
    }
    private void Play(string path,Vector3 point,int slot)
    {
        if(path=="" || !ResourceLoader.Exists(path) || _voices.Count==0)return;
        var voice=_voices[Math.Clamp(slot,0,_voices.Count-1)];
        voice.Stop();voice.Stream=GD.Load<AudioStream>(path);voice.GlobalPosition=point;voice.Play();
    }
    public override void _Process(double delta)
    {
        for(int i=_delayed.Count-1;i>=0;i--){var d=_delayed[i];d.at-=delta;if(d.at<=0){_delayed.RemoveAt(i);d.run();}else _delayed[i]=d;}
        if(_flame>0)
        {
            _flame-=delta;_flameSpawn-=delta;
            while(_flameSpawn<=0){_flameSpawn+=.045;EmitFlame();}
        }
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var fx=_effects[i];fx.Age+=delta;
            if(fx.Age>=fx.Life || !GodotObject.IsInstanceValid(fx.Node)){if(GodotObject.IsInstanceValid(fx.Node))fx.Node.QueueFree();_effects.RemoveAt(i);continue;}
            fx.Update?.Invoke(fx);
        }
    }
    public void End(bool interrupted){ _flame=0;if(interrupted)Release(); }
    public void Complete(){ _flame=0; }
    public void Release()
    {
        _flame=0;_delayed.Clear();
        foreach(var e in _effects)if(GodotObject.IsInstanceValid(e.Node))e.Node.QueueFree();
        _effects.Clear();foreach(var v in _voices)if(GodotObject.IsInstanceValid(v))v.Stop();
    }
    public override void _ExitTree()=>Release();
}
