using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Instance-local AnimationTree. Physics owns movement; this wrapper alone writes the rig.</summary>
public partial class CharacterPresentation : Node3D
{
    [Export] public float TurnSharpness { get; set; } = 15;
    [Export] public float BlendSeconds { get; set; } = 0.16f;
    [Export] public float ForwardOffsetDegrees { get; set; }
    [Export] public float LowerBackCastWeight { get; set; } = 0.3f;
    [Export] public float ChestCastWeight { get; set; } = 0.65f;
    public AnimationPlayer Animator { get; private set; } = null!;
    public Skeleton3D Skeleton { get; private set; } = null!;
    public AnimationTree Tree { get; private set; } = null!;
    public string State { get; private set; } = "Idle";
    public string Locomotion { get; private set; } = "Idle";
    public bool OverrideActive => _reaction != "";
    public object? GestureOwner { get; private set; }
    public Vector3? FacingTarget { get; set; }
    public float MovementBlend => _movement;
    public float GestureWeight => _gestureWeight;
    private AnimationNodeStateMachine _gestures = null!;
    private AnimationNodeStateMachinePlayback _playback = null!;
    private readonly List<AnimationNodeStateMachine> _layerMachines = new();
    private readonly List<AnimationNodeStateMachinePlayback> _layerPlaybacks = new();
    private AnimationNodeAnimation _reactionNode = null!;
    private readonly List<string> _gestureStates = new();
    private string _gestureClip = "", _reaction = "";
    private double _reactionTime;
    private float _movement, _speed, _gestureWeight, _reactionWeight;

    public override void _Ready()
    {
        Animator = FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Single();
        Skeleton = FindChildren("*", "Skeleton3D", true, false).OfType<Skeleton3D>().Single();
        foreach (var name in Animator.GetAnimationLibraryList())
        {
            var library = (AnimationLibrary)Animator.GetAnimationLibrary(name).Duplicate(true);
            Animator.RemoveAnimationLibrary(name); Animator.AddAnimationLibrary(name, library);
        }
        foreach (var clip in new[] { "Idle", "Run", "LightningLoop" })
            Animator.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;
        Animator.RootMotionTrack = new NodePath("");
        Animator.Stop(); BuildTree();
    }

    private void BuildTree()
    {
        var graph = new AnimationNodeBlendTree();
        graph.AddNode("Idle", new AnimationNodeAnimation { Animation = "Idle" });
        graph.AddNode("Run", new AnimationNodeAnimation { Animation = "Run" });
        graph.AddNode("Locomotion", new AnimationNodeBlend2 { Sync=true });
        graph.ConnectNode("Locomotion", 0, "Idle"); graph.ConnectNode("Locomotion", 1, "Run");
        _gestures = new AnimationNodeStateMachine { AllowTransitionToSelf = true };
        graph.AddNode("Gesture", _gestures); AddGestureClips();
        graph.AddNode("StandingCast", new AnimationNodeBlend2 { Sync=true });
        graph.ConnectNode("StandingCast", 0, "Locomotion"); graph.ConnectNode("StandingCast", 1, "Gesture");
        // Root splits into Pelvis and RootSpine -> LowerBack -> Chest -> Chest1.
        // Disjoint masks soften the waist without diluting finger or hand socket motion.
        string previous = "StandingCast";
        foreach (string layer in new[] { "Waist", "Chest", "Upper" })
        {
            var blend = new AnimationNodeBlend2 { FilterEnabled = true,Sync=true };
            var root = Animator.GetNode(Animator.RootNode);
            for (int bone = 0; bone < Skeleton.GetBoneCount(); bone++)
            {
                string name = Skeleton.GetBoneName(bone);
                bool include = layer == "Waist" ? name is "RootSpine" or "LowerBack" :
                    layer == "Chest" ? name == "Chest" : IsDescendant(bone, "Chest1");
                if (include) blend.SetFilterPath(new NodePath(root.GetPathTo(Skeleton) + ":" + name), true);
            }
            // Each tree output has one connection; synchronized replicas avoid evaluating
            // a shared state machine several times per frame through different blend paths.
            var machine=(AnimationNodeStateMachine)_gestures.Duplicate(true);
            _layerMachines.Add(machine);graph.AddNode(layer+"Gesture",machine);
            graph.AddNode(layer, blend); graph.ConnectNode(layer, 0, previous); graph.ConnectNode(layer, 1, layer+"Gesture");
            previous = layer;
        }
        _reactionNode = new AnimationNodeAnimation { Animation = "Hit" };
        graph.AddNode("ReactionClip", _reactionNode);
        graph.AddNode("ReactionSeek", new AnimationNodeTimeSeek()); graph.ConnectNode("ReactionSeek", 0, "ReactionClip");
        graph.AddNode("Reaction", new AnimationNodeBlend2 { Sync=true });
        graph.ConnectNode("Reaction", 0, previous); graph.ConnectNode("Reaction", 1, "ReactionSeek");
        graph.ConnectNode("output", 0, "Reaction");
        Tree = new AnimationTree { Name = "CastingLayers", TreeRoot = graph,
            Deterministic=true, CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual };
        AddChild(Tree); Tree.AnimPlayer = Tree.GetPathTo(Animator);
        Tree.RootNode = Tree.GetPathTo(Animator.GetNode(Animator.RootNode)); Tree.Active = true;
        _playback = (AnimationNodeStateMachinePlayback)Tree.Get("parameters/Gesture/playback");
        foreach(string layer in new[]{"Waist","Chest","Upper"})
        {
            var playback=(AnimationNodeStateMachinePlayback)Tree.Get("parameters/"+layer+"Gesture/playback");
            _layerPlaybacks.Add(playback);playback.Start("Idle");
        }
        _playback.Start("Idle"); Tree.Advance(0);
    }

