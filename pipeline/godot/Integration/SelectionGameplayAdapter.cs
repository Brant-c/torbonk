using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public sealed record GameplayConfiguration(string FactionId,string BaseClassId,string AdvancedClassId,string MapId,
    string AppearancePresetId,string WorldScene,string EnvironmentScene,string CharacterScene)
{
    public ClassProfile Profile=>ClassProfile.Find(AdvancedClassId)!;
}
public static class SelectionGameplayAdapter
{
    private static GameplayConfiguration Configuration(ClassProfile p,PlayableMap map)=>new(p.Faction,p.BaseClass,p.Id,map.Id,p.Appearance,
        map.WorldScene,map.EnvironmentScene,p.CharacterScene);
    public static GameplayConfiguration DevelopmentDefault=>Configuration(ClassProfile.Find("sorcerer")!,MapRegistry.Find("korriban")!);
    public static string? UnavailableReason(SelectionConfiguration selection)=>TryResolve(selection,out _,out string reason)?null:reason;
    public static bool TryResolve(SelectionConfiguration? selection,out GameplayConfiguration configuration,out string reason)
    {
        configuration=null!;reason="Choose a ready Imperial advanced class and a playable destination.";
        if(selection==null || selection.SchemaVersion!=1)return false;
        var map=MapRegistry.Find(selection.MapId);
        if(map==null)return false;
        var p=ClassProfile.Find(selection.AdvancedClassId);
        if(p==null || p.Faction!=selection.FactionId || p.BaseClass!=selection.BaseClassId || !p.Ready)return false;
        if(!selection.Appearance.IsSupportedPreset || selection.Appearance.PresetId!=p.Appearance)
        {reason="The selected appearance does not match this class profile.";return false;}
        if(map.FactionId!=null && map.FactionId!=p.Faction){reason="This destination is not open to the selected faction.";return false;}
        configuration=Configuration(p,map);reason="";return true;
    }
}
