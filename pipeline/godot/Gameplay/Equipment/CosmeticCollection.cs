using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors.Equipment;

// Save data has no functional item or stat fields. Profiles use the frontend's exact IDs.
public sealed class CosmeticProfile
{
    public string DefaultLoadout { get; set; } = "inquisitor_level1";
    public Dictionary<string,string> Equipped { get; set; } = new();
}
public sealed class CosmeticSave
{
    public int SchemaVersion { get; set; } = 1;
    public HashSet<string> Unlocked { get; set; } = new();
    public Dictionary<string,CosmeticProfile> Profiles { get; set; } = new();
}
public sealed class CosmeticCollection
{
    public const string SavePath = "user://cosmetic-collections.json";
    public EquipmentCatalog Catalog { get; } = EquipmentCatalog.Load();
    public CosmeticSave State { get; private set; } = new();
    public string Notice { get; private set; } = "";
    public static string Key(string baseClass,string advanced) => baseClass+"/"+advanced;
    public CharacterEquipmentContext Context(string baseClass,string advanced) => new(
        Catalog.Rigs.Single(r=>r.Id=="april_bmnnew_cloth"),
        Catalog.Classes.Single(c=>c.BaseClassId==baseClass && c.AdvancedClassId==advanced));
    public bool Available(string baseClass,string advanced) =>
        Catalog.Classes.Any(c=>c.BaseClassId==baseClass && c.AdvancedClassId==advanced && c.SupportedSlots.Length>0);
    public bool Compatible(AppearanceDefinition appearance, CharacterEquipmentContext context) =>
        context.Class.SupportedSlots.Contains(appearance.Slot)
        && EquipmentCatalog.Compatible(appearance.Compatibility,context,out _)
        && ResourceLoader.Exists(appearance.Scene);
    public string DefaultAppearance(string slot) => Catalog.Items.Single(i=>
        i.Id==Catalog.Presets.Single(p=>p.Id=="inquisitor_level1").Items[slot]).Appearance;
    public CosmeticProfile Profile(string baseClass,string advanced) => State.Profiles[Key(baseClass,advanced)];
    public CosmeticCollection()
    {
        try
        {
            if (FileAccess.FileExists(SavePath))
            {
                var saved=JsonSerializer.Deserialize<CosmeticSave>(FileAccess.GetFileAsString(SavePath));
                if(saved?.SchemaVersion==1) State=saved;
                else Notice="Collection save version unavailable; using defaults.";
            }
        }
        catch(Exception) { Notice="Collection save could not be read; using defaults."; }
        State.Unlocked ??= new(); State.Profiles ??= new();
        State.Unlocked.RemoveWhere(id=>!Catalog.Appearances.Any(a=>a.Id==id && ResourceLoader.Exists(a.Scene)));
        var starter=Catalog.Presets.Single(p=>p.Id=="inquisitor_level1");
        foreach(var id in starter.Items.Values.Select(id=>Catalog.Items.Single(i=>i.Id==id).Appearance)) State.Unlocked.Add(id);
        var known=Catalog.Classes.Select(c=>Key(c.BaseClassId,c.AdvancedClassId)).ToHashSet();
        foreach(var key in State.Profiles.Keys.Where(k=>!known.Contains(k)).ToArray()) State.Profiles.Remove(key);
        foreach(var cl in Catalog.Classes)
        {
            string key=Key(cl.BaseClassId,cl.AdvancedClassId);
            if(!State.Profiles.TryGetValue(key,out var profile) || profile==null) State.Profiles[key]=profile=new();
            profile.DefaultLoadout=cl.SupportedSlots.Length>0 ? starter.Id : "";
            profile.Equipped ??= new();
            var context=Context(cl.BaseClassId,cl.AdvancedClassId);
            foreach(var slot in profile.Equipped.Keys.ToArray())
            {
                var a=Catalog.Appearances.SingleOrDefault(a=>a.Id==profile.Equipped[slot] && a.Slot==slot);
                if(a==null || !State.Unlocked.Contains(a.Id) || !Compatible(a,context)) profile.Equipped.Remove(slot);
            }
        }
    }
    public bool Save(out string error)
    {
        try
        {
            string path=ProjectSettings.GlobalizePath(SavePath);
            System.IO.File.WriteAllText(path+".tmp",JsonSerializer.Serialize(State,new JsonSerializerOptions {WriteIndented=true}));
            System.IO.File.Move(path+".tmp",path,true); error="";return true;
        }
        catch(Exception ex) { error="Could not save collection: "+ex.Message;return false; }
    }
    public void Apply(EquipmentLoadout loadout,string baseClass,string advanced)
    {
        // Keep the authored functional starter items; only the cosmetic dictionary is restored.
        foreach(var entry in Profile(baseClass,advanced).Equipped)
            loadout.TrySetCosmetic(entry.Key,entry.Value,out _); // Staging failures leave that slot at its starter fallback.
    }
    public bool DeveloperUnlock(out string error)
    {
        if(!OS.GetCmdlineUserArgs().Contains("--wardrobe-dev")) {error="Developer option required.";return false;}
        var before=new HashSet<string>(State.Unlocked);
        foreach(var id in Catalog.Presets.Single(p=>p.Id=="sorcerer_ss_a04").Cosmetics.Values) State.Unlocked.Add(id);
        if(Save(out error))return true;State.Unlocked=before;return false;
    }
    public static void ApplySaved(EquipmentLoadout loadout,string baseClass,string advanced) =>
        new CosmeticCollection().Apply(loadout,baseClass,advanced);
}
