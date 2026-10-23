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
    private double _shockTail;
    public int BoltCount => _bolts.Count;
    public int PlayingAudioVoices => _sounds.Count(s => s.Playing);
    public int LoopingAudioVoices => _sounds.Count(s => s.Playing && s.Stream is AudioStreamWav wav && wav.LoopMode != AudioStreamWav.LoopModeEnum.Disabled);
    public int AudioVoicesStarted { get; private set; }
    public OriginalStormLightning? StormLightning {get;private set;}
    private ShaderMaterial _material=null!;
    private JsonElement _beam;

    public override void _Ready()
    {
        _fx=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Abilities/fx.json"));
        _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Abilities/audio.json"));
        _random.Randomize();
        if(Definition.Id=="storm")
        {
            StormLightning=new OriginalStormLightning {Name="OriginalStormLightning",Actor=Actor,Source=_fx.RootElement};
            AddChild(StormLightning);
        }
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
        _active=true;_age=0;_redraw=0;_shockTail=0;_ending=false;_impact=false;SetProcess(true);
        if(Definition.Id=="storm")
        {
            StormLightning!.Begin();
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
        if(Definition.Id=="shock") { _shockTail=.24;_redraw=0; }
        PlayEvent(Definition.ImpactAudioEvent,false,Definition.Area==AbilityArea.GroundSphere?_center:Aim());
        if(Definition.Area==AbilityArea.SingleTarget) Flash(Aim(),"electric_orb",0.065f,0.24);
    }
    public void End(bool interrupted)
    {
        ClearAreaIndicator();
        _ending=true;_age=0;
        if(interrupted && !_impact) _shockTail=0;
        if(_shockTail<=0)ClearBolts();
        StormLightning?.Clear();
        StopLoops();
    }
    private void ClearVisuals()
    {
        ClearAreaIndicator();
        _active=false;ClearBolts();
        StormLightning?.Clear();
        foreach(var (node,_) in _flashes) node.Free();_flashes.Clear();
        SetProcess(false);
    }
    private void StopLoops()
    {
        foreach(var sound in _sounds) if(sound.Stream is AudioStreamWav w && w.LoopMode!=AudioStreamWav.LoopModeEnum.Disabled) sound.Stop();
    }
    public void Complete() { StopLoops(); if(_shockTail<=0)ClearVisuals(); }
    public void Release() { _shockTail=0;ClearVisuals(); foreach(var sound in _sounds) sound.Stop(); }
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
        if(Definition.Id=="shock" && _shockTail>0)
        {
            _shockTail-=delta;
            if(_shockTail<=0) { ClearBolts(); if(_ending && _flashes.Count==0)ClearVisuals(); }
            else if(_redraw<=0)
            {
                _redraw=.04;ClearBolts();
                DrawBolt(Socket("vfx_hand_left"),Aim(),_beam);
                DrawBolt(Socket("vfx_elbow_left"),Socket("vfx_hand_left"),_beam);
            }
        }
        if(_ending)
        {
            // Impacts finish independently of the short casting recovery. End already stopped channel loops.
            return;
        }
        if(StormLightning!=null)
        {
            StormLightning.Advance(delta,_center,_radius,Actor.GestureOwner is AutomaticAbility ability && ability.Definition.Id==Definition.Id);
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
        var mixer = AudioPresentation.Find(this);
        var mix = mixer?.AbilityMix(name);
        if (mix != null)
        {
            for (int i=0; i<mix.Layers.Length; i++)
            {
                var layer = mix.Layers[i];
                if (layer.Media.Length == 0 || !mixer!.PermitAbilityCombat(name + "/" + i, mix.Priority)) continue;
                PlayMedia(layer.Media[_random.RandiRange(0, layer.Media.Length-1)], loop, point, layer.Gain);
            }
            return;
        }
        if (mixer != null && !mixer.PermitCombat(name)) return;
        var root=_audio.RootElement;
        if(!root.GetProperty("events").TryGetProperty(name,out var id) ||
            !root.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media").EnumerateArray().ToArray();if(media.Length==0)return;
        // One bounded variation per event. Complete Wwise layered graph is retained as evidence.
        int mid=media[_random.RandiRange(0,media.Length-1)].GetInt32();
        PlayMedia(mid, loop, point, -12);
    }
    private void PlayMedia(int mid, bool loop, Vector3 point, float gain)
    {
        // Prefer an idle slot. An impact must never overwrite this ability's sustained loop.
        var sound = _sounds.FirstOrDefault(s => !s.Playing) ?? _sounds.FirstOrDefault(s =>
            s.Stream is AudioStreamWav w && w.LoopMode == AudioStreamWav.LoopModeEnum.Disabled);
        if (sound == null) return;
        var stream=(AudioStreamWav)GD.Load<AudioStreamWav>($"res://Assets/Abilities/Audio/{mid}.wav").Duplicate();
        if(loop) {stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);}
        sound.Stop();sound.Stream=stream;sound.VolumeDb=gain;sound.GlobalPosition=point;sound.Play(); AudioVoicesStarted++;
    }
    public override void _ExitTree() {Release();_fx?.Dispose();_audio?.Dispose();}
}
