using Godot;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SwtorSurvivors.Frontend;

namespace SwtorSurvivors.Integration;

public static class OperationDifficultySmoke
{
    private static void Check(bool ok,string label){if(!ok)throw new Exception(label);GD.Print("OPERATION_DIFFICULTY "+label);}
    private static void Near(float actual,float expected,string label)=>Check(Math.Abs(actual-expected)<=Math.Max(.001f,Math.Abs(expected)*.00001f),label);
    public static async Task Verify(GameWorld world)
    {
        var tree=world.GetTree();var spawner=world.Spawner!;var waves=world.Waves!;
        waves.SetPhysicsProcess(false);world.Player.SetPhysicsProcess(false);world.Combat.SetPhysicsProcess(false);
        world.Abilities.SetPhysicsProcess(false);foreach(var ability in world.Abilities.Abilities)ability.CombatEnabled=false;
        string mode=world.RunConfiguration.OperationDifficulty;
        var tuning=world.RunConfiguration.ModeTuning;
        Check(new SelectionConfiguration().OperationDifficulty=="Hard","legacy and development selections default to Hard");
        var invalid=GameNavigation.LastSelection!;invalid.OperationDifficulty="Invalid";
        Check(!SelectionGameplayAdapter.TryResolve(invalid,out _,out _),"invalid operation modes rejected");
        var baseline=PlanetRunDefinition.Load(world.PlanetRunPath);
        string before=JsonSerializer.Serialize(baseline);
        OperationDifficultyTuning.Resolve("eternity_vault","Hard").Apply(baseline);
        Check(JsonSerializer.Serialize(baseline)==before,"Hard preserves every authored run setting");
        OperationDifficultyTuning.Resolve("korriban","Nightmare").Apply(baseline);
        Check(JsonSerializer.Serialize(baseline)==before,"operation difficulty leaves world tuning unchanged");
        int cap=mode=="Normal"?18:mode=="Nightmare"?30:24;
        Check(spawner.HardActiveLimit==cap && world.PlanetRun!.Difficulty.Population(50)==cap,"mode population cap applied");
        Near((float)world.PlanetRun!.Difficulty.SpawnInterval(50,50),.2f*tuning.SpawnInterval,"mode spawn cadence applied");
        Check(world.PlanetRun.CoreLevel==50 && world.PlanetRun.EnemyExperienceMultiplier==2,"progression cap and XP rewards unchanged");
        var sample=GD.Load<PackedScene>("res://Gameplay/Planets/EternityVault/IceCat.tscn");
        var raw=sample.Instantiate<CharacterBody3D>();var authored=raw.GetNode<EnemyAgent>("Actor");
        float hp=authored.GetNode<Health>("Health").Maximum,interval=authored.AttackInterval;raw.Free();
        EnemyAgent Spawn(PackedScene scene)
        {
            for(int attempt=0;attempt<24;attempt++)if(spawner.TrySpawn(scene,ignoreLimit:true))return spawner.Enemies.Last();
            throw new Exception("No safe test spawn for "+scene.ResourcePath);
        }
        foreach(int level in new[]{1,25,50})
        {
            spawner.ResetSpawner();spawner.PlayerLevel=()=>level;
            var enemy=Spawn(sample);enemy.SetPhysicsProcess(false);
            Near(enemy.Health.Maximum,hp*tuning.EnemyHealth*(float)Math.Pow(1.08,level-1),"enemy health curve level "+level);
            Near(enemy.LevelDamageMultiplier,tuning.EnemyDamage*(float)Math.Pow(1.045,level-1),"enemy damage curve level "+level);
            Near(enemy.AttackInterval,interval*tuning.AttackInterval/(1+Math.Min(.85f,.018f*(level-1))),"enemy attack cadence level "+level);
        }
        spawner.ResetSpawner();spawner.PlayerLevel=()=>50;
        var encounter=new BossEncounterController {Spawner=spawner,Definition=GD.Load<BossEncounterDefinition>("res://Gameplay/Planets/EternityVault/Xrr3Encounter.tres")};
        world.AddChild(encounter);
        for(int attempt=0;attempt<24 && !encounter.Spawned;attempt++)encounter.Tick(2,true);
        Check(encounter.Spawned,"final boss controller spawned");
        var boss=encounter.Boss!;encounter.Tick(2,true);boss.SetPhysicsProcess(false);
        int expectedAdds=mode=="Normal"?3:mode=="Hard"?12:23;
        Check(encounter.RegularEnemyCap(cap)==expectedAdds,"final boss add capacity follows difficulty");
        Near((float)encounter.CadenceMultiplier,mode=="Normal"?3:mode=="Hard"?1.25f:1,"final boss reinforcement cadence follows difficulty");
        Check(encounter.AllowRegularSpawn(cap),"final boss allows reinforcements after introduction");
        Near(boss.Health.Maximum,18000*tuning.EnemyHealth,"XRR-3 base health scaled once at reference level");
        Near(boss.LevelDamageMultiplier,tuning.EnemyDamage,"XRR-3 special damage multiplier");
        var target=new Combatant {Team=CombatTeam.Neutral};target.AddChild(new Health {Name="Health",Maximum=10000});world.AddChild(target);
        target.GlobalPosition=world.Player.GlobalPosition;boss.Target=target;
        Near(boss.DealAttack(30,DamageType.Kinetic),30*tuning.EnemyDamage,"actual special damage respects mode");
        var raid=boss.GetNode<Xrr3Attack>("Xrr3Attack");raid.Tick(10);
        Check(raid.Phase==BossPhase.Telegraph,"boss warns before striking");
        Near(raid.WarningSeconds,1.65f*tuning.WarningTime,"mode-specific boss warning duration");
        Near(raid.MechanicRadius,.19f*tuning.MechanicRadius,"missile radius follows difficulty");
        target.GlobalPosition+=new Vector3(0,0,-2);raid.Tick(raid.WarningSeconds+.01);
        Check(raid.Strikes==1 && raid.Dodged==1,"locked attack remains dodgeable");
        Check(raid.ActiveHazards==(mode=="Normal"?0:2),"missile fires restricted to advanced modes");
        raid.End();Check(raid.ActiveHazards==0,"encounter end clears persistent hazards");
        boss.Health.ApplyDamage(boss.Health.Maximum*.4f);Check(raid.PhaseNumber==2,"second phase remains at original threshold");
        boss.Health.ApplyDamage(boss.Health.Maximum*.3f);Check(raid.PhaseNumber==3,"final phase remains at original threshold");
        target.QueueFree();encounter.QueueFree();
        spawner.PlayerLevel=()=>world.Progression!.Level;
        world.ResetCombatWorld();
        Check(world.RunConfiguration.OperationDifficulty==mode && spawner.HardActiveLimit==cap && world.Progression!.Level==1,"restart retains difficulty and resets progression");
        var fresh=Spawn(sample);fresh.SetPhysicsProcess(false);
        Near(fresh.Health.Maximum,hp*tuning.EnemyHealth,"restart does not compound difficulty scaling");
        await world.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        GD.Print("OPERATION_DIFFICULTY "+mode+" PASS");
    }
}
