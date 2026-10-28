using Godot;
using System.Linq;

namespace SwtorSurvivors;

public partial class EternityVaultSmoke : Node
{
    public override async void _Ready()
    {
        var world = (GameWorld)GetParent();
        if (OS.GetCmdlineUserArgs().Contains("--eternity-vault-sky-review")) world.Rig.AngleDegrees=8;
        if (OS.GetCmdlineUserArgs().Contains("--eternity-vault-door-review")) {world.Rig.AngleDegrees=12;world.Rig.YawDegrees=180;}
        for (int i = 0; i < 120; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        if (DisplayServer.GetName() != "headless")
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng("res://../logs/map-eternity_vault-review.png");
        }
        bool passed = world.ReadyForPlay && world.ExplorationOnly && world.Spawner == null && world.Waves == null
            && world.Player.GlobalPosition.DistanceTo(world.Player.SpawnPosition) < 0.1f
            && world.Player.Recoveries == 0
            && world.EnvironmentRoot.GetMeta("source_room").AsString() == "01_Surface"
            && world.EnvironmentRoot.FindChild("Instance_4611686142220981203",true,false) != null
            && world.FindChildren("SurfaceSnow","CPUParticles3D",true,false).Count == 0
            && Frontend.FrontendCatalog.Load().Maps.Any(m => m.Id == "eternity_vault" && m.GameplayImplemented);
        int platformCrossings=0;
        if (OS.GetCmdlineUserArgs().Contains("--eternity-vault-platform-smoke")) {
            foreach(float x in new[]{-61.3f,-66.7001f}) {
                if(!world.Arena.GroundAt(new Vector3(x-1.5f,3,34),out var start,out _,1,2)) {passed=false;continue;}
                world.Player.SpawnPosition=start+Vector3.Up*.002f;world.Player.ResetPlayer();
                world.Rig.YawDegrees=0;world.Player.TestInput=new Vector2(1,0);
                for(int i=0;i<330;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                world.Player.TestInput=Vector2.Zero;
                bool crossed=world.Player.GlobalPosition.X>x+1.35f && world.Player.Recoveries==0;
                if(crossed)platformCrossings++;else passed=false;
                GD.Print($"PLATFORM_CROSSING {x}: {world.Player.GlobalPosition}, passed={crossed}, {world.Arena.LastRejection}");
            }
        }
        using var receipt = FileAccess.Open("res://../manifests/map-eternity_vault-smoke.json", FileAccess.ModeFlags.Write);
        receipt.StoreString(System.Text.Json.JsonSerializer.Serialize(new { passed, spawn = world.Player.SpawnPosition.ToString(),
            noEncounters = world.Spawner == null && world.Waves == null, recoveries = world.Player.Recoveries, platformCrossings }));
        GD.Print(passed ? "ETERNITY_VAULT_SMOKE_PASSED" : "ETERNITY_VAULT_SMOKE_FAILED");
        GetTree().Quit(passed ? 0 : 1);
    }
}
