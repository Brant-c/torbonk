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
    public int Equipped(AbilityCategory category) => Abilities.Count(a=>a.Unlocked && !a.Definition.IsBasicAttack && !a.Definition.Slotless && !a.InheritsFreeSlot && a.Definition.Category==category);
    public bool AllowHybrid {get;set;}
    public bool Conflicts(AutomaticAbility ability) => !AllowHybrid && ability.Definition.ExclusiveWith!="" && Find(ability.Definition.ExclusiveWith)?.Unlocked==true;
    public bool CanUnlock(AutomaticAbility ability) => ability.Unlocked || ability.Definition.Slotless || !Conflicts(ability) && (ability.InheritsFreeSlot || Equipped(ability.Definition.Category)<Limit(ability.Definition.Category));
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
    public AutomaticAbility? Active => Abilities.FirstOrDefault(a=>a.BlocksCasting);
    public AutomaticAbility[] QuickbarAbilities=>Abilities.Where(a=>a.Unlocked).OrderBy(a=>a.Definition.IsBasicAttack?0:1).Take(12).ToArray();
    public AutomaticAbility? Find(string id) => Abilities.FirstOrDefault(a=>a.Definition.Id==id);
    public void ResetUnlocks() { foreach(var ability in Abilities) {ability.InheritsFreeSlot=false;if(ability.Definition.Id=="lacerate")ability.Definition.Slotless=true;ability.SetUnlocked(ability.Definition.StartsUnlocked);} }
    private int ActivationPriority(AutomaticAbility ability)
    {
        var build=ability.Build;
        if(build?.World.RunConfiguration.AdvancedClassId=="marauder")return build.Pregame.MarauderPriority(ability);
        if(build?.World.RunConfiguration.AdvancedClassId=="assassin") {
            if(ability.Definition.Id=="maul" && build.Pregame.StealthOpening)return -4;
            if(ability.Definition.Id=="shock" && build.Pregame.VoltaicCharges>0)return -2;
            if(ability.Definition.Id=="lacerate")return 1;
        }
        return AllowHybrid && ability.Definition.Id=="lightning"?1:0;
    }
    private int _next;
    private double _activationSpacing;
    public override void _PhysicsProcess(double delta)
    {
        GlobalCooldownRemaining=Math.Max(0,GlobalCooldownRemaining-delta*(Abilities.FirstOrDefault()?.Build?.AssassinRelics?.Haste??1)*(Abilities.FirstOrDefault()?.Build?.MarauderRelics?.Acceleration??1));
        _activationSpacing=Math.Max(0,_activationSpacing-delta);
        foreach(var ability in Abilities) ability.Advance(delta);
        Targeting.Update(delta,false);
        if(_activationSpacing>0 || Abilities.Any(a=>a.Definition.Id=="spike" && a.State==LightningState.Starting)) return;
        var order=Enumerable.Range(0,Abilities.Count).Select(offset=>(_next+offset)%Abilities.Count)
            .OrderBy(i=>ActivationPriority(Abilities[i])).ToArray();
        foreach(int index in order)
        {
            var ability=Abilities[index];
            if(ability.Definition.IsBasicAttack) continue;
            if(ability.Definition.UsesGlobalCooldown && GlobalCooldownRemaining>0) continue;
            if(ability.Definition.Id=="lightning" && Abilities.Any(a=>a!=ability && a.BlocksCasting))continue;
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
        if(_activationSpacing>0 || Abilities.Any(a=>!a.Definition.IsBasicAttack && a.BlocksCasting))return;
        foreach(var basic in Abilities.Where(a=>a.Definition.IsBasicAttack))
            if(basic.TryBegin()){_activationSpacing=Math.Max(0,MinimumActivationSpacing);break;}
    }
    public void Abort() { foreach(var ability in Abilities) ability.Abort();Targeting.Clear();GlobalCooldownRemaining=0;_activationSpacing=0; }
    public void ResetCombat() { Abort();foreach(var ability in Abilities) ability.ResetCombat();_next=0; }
}
