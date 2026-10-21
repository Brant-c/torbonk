using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors.Equipment;

// Local IDs and source IDs deliberately occupy different fields/namespaces.
public sealed record SourceIdentity(string GomId, string Name, string Evidence);
public sealed record Compatibility(string[] RigProfiles, string[] BodyTypes, string[] Species,
    string[] BaseClasses, string[] AdvancedClasses, string[] AnimationProfiles);
public sealed record SlotDefinition(string Id, string[] Categories, string EmptyAppearance, bool CanHide);
public sealed record ItemDefinition(string Id, string DisplayName, string Slot, string Category, string Rarity,
    Compatibility Compatibility, SourceIdentity? Source, string Appearance, string Icon,
    Dictionary<string, float> StatModifiers, bool CosmeticOnly, string[] BlocksSlots);
public sealed record AttachmentDefinition(string Bone, string CorrectionResource, float[] Position, float[] RotationDegrees, float Scale, bool RedBlade);
public sealed record LocomotionDefinition(string LibraryId, string Scene, string Idle, string Run);
public sealed record AppearanceDefinition(string Id, string Slot, Compatibility Compatibility, string Scene,
    string[] MeshNames, string[] RequiredBones, string[] CoversSlots, string[] HidesBaseGroups,
    AttachmentDefinition? Attachment, LocomotionDefinition? Locomotion = null);
public sealed record EquipmentPreset(string Id, string DisplayName, Dictionary<string, string> Items,
    Dictionary<string, string> Cosmetics, bool DeveloperOnly);
public sealed record RigProfile(string Id, string RigFamily, string BodyType, string Species, string AnimationProfile,
    string BaseScene, Dictionary<string, string[]> BaseGroups, Dictionary<string, string> AttachmentBones,
    Dictionary<string, string> EmptyAppearances);
public sealed record ClassEquipmentProfile(string BaseClassId, string AdvancedClassId, string[] AllowedWeapons, string[] SupportedSlots);
public sealed record CharacterEquipmentContext(RigProfile Rig, ClassEquipmentProfile Class);
public sealed record EquipmentSnapshot(Dictionary<string, string> Items, Dictionary<string, string> Cosmetics, string[] HiddenSlots)
{
    public EquipmentSnapshot Copy() => new(new(Items), new(Cosmetics), (string[])HiddenSlots.Clone());
}

public sealed class EquipmentCatalog
{
    public SlotDefinition[] Slots { get; set; } = [];
    public ItemDefinition[] Items { get; set; } = [];
    public AppearanceDefinition[] Appearances { get; set; } = [];
    public EquipmentPreset[] Presets { get; set; } = [];
    public RigProfile[] Rigs { get; set; } = [];
    public ClassEquipmentProfile[] Classes { get; set; } = [];
    public static EquipmentCatalog Load() => JsonSerializer.Deserialize<EquipmentCatalog>(
        FileAccess.GetFileAsString("res://Gameplay/Equipment/catalog.json"))!;

    public static bool Compatible(Compatibility c, CharacterEquipmentContext context, out string error)
    {
        var r=context.Rig; var cl=context.Class;
        bool Match(string[] allowed, string value) => allowed.Length==0 || allowed.Contains(value);
        error = $"Requires rig [{string.Join(',',c.RigProfiles)}], body [{string.Join(',',c.BodyTypes)}], species [{string.Join(',',c.Species)}], animation [{string.Join(',',c.AnimationProfiles)}], class [{string.Join(',',c.BaseClasses)}], advanced [{string.Join(',',c.AdvancedClasses)}]; received {r.Id}/{r.BodyType}/{r.Species}/{r.AnimationProfile}/{cl.BaseClassId}/{cl.AdvancedClassId}.";
        bool pass=Match(c.RigProfiles,r.Id) && Match(c.BodyTypes,r.BodyType) && Match(c.Species,r.Species)
            && Match(c.BaseClasses,cl.BaseClassId) && Match(c.AdvancedClasses,cl.AdvancedClassId) && Match(c.AnimationProfiles,r.AnimationProfile);
        if(pass)error=""; return pass;
    }
}
