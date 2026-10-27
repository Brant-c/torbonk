using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SwtorSurvivors.Talents;

/// <summary>Independent pregame loadouts. Never writes Collections or run artifact state.</summary>
public static class TalentProfiles
{
    private sealed class SaveData
    {
        public int SchemaVersion {get;set;}=1;
        public Dictionary<string,Dictionary<string,int>> Profiles {get;set;}=new();
    }
    public static string SavePath=>ProjectSettings.GlobalizePath("user://talent-profiles.json");
    private static SaveData Read()
    {
        if(!File.Exists(SavePath))return new();
        var data=JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath));
        if(data?.SchemaVersion!=1 || data.Profiles==null)throw new InvalidDataException("Unsupported talent save version");
        return data;
    }
    public static Dictionary<string,int> Load(string classId)
    {
        try{return TalentCatalog.Current.Validate(classId,Read().Profiles.GetValueOrDefault(classId));}
        catch(Exception e) when(e is IOException or JsonException or UnauthorizedAccessException)
        {GD.PushWarning("Talent profile ignored: "+e.Message);return new();}
    }
    public static string? Save(string classId,IReadOnlyDictionary<string,int> ranks)
    {
        try
        {
            var data=Read();data.Profiles[classId]=TalentCatalog.Current.Validate(classId,ranks);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            File.WriteAllText(SavePath+".tmp",JsonSerializer.Serialize(data,new JsonSerializerOptions {WriteIndented=true}));
            File.Move(SavePath+".tmp",SavePath,true);
            return null;
        }
        catch(Exception e) when(e is IOException or JsonException or UnauthorizedAccessException)
        {return "Build could not be saved: "+e.Message;}
    }
}
