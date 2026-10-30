using Godot;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Original raid sky; weather is read from authored FXSpec/PRT dependencies.</summary>
public partial class EternityVaultAtmosphere : Node3D
{
    private GameWorld _world = null!;
    private Node3D _sky = null!;
    private Node3D _clouds = null!;
    public override void _Ready()
    {
        _world = GetParent<GameWorld>();
        RestoreDistantScenery();
        RestoreHorizon();
        RepairSnowOverlays();
        var env = new Godot.Environment {
            // 01_Surface selects exterior_ice_altermative, not the Area default.
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(.139f,.194f,.301f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(.286f,.376f,.594f), AmbientLightEnergy = .5f,
            FogEnabled = true, FogLightColor = new Color(.292f,.386f,.606f),
            FogDensity = .018f, FogSkyAffect = 0,
            TonemapMode = Godot.Environment.ToneMapper.Filmic };
        foreach(var old in _world.FindChildren("*","WorldEnvironment",true,false).OfType<WorldEnvironment>()) old.Environment = env;
        int lightIndex=0;
        foreach(var light in _world.GetChildren().OfType<DirectionalLight3D>()) {
            bool key=lightIndex++==0;
            light.LightColor = key ? Colors.White : new Color(.5f,.5f,.5f);
            light.LightEnergy=key?.65f:.15f;
            if(key) light.RotationDegrees=new Vector3(-38.66f,16.2602f,0);
        }
        _sky = GD.Load<PackedScene>("res://Assets/Maps/eternity_vault/bel_raid_skyscene.glb").Instantiate<Node3D>();
        AddChild(_sky);
        foreach(var mesh in _sky.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()) {
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            for(int i=0;i<mesh.GetSurfaceOverrideMaterialCount();i++) {
                if(mesh.Mesh.SurfaceGetMaterial(i) is StandardMaterial3D original) {
                    var mat=(StandardMaterial3D)original.Duplicate(); mat.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
                    mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
                    mat.DisableFog=true; mat.EmissionEnabled=false;
                    mesh.SetSurfaceOverrideMaterial(i,mat);
                }
            }
        }
        var clouds = new ShaderMaterial { Shader=GD.Load<Shader>("res://Assets/Maps/eternity_vault/RaidClouds.gdshader") };
        clouds.SetShaderParameter("clouds",GD.Load<Texture2D>("res://Assets/Maps/eternity_vault/raid-clouds.png"));
        clouds.SetShaderParameter("mask_map",GD.Load<Texture2D>("res://Assets/Maps/eternity_vault/raid-cloud-mask.png"));
        _clouds=new Node3D {Name="AuthoredRaidCloudLayers"};
        AddChild(_clouds);
        foreach(float offset in new[]{0f,100f}) {
            var dome=GD.Load<PackedScene>("res://Assets/Maps/eternity_vault/raid-cloud-dome.glb").Instantiate<Node3D>();
            _clouds.AddChild(dome); dome.Position=new Vector3(0,0,offset);dome.Scale=new Vector3(200,40,200);
            foreach(var mesh in dome.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()) {
                mesh.MaterialOverride=clouds;mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            }
        }
        AddChild(new EternityVaultWeather());
    }
    private void RestoreHorizon()
    {
        using var doc=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Maps/eternity_vault/surface-fidelity.json"));
        var record=doc.RootElement;
        Vector3 V(string key) {var a=record.GetProperty(key).EnumerateArray().Select(x=>x.GetSingle()).ToArray();return new(a[0],a[1],a[2]);}
        var r=V("rotation")*Mathf.Pi/180;
        var basis=new Basis(Vector3.Up,r.Y)*new Basis(Vector3.Right,r.X)*new Basis(Vector3.Back,r.Z);
        var horizon=GD.Load<PackedScene>(record.GetProperty("scene").GetString()!).Instantiate<Node3D>();
        horizon.Name="AuthoredSurfaceHorizon";AddChild(horizon);
        horizon.Transform=new Transform3D(basis.ScaledLocal(V("scale")),V("position"));
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Assets/Maps/eternity_vault/SourceHorizon.gdshader")};
        material.SetShaderParameter("source_texture",GD.Load<Texture2D>("res://Assets/Maps/eternity_vault/surface-horizon.png"));
        var values=record.GetProperty("material");
        foreach(var pair in new[]{("OpacityFadeMinDistance","minimum_distance"),("OpacityFadeMaxDistance","maximum_distance"),("OpacityFadeMinOpacity","minimum_opacity"),("OpacityFadeMaxOpacity","maximum_opacity")})
            material.SetShaderParameter(pair.Item2,float.Parse(values.GetProperty(pair.Item1).GetString()!,System.Globalization.CultureInfo.InvariantCulture));
        foreach(var mesh in horizon.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()) {
            mesh.MaterialOverride=material;mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        }
        GD.Print("EV_AUTHORED_HORIZON_RESTORED "+record.GetProperty("id").GetString());
    }
    private void RestoreDistantScenery()
    {
        using var doc=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Maps/eternity_vault/surface-distant-scenery.json"));
        foreach(var p in doc.RootElement.EnumerateArray()) {
            string id=p.GetProperty("id").GetString()!;
            if(_world.EnvironmentRoot.FindChild("Instance_"+id,true,false)!=null)continue;
            Vector3 V(string key) {var a=p.GetProperty(key).EnumerateArray().Select(x=>x.GetSingle()).ToArray();return new(a[0],a[1],a[2]);}
            var r=V("rotation")*Mathf.Pi/180;
            var basis=new Basis(Vector3.Up,r.Y)*new Basis(Vector3.Right,r.X)*new Basis(Vector3.Back,r.Z);
            var node=GD.Load<PackedScene>(p.GetProperty("model").GetString()!).Instantiate<Node3D>();
            node.Name="Instance_"+id; AddChild(node);
            node.Transform=new Transform3D(basis.ScaledLocal(V("scale")),V("position"));
        }
    }
    private void RepairSnowOverlays()
    {
        int repaired=0;
        foreach(var instance in _world.EnvironmentRoot.FindChildren("Instance_*","Node3D",true,false).OfType<Node3D>()) {
            if(!instance.HasMeta("source_model"))continue;
            string source=instance.GetMeta("source_model").AsString();
            if(!source.EndsWith("/hth_env_snow_decal.gr2") && !source.EndsWith("/hth_env_snow_plops.gr2"))continue;
            foreach(var mesh in instance.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()) {
            for(int i=0;i<mesh.Mesh.GetSurfaceCount();i++) {
                if(mesh.GetActiveMaterial(i) is not StandardMaterial3D original)continue;
                var mat=(StandardMaterial3D)original.Duplicate();
                // April material: AlphaMode=Test, UsesEmissive=False. BLEND caused sorting seams.
                mat.Transparency=BaseMaterial3D.TransparencyEnum.AlphaScissor;
                mat.AlphaScissorThreshold=.5f;
                mat.AlbedoTexture=GD.Load<Texture2D>("res://Assets/Maps/eternity_vault/snow-overlay-cutout.png");
                mat.EmissionEnabled=false;
                mat.RoughnessTexture=null;mat.Roughness=.9f;mat.MetallicSpecular=.12f;
                mesh.SetSurfaceOverrideMaterial(i,mat);
                repaired++;
            }
            }
        }
        GD.Print($"EV_SNOW_OVERLAYS_REPAIRED {repaired}");
    }
    public override void _Process(double delta)
    {
        _sky.GlobalPosition=_world.Rig.Camera.GlobalPosition+new Vector3(0,-10.5998f,0);
        _clouds.GlobalPosition=_world.Rig.Camera.GlobalPosition;
    }
}
