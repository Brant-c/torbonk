using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;

public sealed class RunArtifactDefinition
{
    public string Id {get;set;}="";
    public string Name {get;set;}="";
    public string Icon {get;set;}="";
    public string Source {get;set;}="";
    public string Description {get;set;}="";
    public PowerRarity Rarity {get;set;}
    public int MinimumTier {get;set;}=1;
    public string RequiresAbility {get;set;}="";
    public string Effect {get;set;}="";
    public float Amount {get;set;}
    public float HealthCost {get;set;}
    public bool PowerItem {get;set;}
}
/// <summary>Temporary equipment and serialized encounter choices. Deliberately has no save or Collections dependency.</summary>
public partial class RunArtifacts : Node
{
    [Export] public int SlotLimit {get;set;}=3;
    [Export] public string CatalogPath {get;set;}="res://Gameplay/RunItems/artifacts.json";
    public GameWorld World {get;set;}=null!;
    public List<RunArtifactDefinition> Catalog {get;private set;}=new();
    public IReadOnlyList<RunArtifactDefinition> Equipped=>_equipped;
    public IReadOnlyList<RunArtifactDefinition> Offers=>_offers;
    public bool Selecting=>_offers.Count>0;
    public bool Pending=>Selecting || _pending.Count>0;
    public int SelectedIndex {get;private set;}=-1;
    public RunArtifactDefinition? Selected=>SelectedIndex>=0?_offers[SelectedIndex]:null;
    public bool Replacing=>Selected!=null && !Selected.PowerItem && _equipped.Count>=SlotLimit;
    public int RewardsOpened {get;private set;}
    public int RewardsClaimed {get;private set;}
    public int EncounterTier {get;private set;}
    public event Action? Changed;
    private readonly List<RunArtifactDefinition> _equipped=new(),_offers=new();
    private readonly HashSet<ulong> _paid=new();
    private readonly Queue<int> _pending=new();
    private readonly RandomNumberGenerator _random=new();
    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;_random.Randomize();
        Catalog=JsonSerializer.Deserialize<List<RunArtifactDefinition>>(FileAccess.GetFileAsString(CatalogPath))!;
        if(Catalog.Select(a=>a.Id).Distinct().Count()!=Catalog.Count)throw new InvalidOperationException("Duplicate run item IDs");
        World.Spawner!.EnemyKilled+=Defeated;
    }
    public void Defeated(EnemyAgent enemy)
    {
        if(enemy.Health.IsAlive || enemy.RewardTier<=0 || !World.PlayerActor.Alive || !_paid.Add(enemy.GetInstanceId()))return;
        _pending.Enqueue(enemy.RewardTier);
    }
    public bool Eligible(RunArtifactDefinition item,int tier) => item.MinimumTier<=tier &&
        !_equipped.Any(a=>a.Id==item.Id) && (item.RequiresAbility=="" || World.Abilities.Find(item.RequiresAbility)?.Unlocked==true);
    public override void _Process(double delta)
    {
        if(!World.PlayerActor.Alive) { if(Pending)Clear();return; }
        if(Selecting || _pending.Count==0 || World.Progression?.Selecting==true)return;
        EncounterTier=_pending.Dequeue();SelectedIndex=-1;
        var pool=Catalog.Where(a=>!a.PowerItem && Eligible(a,EncounterTier)).ToList();
        // Equipment is unique, powers provide useful choices when the build exhausts eligible items.
        if(pool.Count<3)pool.AddRange(Catalog.Where(a=>a.PowerItem && Eligible(a,EncounterTier)));
        while(_offers.Count<3 && pool.Count>0)
        { int i=_random.RandiRange(0,pool.Count-1);_offers.Add(pool[i]);pool.RemoveAt(i); }
        if(_offers.Count!=3)throw new InvalidOperationException("Run reward catalog must supply three distinct eligible choices");
        GetTree().Paused=true;World.Rig.SetControlsEnabled(false);RewardsOpened++;Changed?.Invoke();
    }
    public bool Choose(int index)
    {
        if(!Selecting || SelectedIndex>=0 || index<0 || index>=_offers.Count || !World.PlayerActor.Alive)return false;
        SelectedIndex=index;
        if(Replacing) { Changed?.Invoke();return true; }
        return Equip(-1);
    }
    public void Back() { if(!Replacing)return;SelectedIndex=-1;Changed?.Invoke(); }
    public bool Equip(int replace)
    {
        var item=Selected;if(item==null || !World.PlayerActor.Alive)return false;
        if(!item.PowerItem)
        {
            if(_equipped.Any(a=>a.Id==item.Id))return false;
            if(_equipped.Count>=SlotLimit)
            {
                if(replace<0 || replace>=_equipped.Count)return false;
                var old=_equipped[replace];_equipped[replace]=item;World.Build.EquipmentRemoved(old);
            }
            else _equipped.Add(item);
        }
        else if(item.Effect=="heal")World.PlayerActor.Health.ApplyHealing(item.Amount);
        else if(item.Effect=="shield")World.PlayerActor.Health.ApplyShield(item.Amount,10);
        else if(item.Effect=="recovery")foreach(var a in World.Abilities.Abilities)a.ReduceCooldown(item.Amount);
        RewardsClaimed++;_offers.Clear();SelectedIndex=-1;
        World.Progression!.Recalculate();World.Audio.PlayCue("upgrade_"+item.Rarity.ToString().ToLowerInvariant());
        GetTree().Paused=false;Changed?.Invoke();World.Progression.ResumePending();
        World.Rig.SetControlsEnabled(!GetTree().Paused);return true;
    }
    public void Clear()
    {
        _pending.Clear();_offers.Clear();_equipped.Clear();_paid.Clear();SelectedIndex=-1;
        RewardsOpened=RewardsClaimed=0;GetTree().Paused=false;Changed?.Invoke();
    }
    public override void _ExitTree() { if(GodotObject.IsInstanceValid(World.Spawner))World.Spawner!.EnemyKilled-=Defeated; }
}
