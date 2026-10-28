using Godot;
using System;
using System.Threading.Tasks;

namespace SwtorSurvivors;

/// <summary>Asset-independent regression for NPC movement safety and physics query cost.</summary>
public partial class NpcNavigationSmoke : Node3D
{
    private ArenaBounds _arena = null!;
    private ArenaNavigation _nav = null!;
    private Combatant _target = null!;
    private void Check(string name, bool passed)
    {
        if (!passed) throw new InvalidOperationException(name);
        GD.Print("NPC navigation: " + name);
    }
    private async Task Frames(int count)
    { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }

    private StaticBody3D Box(Vector3 position, Vector3 size, uint layer)
    {
        var body = new StaticBody3D { Position = position, CollisionLayer = layer };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body); return body;
    }
    private EnemyAgent Enemy(Vector3 feet)
    {
        var body = new CharacterBody3D { Position = feet };
        body.AddChild(new CollisionShape3D { Name = "CollisionShape3D", Position = Vector3.Up * .0925f,
            Shape = new CapsuleShape3D { Radius = .03f, Height = .185f } });
        var enemy = new EnemyAgent { Name = "Actor", Target = _target, Navigation = _nav,
            DetectionRange = 10, AttackDamage = 0, Team = CombatTeam.Hostile };
        enemy.AddChild(new Health { Name = "Health" }); body.AddChild(enemy); AddChild(body);
        return enemy;
    }
    public override async void _Ready()
    {
        try
        {
            _arena = new ArenaBounds { Minimum = new Vector2(-4,-4), Maximum = new Vector2(4,4) }; AddChild(_arena);
            _nav = new ArenaNavigation { Arena = _arena }; AddChild(_nav);
            // Two flat platforms separated by a real, uncapped hole at x=1..1.2.
            Box(new Vector3(-1.5f,-.05f,0), new Vector3(5,.1f,8), 1);
            Box(new Vector3(2.6f,-.05f,0), new Vector3(2.8f,.1f,8), 1);
            _target = new Combatant { Position = new Vector3(.8f,.002f,0), Team = CombatTeam.Player };
            _target.AddChild(new Health { Name = "Health", Maximum = 10000 }); AddChild(_target);
            await Frames(3);
            Check("hole cannot be traversed", !_nav.CanWalk(new Vector3(.9f,.002f,0),new Vector3(1.3f,.002f,0)));
            Check("crop edge cannot be traversed", !_nav.CanWalk(new Vector3(-3.8f,.002f,0),new Vector3(-4,.002f,0)));
            var slope = Box(new Vector3(0,.2f,2),new Vector3(1,.1f,1),1);
            slope.Rotation = new Vector3(0,0,Mathf.DegToRad(60)); await Frames(2);
            Check("steep ground is rejected", !_arena.Supported(new Vector3(0,.31f,2),.03f));
            slope.Free(); await Frames(2);

            var enemy = Enemy(new Vector3(-.8f,.002f,0));
            await Frames(2);
            long rays = _arena.GroundRaycasts; var start = enemy.Body.Position;
            await Frames(60);
            long movingRays = _arena.GroundRaycasts-rays;
            Check("NPC pursues across supported ground", enemy.Body.Position.X > start.X+.1f);
            // Old per-tick pre/post traversal alone cost at least 20 rays/tick, excluding steering.
            Check($"moving query budget below old 1200-ray floor ({movingRays} rays)", movingRays < 1200);
            enemy.Body.Free(); await Frames(2);

            // An unengaged, grounded NPC should not repeatedly probe unchanged terrain.
            enemy = Enemy(new Vector3(-2,.002f,0)); enemy.DetectionRange = .01f;
            await Frames(5); rays = _arena.GroundRaycasts; await Frames(60);
            Check("stationary NPC performs no ground raycasts", _arena.GroundRaycasts == rays);
            enemy.Body.Free(); await Frames(2);

            _target.Position = new Vector3(1.5f,.002f,0);
            enemy = Enemy(new Vector3(.94f,.002f,0)); await Frames(120);
            Check("NPC stays on supported side of hole", enemy.Body.Position.X < 1 && enemy.Body.Position.Y > -.02f);
            enemy.DisplaceFrom(enemy.GlobalPosition-Vector3.Right, .5f);
            Check("knockback cannot push NPC across hole", enemy.Body.Position.X < 1 && enemy.Body.Position.Y > -.02f);
            enemy.Body.Free(); await Frames(2);

            var wall = Box(new Vector3(0,.5f,0),new Vector3(.04f,1,4),2);
            _target.Position = new Vector3(.8f,.002f,1);
            enemy = Enemy(new Vector3(-.1f,.002f,0)); await Frames(120);
            Check("NPC follows wall without crossing it", enemy.Body.Position.X < -.04f && enemy.Body.Position.Z > .01f);
            enemy.Body.Free(); wall.Free(); await Frames(2);

            // Tall enclosing prop walls force all steering directions to fail.
            Box(new Vector3(-.1f,.5f,0),new Vector3(.04f,1,1),2);
            Box(new Vector3(-.5f,.5f,0),new Vector3(.04f,1,1),2);
            Box(new Vector3(-.3f,.5f,.2f),new Vector3(.44f,1,.04f),2);
            Box(new Vector3(-.3f,.5f,-.2f),new Vector3(.44f,1,.04f),2);
            _target.Position = new Vector3(.8f,.002f,0);
            enemy = Enemy(new Vector3(-.3f,.002f,0)); enemy.MoveSpeed = 1;
            await Frames(2); long steering = _nav.SteeringChecks; await Frames(120);
            Check("enclosed NPC cannot cross walls", enemy.Body.Position.X < -.14f && enemy.Body.Position.X > -.46f);
            Check("blocked steering retries stay bounded", _nav.SteeringChecks-steering < 30);
            enemy.Body.Free();
            GD.Print("NPC_NAVIGATION_PASSED"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
