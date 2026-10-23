using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Bounded April FXSpec adapter: source ribbons, atlas particles, hand sockets and Wwise PCM only.</summary>
public partial class D2AbilityPresentation : Node3D,IAbilityPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    private readonly List<(Node3D node,double remaining,double total)> _effects=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private JsonDocument _audio=null!,_fx=null!;
    private ShaderMaterial _beam=null!;
    private float _width=.004f,_envelope=.02f,_radius;
    public int ArcsShown {get;private set;}
    public int AudioVoicesStarted {get;private set;}
    public override void _Ready()
    {
        _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/D2/audio.json"));
        _fx=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/D2/fx.json"));
        _beam=new ShaderMaterial {Shader=GD.Load<Shader>("res://Lightning.gdshader")};
        _beam.SetShaderParameter("source_texture",GD.Load<Texture2D>("res://Assets/D2/512_singlearcb_bw.png"));
        _beam.SetShaderParameter("hue_purple_key",.3f);_beam.SetShaderParameter("hue_white_key",.85f);
        if(Definition.Id=="chain")
        {
            var source=_fx.RootElement.GetProperty(Definition.SourceFx).GetProperty("_fxLightningList")[0];
            float Read(string key,float fallback)=>float.TryParse(source.GetProperty(key).GetString(),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float n)?n:fallback;
            _width=Math.Clamp(Read("_fxLightningMaxWidth",.4f)*.1f,.002f,.012f);
            _envelope=Math.Clamp(Read("_fxEnvelopeMaxRadius",.5f)*.1f,.003f,.035f);
        }
        for(int i=0;i<4;i++)
        {
            var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-16,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
            AddChild(voice);_voices.Add(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);
        }
    }
    public Vector3 Hand
    {
        get {int i=Actor.Skeleton.FindBone("vfx_hand_right");return i>=0?(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(i)).Origin:Actor.GlobalPosition+Vector3.Up*.1f;}
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        Release();_radius=radius;Play(Definition.CastAudioEvent,Actor.GlobalPosition);
        Flash(Hand,"electric_orb",.065f,.3,false);
        return Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel() { }
    public void Impact(Vector3 center)
    {
        Play(Definition.ImpactAudioEvent,center);
        if(Definition.Id=="overload")
        {
            Play("Play_ability_sithinquisitor_overload_swoosh",Actor.GlobalPosition);
            var texture=GD.Load<Texture2D>("res://Assets/D2/2x2_elec_blast_ring_alpha.png");
            var wave=new MeshInstance3D {Name="OriginalOverloadWave",Mesh=new PlaneMesh {Size=Vector2.One*_radius*2}};
            var mat=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency=BaseMaterial3D.TransparencyEnum.Alpha,DisableFog=true,BlendMode=BaseMaterial3D.BlendModeEnum.Add,
                CullMode=BaseMaterial3D.CullModeEnum.Disabled,AlbedoTexture=texture,AlbedoColor=new Color(.65f,.4f,1),Uv1Scale=new Vector3(.5f,.5f,1)};
            wave.MaterialOverride=mat;AddChild(wave);wave.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.015f;
            _effects.Add((wave,.55,.55));
        }
    }
    public void Arc(Vector3 start,Vector3 end)
    {
        var direction=(end-start).Normalized();if(direction.IsZeroApprox())return;
        var camera=GetViewport().GetCamera3D();var across=direction.Cross(camera==null?Vector3.Back:(camera.GlobalPosition-start).Normalized()).Normalized();
        if(across.IsZeroApprox())across=Vector3.Right;
        var up=direction.Cross(across).Normalized();
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip,_beam);
        for(int i=0;i<=16;i++)
        {
            float t=i/16f;
            var point=start.Lerp(end,t)+(across*Mathf.Sin(i*7.3f)+up*Mathf.Sin(i*13.1f))*_envelope*Mathf.Sin(t*Mathf.Pi);
            mesh.SurfaceSetColor(Colors.White);mesh.SurfaceSetUV(new Vector2(0,t*3));mesh.SurfaceAddVertex(ToLocal(point-across*_width/2));
            mesh.SurfaceSetColor(Colors.White);mesh.SurfaceSetUV(new Vector2(1,t*3));mesh.SurfaceAddVertex(ToLocal(point+across*_width/2));
        }
        mesh.SurfaceEnd();var bolt=new MeshInstance3D {Name="OriginalChainArc",Mesh=mesh,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(bolt);_effects.Add((bolt,.35,.35));ArcsShown++;
        Flash(end,"electric_orb",.08f,.3,false);
    }
    private void Flash(Vector3 position,string stem,float size,double life,bool atlas)
    {
        var texture=GD.Load<Texture2D>($"res://Assets/D2/{stem}.png");
        var sprite=new Sprite3D {Texture=texture,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,
            Hframes=atlas?2:1,Vframes=atlas?2:1,PixelSize=size/(texture.GetWidth()/(atlas?2:1)),
            MaterialOverride=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                DisableFog=true,BlendMode=BaseMaterial3D.BlendModeEnum.Add,AlbedoTexture=texture,AlbedoColor=new Color(.65f,.4f,1)}};
        AddChild(sprite);sprite.GlobalPosition=position;_effects.Add((sprite,life,life));
    }
    private void Play(string name,Vector3 point)
    {
        if(name=="" || !_audio.RootElement.GetProperty("events").TryGetProperty(name,out var id) ||
            !_audio.RootElement.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media").EnumerateArray().ToArray();if(media.Length==0)return;
        var voice=_voices.FirstOrDefault(v=>!v.Playing)??_voices[0];voice.Stop();
        voice.Stream=GD.Load<AudioStreamWav>($"res://Assets/D2/Audio/{media[0].GetInt32()}.wav");voice.GlobalPosition=point;voice.Play();AudioVoicesStarted++;
    }
    public override void _Process(double delta)
    {
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var (node,remaining,total)=_effects[i];remaining-=delta;
            if(remaining<=0) {node.QueueFree();_effects.RemoveAt(i);continue;}
            if(node is GeometryInstance3D geometry)geometry.Transparency=(float)(1-remaining/total);
            if(node.Name=="OriginalOverloadWave")node.Scale=Vector3.One*(float)(1.15-remaining/total);
            _effects[i]=(node,remaining,total);
        }
    }
    public void End(bool interrupted) { }
    public void Complete() { }
    public void Release() {foreach(var e in _effects)e.node.Free();_effects.Clear();foreach(var voice in _voices)voice.Stop();}
    public override void _ExitTree() {Release();_audio?.Dispose();_fx?.Dispose();}
}
