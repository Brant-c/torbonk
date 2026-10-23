using Godot;

namespace SwtorSurvivors;

/// <summary>World pickup using the recovered blue star/up symbol; XP stays in RunProgression.</summary>
public partial class ExperiencePickup : Node3D
{
    public RunProgression Progression { get; set; } = null!;
    public int Value { get; set; }
    public bool Collecting { get; private set; }
    public bool VisualInitialized { get; private set; }
    private Node3D _visual = null!;
    private double _age, _collectAge;
    private Vector3 _collectStart, _collectTarget;
    public override void _Ready()
    {
        _visual = new Node3D(); AddChild(_visual);
        _visual.AddChild(DropSymbol.Icon(DropSymbolKind.Experience,0.055f));
        _age = GetInstanceId() % 100 * 0.17; VisualInitialized = true;
    }
    public override void _Process(double delta)
    {
        _age += delta;
        if (Collecting)
        {
            _collectAge += delta; float t = Mathf.Clamp((float)_collectAge / 0.22f, 0, 1);
            GlobalPosition = _collectStart.Lerp(_collectTarget, 1 - Mathf.Pow(1-t, 3));
            _visual.Scale = Vector3.One * (1 - t) * (1 + Mathf.Sin(t * Mathf.Pi) * 1.6f);
            if (t >= 1) QueueFree();
            return;
        }
        _visual.Position = Vector3.Up * (Mathf.Sin((float)_age * 2.8f) * 0.006f);
        _visual.Scale = Vector3.One * (0.92f + Mathf.Sin((float)_age * 3) * 0.08f);
    }
    public void BeginCollection(Vector3 target)
    {
        if (Collecting) return;
        Collecting = true; _collectStart = GlobalPosition; _collectTarget = target; _collectAge = 0;
        ProcessMode = ProcessModeEnum.Always; SetPhysicsProcess(false);
    }
    public override void _PhysicsProcess(double delta)
    {
        var player=Progression.World.PlayerActor;
        if (!player.Alive || Progression.Selecting || Collecting) return;
        var target=player.GlobalPosition+Vector3.Up*0.035f;
        float distance=GlobalPosition.DistanceTo(target);
        if (distance<=Progression.PickupRadius) { Progression.Collect(this); return; }
        if (distance<=Progression.MagnetRadius)
            GlobalPosition=GlobalPosition.MoveToward(target,(float)delta*0.8f);
    }
}
