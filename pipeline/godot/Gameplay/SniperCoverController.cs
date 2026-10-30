using Godot;
using System;
using System.Linq;
using System.Text.Json;
namespace SwtorSurvivors;
public partial class SniperCoverController : Node3D
{
    public GameWorld World {get;set;}=null!;
    private Node3D _barrier=null!,_field=null!;
    private Vector3 _fieldScale;
    private double _deploy;
    private ShaderMaterial _fieldMaterial=null!;
    private AudioStreamPlayer3D _voice=null!;
    public bool Active {get;private set;}
    public bool BarrierVisible=>_barrier.Visible;
    public override void _Ready()
    {
        World.Player.Presentation.EnableSniperCover();
        _barrier=new Node3D {Name="OriginalPortableCover",Visible=false};AddChild(_barrier);
        _field=GD.Load<PackedScene>("res://Assets/Agent/sp_portableshield_field_01.glb").Instantiate<Node3D>();_field.Name="ShieldField";_barrier.AddChild(_field);_field.Scale=new Vector3(1.2f,1.1f,1.1f);_field.RotationDegrees=new Vector3(90,0,0);_field.Position=new Vector3(0,0,.025f);_fieldScale=_field.Scale;
        var hardware=new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>("res://Assets/Agent/fx_devices_01_d.png")};
        // Native geo_field has zero diffuse colour: its appearance comes from
        // proj_portable_cover's two texture layers, not the mesh's base grid.
        _fieldMaterial=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/SniperCoverField.gdshader")};
        _fieldMaterial.SetShaderParameter("hex_grid",GD.Load<Texture2D>("res://Assets/Agent/proj_hexgrid_tile.png"));
        _fieldMaterial.SetShaderParameter("glow_bar",GD.Load<Texture2D>("res://Assets/Agent/bw_projector_glowbar_blue_shield.png"));
        foreach(var mesh in _field.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())mesh.MaterialOverride=_fieldMaterial;
        Node3D Part(string asset,string name,Vector3 position,Vector3 rotation,Material material){
            var part=GD.Load<PackedScene>("res://Assets/Agent/"+asset+".glb").Instantiate<Node3D>();part.Name=name;_barrier.AddChild(part);
            // Exported GR2 vertices already use the same units as the FXSpec offsets.
            part.Scale=Vector3.One;part.Position=position;part.RotationDegrees=rotation;
            foreach(var mesh in part.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())mesh.MaterialOverride=material;
            return part;
        }
        // Keep literal source offsets. Convert source facing once at the assembly root.
        Part("sp_portableshield_device_main_01","DeviceMain",new Vector3(0,0,-.05f),Vector3.Zero,hardware);
        Part("sp_portableshield_device_left_01","DeviceLeft",new Vector3(.065f,.11f,0),new Vector3(90,90,0),hardware);
        Part("sp_portableshield_device_right_01","DeviceRight",new Vector3(-.065f,.11f,0),new Vector3(90,-90,0),hardware);
        var projection=new StandardMaterial3D {AlbedoTexture=GD.Load<Texture2D>("res://Assets/Agent/fx_sp_portableshield_projection_01_d.png"),AlbedoColor=new Color(.4f,.4f,.4f,.2f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
        Part("sp_portableshield_projection_01","FieldBeams1",new Vector3(0,.005f,-.035f),new Vector3(Mathf.RadToDeg(Mathf.Atan2(.001f,.002f)),0,0),projection);
        Part("sp_portableshield_projection_02","FieldBeams2",new Vector3(0,.005f,-.035f),new Vector3(Mathf.RadToDeg(Mathf.Atan2(.001f,.002f)),0,0),projection);
        _voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-17,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};AddChild(_voice);AudioPresentation.Find(this)?.RegisterCombatVoice(_voice);
    }
    private void Cue(string name)
    {
        using var audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Agent/audio.json"));var root=audio.RootElement;
        if(!root.GetProperty("events").TryGetProperty(name,out var id) || !root.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media");if(media.GetArrayLength()==0)return;
        _voice.Stream=GD.Load<AudioStream>("res://Assets/Agent/Audio/"+media[0].GetInt32()+".wav");_voice.Play();
    }
    public void SetCover(bool active,double delta)
    {
        if(active!=Active){Active=active;_deploy=0;World.Player.Presentation.SetSniperCover(active);_barrier.Visible=active;_voice.Stop();Cue(active?"Play_ability_spy_portablecover_fieldstartup":"Play_ability_spy_portablecover_fieldshutdown");}
        if(!active)return;
        _deploy+=delta;_field.Scale=_fieldScale*new Vector3(1,1,Mathf.Clamp((float)_deploy/.18f,.01f,1));
        _fieldMaterial.SetShaderParameter("effect_time",(float)_deploy);
        var actor=World.Player.Presentation;_barrier.GlobalTransform=new Transform3D(actor.GlobalBasis.Orthonormalized()*new Basis(Vector3.Up,Mathf.Pi),actor.GlobalPosition-actor.GlobalBasis.Z.Normalized()*.12f+Vector3.Up*.005f);
    }
    public override void _Process(double delta)
    {
        if(!World.Initialized)return;
        SetCover(World.PlayerActor.Alive && World.Player.IsOnFloor() && World.Player.ActualVelocity.Length()<.03f && World.Build.Pregame.AgentPositioned,delta);
    }
    public override void _ExitTree(){if(GodotObject.IsInstanceValid(World.Player.Presentation))World.Player.Presentation.SetSniperCover(false);}
}
