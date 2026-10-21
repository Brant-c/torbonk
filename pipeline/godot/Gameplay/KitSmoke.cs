using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
namespace SwtorSurvivors;
/// <summary>Focused class-kit checks for the profile selected on the command line. Offline Survivors rules, not native-client claims.</summary>
public partial class KitSmoke : Node
{
    private readonly List<string> _checks=new();
    private GameWorld _w=null!;
    private readonly List<Combatant> _fixtures=new();
    private void Check(string text,bool pass){if(!pass)throw new InvalidOperationException(text);_checks.Add(text);GD.Print("KIT: "+text);}
    private static bool Near(float a,float b,float eps=.02f)=>Math.Abs(a-b)<=eps;
    private void Reset()
    {
        _w.Periodic.Clear();
        foreach(var f in _fixtures){f.Health.Restore();f.Health.VulnerabilitySeconds=0;}
        _w.Build.Reset();
        foreach(var a in _w.Abilities.Abilities)a.ResetCombat();
    }
    /// <summary>Starts one ability and runs its lifecycle to completion in fixed steps; returns hit points lost per fixture.</summary>
    private float[] Cast(AutomaticAbility a,bool resetHealth=true)
    {
        if(resetHealth)foreach(var f in _fixtures)f.Health.Restore();
        var before=_fixtures.Select(f=>f.Health.Current).ToArray();
        Check(a.Definition.Id+" starts",a.TryBegin());
        double total=a.Definition.WindupSeconds+a.ChannelSeconds+a.Definition.RecoverySeconds+.2;
        for(double t=0;t<total;t+=.05)a.Advance(.05);
        return _fixtures.Select((f,i)=>before[i]-f.Health.Current).ToArray();
    }
    public async void Run(GameWorld w)
    {
        _w=w;string? failure=null;string id=w.RunConfiguration.AdvancedClassId;
        try
        {
            w.ResetCombatWorld();w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Player.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            foreach(var enemy in w.Spawner.Enemies.ToArray()){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
            for(int i=0;i<3;i++)
            {
                var e=new Combatant {Name="KitTarget"+i,TargetId="KitTarget"+i,Team=CombatTeam.Hostile};e.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(e);
                e.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*(.12f+i*.055f);_fixtures.Add(e);
            }
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var profile=w.RunConfiguration.Profile;
            Check(id+" corridor has line of sight",_fixtures.All(w.Targeting.HasLineOfSight));
            if(profile.Kit.Length>0)
            {
            // Registry and slots.
            var kit=profile.Kit.Select(k=>w.Abilities.Find(k)).ToArray();
            Check(id+" kit abilities are wired and locked",kit.All(k=>k!=null && !k.Unlocked) && w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
            Check(id+" pool offers unlock for every kit ability",profile.Kit.All(k=>w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==k)));
            Check(id+" pool contains two mechanic talents",w.Progression!.Upgrades.Count(u=>u.Effect==UpgradeEffect.Mechanic)==2);
            Check(id+" damage track covers the whole kit",w.Progression.Upgrades.Any(u=>u.Effect==UpgradeEffect.Damage && u.AbilityId==""));
            foreach(var k in profile.Kit)Check(id+" grants "+k,w.Progression.Grant("unlock_"+k));
            Check(id+" slot limits respected",w.Abilities.Equipped(AbilityCategory.Offensive)<=4 && w.Abilities.Equipped(AbilityCategory.Utility)<=2 && kit.All(k=>k!.Unlocked));
            Check(id+" action bar lists starter and kit",w.Abilities.Abilities.Count(a=>a.Unlocked)==1+profile.Kit.Length);
            // Every damaging kit ability hits and publishes through the normal path; presentation has no damage authority.
            foreach(var k in kit.Where(k=>k!.Definition.Effect==AbilityEffect.Damage))
            {
                Reset();var lost=Cast(k!);
                Check(k!.Definition.Id+" damages its primary target",lost[0]>0);
                if(k.Definition.Area==AbilityArea.TargetSplash)Check(k.Definition.Id+" splash reaches neighbours",lost[1]>0 && lost[2]>0);
                else if(k.Definition.Area==AbilityArea.SingleTarget)Check(k.Definition.Id+" is single-target",lost[1]==0 && lost[2]==0);
            }
            // Class mechanics.
            var b=w.Build;var P=w.Periodic;
            if(id=="marauder")
            {
                var rupture=w.Abilities.Find("rupture")!;var annihilate=w.Abilities.Find("annihilate")!;var scream=w.Abilities.Find("forcescream")!;var berserk=w.Abilities.Find("berserk")!;
                Reset();var plain=Cast(annihilate);Reset();P.Apply(rupture,_fixtures[0]);var bleeding=Cast(annihilate,false);
                Check("Annihilate: +50% against a bleeding target",Near(plain[0],26) && Near(bleeding[0],39));
                Reset();Cast(scream);
                Check("Force Scream bleeds primary and both bystanders",P.Count==3);
                float h0=_fixtures[0].Health.Current,h1=_fixtures[1].Health.Current;P.Advance(1.05);
                Check("bystanders bleed at half strength",Near(h0-_fixtures[0].Health.Current,4) && Near(h1-_fixtures[1].Health.Current,2));
                Reset();Check("Berserk fires near enemies",berserk.TryBegin());for(int i=0;i<30;i++)berserk.Advance(.05);
                Check("Berserk grants +25% damage",b.Buffed && Near(b.DamageMultiplier(annihilate,_fixtures[0]),1.25f));
                b._PhysicsProcess(6.1);Check("Berserk expires",!b.Buffed && Near(b.DamageMultiplier(annihilate,_fixtures[0]),1));
                Reset();Check("Last Gasp is eligible",w.Progression.Grant("marauder_last_gasp"));
                P.Apply(rupture,_fixtures[0]);_fixtures[0].Health.ApplyDamage(1000000,DamageType.Energy);b.PassOn(_fixtures[0]);
                Check("Last Gasp passes the bleed to the nearest enemy",P.Has(rupture,_fixtures[1]) && !P.Has(rupture,_fixtures[2]));
                Reset();Check("Blood Rhythm is eligible",w.Progression.Grant("marauder_blood_rhythm"));
                Cast(rupture);float cd=(float)rupture.CooldownRemaining;P.Advance(4.05);
                Check("Blood Rhythm refunds 0.5 s on the fourth bleed tick",Near((float)rupture.CooldownRemaining,cd-.5f));
            }
            else if(id=="operative")
            {
                var dart=w.Abilities.Find("dart")!;var grenade=w.Abilities.Find("corrosivegrenade")!;var weak=w.Abilities.Find("weakeningblast")!;var stim=w.Abilities.Find("adrenalstim")!;
                Reset();var g=Cast(grenade);
                Check("Corrosive Grenade poisons every enemy it touches",P.Count==3 && g.All(x=>Near(x,5)));
                Reset();Cast(weak);
                Check("Weakening Blast marks the target for 5 s",Near((float)_fixtures[0].Health.VulnerabilitySeconds,5,.4f));
                Reset();float baseline=Cast(dart)[0];
                Reset();_fixtures[0].Health.VulnerabilitySeconds=5;var marked=Cast(dart,false)[0];
                Check("marked target takes +20%",Near(marked,baseline*1.2f,.1f));
                Reset();Check("Adrenal Stim fires when enemies approach",stim.TryBegin());for(int i=0;i<30;i++)stim.Advance(.05);
                Check("Adrenal Stim adds speed and damage",b.Buffed && Near(b.SpeedMultiplier,1.2f) && Near(b.DamageMultiplier(dart,_fixtures[0]),1.1f));
                b._PhysicsProcess(5.1);Check("Adrenal Stim expires",!b.Buffed && Near(b.SpeedMultiplier,1));
                Reset();Check("Contagion is eligible",w.Progression.Grant("operative_contagion"));
                P.Apply(dart,_fixtures[0]);P.Advance(3.05);
                Check("Contagion spreads poison on the third tick",P.Has(dart,_fixtures[1]) && P.Count>=2);
                Reset();Check("Lethal Dose is eligible",w.Progression.Grant("operative_lethal_dose"));
                P.Apply(dart,_fixtures[0]);P.Apply(dart,_fixtures[0]);float before=_fixtures[0].Health.Current;P.Advance(1.05);
                Check("Lethal Dose deepens a refreshed poison by 10%",Near(before-_fixtures[0].Health.Current,9.9f) && P.Count==1);
            }
            }
            // Starter presentation regressions (D3 corrections): additive FX, lethal tails, flame pulses.
            Reset();
            var starter=w.Combat;var visual=(D3AbilityPresentation)starter.Presentation;
            if(starter.Definition.PresentationKind is "rifle" or "bracer")
            {
                _fixtures[0].Health.ApplyDamage(10000-1,DamageType.Energy);
                Check(starter.Definition.Id+" lethal hit retains its presentation",starter.TryBegin());
                for(int i=0;i<4;i++)starter.Advance(.05);
                Check(starter.Definition.Id+" effects survive a killing blow",!_fixtures[0].Alive && visual.EffectCount>0);
            }
            if(starter.Definition.PresentationKind=="flame")
            {
                Reset();var lost=Cast(starter);
                Check("Flame Sweep pulses four times along the spin",lost.All(x=>Near(x,17,.05f)));
                Reset();Check("Flame Sweep restarts",starter.TryBegin());for(int i=0;i<8;i++)starter.Advance(.05);
                for(int i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check("Flame emitter runs during the spin with additive original textures",visual.Emitting && visual.AdditiveBound && visual.EffectCount>0);
                for(int i=0;i<40;i++)starter.Advance(.05);
                Check("Flame emission stops with the spin",!visual.Emitting);
            }
            Reset();
            Check("no kit presentation raised a duplicate hit",_fixtures.All(f=>f.Health.Current==10000));
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/kit-smoke-{id}.json"),JsonSerializer.Serialize(new{@class=id,passed=failure==null,checks=_checks,failure},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
