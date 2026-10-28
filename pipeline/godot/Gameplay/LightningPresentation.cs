using Godot;

namespace SwtorSurvivors;

/// <summary>Interop adapter for the A3 effect; supplies the torso fallback for enemies without attachment rigs.</summary>
public partial class LightningPresentation : Node, IAbilityPresentation
{
    public Node3D Effect { get; private set; } = null!;
    public CharacterPresentation Actor { get; set; } = null!;
    public double StartSeconds => Actor.Animator.GetAnimation("LightningStart").Length;
    public double EndSeconds => System.Math.Max(0.5, Actor.Animator.GetAnimation("LightningEnd").Length);

    public override void _Ready()
    {
        Effect = (Node3D)GD.Load<GDScript>("res://ForceLightning.gd").New();
        Effect.Name = "OriginalForceLightning";
        AddChild(Effect);
        Effect.Set("externally_driven",true);
    }

    public bool Start(Combatant target)
    {
        Variant targetRig = target.Presentation == null ? default : Variant.From(target.Presentation.Skeleton);
        if (!Effect.Call("configure", Actor.Skeleton, Actor.Animator, target,
            targetRig,target.AimHeight).AsBool())
        { GD.PushError("Lightning configure failed: " + Effect.Get("last_error").AsString()); return false; }
        if (Effect.Call("start_channel").AsBool()) return true;
        Release(); return false;
    }

    public void End(bool interrupted)
    {
        Effect.Call("stop_channel");
    }

    public bool Begin(Combatant target, Vector3 center, float radius) => Start(target);
    public void Channel() => Effect.Call("external_channel");
    public void Impact(Vector3 center) { }

    public void Release()
    {
        if (GodotObject.IsInstanceValid(Effect)) Effect.Call("interrupt", false);
    }

    public Godot.Collections.Dictionary Snapshot() => Effect.Call("snapshot").AsGodotDictionary();
}
