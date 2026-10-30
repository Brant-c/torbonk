using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Bounded presentation of the recovered April T2.2 textures, gestures and Wwise media.</summary>
public partial class SorcererRefinementPresentation : Node3D,IAbilityPresentation,ITargetImpactPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    private readonly List<(Node3D node,double life,double total,bool follow)> _effects=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private JsonDocument _audio=null!;
    private Combatant? _target;
    private double _buffLife,_trailIn;
    private Vector3 _lastPosition;
    public int AudioVoicesStarted {get;private set;}
    public int TargetImpacts {get;private set;}
    public bool BuffLoopPlaying=>_voices.Count>2 && _voices[2].Playing && _voices[2].Stream is AudioStreamWav w && w.LoopMode!=AudioStreamWav.LoopModeEnum.Disabled;
    public int EffectCount=>_effects.Count;
    public override void _Ready()
    {
        _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/SorcererRefinement/audio.json"));
        for(int i=0;i<3;i++)
        {
            var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-18,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
            AddChild(voice);_voices.Add(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);
        }
    }
    private void Play(string name,int slot,Vector3 at,bool loop=false)
    {
        if(name=="" || !_audio.RootElement.GetProperty("events").TryGetProperty(name,out var id) ||
            !_audio.RootElement.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media");if(media.GetArrayLength()==0)return;
        var voice=_voices[slot];voice.GlobalPosition=at;
        var stream=(AudioStreamWav)GD.Load<AudioStreamWav>($"res://Assets/SorcererRefinement/Audio/{media[0].GetInt32()}.wav").Duplicate();
        if(loop){stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);}
        voice.Stream=stream;
        voice.Play();AudioVoicesStarted++;
    }
    private void Flash(string stem,Vector3 at,float size,double life,int atlas=1,bool follow=false)
    {
        var tex=GD.Load<Texture2D>($"res://Assets/SorcererRefinement/{stem}.png");
        // These original black-backed glow/spark textures require additive composition.
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/SorcererParticle.gdshader")};
        material.SetShaderParameter("source_texture",tex);material.SetShaderParameter("brightness",1.5f);
        var sprite=new Sprite3D {Texture=tex,MaterialOverride=material,Hframes=atlas,Vframes=atlas,PixelSize=size/(tex.GetWidth()/atlas),
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Shaded=false,Modulate=new Color(.8f,.5f,1),
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(sprite);sprite.GlobalPosition=at;_effects.Add((sprite,life,life,follow));
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        Release();_target=target;Play(Definition.CastAudioEvent,0,Actor.GlobalPosition);
        if(Definition.Id=="lightningstrike")Flash("electric_rings_bw_02",Actor.GlobalPosition+Vector3.Up*.13f,.08f,.4,2);
        if(Definition.Id=="crushingdarkness")Flash("2x2_dark_energy_clouds_02",Actor.GlobalPosition+Vector3.Up*.13f,.12f,.7,2);
        return Definition.StartClip=="" || Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel(){}
    public void Arc(Vector3 from,Vector3 to)
    {
        var texture=GD.Load<Texture2D>("res://Assets/SorcererRefinement/512_singlearcb_bw.png");
        var material=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode=BaseMaterial3D.BlendModeEnum.Add,AlbedoTexture=texture,AlbedoColor=new Color(.7f,.3f,1),CullMode=BaseMaterial3D.CullModeEnum.Disabled};
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip,material);
        var across=(to-from).Cross(Vector3.Up).Normalized()*.009f;if(across.IsZeroApprox())across=Vector3.Right*.009f;
        for(int i=0;i<=10;i++)
        {
            float t=i/10f;var point=from.Lerp(to,t)+Vector3.Up*(Mathf.Sin(t*43)*.012f*Mathf.Sin(t*Mathf.Pi));
            mesh.SurfaceSetUV(new Vector2(t,0));mesh.SurfaceAddVertex(ToLocal(point-across));
            mesh.SurfaceSetUV(new Vector2(t,1));mesh.SurfaceAddVertex(ToLocal(point+across));
        }
        mesh.SurfaceEnd();var node=new MeshInstance3D {Mesh=mesh,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(node);_effects.Add((node,.28,.28,false));
    }
    public void Impact(Vector3 center)
    {
        Play(Definition.ImpactAudioEvent,1,center);
        switch(Definition.Id)
        {
            case "lightningstrike":
                break; // Contacts are dispatched after positive damage, for every victim.
            case "crushingdarkness":
                Flash("2x2_dark_energy_clouds_02",center,.22f,.8,2);break;
            case "polarityshift":
                _buffLife=6;Flash("bw_fx_2x2_sparknglow_01",center,.22f,6,2,true);
                Play("Play_ability_sithinquisitor_polaritycharge_buffloop",2,center,true);break;
            case "forcespeed":
                _buffLife=2;_lastPosition=Actor.GlobalPosition;
                Flash("glow_basic_nomips",center,.13f,.35,1,true);
                Play("Play_ability_sithinquisitor_forcespeed_channelloop",2,center,true);break;
        }
    }
    public void TargetImpact(Combatant target)
    {
        if(Definition.Id!="lightningstrike")return;
        TargetImpacts++;var at=target.AimPosition;
        Arc(Actor.GlobalPosition+Vector3.Up*.13f,at);Flash("lightning03",at,.19f,.35);Flash("electric_rings_bw_02",at,.14f,.35,2);
    }
    public override void _Process(double delta)
    {
        if(GetParent() is GameWorld world && _buffLife>0){
            bool active=world.PlayerActor.Alive && (Definition.Id=="polarityshift"?world.Build.Pregame.PolarityActive:world.Build.Buffed);
            if(!active){_buffLife=0;_voices[2].Stop();foreach(var e in _effects)if(GodotObject.IsInstanceValid(e.node))e.node.QueueFree();_effects.Clear();}
            else _voices[2].GlobalPosition=Actor.GlobalPosition;
        }
        if(_buffLife>0){
            if(Definition.Id=="forcespeed")
            {
                _trailIn-=delta;
                if(_trailIn<=0 && Actor.GlobalPosition.DistanceSquaredTo(_lastPosition)>.00001f)
                {Flash("glow_basic_nomips",Actor.GlobalPosition+Vector3.Up*.08f,.08f,.3);_trailIn=.06;_lastPosition=Actor.GlobalPosition;}
            }
            _buffLife-=delta;if(_buffLife<=0)_voices[2].Stop();}
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var e=_effects[i];e.life-=delta;
            if(e.life<=0){e.node.QueueFree();_effects.RemoveAt(i);continue;}
            if(e.follow)e.node.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.09f;
            if(e.node is Sprite3D sprite){sprite.Frame=(int)((e.total-e.life)*16)%(sprite.Hframes*sprite.Vframes);sprite.Modulate=new Color(.8f,.5f,1,(float)Math.Min(1,e.life/.25));}
            _effects[i]=e;
        }
    }
    public void End(bool interrupted){if(interrupted)Release();}
    public void Complete(){}
    public void Release(){foreach(var e in _effects)if(GodotObject.IsInstanceValid(e.node))e.node.QueueFree();_effects.Clear();foreach(var v in _voices)v.Stop();_buffLife=0;_target=null;}
    public override void _ExitTree(){Release();_audio?.Dispose();}
}
