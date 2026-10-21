using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors.Equipment;

/// <summary>Inventory/loot integration boundary. No actor, health or animation dependency.</summary>
public sealed class EquipmentLoadout
{
    public EquipmentCatalog Catalog { get; }
    public CharacterEquipmentContext Context { get; }
    private EquipmentSnapshot _current = new(new(),new(),[]);
    public EquipmentSnapshot Current => _current.Copy();
    public event Action<EquipmentSnapshot>? Changed;
    // Visual staging must succeed before either state or its observers change.
    public Func<EquipmentSnapshot, string?>? ApplyAppearance { private get; set; }
    public EquipmentLoadout(EquipmentCatalog catalog, CharacterEquipmentContext context) { Catalog=catalog;Context=context; }
    private bool Supports(string slot,out string error)
    {
        bool supported=Context.Class.SupportedSlots.Contains(slot) && Catalog.Slots.Any(s=>s.Id==slot);
        error=supported ? "" : "Unsupported equipment slot: "+slot;return supported;
    }
    public bool TryEquip(string slot, string itemId, out string error)
    { if(!Supports(slot,out error))return false;var next=Current; next.Items[slot]=itemId; return TryApply(next,out error); }
    public bool TryUnequip(string slot, out string error)
    { if(!Supports(slot,out error))return false;var next=Current; next.Items.Remove(slot); return TryApply(next,out error); }
    public bool TrySetCosmetic(string slot, string? appearanceId, out string error)
    { if(!Supports(slot,out error))return false;var next=Current; if(appearanceId==null)next.Cosmetics.Remove(slot);else next.Cosmetics[slot]=appearanceId;return TryApply(next,out error); }
    public bool TryHide(string slot, bool hidden, out string error)
    { if(!Supports(slot,out error))return false;var next=Current; var slots=next.HiddenSlots.ToHashSet(); if(hidden)slots.Add(slot);else slots.Remove(slot);return TryApply(next with { HiddenSlots=slots.ToArray() },out error); }
    public bool TryPreset(string id, bool allowDeveloper, out string error)
    {
        var preset=Catalog.Presets.SingleOrDefault(p=>p.Id==id);
        if(preset==null || (preset.DeveloperOnly && !allowDeveloper)) { error="Unknown or developer-only preset: "+id;return false; }
        return TryApply(new(new(preset.Items),new(preset.Cosmetics),[]),out error);
    }
    public bool TryApply(EquipmentSnapshot proposed, out string error)
    {
        // Snapshot incoming dictionaries; callers and event subscribers cannot mutate committed state.
        var next=proposed.Copy(); error="";
        foreach(var slot in next.Items.Keys.Concat(next.Cosmetics.Keys).Concat(next.HiddenSlots).Distinct())
            if(!Context.Class.SupportedSlots.Contains(slot) || !Catalog.Slots.Any(s=>s.Id==slot))
            { error="Unsupported equipment slot: "+slot;return false; }
        foreach(var entry in next.Items)
        {
            var item=Catalog.Items.SingleOrDefault(i=>i.Id==entry.Value);var slot=Catalog.Slots.Single(s=>s.Id==entry.Key);
            if(item==null || item.Slot!=slot.Id || !slot.Categories.Contains(item.Category) || item.CosmeticOnly)
            { error="Item cannot be equipped in "+slot.Id+": "+entry.Value;return false; }
            if(!EquipmentCatalog.Compatible(item.Compatibility,Context,out error))return false;
            if(slot.Id is "main_hand" or "off_hand" && !Context.Class.AllowedWeapons.Contains(item.Category))
            { error="Weapon proficiency unavailable: "+item.Category;return false; }
            if(item.BlocksSlots.Any(next.Items.ContainsKey)) { error="Conflicting occupied slots for "+item.Id;return false; }
        }
        foreach(var slot in next.HiddenSlots)
            if(!Catalog.Slots.Single(s=>s.Id==slot).CanHide) { error="Slot cannot be hidden: "+slot;return false; }
        foreach(var entry in next.Cosmetics)
        {
            var visual=Catalog.Appearances.SingleOrDefault(a=>a.Id==entry.Value);
            if(visual==null || visual.Slot!=entry.Key) { error="Invalid cosmetic for "+entry.Key;return false; }
            if(!EquipmentCatalog.Compatible(visual.Compatibility,Context,out error))return false;
        }
        string? visualError=ApplyAppearance?.Invoke(next.Copy());
        if(visualError!=null) { error=visualError;return false; }
        _current=next; Changed?.Invoke(Current);return true;
    }
    // Consumers replace their equipment modifier group using this aggregate; cosmetics never enter it.
    public IReadOnlyDictionary<string,float> StatModifiers()
    {
        var totals=new Dictionary<string,float>();
        foreach(var id in _current.Items.Values)
            foreach(var modifier in Catalog.Items.Single(i=>i.Id==id).StatModifiers)
                totals[modifier.Key]=totals.GetValueOrDefault(modifier.Key)+modifier.Value;
        return totals;
    }
    public Dictionary<string,AppearanceDefinition> ResolveVisuals(EquipmentSnapshot state)
    {
        var result=new Dictionary<string,AppearanceDefinition>();
        foreach(var slot in Catalog.Slots.Where(s=>Context.Class.SupportedSlots.Contains(s.Id)))
        {
            if(state.HiddenSlots.Contains(slot.Id))continue;
            string id=state.Cosmetics.GetValueOrDefault(slot.Id) ?? "";
            if(id=="" && state.Items.TryGetValue(slot.Id,out var itemId)) id=Catalog.Items.Single(i=>i.Id==itemId).Appearance;
            if(id=="")id=Context.Rig.EmptyAppearances.GetValueOrDefault(slot.Id) ?? slot.EmptyAppearance;
            if(id!="")result[slot.Id]=Catalog.Appearances.Single(a=>a.Id==id);
        }
        // Authored multi-slot garments own covered regions, avoiding overlapping replacement meshes.
        var covered=result.Values.SelectMany(a=>a.CoversSlots).ToHashSet();
        foreach(string slot in covered)result.Remove(slot);
        return result;
    }
}
