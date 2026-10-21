using Godot;
using System.Linq;

namespace SwtorSurvivors.UI;

/// <summary>Portrait of the actual imported character, rendered once in an isolated Godot world.</summary>
public partial class CharacterPortrait : Control
{
    private TextureRect _face=null!;
    private SubViewport _viewport=null!;
    private Node3D _scene=null!;
    private Node3D? _model;
    private Camera3D _camera=null!;
    private string _idleClip="Idle";
    public string ModelScenePath { get; private set; } = "";
    public bool ShowPlayerOnReady { get; set; } = true;
    public bool Mirrored { get; set; }
    public bool FaceVisible { get => _face.Visible; set => _face.Visible=value; }
    public override void _Ready()
    {
        // Native unit chrome is drawn by CombatHud; this control fills only its aperture.
        CustomMinimumSize=new Vector2(78,78);Size=CustomMinimumSize;
        _viewport=new SubViewport { Size=new Vector2I(256,256), OwnWorld3D=true,
            RenderTargetUpdateMode=SubViewport.UpdateMode.Disabled,
            Msaa3D=Viewport.Msaa.Msaa4X, TransparentBg=false };
        AddChild(_viewport);
        _scene=new Node3D();_viewport.AddChild(_scene);
        _scene.AddChild(new WorldEnvironment { Environment=new Godot.Environment {
            BackgroundMode=Godot.Environment.BGMode.Color,BackgroundColor=new Color("071724"),
            AmbientLightSource=Godot.Environment.AmbientSource.Color,AmbientLightColor=new Color("b8d3ec"),AmbientLightEnergy=0.75f } });
        _camera=new Camera3D { Projection=Camera3D.ProjectionType.Orthogonal,Size=0.058f,Near=0.002f,Far=2,Current=true };
        _scene.AddChild(_camera);
        var light=new DirectionalLight3D { RotationDegrees=new Vector3(-20,155,0),LightEnergy=1.3f,LightColor=new Color("ffdebd") };
        _scene.AddChild(light);
        _face=new TextureRect { ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=_viewport.GetTexture(),Size=Size,
            MouseFilter=MouseFilterEnum.Ignore,FlipH=Mirrored,Visible=false };
        // Clip only the rendered head; the surrounding contour is original SWTOR artwork.
        _face.Material=new ShaderMaterial {Shader=new Shader {Code="shader_type canvas_item; void fragment(){ vec2 p=(UV-vec2(0.5))*2.0; float a=1.0-smoothstep(0.97,1.0,length(p)); COLOR=texture(TEXTURE,UV)*COLOR; COLOR.a*=a; }"}};
        AddChild(_face);
        if(ShowPlayerOnReady) ShowModel("res://Assets/sorcerer.glb","Idle");
    }
    public void SetTarget(Combatant? target)
    {
        if(!GodotObject.IsInstanceValid(target) || !target!.Alive) { FaceVisible=false;return; }
        Node3D? presentation=(target as EnemyAgent)?.Visual ?? (Node3D?)target.Presentation;
        var original=presentation?.GetChildren().OfType<Node3D>().FirstOrDefault(node=>!string.IsNullOrEmpty(node.SceneFilePath));
        if(original==null) { FaceVisible=false;return; }
        ShowModel(original.SceneFilePath,(target as EnemyAgent)?.Visual?.IdleClip??"Idle");
    }
    private void ShowModel(string path,string idleClip)
    {
        if(ModelScenePath==path && _idleClip==idleClip) { FaceVisible=true;return; }
        if(_model!=null) { _scene.RemoveChild(_model);_model.QueueFree(); }
        ModelScenePath=path;_idleClip=idleClip;
        _model=GD.Load<PackedScene>(path).Instantiate<Node3D>();_scene.AddChild(_model);
        var animator=_model.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().FirstOrDefault();
        if(animator!=null && animator.HasAnimation(idleClip))
        { animator.Play(idleClip);animator.Advance(0);animator.Pause(); }
        var skeleton=_model.FindChildren("*","Skeleton3D",true,false).OfType<Skeleton3D>().FirstOrDefault();
        int head=skeleton?.FindBone("Head")??-1;
        var focus=head>=0 ? skeleton!.GlobalTransform*skeleton.GetBoneGlobalPose(head).Origin : new Vector3(0,0.16f,0);
        float size=0.058f;
        if(!ShowPlayerOnReady && head>=0)
        {
            int neck=skeleton!.GetBoneParent(head);
            if(neck>=0) size=Mathf.Clamp(skeleton.GetBoneGlobalPose(head).Origin.DistanceTo(skeleton.GetBoneGlobalPose(neck).Origin)*4,0.05f,0.18f);
        }
        focus+=Vector3.Up*(ShowPlayerOnReady?0.006f:size*0.1f);
        _camera.Size=size;
        _camera.Position=focus+(ShowPlayerOnReady?new Vector3(0.02f,0.001f,-0.14f):new Vector3(size*0.35f,size*0.02f,-size*3));
        _camera.LookAt(focus);
        _viewport.RenderTargetUpdateMode=SubViewport.UpdateMode.Once;
        FaceVisible=true;
    }
    public override GodotObject _MakeCustomTooltip(string forText) => new SwtorTooltip(forText);
}
