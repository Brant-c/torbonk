using SwtorSurvivors.Frontend;
namespace SwtorSurvivors.Integration;
public sealed record GameplayConfiguration(string FactionId,string BaseClassId,string AdvancedClassId,string MapId,
    string AppearancePresetId,string WorldScene,string EnvironmentScene,string CharacterScene)
{
    public ClassProfile Profile=>ClassProfile.Find(AdvancedClassId)!;
}
public static class SelectionGameplayAdapter
{
    private static GameplayConfiguration Configuration(ClassProfile p)=>new(p.Faction,p.BaseClass,p.Id,"korriban",p.Appearance,
        "res://Gameplay/GameWorld.tscn","res://Assets/Korriban/KorribanEnvironment.tscn",p.CharacterScene);
    public static GameplayConfiguration DevelopmentDefault=>Configuration(ClassProfile.Find("sorcerer")!);
    public static string? UnavailableReason(SelectionConfiguration selection)=>TryResolve(selection,out _,out string reason)?null:reason;
    public static bool TryResolve(SelectionConfiguration? selection,out GameplayConfiguration configuration,out string reason)
    {
        configuration=null!;reason="Choose a ready Imperial advanced class and Korriban.";
        if(selection==null || selection.SchemaVersion!=1 || selection.MapId!="korriban")return false;
        var p=ClassProfile.Find(selection.AdvancedClassId);
        if(p==null || p.Faction!=selection.FactionId || p.BaseClass!=selection.BaseClassId || !p.Ready)return false;
        if(!selection.Appearance.IsSupportedPreset || selection.Appearance.PresetId!=p.Appearance)
        {reason="The selected appearance does not match this class profile.";return false;}
        configuration=Configuration(p);reason="";return true;
    }
}
