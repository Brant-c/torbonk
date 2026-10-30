using Godot;
using System.Linq;

namespace SwtorSurvivors.Frontend;

/// <summary>Presentation-only imported model. Never instantiates a gameplay actor.</summary>
public partial class OperationModelPreview : Control
{
    private SubViewport _viewport = null!;
    private Node3D _world = null!, _model = null!;
    private bool _dragging;
    public bool HasModel { get; private set; }
    public string CurrentModelPath {get;private set;}="";
    private Camera3D? _camera;
    public override void _Ready()
    {
        _viewport = new SubViewport { Size = new Vector2I(1400,500), OwnWorld3D = true,
            TransparentBg = true, RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled,
            Msaa3D = Viewport.Msaa.Msaa2X };
        AddChild(_viewport);
        _world = new Node3D(); _viewport.AddChild(_world);
        _world.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0,0,0,0),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("b8d3ec"), AmbientLightEnergy = .65f } });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-30,-35,0), LightColor = new Color("ffe4cc"), LightEnergy = 1.5f });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-15,145,0), LightColor = new Color("70bdeb"), LightEnergy = .8f });
        var render = new TextureRect { Texture = _viewport.GetTexture(), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(render); render.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        VisibilityChanged += () => { _dragging=false; UpdateRendering(); };
        MouseExited += () => _dragging=false;
        GuiInput += e => {
            if(e is InputEventMouseButton {ButtonIndex:MouseButton.Left} b) { _dragging=b.Pressed; if(b.DoubleClick && HasModel)_model.Rotation=Vector3.Zero; }
            if(e is InputEventMouseMotion m && _dragging && HasModel) _model.RotateY(m.Relative.X*.008f);
        };
    }
    public void ShowModel(string path)
    {
        if(HasModel && CurrentModelPath==path)return;
        if(GodotObject.IsInstanceValid(_model))_model.Free();
        if(GodotObject.IsInstanceValid(_camera))_camera!.Free();
        HasModel=false;CurrentModelPath="";
        _model=GD.Load<PackedScene>(path).Instantiate<Node3D>(); _world.AddChild(_model);
        var animator=_model.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().FirstOrDefault();
        if(animator?.HasAnimation("Idle")==true) {
            animator.GetAnimation("Idle").LoopMode=Animation.LoopModeEnum.Linear;animator.Play("Idle");
        }
        bool first=true; Aabb bounds=default;
        foreach(var mesh in _model.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        { var box=mesh.GlobalTransform*mesh.GetAabb();bounds=first?box:bounds.Merge(box);first=false; }
        if(first) { _model.QueueFree();return; }
        var center=bounds.GetCenter();
        // Rotate around the model's center, keeping a wide silhouette inside the panel.
        _model.Position-=center;
        float distance=Mathf.Max(bounds.Size.Y,bounds.Size.X/(1400f/500)) / (2*Mathf.Tan(Mathf.DegToRad(16))) * 1.25f;
        var camera=new Camera3D { Fov=32,Near=.001f,Far=1000,Current=true,
            Position=new Vector3(.36f,.13f,1).Normalized()*distance };
        _world.AddChild(camera);camera.LookAt(Vector3.Zero);
        _camera=camera;CurrentModelPath=path;
        HasModel=true;TooltipText="Drag to rotate · Double-click to reset";UpdateRendering();
    }
    private void UpdateRendering() => _viewport.RenderTargetUpdateMode=HasModel && IsVisibleInTree()
        ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
}