    private bool IsDescendant(int bone, string ancestor)
    {
        for (int i = bone; i >= 0; i = Skeleton.GetBoneParent(i))
            if (Skeleton.GetBoneName(i) == ancestor) return true;
        return false;
    }
    private static string ClipState(string clip) => clip.Replace('/', '_');
    private void AddGestureClips()
    {
        foreach (string clip in Animator.GetAnimationList())
        {
            string state = ClipState(clip);
            if (clip == "RESET" || _gestureStates.Contains(state)) continue;
            _gestures.AddNode(state, new AnimationNodeAnimation { Animation = clip });
            foreach(var machine in _layerMachines) machine.AddNode(state,new AnimationNodeAnimation { Animation=clip });
            foreach (string other in _gestureStates)
            {
                _gestures.AddTransition(other, state, new AnimationNodeStateMachineTransition { XfadeTime = BlendSeconds });
                _gestures.AddTransition(state, other, new AnimationNodeStateMachineTransition { XfadeTime = BlendSeconds });
                foreach(var machine in _layerMachines)
                {
                    machine.AddTransition(other,state,new AnimationNodeStateMachineTransition { XfadeTime=BlendSeconds });
                    machine.AddTransition(state,other,new AnimationNodeStateMachineTransition { XfadeTime=BlendSeconds });
                }
            }
            _gestureStates.Add(state);
        }
    }

