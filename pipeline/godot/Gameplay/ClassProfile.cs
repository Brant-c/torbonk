using Godot;
using System;
using System.Linq;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Game-local composition keys; source identities live in the asset receipts.</summary>
public sealed class ClassProfile
{
    public string Id {get;set;}="";
    public string Faction {get;set;}="empire";
    public string BaseClass {get;set;}="";
    public string Appearance {get;set;}="";
    public string EquipmentPreset {get;set;}="";
    public string Rig {get;set;}="april_bmnnew_cloth";
    public string Body {get;set;}="bmn";
    public string CharacterScene {get;set;}="res://Assets/sorcerer.glb";
    public string AnimationScene {get;set;}="res://Assets/D3/casts.glb";
    public string Idle {get;set;}="";
    public string Run {get;set;}="";
    public string StartingAbility {get;set;}="";
    public string BasicAttack {get;set;}="";
    public string Presentation {get;set;}="d3";
    public int UtilitySlots {get;set;} = -1;
    public int OffensiveSlots {get;set;} = -1;
    public float Health {get;set;}=100;
    public float Movement {get;set;}=1;
    public string[] AbilityCategories {get;set;}=["offensive","utility"];
    public string[] Kit {get;set;}=[];
    public string[] TalentPool {get;set;}=[];
    public string[] ArtifactEffects {get;set;}=["guardian","sacrifice","heal","shield","recovery"];
    public bool Ready => ResourceLoader.Exists(CharacterScene) && ResourceLoader.Exists(AnimationScene)
        && ResourceLoader.Exists($"res://Gameplay/Abilities/{StartingAbility}.tres")
        && (BasicAttack=="" || ResourceLoader.Exists($"res://Gameplay/Abilities/{BasicAttack}.tres") && ResourceLoader.Exists("res://Assets/BasicAttacks/casts.glb"))
        && Equipment.EquipmentCatalog.Load().Presets.FirstOrDefault(p=>p.Id==EquipmentPreset) is {} preset
        && preset.Items.Values.All(id=>Equipment.EquipmentCatalog.Load().Items.FirstOrDefault(i=>i.Id==id) is {} item
            && Equipment.EquipmentCatalog.Load().Appearances.FirstOrDefault(a=>a.Id==item.Appearance) is {} a && ResourceLoader.Exists(a.Scene));
    private static ClassProfile[]? _all;
    public static ClassProfile[] All => _all??=JsonSerializer.Deserialize<ClassProfile[]>(FileAccess.GetFileAsString("res://Gameplay/classes.json"))!;
    public static ClassProfile? Find(string id) => All.SingleOrDefault(p=>p.Id==id);
}
