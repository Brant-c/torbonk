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
        if(!a.Unlocked)
        {
            var replace=_w.Abilities.Abilities.FirstOrDefault(other=>other.Unlocked && other!=_w.Combat && !other.Definition.IsBasicAttack && other.Definition.Category==a.Definition.Category);
            replace?.SetUnlocked(false);Check(a.Definition.Id+" equips for isolated fixture",_w.Abilities.Unlock(a.Definition.Id));
        }
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
            if(id=="assassin" && !_fixtures.All(w.Targeting.HasLineOfSight)) {
                for(int angle=0;angle<32;angle++) {
                    var direction=new Vector3(Mathf.Cos(angle*Mathf.Tau/32),0,Mathf.Sin(angle*Mathf.Tau/32));
                    for(int i=0;i<_fixtures.Count;i++)_fixtures[i].GlobalPosition=w.PlayerActor.GlobalPosition+direction*(.12f+i*.055f);
                    if(_fixtures.All(w.Targeting.HasLineOfSight))break;
                }
            }
            Check(id+" corridor has line of sight",_fixtures.All(w.Targeting.HasLineOfSight));
            if(profile.Kit.Length>0)
            {
            // Registry and slots.
            var kit=profile.Kit.Select(k=>w.Abilities.Find(k)).ToArray();
            Check(id+" kit abilities are wired and locked",kit.All(k=>k!=null && !k.Unlocked) && w.Abilities.Abilities.Count(a=>a.Unlocked)==(id=="assassin"?1:2));
            Check(id+" pool offers unlock for every kit ability",profile.Kit.All(k=>w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==k)));
            if(id is "powertech" or "mercenary")
            {
                // Bounty Hunters use scoped, rarity-rolled power cards instead of generic stat spam.
                var damaging=kit.Where(k=>k!.Definition.Effect==AbilityEffect.Damage && k.Definition.Category==AbilityCategory.Offensive).ToArray();
                Check(id+" every offensive kit ability has an uncapped power card",damaging.All(k=>w.Progression!.Upgrades.Any(u=>u.Effect==UpgradeEffect.AbilityPower && u.AbilityId==k!.Definition.Id && u.DamagePerRank>=.6f && u.MaximumRank==0)));
                Check(id+" pool drops generic damage/range/cooldown/speed cards",!w.Progression!.Upgrades.Any(u=>u.Effect is UpgradeEffect.Damage or UpgradeEffect.Range or UpgradeEffect.Cooldown or UpgradeEffect.Speed));
                Check(id+" movement card is 3% per rank, capped",w.Progression.Upgrades.Single(u=>u.Mechanic=="bh_thrusters").MaximumRank==3);
                var talents=Talents.TalentCatalog.Current.ForClass(id);
                Check(id+" has three 3x3 talent trees",talents.Length==27 && talents.GroupBy(t=>t.Tree).All(g=>g.Count()==9 && g.Select(t=>(t.Tier,t.Column)).Distinct().Count()==9));
                Check(id+" talent icons are recovered April artwork",talents.All(t=>ResourceLoader.Exists(t.Icon) && t.Source.StartsWith("abl.bounty_hunter.",StringComparison.Ordinal)));
                Check(id+" one-time talent picks exist for every hook",talents.SelectMany(t=>t.Effects).Where(e=>e.Kind=="hook" && e.Key is not ("pt_jet_speed" or "mc_tracer_conversion" or "pt_dart_conversion")).All(e=>w.Progression.Upgrades.Any(u=>u.Effect==UpgradeEffect.Mechanic && u.Mechanic==e.Key && u.MaximumRank==1)));
                Check(id+" every card icon exists",w.Progression.Upgrades.All(u=>ResourceLoader.Exists("res://Assets/UI/icons/"+u.Icon+".png")));
            }
            else
            {
            Check(id+" pool contains two mechanic talents",w.Progression!.Upgrades.Count(u=>u.Effect==UpgradeEffect.Mechanic)==2);
            Check(id+" damage track covers the whole kit",w.Progression.Upgrades.Any(u=>u.Effect==UpgradeEffect.Damage && u.AbilityId==""));
            }
            foreach(var k in profile.Kit)
            {
                var ability=w.Abilities.Find(k)!;
                if(!w.Abilities.CanUnlock(ability))
                {
                    Check(id+" full slots reject "+k,!w.Progression.Grant("unlock_"+k));
                    kit.First(other=>other!.Unlocked && other.Definition.Category==ability.Definition.Category)!.SetUnlocked(false);
                }
                Check(id+" grants "+k,w.Progression.Grant("unlock_"+k));
            }
            Check(id+" slot limits respected",w.Abilities.Equipped(AbilityCategory.Offensive)<=(id=="assassin"?6:4) && w.Abilities.Equipped(AbilityCategory.Utility)<=w.Abilities.UtilityLimit);
            Check(id+" action bar lists starter, basic and owned kit",w.Abilities.Abilities.Count(a=>a.Unlocked)==(id=="assassin"?1:2)+kit.Count(k=>k!.Unlocked));
            // Stand-still channels (Death From Above) need a grounded, idle player body.
            if(id=="mercenary"){w.Player.TestInput=Vector2.Zero;w.Player.Velocity=Vector3.Zero;w.Player.SetPhysicsProcess(true);for(int i=0;i<6;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);w.Player.SetPhysicsProcess(false);}
            // Every damaging kit ability hits and publishes through the normal path; presentation has no damage authority.
            foreach(var k in kit.Where(k=>k!.Definition.Effect==AbilityEffect.Damage))
            {
                Reset();var lost=Cast(k!);
                Check(k!.Definition.Id+" damages its primary target",lost[0]>0);
                if(k.Definition.Area==AbilityArea.TargetSplash)Check(k.Definition.Id+" splash reaches neighbours",lost[1]>0 && lost[2]>0);
                else if(k.Definition.Area==AbilityArea.SingleTarget)Check(k.Definition.Id+" is single-target",lost[1]==0 && lost[2]==0);
            }
            // Class mechanics.
            if(id=="assassin")
            {
                w.Abilities.Find("affliction")!.SetUnlocked(false);
                w.Abilities.Find("deathfield")!.SetUnlocked(false);
                w.Abilities.Unlock("creepingterror");w.Abilities.Unlock("discharge");
            }
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
                Check("Corrosive Grenade poisons every enemy it touches",P.Count==3 && g.All(x=>Near(x,5)));for(int i=0;i<4;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check("grenade releases the green gas cloud",((D3AbilityPresentation)grenade.Presentation).EffectCount>=10);
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
            else if(id=="assassin")
            {
                var lacerate=w.Combat;var terror=w.Abilities.Find("creepingterror")!;var shroud=w.Abilities.Find("forceshroud")!;var discharge=w.Abilities.Find("discharge")!;
                Reset();Cast(terror);
                Check("Creeping Terror afflicts its target",P.Count==1 && P.Has(terror,_fixtures[0]));
                Reset();var d=Cast(discharge);
                Check("Discharge hits and afflicts everything around you",d.All(x=>x>0) && P.Count==3);
                Reset();Cast(shroud);
                Check("Force Shroud raises a shield",w.PlayerActor.Health.Shielded);for(int i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);Check("Force Shroud shows the shield effect",((D3AbilityPresentation)shroud.Presentation).ShieldVisible);
                w.PlayerActor.Health.ClearShield();
                Cast(terror);Reset();Check("Contagion is eligible",w.Progression.Grant("assassin_contagion"));
                P.Apply(terror,_fixtures[0]);P.Advance(3.05);
                Check("Contagion spreads the affliction on the third tick",P.Has(terror,_fixtures[1]));
                Reset();Check("Static Blades is eligible",w.Progression.Grant("assassin_static_blades"));
                P.Apply(terror,_fixtures[0]);P.Advance(4);Cast(lacerate,false);P.Advance(4);
                Check("Static Blades: Lacerate refreshes the affliction beyond its normal 6 s",P.Has(terror,_fixtures[0]));
            }
            else if(id=="juggernaut")
            {
                var smash=w.Combat;var slash=w.Abilities.Find("viciousslash")!;var crush=w.Abilities.Find("crushingblow")!;var roar=w.Abilities.Find("intimidatingroar")!;
                var leap=w.Player.Presentation.Animator.GetAnimation("D3/SmashLeap");
                Check("Smash plays the full original leap: launch and descent, about 2.4 s",Near((float)leap.Length,2.4f,.05f) && smash.CooldownSeconds>=(float)leap.Length);
                Check("Smash slam lands after the leap's launch phase",smash.Definition.WindupSeconds>1.0f && smash.Definition.WindupSeconds<(float)leap.Length);
                Reset();var c1=Cast(crush);Check("Crushing Blow is a single heavy hit",Near(c1[0],60) && c1[1]==0);
                Reset();var r=Cast(roar);Check("Intimidating Roar is a utility burst on all nearby enemies",r.All(x=>Near(x,4)) && roar.Definition.Category==AbilityCategory.Utility);
                Reset();Check("Rage is eligible",w.Progression.Grant("juggernaut_rage"));
                for(int i=0;i<5;i++){slash.ResetCombat();Cast(slash,false);}
                Check("Rage builds five stacks for +25% damage",b.RageStacks==5 && Near(b.DamageMultiplier(slash,_fixtures[0]),1.25f));
                b._PhysicsProcess(8.1);Check("Rage fades after 8 s",b.RageStacks==0 && Near(b.DamageMultiplier(slash,_fixtures[0]),1));
                Reset();Check("Shockwave is eligible",w.Progression.Grant("juggernaut_shockwave"));
                smash.ResetCombat();Cast(smash);Check("Shockwave leaves Smash damage unchanged",Near(_fixtures[0].Health.Maximum-_fixtures[0].Health.Current,42,.05f));
            }
            else if(id=="powertech")
            {
                var sweep=w.Combat;var burst=w.Abilities.Find("flameburst")!;var missile=w.Abilities.Find("incendiarymissile")!;var ion=w.Abilities.Find("ionshield")!;
                var blade=w.Abilities.Find("retractableblade")!;var dart=w.Abilities.Find("explosivedart")!;var immolate=w.Abilities.Find("immolate")!;var rail=w.Abilities.Find("railshot")!;
                // Isolated fixtures: equip the interacting pieces directly instead of trading slots.
                foreach(var owned in new[]{missile,blade,dart,immolate,rail,ion})owned.SetUnlocked(true);
                Reset();w.Build.Pregame.Reset();Cast(missile);Check("Incendiary Missile burns one target",P.Count==1);
                Reset();Cast(blade);Check("Retractable Blade bleeds its target",P.Count==1 && P.Has(blade,_fixtures[0]));
                Reset();w.Build.Pregame.Reset();P.Apply(missile,_fixtures[0]);var stuck=Cast(dart,false);
                Check("Explosive Dart sticks first and only scratches its target",w.Build.Pregame.DartAttached(_fixtures[0]) && stuck[0]>0 && stuck[0]<dart.DamagePerTick*.5f && stuck[1]==0);
                var beforeBlast=_fixtures.Select(f=>f.Health.Current).ToArray();w.Build.Pregame.Advance(2.05);
                var d=_fixtures.Select((f,i)=>beforeBlast[i]-f.Health.Current).ToArray();
                Check("Explosive Dart detonates after its fuse, hitting the whole blast",w.Build.Pregame.PendingDarts==0 && d.All(x=>Near(x,d[0],.05f)) && d[0]>=dart.DamagePerTick*.99f);
                Check("Explosive Dart applies the Blade bleed to everything it hits",_fixtures.All(f=>P.Has(blade,f)));
                Check("Explosive Dart copies your burn across the blast",_fixtures.All(f=>P.Has(missile,f)));
                // Prototype Cylinders: bleed-only spreading in exchange for a harder, wider blast.
                float plainBlast=d[0];Check("Prototype Cylinders is eligible with Explosive Dart and Retractable Blade",w.Progression.Grant("powertech_blade_focus"));
                Reset();w.Build.Pregame.Reset();P.Apply(missile,_fixtures[0]);Cast(dart,false);var focusBefore=_fixtures.Select(f=>f.Health.Current).ToArray();w.Build.Pregame.Advance(2.05);
                Check("Prototype Cylinders spreads only the Blade bleed",_fixtures.All(f=>P.Has(blade,f)) && !P.Has(missile,_fixtures[1]) && !P.Has(missile,_fixtures[2]));
                Check("Prototype Cylinders blast hits 40% harder (plus its bleed burst)",focusBefore[1]-_fixtures[1].Health.Current>=plainBlast*1.39f);
                var detonator=w.Abilities.Find("thermaldetonator")!;detonator.SetUnlocked(true);
                Reset();w.Build.Pregame.Reset();Cast(detonator,false);Check("Thermal Detonator clamps on before it blows",w.Build.Pregame.DartAttached(_fixtures[0]));
                var tdBefore=_fixtures.Select(f=>f.Health.Current).ToArray();w.Build.Pregame.Advance(1.05);float tdPlain=tdBefore[2]-_fixtures[2].Health.Current;
                Check("Thermal Detonator blasts every enemy after 1 second",w.Build.Pregame.PendingDarts==0 && _fixtures.All(f=>f.Health.Current<10000) && Near(tdPlain,detonator.DamagePerTick,.1f));
                Reset();w.Build.Pregame.Reset();P.Apply(blade,_fixtures[0]);P.Apply(blade,_fixtures[1]);Cast(detonator,false);
                tdBefore=_fixtures.Select(f=>f.Health.Current).ToArray();w.Build.Pregame.Advance(1.05);
                Check("Thermal Detonator gains 15% per bleeding enemy in its blast",Near(tdBefore[2]-_fixtures[2].Health.Current,tdPlain*1.3f,.15f));
                var profileRanks=Talents.TalentProfiles.Load("powertech");
                Talents.TalentProfiles.Save("powertech",new Dictionary<string,int>{["advanced_prototype.explosive_dart"]=1});
                Check("Advanced Prototype talent turns the Flame Sweep starter into Explosive Dart",Talents.TalentRuntime.BountyStartingAbility(profile)=="explosivedart");
                Talents.TalentProfiles.Save("powertech",new Dictionary<string,int>());
                Check("without the talent Powertech still starts with Flame Sweep",Talents.TalentRuntime.BountyStartingAbility(profile)=="flamesweep");
                Talents.TalentProfiles.Save("powertech",profileRanks);
                Reset();var cold=Cast(immolate)[0];Reset();P.Apply(missile,_fixtures[0]);var hot=Cast(immolate,false)[0];
                Check("Immolate deals 50% more to burning targets",Near(hot,cold*1.5f,.1f));
                Reset();var clean=Cast(rail)[0];Check("Puncture is eligible with Rail Shot and Retractable Blade",w.Progression.Grant("powertech_puncture"));
                Reset();P.Apply(blade,_fixtures[0]);var bled=Cast(rail,false)[0];
                Check("Puncture: Rail Shot +40% against bleeding targets",Near(bled,clean*1.4f,.1f));
                Reset();Check("Ion Shield fires when enemies are close",ion.TryBegin());for(int i=0;i<12;i++)ion.Advance(.05);
                Check("Energy Shield raises a 30-point shield",w.PlayerActor.Health.Shielded);for(int i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check("Energy Shield shows the hexagon shield effect while it holds",((D3AbilityPresentation)ion.Presentation).ShieldVisible);
                w.PlayerActor.Health.ClearShield();for(int i=0;i<3;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check("shield effect ends with the shield",!((D3AbilityPresentation)ion.Presentation).ShieldVisible);
                Reset();P.Apply(immolate,_fixtures[0]);Cast(sweep,false);
                Check("Flame Sweep spreads Immolate's burn without a card",_fixtures.All(f=>P.Has(immolate,f)));
                Reset();P.Apply(missile,_fixtures[0]);P.Advance(5.5);_fixtures[0].Health.ApplyDamage(1000000,DamageType.Energy);b.PassOn(_fixtures[0]);
                Check("a dying burning enemy passes its burn on with time to spread again",P.Has(missile,_fixtures[1]) && P.Remaining(missile,_fixtures[1])>=2.9);
                Reset();var rocket=w.Abilities.Find("rocketpunch")!;rocket.SetUnlocked(true);
                Check("Iron Fist is eligible with Rocket Punch",w.Progression.Grant("powertech_iron_fist"));
                var punch=Cast(rocket);
                Check("Iron Fist cleaves and bleeds every enemy hit",punch.All(x=>x>0) && _fixtures.All(f=>P.Has(blade,f)));
            }
            else if(id=="sniper")
            {
                var shot=w.Combat;var ambush=w.Abilities.Find("ambush")!;var series=w.Abilities.Find("seriesofshots")!;var probe=w.Abilities.Find("shieldprobe")!;
                Reset();var a1=Cast(ambush);Check("Ambush is one big hit",Near(a1[0],80) && a1[1]==0);
                Reset();var s1=Cast(series);Check("Series of Shots lands three shots",Near(s1[0],42,.1f));
                Reset();Check("Shield Probe fires when enemies approach",probe.TryBegin());for(int i=0;i<12;i++)probe.Advance(.05);
                Check("Shield Probe raises a shield",w.PlayerActor.Health.Shielded);w.PlayerActor.Health.ClearShield();
                Reset();Check("Target Lock is eligible",w.Progression.Grant("sniper_target_lock"));
                var seen=new List<float>();for(int i=0;i<6;i++){shot.ResetCombat();seen.Add(Cast(shot)[0]);}
                Check("Target Lock adds 10% per consecutive hit, capped at four",Near(seen[0],32,.05f) && Near(seen[1],35.2f,.05f) && Near(seen[4],44.8f,.05f) && Near(seen[5],44.8f,.05f));
                Reset();Check("Followthrough is eligible",w.Progression.Grant("sniper_followthrough"));
                Cast(shot);Check("Sniper Shot is on cooldown",shot.CooldownRemaining>0);
                _fixtures[0].Health.ApplyDamage(_fixtures[0].Health.Current-1,DamageType.Energy);ambush.ResetCombat();Cast(ambush,false);
                Check("Followthrough resets Sniper Shot on an Ambush kill",!_fixtures[0].Alive && shot.CooldownRemaining==0);
            }
            else if(id=="mercenary")
            {
                var tracer=w.Abilities.Find("tracer")!;var heat=w.Abilities.Find("heatseeker")!;var unload=w.Abilities.Find("unload")!;var R=w.Build.Pregame;
                var missile=w.Abilities.Find("incendiarymissile")!;var rail=w.Abilities.Find("railshot")!;var power=w.Abilities.Find("powershot")!;
                var fusion=w.Abilities.Find("fusionmissile")!;var dart=w.Abilities.Find("explosivedart")!;var tso=w.Abilities.Find("thermalsensoroverride")!;var kolto=w.Abilities.Find("koltomissile")!;
                var blast=w.Abilities.Find("missileblast")!;var dfa=w.Abilities.Find("deathfromabove")!;
                Check("Mercenary starts with Power Shot and Rapid Shots",w.Combat==power && w.Abilities.Abilities.Any(a=>a.Definition.IsBasicAttack && a.Definition.Icon=="rapidshots"));
                var mercRanks=Talents.TalentProfiles.Load("mercenary");
                Talents.TalentProfiles.Save("mercenary",new Dictionary<string,int>{["arsenal.tracer_missile"]=1});
                Check("Arsenal talent turns the Power Shot starter into Tracer Missile",Talents.TalentRuntime.BountyStartingAbility(profile)=="tracer");
                Talents.TalentProfiles.Save("mercenary",mercRanks);
                Check("Tracer Missile is a kit unlock",w.Progression.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId=="tracer"));
                foreach(var owned in new[]{tracer,heat,unload,missile,rail,power,fusion,dart,tso,kolto,blast,dfa})owned.SetUnlocked(true);
                Reset();R.Reset();var h=Cast(heat);Check("Heatseeker Missiles hit the cluster",h.All(x=>x>0));
                Reset();var u=Cast(unload);Check("Unload lands four shots",Near(u[0],44,.1f) && u[1]==0);
                Reset();R.Reset();for(int i=0;i<3;i++){tracer.ResetCombat();Cast(tracer,false);}
                Check("Tracer Missile stacks Heat Signature on its target",R.HeatSignature(_fixtures[0])==3);
                Check("Heatseeker gains 15% per Heat Signature stack",Near(b.DamageMultiplier(heat,_fixtures[0]),1.45f));
                Cast(heat,false);Check("Heatseeker consumes Heat Signature",_fixtures.All(f=>R.HeatSignature(f)==0));
                Reset();R.Reset();Cast(power);Check("Power Shot marks Heat Signature",R.HeatSignature(_fixtures[0])==1);
                Reset();R.Reset();Cast(blast);Check("Missile Blast gives every enemy hit a Heat Signature stack",_fixtures.All(f=>R.HeatSignature(f)==1));
                Reset();R.Reset();Cast(unload);Check("Unload goes on cooldown",unload.CooldownRemaining>0);
                for(int i=0;i<4;i++){tracer.ResetCombat();Cast(tracer,false);}
                Check("Barrage: every 4th Tracer or Power Shot readies Unload",unload.CooldownRemaining==0);
                Reset();var plain=Cast(power)[0];Reset();P.Apply(missile,_fixtures[0]);var dotted=Cast(power,false)[0];
                Check("Power Shot gains 15% per DoT on the target",Near(dotted,plain*1.15f,.1f));
                Reset();P.Apply(missile,_fixtures[0]);Cast(fusion,false);
                Check("Fusion Missile burns and spreads burns across its blast",_fixtures.All(f=>P.Has(fusion,f) && P.Has(missile,f)));
                Reset();R.Reset();P.Apply(fusion,_fixtures[2]);Cast(dart,false);
                Check("Explosive Dart waits on its fuse before spreading",R.PendingDarts==1 && !P.Has(fusion,_fixtures[0]));
                R.Advance(2.05);Check("Explosive Dart detonates and spreads Mercenary DoTs to everything it hits",R.PendingDarts==0 && _fixtures.All(f=>P.Has(fusion,f)));
                Reset();R.Reset();float baseY=w.Player.Presentation.Position.Y;Check("Death From Above starts while standing still",dfa.TryBegin());
                for(int i=0;i<10;i++){dfa.Advance(.05);R.Advance(.05);}
                Check("Death From Above: immune, rooted and lifted while channeling",R.DeathFromAboveActive && R.IncomingMultiplier==0 && w.Player.MovementLocked && w.Player.Presentation.Position.Y>baseY+.01f);
                Check("Death From Above shows the Imperial area projection",((D3AbilityPresentation)dfa.Presentation).BountyAreaVisible);
                for(int i=0;i<90;i++){dfa.Advance(.05);R.Advance(.05);}
                Check("Death From Above lands and releases movement",!R.DeathFromAboveActive && !w.Player.MovementLocked && Math.Abs(w.Player.Presentation.Position.Y-baseY)<.005f);
                Reset();Cast(rail);Check("Thermal Sensor Override waits for a long cooldown, then readies it",tso.TryBegin() && rail.CooldownRemaining==0);
                Reset();R.Reset();w.PlayerActor.Health.ApplyDamage(w.PlayerActor.Health.Maximum*.5f);float hp=w.PlayerActor.Health.Current;Cast(kolto);
                Check("Kolto Missile heals a little and never more than 4",w.PlayerActor.Health.Current>hp && w.PlayerActor.Health.Current-hp<=4.001f);
                Check("Kolto field reduces incoming damage while you stand in it",R.InKoltoField && R.IncomingMultiplier<.9f);
                w.PlayerActor.Health.Restore();R.Reset();
                Reset();Check("Chain Reaction is eligible",w.Progression.Grant("mercenary_chain_reaction"));
                tracer.ResetCombat();Cast(tracer);_fixtures[0].Health.ApplyDamage(1000000,DamageType.Energy);
                float n1=_fixtures[1].Health.Current,n2=_fixtures[2].Health.Current;b.PassOn(_fixtures[0]);
                Check("an enemy carrying Heat Signature explodes when it dies",Near(n1-_fixtures[1].Health.Current,20) && Near(n2-_fixtures[2].Health.Current,20));
            }
            }
            if(id is "juggernaut" or "marauder" or "assassin")
            {
                var hums=w.Player.FindChildren("OriginalSaberHum","AudioStreamPlayer3D",true,false).OfType<AudioStreamPlayer3D>().ToArray();
                Check(id+" saber hum is imported, looping and audible",hums.Length>=1 && hums.All(h=>h.Playing && h.VolumeDb>=-24 && h.Stream is AudioStreamWav {LoopMode:AudioStreamWav.LoopModeEnum.Forward}));
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
                Check("Flame Sweep cannot recast before its animation ends",starter.CooldownDuration>=starter.Definition.GestureSeconds && starter.CooldownDuration>=4);
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
