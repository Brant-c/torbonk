using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors.Equipment;

public sealed class EvLootSettings
{
    public int MobCreditsMin {get;set;}=3;
    public int MobCreditsMax {get;set;}=9;
    public int MinibossCredits {get;set;}=150;
    public int BossCredits {get;set;}=500;
    public int BossRolls {get;set;}=2;
    public double MinibossDropChance {get;set;}=.35;
    public double CompatibleWeight {get;set;}=4;
    public double NightmareRank3Chance {get;set;}=.15;
    public int DuplicateCredits {get;set;}=200;
}
// Encounter completion, rather than individual group members, owns equipment rolls.
public partial class EvLoot : Node
{
    public GameWorld World {get;set;}=null!;
    private readonly HashSet<string> _awarded=new();
    private readonly HashSet<EnemyAgent> _killed=new();
    private readonly List<CreditPickup> _credits=new();
    public int PendingCredits=>_credits.Where(p=>GodotObject.IsInstanceValid(p) && !p.Collecting).Sum(p=>p.Value);
    private readonly Random _random=new();
    private EvLootSettings _settings=null!;
    private PersistentInventory Inventory=>UI.EquipmentWindows.Instance.Inventory;
    public override void _Ready()
    {
        _settings=JsonSerializer.Deserialize<EvLootSettings>(FileAccess.GetFileAsString("res://Gameplay/Equipment/ev-loot.json"))!;
        World.Spawner!.EnemyKilled+=OnKill;
        World.Waves!.Encounter.StateChanged+=OnEncounter;
    }
    public void ResetRun(){_awarded.Clear();_killed.Clear();foreach(var p in GetChildren().OfType<CreditPickup>())p.QueueFree();_credits.Clear();}
    public void DropCredits(Vector3 position,int amount)
    {
        if(amount<=0)return;
        _credits.RemoveAll(p=>!GodotObject.IsInstanceValid(p) || p.Collecting);
        if(_credits.Count>=128){_credits.OrderBy(p=>p.GlobalPosition.DistanceSquaredTo(position)).First().Value+=amount;return;}
        var pickup=new CreditPickup {World=World,Value=amount,SaveCredits=SaveCredits};
        AddChild(pickup);pickup.GlobalPosition=position+Vector3.Up*.035f;_credits.Add(pickup);
    }
    private bool SaveCredits(int amount)
    {
        bool saved=Inventory.Award(amount,Array.Empty<(string,int)>(),0,out _);
        if(!saved)World.Rewards!.Announce("LOOT SAVE FAILED",Inventory.Notice);
        return saved;
    }
    private void OnKill(EnemyAgent enemy)
    {
        if(!_killed.Add(enemy) || enemy.IsEncounterMember)return;
        DropCredits(enemy.GlobalPosition,_random.Next(_settings.MobCreditsMin,_settings.MobCreditsMax+1));
    }
    private void OnEncounter(EncounterState state)
    {
        if(state!=EncounterState.Defeated || World.RunConfiguration.MapId is not ("eternity_vault" or "soa_p3"))return;
        string id=World.Waves!.Encounter.Definition.Id;
        if(!_awarded.Add(id))return;
        bool boss=id is "eternity_vault.xrr3" or "eternity_vault.soa" or "soa.soa";
        int credits=boss?_settings.BossCredits:_settings.MinibossCredits;
        int rolls=boss?_settings.BossRolls:(_random.NextDouble()<_settings.MinibossDropChance?1:0);
        string cls=World.RunConfiguration.AdvancedClassId;
        var pool=Inventory.Items.Where(i=>i.Kind=="relic" && PersistentInventory.VisibleForClass(i,cls)).ToArray();
        var drops=new List<(string id,int rank)>();
        for(int n=0;n<rolls && pool.Length>0;n++) {
            double Weight(Accessory i)=>PersistentInventory.Compatible(i,cls)?_settings.CompatibleWeight:1;
            double pick=_random.NextDouble()*pool.Sum(Weight);
            var item=pool.Last();foreach(var candidate in pool){pick-=Weight(candidate);if(pick<=0){item=candidate;break;}}
            int rank=World.RunConfiguration.OperationDifficulty switch {"Normal"=>0,"Hard"=>1,_=>_random.NextDouble()<_settings.NightmareRank3Chance?3:2};
            drops.Add((item.Id,rank));
        }
        if(Inventory.Award(0,drops,_settings.DuplicateCredits,out var lines)) {
            DropCredits(World.Waves.Encounter.Boss!.GlobalPosition,credits);
            World.Rewards!.Announce("LOOT ACQUIRED",credits+" Credits dropped", "Move near the green Credit icon to collect");
            for(int n=0;n<lines.Length;n++)World.Rewards.AnnounceItem(lines[n],Inventory.Items.Single(i=>i.Id==drops[n].id));
        } else World.Rewards!.Announce("LOOT SAVE FAILED",Inventory.Notice);
    }
    public override void _ExitTree()
    {
        if(GodotObject.IsInstanceValid(World.Spawner))World.Spawner!.EnemyKilled-=OnKill;
        if(GodotObject.IsInstanceValid(World.Waves))World.Waves!.Encounter.StateChanged-=OnEncounter;
    }
}
