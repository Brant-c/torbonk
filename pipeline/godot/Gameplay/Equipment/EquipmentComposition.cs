using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors.Equipment;

/// <summary>Content binding; generic assembly and loadout contain no class-ID branches.</summary>
public static class EquipmentComposition
{
    public static CharacterAppearanceAssembly Create(Node parent,Node3D model,Skeleton3D skeleton,string baseClass,string advancedClass)
    {
        var catalog=EquipmentCatalog.Load();
        var context=new CharacterEquipmentContext(catalog.Rigs.Single(r=>r.Id=="april_bmnnew_cloth"),
            catalog.Classes.Single(c=>c.BaseClassId==baseClass && c.AdvancedClassId==advancedClass));
        var assembly=new CharacterAppearanceAssembly { Name="CharacterEquipment" };parent.AddChild(assembly);
        assembly.Initialize(model,skeleton,catalog,context);
        if(!assembly.Loadout.TryPreset("inquisitor_level1",false,out string error))throw new InvalidOperationException(error);
        CosmeticCollection.ApplySaved(assembly.Loadout,baseClass,advancedClass);
        return assembly;
    }
}
