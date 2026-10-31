using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;

public static class SniperLethalitySmoke
{
    public static async Task Check(GameWorld w,Action<string,bool> check)
    {
        var catalog=TalentCatalog.Current;var p=w.Progression!;var original=w.Build.Pregame;
        var property=typeof(RunBuildEffects).GetProperty(nameof(RunBuildEffects.Pregame))!;
        var operativeBefore=TalentProfiles.Load("operative");
        var shared=catalog.ForClass("operative").Where(t=>t.Tree=="agent_lethality").ToArray();
        var sniper=catalog.ForClass("sniper").Where(t=>t.Tree=="agent_lethality").ToArray();
        check("Lethality has nine independent talents per class",shared.Length==9 && sniper.Length==9 && !shared.Select(t=>t.Id).Intersect(sniper.Select(t=>t.Id)).Any());
        check("Operative retains its mobile spread and sustain",shared.Any(t=>t.Effects.Any(e=>e.Key=="agent_toxic_spread")) && shared.Any(t=>t.Effects.Any(e=>e.Key=="agent_toxic_resilience")) && sniper.All(t=>t.Classes.SequenceEqual(new[]{"sniper"})));
        try {
            var legacy=shared.ToDictionary(t=>t.Id,t=>1);
            check("legacy Sniper allocation saves",TalentProfiles.Save("sniper",legacy)==null);
            var migrated=TalentProfiles.Load("sniper");
            check("all nine legacy positions migrate without losing points",migrated.Count==9 && sniper.All(t=>migrated.GetValueOrDefault(t.Id)==1) && !migrated.Keys.Any(k=>k.StartsWith("agent_lethality.")));
            check("migration does not alter Operative profile",TalentProfiles.Load("operative").Count==operativeBefore.Count && operativeBefore.All(p=>TalentProfiles.Load("operative").GetValueOrDefault(p.Key)==p.Value));
            var rt=new TalentRuntime(w);property.SetValue(w.Build,rt);p.ResetRun();rt.Advance(.61);
            AutomaticAbility A(string id)=>w.Abilities.Find(id)!;
            foreach(string id in new[]{"dart","corrosivegrenade","agentcull","weakeningblast","interrogationprobe","shieldprobe"})A(id).SetUnlocked(true);
            p.Recalculate();
            var a=new Combatant{Name="LethalityVictim",Team=CombatTeam.Hostile};a.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(a);a.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.14f;
            var b=new Combatant{Name="LethalityNeighbor",Team=CombatTeam.Hostile};b.AddChild(new Health{Name="Health",Maximum=100000});w.AddChild(b);b.GlobalPosition=a.GlobalPosition+Vector3.Right*.04f;
            await w.ToSignal(w.GetTree(),SceneTree.SignalName.ProcessFrame);
            var dart=A("dart");var grenade=A("corrosivegrenade");var cull=A("agentcull");var weak=A("weakeningblast");
            check("Sniper grenade gets area instead of Operative tick speed",grenade.Radius>=grenade.Definition.Radius*1.29f && Math.Abs(rt.PeriodicInterval(dart)-1)<.01);
            check("Sniper Dart gets its shorter duration tradeoff",rt.PeriodicDuration(dart)==dart.Definition.PeriodicDuration+2);
            w.Periodic.Apply(dart,a);w.Periodic.Apply(grenade,a);w.Periodic.Apply(A("interrogationprobe"),a);
            float hp=a.Health.Current;rt.DirectHit(cull,a);check("Cull echoes owned poisons",a.Health.Current<hp-3 && !w.Periodic.Has(dart,b));
            w.PlayerActor.Health.ClearShield();rt.Advance(6);rt.DirectHit(cull,a);float shield=w.PlayerActor.Health.ShieldRemaining;rt.DirectHit(cull,b);check("Toxic Focus grants a bounded cover shield",shield==8 && w.PlayerActor.Health.ShieldRemaining==shield);
            double remaining=w.Periodic.Remaining(dart,a);rt.DirectHit(weak,a);check("Weakening extends owned poison",w.Periodic.Remaining(dart,a)==remaining+2);
            for(int i=0;i<6;i++)rt.DirectHit(weak,a);check("poison extension has a duration cap",w.Periodic.Remaining(dart,a)==rt.PeriodicDuration(dart)+4);
            w.Player.Presentation.ResetPresentation();weak.ResetCombat();w.Targeting.Update(0,true);check("talented Weakening Blast actually begins",weak.TryBegin());for(int i=0;i<150 && weak.Casting;i++)weak.Advance(.025);
            check("Weakening spreads poisons through its group",w.Periodic.Has(dart,b) && w.Periodic.Has(grenade,b));check("contagion does not spread probes",!w.Periodic.Has(A("interrogationprobe"),b));
            w.Player.Presentation.ResetPresentation();cull.ResetCombat();check("talented Cull begins in cover",cull.TryBegin());cull.Advance(.1);double cd=cull.CooldownRemaining;rt.Advance(3);rt.PeriodicTick(dart);rt.PeriodicTick(grenade);
            check("poison cooldown feedback is shared across effects",Math.Abs(cd-cull.CooldownRemaining-.75)<.01);
            check("Zeroing In is available to a poison kit",p.Grant("sniper_decay"));check("poison ticks accelerate in cover",rt.PeriodicInterval(dart)<.9f);
            cull.Interrupt();weak.Interrupt();w.Player.TestInput=Vector2.Right;w.Player.SetPhysicsProcess(true);
            for(int i=0;i<8;i++)await w.ToSignal(w.GetTree(),SceneTree.SignalName.PhysicsFrame);rt.Advance(.05);
            check("leaving cover removes ranged poison benefits",!rt.AgentPositioned && rt.DirectBonus(dart,a)==0 && Math.Abs(rt.PeriodicInterval(dart)-1)<.01);
            cd=cull.CooldownRemaining;rt.Advance(3);rt.PeriodicTick(dart);check("mobile poison ticks do not grant cover recovery",cull.CooldownRemaining==cd);
            w.Player.TestInput=Vector2.Zero;for(int i=0;i<30;i++)await w.ToSignal(w.GetTree(),SceneTree.SignalName.PhysicsFrame);w.Player.SetPhysicsProcess(false);rt.Advance(.7);
            check("settling back into cover restores poison benefits",rt.AgentPositioned && rt.DirectBonus(dart,a)>=.34f && rt.PeriodicInterval(dart)<.9f);
            w.Periodic.RemoveSource(dart);w.Periodic.RemoveSource(grenade);w.Periodic.RemoveSource(A("interrogationprobe"));a.QueueFree();b.QueueFree();
        }finally {
            w.Player.TestInput=Vector2.Zero;w.Player.SetPhysicsProcess(false);property.SetValue(w.Build,original);TalentProfiles.Save("sniper",original.Ranks);p.ResetRun();
        }
    }
}
