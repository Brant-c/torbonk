using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;
/// <summary>Fair GCD admission; each ability advances independently of gestures and other effects.</summary>
public partial class AbilityController : Node
{
    [Export] public float GlobalCooldownSeconds { get; set; } = 1.5f;
    [Export] public int OffensiveLimit { get; set; } = 4;
    [Export] public int UtilityLimit { get; set; } = 2;
    public int Limit(AbilityCategory category) => Math.Max(0,category==AbilityCategory.Offensive?OffensiveLimit:UtilityLimit);
    public int Equipped(AbilityCategory category) => Abilities.Count(a=>a.Unlocked && !a.Definition.IsBasicAttack && a.Definition.Category==category);
    public bool AllowHybrid {get;set;}
    public bool Conflicts(AutomaticAbility ability) => !AllowHybrid && ability.Definition.ExclusiveWith!="" && Find(ability.Definition.ExclusiveWith)?.Unlocked==true;
    public bool CanUnlock(AutomaticAbility ability) => ability.Unlocked || !Conflicts(ability) && Equipped(ability.Definition.Category)<Limit(ability.Definition.Category);
    public bool Unlock(string id)
    {
        var ability=Find(id);if(ability==null || !CanUnlock(ability))return false;
        ability.SetUnlocked(true);return true;
    }
    public List<AutomaticAbility> Abilities { get; } = new();
    public TargetingService Targeting { get; set; } = null!;
    public double GlobalCooldownRemaining { get; private set; }
    public double GlobalCooldownDuration { get; private set; } = 1.5;
    // Off-GCD exceptions still do not all activate on the same frame.
    [Export] public float MinimumActivationSpacing { get; set; } = 0.18f;
    public AutomaticAbility? Active => Abilities.FirstOrDefault(a=>a.Casting);
    public AutomaticAbility? Find(string id) => Abilities.FirstOrDefault(a=>a.Definition.Id==id);
    public void ResetUnlocks() { foreach(var ability in Abilities) ability.SetUnlocked(ability.Definition.StartsUnlocked); }
    private int _next;
    private double _activationSpacing;
    public override void _PhysicsProcess(double delta)
    {
        GlobalCooldownRemaining=Math.Max(0,GlobalCooldownRemaining-delta);
        _activationSpacing=Math.Max(0,_activationSpacing-delta);
        foreach(var ability in Abilities) ability.Advance(delta);
        Targeting.Update(delta,false);
        if(_activationSpacing>0) return;
        for(int offset=0;offset<Abilities.Count;offset++)
        {
            int index=(_next+offset)%Abilities.Count;
            var ability=Abilities[index];
            if(ability.Definition.IsBasicAttack) continue;
            if(ability.Definition.UsesGlobalCooldown && GlobalCooldownRemaining>0) continue;
            if(!ability.TryBegin()) continue;
            foreach(var basic in Abilities.Where(a=>a.Definition.IsBasicAttack && a.Casting))basic.Interrupt();
            _next=(index+1)%Abilities.Count;
            _activationSpacing=Math.Max(0,MinimumActivationSpacing);
            // Never reset a running GCD for an exception which may trigger another GCD.
            if(ability.Definition.TriggersGlobalCooldown && GlobalCooldownRemaining<=0)
                GlobalCooldownRemaining=GlobalCooldownDuration=float.IsFinite(GlobalCooldownSeconds)?Math.Max(0,GlobalCooldownSeconds):1.5;
            break;
        }
        // Weapon attacks fill gaps after normal abilities have had first refusal.
        // Their low-priority gesture can be replaced immediately by a class ability.
        if(_activationSpacing>0 || Abilities.Any(a=>!a.Definition.IsBasicAttack && a.Casting))return;
        foreach(var basic in Abilities.Where(a=>a.Definition.IsBasicAttack))
            if(basic.TryBegin()){_activationSpacing=Math.Max(0,MinimumActivationSpacing);break;}
    }
    public void Abort() { foreach(var ability in Abilities) ability.Abort();Targeting.Clear();GlobalCooldownRemaining=0;_activationSpacing=0; }
    public void ResetCombat() { Abort();foreach(var ability in Abilities) ability.ResetCombat();_next=0; }
}
