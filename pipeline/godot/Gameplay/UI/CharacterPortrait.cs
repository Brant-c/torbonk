using Godot;
using System.Linq;

namespace SwtorSurvivors.UI;

/// <summary>Portrait of the actual imported character, rendered once in an isolated Godot world.</summary>
public partial class CharacterPortrait : Control
{
    private TextureRect _face=null!;
    public bool Mirrored { get; set; }
    public bool FaceVisible { get => _face.Visible; set => _face.Visible=value; }
    public override void _Ready()
    {
        // Native unit chrome is drawn by CombatHud; this control fills only its aperture.
        CustomMinimumSize=new Vector2(78,78);Size=CustomMinimumSize;
        var viewport=new SubViewport { Size=new Vector2I(256,256), OwnWorld3D=true,
            RenderTargetUpdateMode=SubViewport.UpdateMode.Once,
            Msaa3D=Viewport.Msaa.Msaa4X, TransparentBg=false };
        AddChild(viewport);
        var scene=new Node3D();viewport.AddChild(scene);
        scene.AddChild(new WorldEnvironment { Environment=new Godot.Environment {
            BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("071724"),
            AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new Color("b8d3ec"),AmbientLightEnergy=0.75f } });
        var model=GD.Load<PackedScene>("res://Assets/sorcerer.glb").Instantiate<Node3D>();scene.AddChild(model);
        var animator=model.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().Single();
        animator.Play("Idle");animator.Advance(0);animator.Pause();
        var skeleton=model.FindChildren("*","Skeleton3D",true,false).OfType<Skeleton3D>().Single();
        int head=skeleton.FindBone("Head");
        var focus=head>=0 ? skeleton.GlobalTransform*skeleton.GetBoneGlobalPose(head).Origin : new Vector3(0,0.16f,0);
        focus+=Vector3.Up*0.006f;
        var camera=new Camera3D { Projection=Camera3D.ProjectionType.Orthogonal,Size=0.058f,Near=0.002f,Far=1,Current=true };
        scene.AddChild(camera);camera.Position=focus+new Vector3(0.02f,0.001f,-0.14f);camera.LookAt(focus);
        var light=new DirectionalLight3D { RotationDegrees=new Vector3(-20,155,0),LightEnergy=1.3f,LightColor=new Color("ffdebd") };
        scene.AddChild(light);
        _face=new TextureRect { ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=viewport.GetTexture(),Size=Size,
            MouseFilter=MouseFilterEnum.Ignore,FlipH=Mirrored };
        // Clip only the rendered head; the surrounding contour is original SWTOR artwork.
        _face.Material=new ShaderMaterial {Shader=new Shader {Code="shader_type canvas_item; void fragment(){ vec2 p=(UV-vec2(0.5))*2.0; float a=1.0-smoothstep(0.97,1.0,length(p)); COLOR=texture(TEXTURE,UV)*COLOR; COLOR.a*=a; }"}};
        AddChild(_face);
    }
    public override GodotObject _MakeCustomTooltip(string forText) => new SwtorTooltip(forText);
}
