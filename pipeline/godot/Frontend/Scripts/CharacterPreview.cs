using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace SwtorSurvivors.Frontend;

/// <summary>Model registry comes from class data. Never substitutes a Sorcerer for another class.</summary>
public partial class CharacterPreview : Control
{
    private SubViewport _viewport = null!;
    private Node3D _world = null!, _rotation = null!;
    private Camera3D _camera = null!;
    private TextureRect _render = null!, _art = null!;
    private Label _caption = null!;
    private bool _dragging;
    private float _facing;
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop; ClipContents = true;
        _art = FrontendSkin.Art(FrontendSkin.Assets + "Recovered/cdx.advanced_classes.sith_sorcerer.png", Size);
        _art.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_art);
        _viewport = new SubViewport { Size = new Vector2I(720, 800), OwnWorld3D = true, TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled, Msaa3D = Viewport.Msaa.Msaa2X };
        AddChild(_viewport); _world = new Node3D(); _viewport.AddChild(_world);
        _world.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0,0,0,0),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("b8d3ec"), AmbientLightEnergy = .65f } });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-30,155,0), LightColor = new Color("ffe4cc"), LightEnergy = 1.5f });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-20,-30,0), LightColor = new Color("70bdeb"), LightEnergy = .8f });
        _rotation = new Node3D(); _world.AddChild(_rotation);
        _camera = new Camera3D { Position = new Vector3(0,1.05f,-3.8f), Fov = 34, Near = .01f, Far = 20, Current = true };
        _world.AddChild(_camera); _camera.LookAt(new Vector3(0,.98f,0));
        _render = new TextureRect { Texture = _viewport.GetTexture(), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore };
        _render.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_render);
        _caption = FrontendSkin.Label("", 17); _caption.Position = new Vector2(20, Size.Y-42); AddChild(_caption);
        GuiInput += OnInput;
        MouseExited += () => _dragging = false;
        Resized += () => { if (_caption != null) _caption.Position = new Vector2(20, Size.Y-42); };
    }
    public void ShowClass(string artwork, AdvancedClassDefinition? advanced = null)
    {
        foreach (var c in _rotation.GetChildren()) { _rotation.RemoveChild(c); c.QueueFree(); }
        _art.Texture = FrontendSkin.Texture(artwork);
        bool hasModel = advanced != null && !string.IsNullOrEmpty(advanced.PreviewScene)
            && ResourceLoader.Exists(advanced.PreviewScene);
        _art.Visible = !hasModel; _render.Visible = hasModel;
        _viewport.RenderTargetUpdateMode = hasModel ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
        _caption.Text = hasModel ? "Drag to rotate  •  Scroll to zoom  •  Double-click to reset" : "Original SWTOR class artwork  •  3D preview unavailable";
        _dragging = false;
        if (!hasModel) return;
        var placement = new Node3D(); _rotation.AddChild(placement);
        var model = GD.Load<PackedScene>(advanced!.PreviewScene).Instantiate<Node3D>(); placement.AddChild(model);
        var animator = model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().FirstOrDefault();
        if (animator != null && animator.HasAnimation(advanced.IdleAnimation))
        {
            // Imported resources are shared; duplicate the library before changing its loop flag.
            foreach (var key in animator.GetAnimationLibraryList())
            { var lib = (AnimationLibrary)animator.GetAnimationLibrary(key).Duplicate(true); animator.RemoveAnimationLibrary(key); animator.AddAnimationLibrary(key, lib); }
            animator.GetAnimation(advanced.IdleAnimation).LoopMode = Animation.LoopModeEnum.Linear;
            animator.Play(advanced.IdleAnimation); animator.Advance(0);
        }
        var skeleton = model.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().FirstOrDefault();
        float height = .18f;
        if (skeleton != null && skeleton.FindBone("Head") >= 0)
            height = (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(skeleton.FindBone("Head")).Origin).Y * 1.12f;
        placement.Scale = Vector3.One * (1.8f / Mathf.Max(.01f, height));
        _facing = advanced.FacingDegrees; ResetView();
    }
    public void ResetView() { _rotation.RotationDegrees = new Vector3(0,_facing,0); _camera.Fov = 34; }
    private void OnInput(InputEvent e)
    {
        if (!_render.Visible) return;
        if (e is InputEventMouseButton b)
        {
            if (b.ButtonIndex == MouseButton.Left) { _dragging = b.Pressed; if (b.DoubleClick) ResetView(); }
            if (b.Pressed && b.ButtonIndex == MouseButton.WheelUp) _camera.Fov = Mathf.Clamp(_camera.Fov-2,25,45);
            if (b.Pressed && b.ButtonIndex == MouseButton.WheelDown) _camera.Fov = Mathf.Clamp(_camera.Fov+2,25,45);
        }
        if (e is InputEventMouseMotion m && _dragging) _rotation.RotateY(-m.Relative.X * .009f);
    }
}
