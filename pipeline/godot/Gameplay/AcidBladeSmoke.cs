using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class AgentExpansionSmoke
{
    private void CheckAcidBlade(GameWorld w,Combatant first,Combatant second,Action<string,bool> check)
    {
        var previous=w.Build.Pregame;var prop=typeof(RunBuildEffects).GetProperty(nameof(RunBuildEffects.Pregame))!;
        var dart=w.Abilities.Find("dart")!;var backstab=w.Abilities.Find("backstab")!;
        var grenade=w.Abilities.Find("corrosivegrenade")!;
        var visual=(D3AbilityPresentation)backstab.Presentation;
        Combatant? foreign=null;AutomaticAbility? foreignDart=null;
        try{
            TalentProfiles.Save("operative",new Dictionary<string,int>{{"concealment.acid_blade",1},{"concealment.revitalizers",1}});
            var acid=new TalentRuntime(w);prop.SetValue(w.Build,acid);w.Abilities.ResetUnlocks();w.Progression!.ResetRun();w.Periodic.RemoveSource(dart);w.Periodic.RemoveSource(grenade);
            check("Acid Blade works with the manually cast Dart replaced",!dart.Unlocked);
            float h=first.Health.Current;int bursts=visual.AcidBladeDetonationsPresented;
            acid.DirectHit(backstab,first);
            check("unpoisoned target receives Dart without a burst",first.Health.Current==h && w.Periodic.Has(dart,first) && visual.AcidBladeDetonationsPresented==bursts);
            w.Periodic.Advance(1.05);
            float perTick=dart.Definition.PeriodicDamage*dart.DamagePerTick/dart.Definition.Damage;
            float expected=7*perTick*w.Build.DamageMultiplier(dart,first)*acid.PeriodicMultiplier(first);
            h=first.Health.Current;string last="";
            void Record(HealthChange change){if(change.Kind==CombatEventKind.Damage)last=change.AbilityId;}
            first.Health.Resolved+=Record;
            acid.DirectHit(backstab,first);first.Health.Resolved-=Record;
            check("existing Dart bursts for exactly its seven remaining ticks",Math.Abs(h-first.Health.Current-expected)<.01);
            check("burst is credited to Acid Blade and presents a corrosive explosion",last=="acid_blade" && visual.AcidBladeDetonationsPresented==bursts+1);
            check("burst reapplies one full-duration Dart",w.Periodic.On(first).Count(a=>a==dart)==1 && Math.Abs(w.Periodic.Remaining(dart,first)-acid.PeriodicDuration(dart))<.0001);
            h=first.Health.Current;w.Periodic.Advance(.5);check("reapplied Dart starts a fresh tick clock",first.Health.Current==h);
            w.Periodic.Advance(.51);check("reapplied poison continues dealing damage",first.Health.Current<h);

            w.Periodic.RemoveSource(dart);w.Periodic.Apply(grenade,first,relicApplied:true);acid.DirectHit(backstab,first);acid.DirectHit(backstab,first);
            check("Acid Blade consumes Dart without consuming Corrosive Grenade",w.Periodic.Has(grenade,first));w.Periodic.RemoveSource(grenade);

            foreign=new Combatant {Name="ForeignPoisonCaster",Team=CombatTeam.Player};foreign.AddChild(new Health {Name="Health",Maximum=100});w.AddChild(foreign);
            foreignDart=new AutomaticAbility {Caster=foreign};foreignDart.Configure(dart.Definition);
            w.Periodic.RemoveSource(dart);w.Periodic.Apply(foreignDart,first,relicApplied:true);h=first.Health.Current;bursts=visual.AcidBladeDetonationsPresented;
            acid.DirectHit(backstab,first);
            check("another caster's Dart neither triggers nor gets consumed",first.Health.Current==h && visual.AcidBladeDetonationsPresented==bursts && w.Periodic.Has(foreignDart,first) && w.Periodic.Has(dart,first));
            acid.DirectHit(backstab,first);check("own Dart can detonate alongside an untouched foreign Dart",w.Periodic.Has(foreignDart,first));
            w.Periodic.RemoveSource(foreignDart);

            w.Periodic.RemoveSource(dart);w.Periodic.Apply(dart,first,relicApplied:true);w.Periodic.Advance(8.1);h=first.Health.Current;bursts=visual.AcidBladeDetonationsPresented;acid.DirectHit(backstab,first);
            check("expired Dart is reapplied without a phantom explosion",first.Health.Current==h && visual.AcidBladeDetonationsPresented==bursts && w.Periodic.Has(dart,first));

            var doomed=new Combatant {Name="AcidBladeLethalFixture",Team=CombatTeam.Hostile};doomed.AddChild(new Health {Name="Health",Maximum=5});w.AddChild(doomed);doomed.GlobalPosition=first.GlobalPosition;w.Periodic.Apply(dart,doomed,relicApplied:true);acid.DirectHit(backstab,doomed);
            check("lethal burst consumes poison without reapplying to a corpse",!doomed.Alive && !w.Periodic.Has(dart,doomed));doomed.QueueFree();

            w.Periodic.RemoveSource(dart);w.Periodic.Apply(dart,first,relicApplied:true);w.Periodic.Apply(dart,second,relicApplied:true);
            backstab.SetUnlocked(true);backstab.ResetCombat();w.Player.Presentation.ResetPresentation();w.Targeting.Update(0,true);
            float firstBefore=first.Health.Current,secondBefore=second.Health.Current;
            float full=8*perTick;float hit=backstab.DamagePerTick*w.Build.DamageMultiplier(backstab,first);
            bursts=visual.AcidBladeDetonationsPresented;check("real Backstab starts for Acid Blade cleave",backstab.TryBegin());for(int i=0;i<40;i++)backstab.Advance(.05);
            check("real cleave detonates each victim once and reapplies both poisons",Math.Abs(firstBefore-first.Health.Current-hit-full*w.Build.DamageMultiplier(dart,first))<.02 && Math.Abs(secondBefore-second.Health.Current-hit-full*w.Build.DamageMultiplier(dart,second))<.02 && w.Periodic.Has(dart,first) && w.Periodic.Has(dart,second) && visual.AcidBladeDetonationsPresented==bursts+2);
            backstab.ResetCombat();
            TalentProfiles.Save("operative",new Dictionary<string,int>{{"concealment.revitalizers",1}});var noAcid=new TalentRuntime(w);prop.SetValue(w.Build,noAcid);h=first.Health.Current;bursts=visual.AcidBladeDetonationsPresented;noAcid.DirectHit(backstab,first);
            check("poison explosion requires the Acid Blade talent",first.Health.Current==h && visual.AcidBladeDetonationsPresented==bursts);
        }finally{
            if(foreignDart!=null){w.Periodic.RemoveSource(foreignDart);foreignDart.Free();}
            foreign?.QueueFree();w.Periodic.RemoveSource(dart);w.Periodic.RemoveSource(grenade);
            prop.SetValue(w.Build,previous);TalentProfiles.Save("operative",previous.Ranks);
        }
    }
}
