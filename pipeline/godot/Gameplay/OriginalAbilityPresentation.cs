using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Bounded FXSpec adapter. Original pixels, sockets, clips and PCM; no combat side effects.</summary>
public partial class OriginalAbilityPresentation : Node3D, IAbilityPresentation
{
    public CharacterPresentation Actor { get; set; } = null!;
    public AbilityDefinition Definition { get; set; } = null!;
    public Func<float>? EffectiveRadius { get; set; }
    public StormAreaIndicator? AreaIndicator { get; private set; }
    private JsonDocument _fx=null!, _audio=null!;
    private readonly List<MeshInstance3D> _bolts=new();
    private readonly List<(Sprite3D node,double remaining)> _flashes=new();
    private readonly List<AudioStreamPlayer3D> _sounds=new();
    private readonly RandomNumberGenerator _random=new();
    private Combatant? _target;
    private Vector3 _center;
    private float _radius;
    private bool _active, _impact, _ending;
    private double _age, _redraw;
    private int _soundSlot;
    private ShaderMaterial _material=null!;
    private JsonElement _beam;

    public override void _Ready()
    {
        _fx=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Abilities/fx.json"));
        _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Abilities/audio.json"));
        _random.Randomize();
        var list=_fx.RootElement.GetProperty(Definition.SourceFx).GetProperty("_fxLightningList");
        _beam=list[0];
        string texture=Text(_beam,"_fxTextureName");
        string stem=System.IO.Path.GetFileNameWithoutExtension(texture.Replace('\\','/')).ToLowerInvariant();
        _material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Lightning.gdshader")};
        _material.SetShaderParameter("source_texture",GD.Load<Texture2D>($"res://Assets/Abilities/{stem}.png"));
        _material.SetShaderParameter("hue_purple_key",0.3f);_material.SetShaderParameter("hue_white_key",0.85f);
        for(int i=0;i<8;i++)
        {
            var sound=new AudioStreamPlayer3D {AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled,VolumeDb=-12,MaxDistance=6, Bus="B7Combat"};
            AddChild(sound);_sounds.Add(sound);
            AudioPresentation.Find(this)?.RegisterCombatVoice(sound);
        }
        ProcessPriority=100;SetProcess(false);
    }
    private static string Text(JsonElement e,string key,string fallback="") =>
        e.TryGetProperty(key,out var v) && v.ValueKind==JsonValueKind.String ? v.GetString()??fallback:fallback;
    private static float Number(JsonElement e,string key,float fallback) =>
        float.TryParse(Text(e,key),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var n)?n:fallback;
    private Vector3 Socket(string name)
    {
        int index=Actor.Skeleton.FindBone(name);
        return index>=0?(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(index)).Origin:Actor.GlobalPosition;
    }
    private Vector3 Aim()
    {
        if(GodotObject.IsInstanceValid(_target) && _target!.Alive)
        {
            var rig=_target.Presentation?.Skeleton;int index=rig?.FindBone("vfx_aim01")??-1;
            return index>=0?(rig!.GlobalTransform*rig.GetBoneGlobalPose(index)).Origin:_target.AimPosition;
        }
        return _center+Vector3.Up*0.1f;
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        Release();_target=target;_center=center;_radius=radius;
        if(!Actor.Animator.HasAnimation(Definition.StartClip))
        { GD.PushError("Missing original clip "+Definition.StartClip);return false; }
        _active=true;_age=0;_redraw=0;_ending=false;_impact=false;SetProcess(true);
        if(Definition.Id=="storm")
        {
            AreaIndicator=new StormAreaIndicator {Name="OriginalGreenStormArea"};AddChild(AreaIndicator);
            AreaIndicator.UpdateArea(_center,_radius);
        }
        PlayEvent(Definition.CastAudioEvent,false,Actor.GlobalPosition);
        return true;
    }
    public void Channel()
    {
        if(Definition.Id=="shock") PlayEvent("Play_ability_sithinquisitor_shock_cast",false,Actor.GlobalPosition);
        PlayEvent(Definition.LoopAudioEvent,true,_center);
    }
    public void Impact(Vector3 center)
    {
        _impact=true;
        PlayEvent(Definition.ImpactAudioEvent,false,Definition.Area==AbilityArea.GroundSphere?_center:Aim());
        if(Definition.Area==AbilityArea.SingleTarget) Flash(Aim(),"electric_orb",0.065f,0.24);
    }
    public void End(bool interrupted)
    {
        ClearAreaIndicator();
        _ending=true;_age=0;ClearBolts();
        foreach(var sound in _sounds) if(sound.Stream is AudioStreamWav w && w.LoopMode!=AudioStreamWav.LoopModeEnum.Disabled) sound.Stop();
    }
    public void Release()
    {
        ClearAreaIndicator();
        _active=false;ClearBolts();
        foreach(var (node,_) in _flashes) node.Free();_flashes.Clear();
        foreach(var sound in _sounds) sound.Stop();
        SetProcess(false);
    }
    public override void _Process(double delta)
    {
        if(!_active)return;
        if(!_ending && Definition.Id=="storm")
        {
            _radius=EffectiveRadius?.Invoke()??_radius;
            AreaIndicator?.UpdateArea(_center,_radius);
        }
        _age+=delta;_redraw-=delta;
        for(int i=_flashes.Count-1;i>=0;i--)
        {
            var (node,remaining)=_flashes[i];remaining-=delta;
            if(remaining<=0) {node.Free();_flashes.RemoveAt(i);}
            else {node.Transparency=Mathf.Clamp(1-(float)(remaining/0.3),0,1);_flashes[i]=(node,remaining);}
        }
        if(_ending)
        {
            foreach(var sound in _sounds) sound.VolumeDb=-12-(float)_age*60;
            return;
        }
        if(_redraw>0)return;
        _redraw=Definition.Area==AbilityArea.GroundSphere?0.12:0.06;ClearBolts();
        if(Definition.Area==AbilityArea.SingleTarget)
        {
            if(!_impact || _age>0.65)return;
            // April Shock explicitly uses vfx_hand_left and vfx_elbow_left.
            DrawBolt(Socket("vfx_hand_left"),Aim(),_beam);
            DrawBolt(Socket("vfx_elbow_left"),Socket("vfx_hand_left"),_beam);
        }
        else if(_age>=0.55)
        {
            // Particle EmitFXSpec places original lightning strikes around a fixed ground center.
            for(int i=0;i<3;i++)
            {
                float angle=_random.RandfRange(0,Mathf.Tau),r=Mathf.Sqrt(_random.Randf())*_radius;
                var end=_center+new Vector3(Mathf.Cos(angle)*r,0.012f,Mathf.Sin(angle)*r);
                DrawBolt(end+Vector3.Up*_random.RandfRange(0.35f,0.65f),end,_beam);
                Flash(end,"glow_basic_alpha",0.065f,0.18);
            }
            if(Actor.GestureOwner is AutomaticAbility ability && ability.Definition.Id==Definition.Id)
                DrawBolt(Socket("vfx_elbow_right"),Actor.GlobalPosition+Vector3.Right*0.07f,_beam);
        }
    }
    private void DrawBolt(Vector3 start,Vector3 end,JsonElement source)
    {
        var direction=(end-start).Normalized();
        if(direction.IsZeroApprox())return;
        var camera=GetViewport().GetCamera3D();
        var across=direction.Cross(camera==null?Vector3.Back:(camera.GlobalPosition-start).Normalized()).Normalized();
        if(across.IsZeroApprox())across=Vector3.Right;
        var up=direction.Cross(across).Normalized();
        float width=Mathf.Clamp(Number(source,"_fxLightningMaxWidth",0.04f)*0.1f,0.0015f,0.012f);
        float envelope=Mathf.Clamp(Number(source,"_fxEnvelopeMaxRadius",0.1f)*0.1f,0.003f,0.035f);
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip,_material);
        for(int i=0;i<=16;i++)
        {
            float t=i/16f;var point=start.Lerp(end,t)+(across*_random.RandfRange(-1,1)+up*_random.RandfRange(-1,1))*envelope*Mathf.Sin(t*Mathf.Pi);
            mesh.SurfaceSetColor(Colors.White);mesh.SurfaceSetUV(new Vector2(0,t*3+(float)_age));mesh.SurfaceAddVertex(ToLocal(point-across*width/2));
            mesh.SurfaceSetColor(Colors.White);mesh.SurfaceSetUV(new Vector2(1,t*3+(float)_age));mesh.SurfaceAddVertex(ToLocal(point+across*width/2));
        }
        mesh.SurfaceEnd();var node=new MeshInstance3D {Mesh=mesh,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(node);_bolts.Add(node);
    }
    private void Flash(Vector3 point,string texture,float size,double life)
    {
        var sprite=new Sprite3D {Texture=GD.Load<Texture2D>($"res://Assets/Abilities/{texture}.png"),
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Shaded=false,Modulate=new Color(0.6f,0.25f,1),NoDepthTest=false};
        sprite.PixelSize=size/sprite.Texture.GetWidth();AddChild(sprite);sprite.GlobalPosition=point;_flashes.Add((sprite,life));
    }
    private void ClearBolts() {foreach(var bolt in _bolts)bolt.Free();_bolts.Clear();}
    private void ClearAreaIndicator()
    {
        if(GodotObject.IsInstanceValid(AreaIndicator)) AreaIndicator!.Free();
        AreaIndicator=null;
    }
    private void PlayEvent(string name,bool loop,Vector3 point)
    {
        if(string.IsNullOrEmpty(name))return;
        if (AudioPresentation.Find(this) is { } mixer && !mixer.PermitCombat(name)) return;
        var root=_audio.RootElement;
        if(!root.GetProperty("events").TryGetProperty(name,out var id) ||
            !root.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media").EnumerateArray().ToArray();if(media.Length==0)return;
        // One bounded variation per event. Complete Wwise layered graph is retained as evidence.
        int mid=media[_random.RandiRange(0,media.Length-1)].GetInt32();
        var stream=(AudioStreamWav)GD.Load<AudioStreamWav>($"res://Assets/Abilities/Audio/{mid}.wav").Duplicate();
        if(loop) {stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);}
        var sound=_sounds[_soundSlot++%_sounds.Count];sound.Stop();sound.Stream=stream;sound.VolumeDb=-12;sound.GlobalPosition=point;sound.Play();
    }
    public override void _ExitTree() {Release();_fx?.Dispose();_audio?.Dispose();}
}
