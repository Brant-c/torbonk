using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Godot;
namespace SwtorSurvivors.Equipment;

public sealed record Accessory(string Id, string Name, string Kind, string Icon, string Rarity, string[] Classes, string Description)
{
    public string[] Stats {get;init;}=[];
    public string[] Effects {get;init;}=[];
}
public sealed class InventorySave
{
    public int Version { get; set; } = 1;
    public int Credits { get; set; } = 10000;
    public Dictionary<string,int> Owned { get; set; } = new();
    public Dictionary<string,Dictionary<string,string>> Loadouts { get; set; } = new();
}
// Separate from Collections, run rewards and artifact state. Mutations commit before becoming visible.
public sealed class PersistentInventory
{
    public static readonly string[] Slots = ["implant1","implant2","earpiece","relic1","relic2"];
    public static string Kind(string slot) => slot.StartsWith("implant") ? "implant" : slot.StartsWith("relic") ? "relic" : "earpiece";
    public Accessory[] Items { get; }
    public int[] Costs { get; }
    public InventorySave State { get; private set; }
    public string Notice { get; private set; } = "";
    private readonly string _path;
    public PersistentInventory(string? path=null)
    {
        _path=path ?? ProjectSettings.GlobalizePath("user://equipment-v1.json");
        var config=JsonSerializer.Deserialize<AccessoryCatalog>(Godot.FileAccess.GetFileAsString("res://Gameplay/Equipment/accessories.json"))!;
        Items=config.Items;Costs=config.UpgradeCosts;
        if(File.Exists(_path))
        {
            try { State=JsonSerializer.Deserialize<InventorySave>(File.ReadAllText(_path))!;
                if(State==null || State.Version!=1 || State.Credits<0 || State.Owned==null || State.Loadouts==null || State.Owned.Any(p=>!Items.Any(i=>i.Id==p.Key) || p.Value<0 || p.Value>3) || State.Loadouts.Any(p=>p.Value==null || p.Value.Any(s=>!Slots.Contains(s.Key) || !State.Owned.ContainsKey(s.Value) || Kind(s.Key)!=Items.Single(i=>i.Id==s.Value).Kind || !Compatible(Items.Single(i=>i.Id==s.Value),p.Key)) || p.Value.Values.Distinct().Count()!=p.Value.Count)) throw new InvalidDataException("Invalid equipment profile");
            } catch(Exception e) { State=new(); Notice="Equipment profile could not be loaded. Original file preserved: "+e.Message; _blocked=true; }
        }
        else { State=new(); foreach(var i in Items) State.Owned[i.Id]=0; Commit(State); }
    }
    private bool _blocked;
    public static bool Compatible(Accessory item,string cls)=>item.Classes.Length==0 || item.Classes.Contains(cls);
    public string Equipped(string cls,string slot)=>State.Loadouts.GetValueOrDefault(cls)?.GetValueOrDefault(slot) ?? "";
    public IEnumerable<Accessory> BagItems(string cls)=>Items.Where(i=>State.Owned.ContainsKey(i.Id) && State.Loadouts.GetValueOrDefault(cls)?.ContainsValue(i.Id)!=true);
    public bool Equip(string cls,string slot,string id)
    {
        var item=Items.FirstOrDefault(i=>i.Id==id);
        if(!Slots.Contains(slot) || item==null || !State.Owned.ContainsKey(id) || Kind(slot)!=item.Kind || !Compatible(item,cls))return false;
        return Change(s=>{if(!s.Loadouts.ContainsKey(cls))s.Loadouts[cls]=new();var l=s.Loadouts[cls];foreach(var key in l.Where(p=>p.Value==id).Select(p=>p.Key).ToArray())l.Remove(key);l[slot]=id;});
    }
    public bool Unequip(string cls,string slot)=>Slots.Contains(slot) && Change(s=>{if(s.Loadouts.TryGetValue(cls,out var l))l.Remove(slot);});
    public bool Upgrade(string id)=>State.Owned.TryGetValue(id,out int rank) && rank<3 && State.Credits>=Costs[rank] && Change(s=>{s.Credits-=Costs[rank];s.Owned[id]++;});
    private bool Change(Action<InventorySave> change){var next=JsonSerializer.Deserialize<InventorySave>(JsonSerializer.Serialize(State))!;change(next);return Commit(next);}
    private bool Commit(InventorySave next)
    {
        if(_blocked)return false;
        try {Directory.CreateDirectory(Path.GetDirectoryName(_path)!);File.WriteAllText(_path+".tmp",JsonSerializer.Serialize(next,new JsonSerializerOptions{WriteIndented=true}));File.Move(_path+".tmp",_path,true);State=next;Notice="Saved";return true;}
        catch(Exception e){Notice="Save failed: "+e.Message;return false;}
    }
}
public sealed class AccessoryCatalog {public Accessory[] Items {get;set;}=[];public int[] UpgradeCosts {get;set;}=[500,1500,3000];}



