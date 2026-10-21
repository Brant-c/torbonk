using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwtorSurvivors.Equipment;

namespace SwtorSurvivors.Frontend;

/// <summary>Model registry comes from class data. Never substitutes a Sorcerer for another class.</summary>
public partial class CharacterPreview : Control
{
    public CharacterAppearanceAssembly? Assembly { get; private set; }
    private SubViewport _viewport = null!;
    private Node3D _world = null!, _rotation = null!;
    private Camera3D _camera = null!;
    private TextureRect _render = null!, _art = null!;
    private bool _dragging;
    private float _facing;
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        _art = FrontendSkin.Art(FrontendSkin.Assets + "Recovered/cdx.advanced_classes.sith_sorcerer.png", Size);
        _art.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_art);
        _viewport = new SubViewport { Size = new Vector2I(720, 800), OwnWorld3D = true, World2D = new World2D(), CanvasCullMask = 0, TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled, Msaa3D = Viewport.Msaa.Msaa2X };
        AddChild(_viewport);
        RenderingServer.ViewportSetDisable2D(_viewport.GetViewportRid(),true);
        _world = new Node3D(); _viewport.AddChild(_world);
        _world.AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0,0,0,0),
            AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("b8d3ec"), AmbientLightEnergy = .65f } });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-30,155,0), LightColor = new Color("ffe4cc"), LightEnergy = 1.5f });
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-20,-30,0), LightColor = new Color("70bdeb"), LightEnergy = .8f });
        _rotation = new Node3D {Name="PreviewRotation"}; _world.AddChild(_rotation);
        _camera = new Camera3D { Position = new Vector3(0,1.12f,-4.25f), Fov = 34, Near = .01f, Far = 20, Current = true };
        _world.AddChild(_camera); _camera.LookAt(new Vector3(0,1.10f,0));
        _render = new TextureRect { Texture = _viewport.GetTexture(), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore };
        _render.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); AddChild(_render);
        GuiInput += OnInput;
        MouseExited += () => _dragging = false;
    }
    public void ShowClass(string artwork, AdvancedClassDefinition? advanced = null)
    {
        Assembly=null;
        foreach (var c in _rotation.GetChildren()) { _rotation.RemoveChild(c); c.QueueFree(); }
        _art.Texture = FrontendSkin.Texture(artwork);
        bool hasModel = advanced != null && !string.IsNullOrEmpty(advanced.PreviewScene)
            && ResourceLoader.Exists(advanced.PreviewScene);
        _art.Visible = !hasModel; _render.Visible = hasModel;
        _viewport.RenderTargetUpdateMode = hasModel ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
        TooltipText = hasModel ? "Drag to rotate · Scroll to zoom · Double-click to reset" : "";
        _dragging = false;
        if (!hasModel) return;
        var placement = new Node3D(); _rotation.AddChild(placement);
        var model = GD.Load<PackedScene>(advanced!.PreviewScene).Instantiate<Node3D>(); placement.AddChild(model);
        var profile=ClassProfile.Find(advanced.Id);
        var skeleton = model.FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().FirstOrDefault();
        var animator = model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().FirstOrDefault();
        if (animator != null && animator.HasAnimation(advanced.IdleAnimation))
        {
            // Imported resources are shared; duplicate the library before changing its loop flag.
            foreach (var key in animator.GetAnimationLibraryList())
            { var lib = (AnimationLibrary)animator.GetAnimationLibrary(key).Duplicate(true); animator.RemoveAnimationLibrary(key); animator.AddAnimationLibrary(key, lib); }
            string idle=ClassProfile.Find(advanced.Id)?.Idle=="" && skeleton!=null ? AddSaberStance(animator,skeleton) : advanced.IdleAnimation;
            animator.GetAnimation(idle).LoopMode = Animation.LoopModeEnum.Linear;
            animator.Play(idle); animator.Advance(0);
        }
        // The same D1 composition restores cosmetic saves in both preview and gameplay.
        // This instance owns only its rig/assembly; no actor, ability or audio service is created.
        if (profile!=null && skeleton!=null)
        {
            Assembly=EquipmentComposition.Create(model,model,skeleton,profile.BaseClass,advanced.Id);
            if(profile.Idle!="" && animator!=null)
            {
                var source=GD.Load<PackedScene>(profile.AnimationScene).Instantiate<Node3D>();
                var sourcePlayer=source.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().Single();
                var clip=(Animation)sourcePlayer.GetAnimation(profile.Idle).Duplicate(true);
                for(int t=clip.GetTrackCount()-1;t>=0;t--)
                { string bone=clip.TrackGetPath(t).GetConcatenatedSubNames();if(skeleton.FindBone(bone)<0){clip.RemoveTrack(t);continue;}clip.TrackSetPath(t,new NodePath(animator.GetNode(animator.RootNode).GetPathTo(skeleton)+":"+bone)); }
                clip.LoopMode=Animation.LoopModeEnum.Linear;var library=new AnimationLibrary();library.AddAnimation("Idle",clip);animator.AddAnimationLibrary("Class",library);animator.Play("Class/Idle");animator.Advance(0);source.Free();
            }
        }
        float height = .18f;
        if (skeleton != null && skeleton.FindBone("Head") >= 0)
            height = (skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(skeleton.FindBone("Head")).Origin).Y * 1.12f;
        placement.Scale = Vector3.One * (1.8f / Mathf.Max(.01f, height));
        _facing = advanced.FacingDegrees; ResetView();
    }
    private static string AddSaberStance(AnimationPlayer animator,Skeleton3D skeleton)
    {
        // B6 already converted the original BMN cb_knight_saber_idle_1.jba.
        // Retarget its bone paths into this viewer instance, without changing either imported GLB.
        var source=GD.Load<PackedScene>("res://Assets/Enemies/looter.glb").Instantiate<Node3D>();
        try
        {
            var player=source.FindChildren("*","AnimationPlayer",true,false).OfType<AnimationPlayer>().Single();
            var stance=(Animation)player.GetAnimation("Idle").Duplicate(true);
            var skeletonPath=animator.GetNode(animator.RootNode).GetPathTo(skeleton);
            for (int track=stance.GetTrackCount()-1;track>=0;track--)
            {
                string bone=stance.TrackGetPath(track).GetConcatenatedSubNames();
                if (skeleton.FindBone(bone)<0) { stance.RemoveTrack(track); continue; }
                stance.TrackSetPath(track,new NodePath(skeletonPath+":"+bone));
            }
            var library=new AnimationLibrary(); library.AddAnimation("SaberIdle",stance);
            animator.AddAnimationLibrary("Preview",library); return "Preview/SaberIdle";
        }
        finally { source.Free(); }
    }
    public void ResetView() { _rotation.RotationDegrees = new Vector3(0,_facing,0); _camera.Fov = 34; }
    public override GodotObject _MakeCustomTooltip(string text) => new SwtorSurvivors.UI.SwtorTooltip(text);
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
