using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors.Frontend;

/// <summary>Portable integration contract. This class has no Godot or gameplay dependency.</summary>
public sealed class SelectionConfiguration
{
    public int SchemaVersion { get; set; } = 1;
    public string FactionId { get; set; } = "empire";
    public string BaseClassId { get; set; } = "sith_inquisitor";
    public string AdvancedClassId { get; set; } = "sorcerer";
    public AppearanceConfiguration Appearance { get; set; } = new();
    public string MapId { get; set; } = "korriban";
    // Never deserialize a cached playable flag as authority. Always derive from current content.
    public bool Playable => Integration.SelectionGameplayAdapter.TryResolve(this,out _,out _);
    public string PlayableStatus => Playable ? "content_available" : "preview_only";
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    public SelectionConfiguration Snapshot() => JsonSerializer.Deserialize<SelectionConfiguration>(ToJson())!;
}

public sealed class AppearanceConfiguration
{
    public string PresetId { get; set; } = "inquisitor_level1_april2012_bmn";
    public string SpeciesId { get; set; } = "human";
    public string BodyPresentationId { get; set; } = "male";
    public string BodyTypeId { get; set; } = "bmn";
    public string HeadId { get; set; } = "head_human_bmn_caucasian_a01";
    public string HairId { get; set; } = "hair_human_bmn_non_a01";
    public string SkinToneId { get; set; } = "source_material_a01c01";
    public string ComplexionId { get; set; } = "source_default";
    public string EquipmentAppearanceId { get; set; } = "inq_starting_cdps1_t1x1";
    public static AppearanceConfiguration ForClass(string advanced)
    {
        var p=ClassProfile.Find(advanced);
        return p==null?new():new() {PresetId=p.Appearance,EquipmentAppearanceId=p.EquipmentPreset};
    }
    public bool IsSupportedPreset => ClassProfile.All.Any(p=>p.Appearance==PresetId &&
        (p.EquipmentPreset==EquipmentAppearanceId || p.Id=="sorcerer" && EquipmentAppearanceId=="inq_starting_cdps1_t1x1")) && SpeciesId == "human"
        && BodyPresentationId == "male" && BodyTypeId == "bmn" && HeadId == "head_human_bmn_caucasian_a01"
        && HairId == "hair_human_bmn_non_a01" && SkinToneId == "source_material_a01c01"
        && ComplexionId == "source_default";
}
