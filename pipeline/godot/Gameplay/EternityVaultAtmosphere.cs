using Godot;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Original raid sky; weather is read from authored FXSpec/PRT dependencies.</summary>
public partial class EternityVaultAtmosphere : Node3D
{
    private GameWorld _world = null!;
    private Node3D _sky = null!;
    public override void _Ready()
    {
        _world = GetParent<GameWorld>();
        var env = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(.28f,.34f,.42f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(.65f,.74f,.86f), AmbientLightEnergy = .3f,
            FogEnabled = false,
            TonemapMode = Godot.Environment.ToneMapper.Filmic };
        foreach(var old in _world.FindChildren("*","WorldEnvironment",true,false).OfType<WorldEnvironment>()) old.Environment = env;
        int lightIndex=0;
        foreach(var light in _world.GetChildren().OfType<DirectionalLight3D>()) {
            light.LightColor = new Color(.83f,.9f,1); light.LightEnergy=lightIndex++==0?.55f:.15f;
        }
        _sky = GD.Load<PackedScene>("res://Assets/Maps/eternity_vault/bel_raid_skyscene.glb").Instantiate<Node3D>();
        AddChild(_sky);
        foreach(var mesh in _sky.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()) {
            mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            for(int i=0;i<mesh.GetSurfaceOverrideMaterialCount();i++) {
                if(mesh.Mesh.SurfaceGetMaterial(i) is StandardMaterial3D original) {
                    var mat=(StandardMaterial3D)original.Duplicate(); mat.ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded;
                    mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled; mesh.SetSurfaceOverrideMaterial(i,mat);
                }
            }
        }
        AddChild(new EternityVaultWeather());
    }
    public override void _Process(double delta)
    {
        _sky.GlobalPosition=_world.Rig.Camera.GlobalPosition+new Vector3(0,-10.5998f,0);
    }
}
