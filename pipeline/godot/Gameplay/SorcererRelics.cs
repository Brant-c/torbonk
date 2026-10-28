using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;

// Equipment remains the sole owner of persistent ranks and slot admission.
public static class SorcererRelics
{
    public static int Rank(AutomaticAbility ability,string id)
    {
        if(ability.Build?.World.RunConfiguration.AdvancedClassId!="sorcerer" || ability.Caster!=ability.Build.World.PlayerActor)return -1;
        var inventory=UI.EquipmentWindows.Instance?.Inventory;
        if(inventory==null)return -1;
        return PersistentInventory.Slots.Where(s=>s.StartsWith("relic")).Any(s=>inventory.Equipped("sorcerer",s)==id)
            ?inventory.State.Owned.GetValueOrDefault(id,-1):-1;
    }
}
