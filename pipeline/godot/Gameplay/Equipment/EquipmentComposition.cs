using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors.Equipment;

/// <summary>Content binding; generic assembly and loadout contain no class-ID branches.</summary>
public static class EquipmentComposition
{
    public static CharacterEquipmentContext Context(EquipmentCatalog catalog,string baseClass,string advancedClass)
    {
        var profile=ClassProfile.Find(advancedClass);
        var rig=catalog.Rigs.Single(r=>r.Id==(profile?.Rig??"april_bmnnew_cloth"));
        if(profile!=null)
        {
            var preset=catalog.Presets.Single(p=>p.Id==profile.EquipmentPreset);
            rig=rig with { EmptyAppearances=preset.Items.Where(i=>i.Key is not ("main_hand" or "off_hand"))
                .ToDictionary(i=>i.Key,i=>catalog.Items.Single(item=>item.Id==i.Value).Appearance) };
        }
        return new(rig,catalog.Classes.Single(c=>c.BaseClassId==baseClass && c.AdvancedClassId==advancedClass));
    }
    public static LocomotionDefinition? Locomotion(CharacterAppearanceAssembly assembly,ClassProfile profile)=>profile.Idle!=""
        ? new("D3",profile.AnimationScene,profile.Idle,profile.Run):assembly.Locomotion;
    public static CharacterAppearanceAssembly Create(Node parent,Node3D model,Skeleton3D skeleton,string baseClass,string advancedClass)
    {
        var catalog=EquipmentCatalog.Load();
        var context=Context(catalog,baseClass,advancedClass);
        var assembly=new CharacterAppearanceAssembly { Name="CharacterEquipment" };parent.AddChild(assembly);
        assembly.Initialize(model,skeleton,catalog,context);
        var profile=ClassProfile.Find(advancedClass)??throw new InvalidOperationException("No playable class profile: "+advancedClass);
        if(!assembly.Loadout.TryPreset(profile.EquipmentPreset,false,out string error))throw new InvalidOperationException(error);
        CosmeticCollection.ApplySaved(assembly.Loadout,baseClass,advancedClass);
        return assembly;
    }
}
