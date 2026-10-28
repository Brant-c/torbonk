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
    [Export] public float JumpBlendSeconds { get; set; } = 0.06f;
    [Export] public float TakeoffAscentFraction { get; set; } = 0.5f;
    [Export] public float FallAnimationGraceSeconds { get; set; } = 0.08f;
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
    public string JumpPhase { get; private set; } = "";
    public string JumpClip => _jumpNode.Animation;
    public float JumpWeight => _jumpWeight;
    public double JumpClipSeconds => _jumpClock;
    public bool JumpAnimationsLoaded => Animator.HasAnimation("Jump/Takeoff");
    public string IdleClip => _idleNode.Animation;
    public string RunClip => _runNode.Animation;
    private AnimationNodeAnimation _idleNode = null!, _runNode = null!;
    private AnimationNodeStateMachine _gestures = null!;
    private AnimationNodeStateMachinePlayback _playback = null!;
    private readonly List<AnimationNodeStateMachine> _layerMachines = new();
    private readonly List<AnimationNodeStateMachinePlayback> _layerPlaybacks = new();
    private AnimationNodeAnimation _reactionNode = null!;
    private AnimationNodeAnimation _jumpNode = null!;
    private readonly List<string> _gestureStates = new();
    private string _gestureClip = "", _reaction = "";
    private double _reactionTime;
    private float _movement, _speed, _gestureWeight, _reactionWeight;
    public bool FullBodyGesture {get;private set;}
    public bool KeepFullBodyWhileMoving {get;private set;}
    private float _jumpWeight,_movingFullBodyWeight;
    private int _gesturePriority;
    private double _jumpClock, _takeoffDuration, _ungroundedSeconds;

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
        _idleNode = new AnimationNodeAnimation { Animation = "Idle" };
        _runNode = new AnimationNodeAnimation { Animation = "Run" };
        graph.AddNode("Idle", _idleNode);
        graph.AddNode("Run", _runNode);
        AddDirectionalLocomotion(graph);
        graph.AddNode("Locomotion", new AnimationNodeBlend2 { Sync=true });
        graph.ConnectNode("Locomotion", 0, "Idle"); graph.ConnectNode("Locomotion", 1, "RunSpeed");
        _jumpNode = new AnimationNodeAnimation { Animation = "Idle" };
        graph.AddNode("JumpClip", _jumpNode);
        graph.AddNode("JumpSeek", new AnimationNodeTimeSeek()); graph.ConnectNode("JumpSeek", 0, "JumpClip");
        graph.AddNode("JumpLocomotion", new AnimationNodeBlend2 { Sync = true });
        graph.ConnectNode("JumpLocomotion", 0, "Locomotion"); graph.ConnectNode("JumpLocomotion", 1, "JumpSeek");
        _gestures = new AnimationNodeStateMachine { AllowTransitionToSelf = true };
        graph.AddNode("Gesture", _gestures); AddGestureClips();
        graph.AddNode("GestureRate",new AnimationNodeTimeScale());graph.ConnectNode("GestureRate",0,"Gesture");
        graph.AddNode("StandingCast", new AnimationNodeBlend2 { Sync=true });
        graph.ConnectNode("StandingCast", 0, "JumpLocomotion"); graph.ConnectNode("StandingCast", 1, "GestureRate");
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
            graph.AddNode(layer+"Rate",new AnimationNodeTimeScale());graph.ConnectNode(layer+"Rate",0,layer+"Gesture");
            graph.AddNode(layer, blend); graph.ConnectNode(layer, 0, previous); graph.ConnectNode(layer, 1, layer+"Rate");
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
            if (clip.StartsWith("Jump/", StringComparison.Ordinal) || clip.StartsWith("Travel/",StringComparison.Ordinal)) continue;
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

    public void Present(Vector3 actualVelocity, double delta, float? facingYawDegrees = null)
    {
        var flat = new Vector3(actualVelocity.X, 0, actualVelocity.Z); _speed = flat.Length();_movementVelocity=flat;
        Locomotion = _speed > (Locomotion == "Run" ? 0.025f : 0.055f) ? "Run" : "Idle";
        var facing = GestureOwner!=null && FacingTarget.HasValue ? FacingTarget.Value-GlobalPosition : _speed > 0.025f ? flat : FacingTarget.HasValue ? FacingTarget.Value - GlobalPosition : Vector3.Zero;
        if (facingYawDegrees.HasValue)
            Rotation = new Vector3(0, Mathf.DegToRad(facingYawDegrees.Value + ForwardOffsetDegrees), 0);
        else if (new Vector2(facing.X, facing.Z).Length() > 0.001f)
        {
            float target = Mathf.Atan2(-facing.X, -facing.Z) + Mathf.DegToRad(ForwardOffsetDegrees);
            Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, target, 1 - Mathf.Exp(-TurnSharpness * (float)delta)), 0);
        }
    }
    public override void _Process(double delta)
    {
        UpdateJumpAnimation(delta);
        float step = (float)delta / Math.Max(0.01f, BlendSeconds);
        _movement = Mathf.MoveToward(_movement, Locomotion == "Run" ? 1 : 0, step);
        _gestureWeight = Mathf.MoveToward(_gestureWeight, GestureOwner != null ? 1 : 0, step);
        _movingFullBodyWeight=Mathf.MoveToward(_movingFullBodyWeight,KeepFullBodyWhileMoving && GestureOwner!=null?1:0,step);
        if (_reaction == "Hit" && (_reactionTime -= delta) <= 0) _reaction = "";
        _reactionWeight = Mathf.MoveToward(_reactionWeight, OverrideActive ? 1 : 0, step);
        UpdateDirectionalLocomotion();
        Tree.Set("parameters/Locomotion/blend_amount", _movement);
        Tree.Set("parameters/JumpLocomotion/blend_amount", _jumpWeight);
        // Briefly crossfade the original standing pose into the moving bone masks.
        Tree.Set("parameters/StandingCast/blend_amount", Mathf.Max(_movingFullBodyWeight,1-Mathf.Max(_movement,_jumpWeight)) * _gestureWeight);
        Tree.Set("parameters/Waist/blend_amount", _gestureWeight * Mathf.Clamp(LowerBackCastWeight, 0, 1));
        Tree.Set("parameters/Chest/blend_amount", _gestureWeight * Mathf.Clamp(ChestCastWeight, 0, 1));
        Tree.Set("parameters/Upper/blend_amount", _gestureWeight);
        Tree.Set("parameters/Reaction/blend_amount", _reactionWeight);
        Tree.Advance(delta);
        State = OverrideActive ? _reaction : GestureOwner != null ? _gestureClip : JumpPhase != "" ? "Jump/" + JumpPhase : Locomotion;
    }

    /// <summary>Physics supplies contact and ascent/descent. Animation never moves the body.</summary>
    public void PresentJump(bool grounded, bool tookOff, float verticalVelocity, float timeToApex, double delta)
    {
        if (!JumpAnimationsLoaded || _reaction == "Death") return;
        _ungroundedSeconds = grounded ? 0 : _ungroundedSeconds + delta;
        if (tookOff)
        {
            _takeoffDuration = Math.Max(0.05, timeToApex * Mathf.Clamp(TakeoffAscentFraction, 0.1f, 1));
            SetJumpPhase("Takeoff");
        }
        else if (!grounded)
        {
            // Floor snapping/respawn can briefly report no contact without a real fall.
            if (JumpPhase == "" || JumpPhase.StartsWith("Land", StringComparison.Ordinal))
            {
                if (_ungroundedSeconds >= FallAnimationGraceSeconds) SetJumpPhase("Falling");
            }
            else if (JumpPhase == "Takeoff" && _jumpClock >= _takeoffDuration)
                SetJumpPhase(verticalVelocity > 0 ? "Airborne" : "Falling");
            else if (JumpPhase == "Airborne" && verticalVelocity <= 0) SetJumpPhase("Falling");
        }
        else if (JumpPhase is "Takeoff" or "Airborne" or "Falling")
            SetJumpPhase(_speed > 0.055f ? "LandRun" : "LandIdle");
        else if ((JumpPhase == "LandIdle" && _speed > 0.055f) || (JumpPhase == "LandRun" && _speed < 0.025f))
            JumpPhase = ""; // Respond immediately if locomotion changes during landing.
    }

    private void SetJumpPhase(string phase)
    {
        JumpPhase = phase; _jumpClock = 0; _jumpNode.Animation = "Jump/" + phase;
    }

    private void UpdateJumpAnimation(double delta)
    {
        if (JumpPhase != "") _jumpClock += delta;
        double length = Animator.GetAnimation(_jumpNode.Animation).Length;
        bool landing = JumpPhase.StartsWith("Land", StringComparison.Ordinal);
        float target = JumpPhase == "" || (landing && _jumpClock >= length - JumpBlendSeconds) ? 0 : 1;
        _jumpWeight = Mathf.MoveToward(_jumpWeight, target, (float)delta / Mathf.Max(0.01f, JumpBlendSeconds));
        double sample = JumpPhase == "Takeoff" ? Math.Min(length, _jumpClock / _takeoffDuration * length) :
            JumpPhase is "Airborne" or "Falling" ? _jumpClock % Math.Max(0.01, length) : Math.Min(_jumpClock, length);
        if (JumpPhase != "" || _jumpWeight > 0) Tree.Set("parameters/JumpSeek/seek_request", sample);
        if (landing && _jumpClock >= length) JumpPhase = "";
    }

    public void ResetJumpPresentation()
    {
        JumpPhase = ""; _jumpClock = _ungroundedSeconds = _jumpWeight = 0; _jumpNode.Animation = "Idle";
        Tree.Set("parameters/JumpLocomotion/blend_amount", 0.0);
        Tree.Set("parameters/JumpSeek/seek_request", 0.0);
    }
    // Ordinary busy gestures are skipped. A sustained higher-priority cast can crossfade in; never queue.
    public bool TryBeginGesture(object owner, string clip, int priority=0, bool fullBody=false, bool keepFullBodyWhileMoving=false)
    {
        if (OverrideActive || !Animator.HasAnimation(clip) ||
            (GestureOwner!=null && priority<=_gesturePriority) || (GestureOwner==null && _gestureWeight>0.05f && priority<=0)) return false;
        bool crossfade=GestureOwner!=null || _gestureWeight>0.05f;
        GestureOwner = owner; _gestureClip = clip;_gesturePriority=priority;
        FullBodyGesture=fullBody;KeepFullBodyWhileMoving=keepFullBodyWhileMoving;
        SetGestureRate(1);
        if(crossfade)_playback.Travel(ClipState(clip));else _playback.Start(ClipState(clip));
        foreach(var playback in _layerPlaybacks)
            if(crossfade)playback.Travel(ClipState(clip));else playback.Start(ClipState(clip));
        return true;
    }
    /// <summary>True when TryBeginGesture would start now; lets full-body abilities refuse to cast without their animation.</summary>
    public bool CanBeginGesture(string clip, int priority=0) => !OverrideActive && Animator.HasAnimation(clip) &&
        !(GestureOwner!=null && priority<=_gesturePriority);
    public void SetGestureClip(object owner, string clip)
    {
        if (GestureOwner != owner || string.IsNullOrEmpty(clip) || !Animator.HasAnimation(clip) || _gestureClip==clip) return;
        _gestureClip = clip; _playback.Travel(ClipState(clip));
        foreach(var playback in _layerPlaybacks) playback.Travel(ClipState(clip));
    }
    public void FitGesture(object owner,double duration,bool repeatFinished=false)
    {
        if(GestureOwner!=owner || duration<=0 || !Animator.HasAnimation(_gestureClip))return;
        double length=Animator.GetAnimation(_gestureClip).Length;
        SetGestureRate((float)(length/duration));
        if(repeatFinished && _playback.GetCurrentPlayPosition()>=length-.00001){
            _playback.Start(ClipState(_gestureClip));
            foreach(var playback in _layerPlaybacks)playback.Start(ClipState(_gestureClip));
        }
    }
    private void SetGestureRate(float rate)
    {
        foreach(string node in new[]{"GestureRate","WaistRate","ChestRate","UpperRate"})Tree.Set("parameters/"+node+"/scale",rate);
    }
    public void ReleaseMovingBody(object owner)
    {
        if(GestureOwner==owner)KeepFullBodyWhileMoving=false;
    }
    public void ReleaseGesture(object owner)
    {
        if (GestureOwner != owner) return;
        GestureOwner = null; FacingTarget = null;
        FullBodyGesture=false;KeepFullBodyWhileMoving=false;
        _gesturePriority=0;
    }
    public void ResetPresentation()
    {
        ResetJumpPresentation();
        GestureOwner = null; FullBodyGesture=false;KeepFullBodyWhileMoving=false; _reaction = ""; FacingTarget = null; _speed = _movement = _gestureWeight = _reactionWeight = 0;
        _movementVelocity=Vector3.Zero;LocomotionDirection=Vector2.Up;_movingFullBodyWeight=0;
        Locomotion = State = "Idle"; _playback.Start("Idle");
        foreach(var playback in _layerPlaybacks) playback.Start("Idle");
        _Process(0);
    }
    public void LoadAbilityAnimations()
    {
        LoadAbilityLibrary("res://Assets/Abilities/casts.glb", "B5");
        LoadAbilityLibrary("res://Assets/B8/casts.glb", "B8");
        LoadAbilityLibrary("res://Assets/D2/casts.glb", "D2");
    }
    /// <summary>Equipment selects compatible original locomotion clips; the existing blend tree keeps running.</summary>
    public void SetEquipmentLocomotion(Equipment.LocomotionDefinition? profile)
    {
        string idle="Idle",run="Run";
        if(profile!=null)
        {
            if(!Animator.HasAnimationLibrary(profile.LibraryId))LoadAbilityLibrary(profile.Scene,profile.LibraryId,false);
            idle=profile.LibraryId+"/"+profile.Idle;run=profile.LibraryId+"/"+profile.Run;
            if(!Animator.HasAnimation(idle) || !Animator.HasAnimation(run))
                throw new InvalidOperationException("Equipment locomotion clips unavailable: "+profile.LibraryId);
            Animator.GetAnimation(idle).LoopMode=Animator.GetAnimation(run).LoopMode=Animation.LoopModeEnum.Linear;
        }
        if(IdleClip==idle && RunClip==run)return;
        _idleNode.Animation=idle;_runNode.Animation=run;
    }
    public void LoadMarauderAnimations()
    {
        LoadAbilityLibrary("res://Assets/WarriorLibrary/casts.glb","Marauder",false);
        var lib=Animator.GetAnimationLibrary("Marauder");
        foreach(var name in lib.GetAnimationList()) {
            var a=lib.GetAnimation(name);double duration=name=="cb_2saber_sp3_attack_right_6"?1.5:name.ToString().Contains("charge")?.65:name.ToString().Contains("throw")?a.Length:Math.Min(a.Length,.95);
            double factor=duration/a.Length;
            for(int t=0;t<a.GetTrackCount();t++)for(int k=0;k<a.TrackGetKeyCount(t);k++)a.TrackSetKeyTime(t,k,a.TrackGetKeyTime(t,k)*factor);
            a.Length=duration;
        }
        lib.AddAnimation("DualThrow",JoinAnimations(lib.GetAnimation("cb_saber_throw_start"),lib.GetAnimation("cb_saber_throw_end"),"Root",1));
        AddGestureClips();
    }
    public void LoadJumpAnimations() => LoadAbilityLibrary("res://Assets/Jump/jump.glb", "Jump", false);
    public void LoadAbilityLibrary(string path, string key, bool gestureClips = true)
    {
        var imported = GD.Load<PackedScene>(path).Instantiate<Node3D>();
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
            if (name is "StormLoop" or "Airborne" or "Falling" || key=="PT" && name=="ThrowLoop" || key=="Sorc" && name=="BlastCharge") animation.LoopMode = Animation.LoopModeEnum.Linear;
            library.AddAnimation(name, animation);
        }
        // Smash is the original ground-smash leap: enter (launch) then exit (descent and slam). The 8 s clip and hover loop between them are not used.
        if (library.HasAnimation("SmashEnter") && library.HasAnimation("SmashExit"))
            library.AddAnimation("SmashLeap", JoinAnimations(library.GetAnimation("SmashEnter"), library.GetAnimation("SmashExit"), "Root", LeapHeightScale));
        Animator.AddAnimationLibrary(key, library); imported.Free();
        if (gestureClips) AddGestureClips();
    }
    /// <summary>The recovered leap rises ~24 m (original centimetre units) and the world here is a tenth scale; compress only the Root's offset from its starting pose.</summary>
    public const float LeapHeightScale = 0.18f;
    private static Animation JoinAnimations(Animation first, Animation second, string scaledBone, float scale)
    {
        var joined = new Animation { Length = first.Length + second.Length, LoopMode = Animation.LoopModeEnum.None };
        var tracks = new Dictionary<(Animation.TrackType, string), int>();
        Vector3? rootOrigin = null;
        void Copy(Animation source, float offset, bool scaleRoot)
        {
            for (int t = 0; t < source.GetTrackCount(); t++)
            {
                var type = source.TrackGetType(t); string path = source.TrackGetPath(t);
                if (type is not (Animation.TrackType.Position3D or Animation.TrackType.Rotation3D or Animation.TrackType.Scale3D)) continue;
                if (!tracks.TryGetValue((type, path), out int target))
                { target = joined.AddTrack(type); joined.TrackSetPath(target, new NodePath(path)); tracks[(type, path)] = target; }
                bool root = type == Animation.TrackType.Position3D && path.EndsWith(":" + scaledBone);
                for (int k = 0; k < source.TrackGetKeyCount(t); k++)
                {
                    double time = source.TrackGetKeyTime(t, k) + offset; var value = source.TrackGetKeyValue(t, k);
                    if (root) { rootOrigin ??= (Vector3)source.TrackGetKeyValue(t, 0); var origin = rootOrigin.Value; value = origin + ((Vector3)value - origin) * scale; }
                    switch (type)
                    {
                        case Animation.TrackType.Position3D: joined.PositionTrackInsertKey(target, time, (Vector3)value); break;
                        case Animation.TrackType.Rotation3D: joined.RotationTrackInsertKey(target, time, (Quaternion)value); break;
                        default: joined.ScaleTrackInsertKey(target, time, (Vector3)value); break;
                    }
                }
            }
        }
        Copy(first, 0, true); Copy(second, (float)first.Length, true);
        return joined;
    }
    public void PlayReaction(string clip)
    {
        if (_reaction == "Death" || (_reaction == "Hit" && clip == "Hit") || !Animator.HasAnimation(clip)) return;
        if (clip == "Death") ResetJumpPresentation();
        if (GestureOwner != null) ReleaseGesture(GestureOwner);
        _reaction = clip; _reactionTime = Animator.GetAnimation(clip).Length;
        _reactionNode.Animation = clip; Tree.Set("parameters/ReactionSeek/seek_request", 0.0);
        State = clip;
    }
}
