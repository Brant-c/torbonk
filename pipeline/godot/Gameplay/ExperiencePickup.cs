using Godot;

namespace SwtorSurvivors;

/// <summary>World mote using the recovered loot_core_glow texture; XP stays in RunProgression.</summary>
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
        // A 3D octahedron gives the glow a silhouette from every camera angle.
        var mesh = new ImmediateMesh(); mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        var equator = new[] { Vector3.Right, Vector3.Forward, Vector3.Left, Vector3.Back };
        for (int i = 0; i < 4; i++)
        {
            mesh.SurfaceAddVertex(Vector3.Up * 0.014f); mesh.SurfaceAddVertex(equator[i] * 0.008f); mesh.SurfaceAddVertex(equator[(i+1)%4] * 0.008f);
            mesh.SurfaceAddVertex(Vector3.Down * 0.014f); mesh.SurfaceAddVertex(equator[(i+1)%4] * 0.008f); mesh.SurfaceAddVertex(equator[i] * 0.008f);
        }
        mesh.SurfaceEnd();
        var color = new Color(0.28f, 1, 0.88f);
        _visual.AddChild(new MeshInstance3D { Mesh = mesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled, AlbedoColor = color, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 2 } });
        _visual.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = Vector2.One * 0.065f },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled, AlbedoColor = new Color(0.2f, 0.8f, 0.7f, 0.65f),
                AlbedoTexture = GD.Load<Texture2D>("res://Assets/Presentation/loot_glow.png"), NoDepthTest = false } });
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
        _visual.Rotation = new Vector3(0, (float)_age * 1.1f, 0);
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