    public void Present(Vector3 actualVelocity, double delta)
    {
        var flat = new Vector3(actualVelocity.X, 0, actualVelocity.Z); _speed = flat.Length();
        Locomotion = _speed > (Locomotion == "Run" ? 0.025f : 0.055f) ? "Run" : "Idle";
        var facing = _speed > 0.025f ? flat : FacingTarget.HasValue ? FacingTarget.Value - GlobalPosition : Vector3.Zero;
        if (new Vector2(facing.X, facing.Z).Length() > 0.001f)
        {
            float target = Mathf.Atan2(-facing.X, -facing.Z) + Mathf.DegToRad(ForwardOffsetDegrees);
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, target, 1 - Mathf.Exp(-TurnSharpness * (float)delta)), 0);
        }
    }
    public override void _Process(double delta)
    {
        float step = (float)delta / Math.Max(0.01f, BlendSeconds);
        _movement = Mathf.MoveToward(_movement, Locomotion == "Run" ? 1 : 0, step);
        _gestureWeight = Mathf.MoveToward(_gestureWeight, GestureOwner != null ? 1 : 0, step);
        if (_reaction == "Hit" && (_reactionTime -= delta) <= 0) _reaction = "";
        _reactionWeight = Mathf.MoveToward(_reactionWeight, OverrideActive ? 1 : 0, step);
        Tree.Set("parameters/Locomotion/blend_amount", _movement);
        // Briefly crossfade the original standing pose into the moving bone masks.
        Tree.Set("parameters/StandingCast/blend_amount", (1 - _movement) * _gestureWeight);
        Tree.Set("parameters/Waist/blend_amount", _gestureWeight * Mathf.Clamp(LowerBackCastWeight, 0, 1));
        Tree.Set("parameters/Chest/blend_amount", _gestureWeight * Mathf.Clamp(ChestCastWeight, 0, 1));
        Tree.Set("parameters/Upper/blend_amount", _gestureWeight);
        Tree.Set("parameters/Reaction/blend_amount", _reactionWeight);
        Tree.Advance(delta);
        State = OverrideActive ? _reaction : GestureOwner != null ? _gestureClip : Locomotion;
    }
    // Busy gestures are skipped, never queued past their damage event.
    public bool TryBeginGesture(object owner, string clip)
    {
        if (OverrideActive || GestureOwner != null || _gestureWeight > 0.05f || !Animator.HasAnimation(clip)) return false;
        GestureOwner = owner; _gestureClip = clip; _playback.Start(ClipState(clip));
        foreach(var playback in _layerPlaybacks) playback.Start(ClipState(clip));
        return true;
    }
    public void SetGestureClip(object owner, string clip)
    {
        if (GestureOwner != owner || string.IsNullOrEmpty(clip) || !Animator.HasAnimation(clip)) return;
        _gestureClip = clip; _playback.Travel(ClipState(clip));
        foreach(var playback in _layerPlaybacks) playback.Travel(ClipState(clip));
    }
    public void ReleaseGesture(object owner)
    {
        if (GestureOwner != owner) return;
        GestureOwner = null; FacingTarget = null;
    }
    public void ResetPresentation()
    {
        GestureOwner = null; _reaction = ""; FacingTarget = null; _speed = _movement = _gestureWeight = _reactionWeight = 0;
        Locomotion = State = "Idle"; _playback.Start("Idle");
        foreach(var playback in _layerPlaybacks) playback.Start("Idle");
        _Process(0);
    }
    public void LoadAbilityAnimations()
    {
        var imported = GD.Load<PackedScene>("res://Assets/Abilities/casts.glb").Instantiate<Node3D>();
        var source = imported.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().Single();
        var library = new AnimationLibrary();
        foreach (var name in source.GetAnimationList())
        {
            if (name == "RESET") continue;
            var animation = (Animation)source.GetAnimation(name).Duplicate(true);
            for (int i = animation.GetTrackCount() - 1; i >= 0; i--)
            {
                string bone = animation.TrackGetPath(i).GetConcatenatedSubNames();
                if (Skeleton.FindBone(bone) < 0) { animation.RemoveTrack(i); continue; }
                animation.TrackSetPath(i, new NodePath(Animator.GetNode(Animator.RootNode).GetPathTo(Skeleton) + ":" + bone));
            }
            if (name == "StormLoop") animation.LoopMode = Animation.LoopModeEnum.Linear;
            library.AddAnimation(name, animation);
        }
        Animator.AddAnimationLibrary("B5", library); imported.Free(); AddGestureClips();
    }
    public void PlayReaction(string clip)
    {
        if (_reaction == "Death" || (_reaction == "Hit" && clip == "Hit") || !Animator.HasAnimation(clip)) return;
        if (GestureOwner != null) ReleaseGesture(GestureOwner);
        _reaction = clip; _reactionTime = Animator.GetAnimation(clip).Length;
        _reactionNode.Animation = clip; Tree.Set("parameters/ReactionSeek/seek_request", 0.0);
        State = clip;
    }
}
