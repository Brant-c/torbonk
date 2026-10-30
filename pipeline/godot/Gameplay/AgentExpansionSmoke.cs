using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using SwtorSurvivors.Talents;
namespace SwtorSurvivors;
public partial class AgentExpansionSmoke : Node
{
    public async void Run(GameWorld w)
    {
        var checks=new List<string>();string? failure=null;
        void Check(string name,bool ok){if(!ok)throw new Exception(name);checks.Add(name);}
        try {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Build.SetPhysicsProcess(false);
            w.Player.TestInput=Vector2.Zero;
            for(int i=0;i<30;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
            w.Player.SetPhysicsProcess(false);
            foreach(var e in w.Spawner.Enemies){e.CombatEnabled=false;e.SetPhysicsProcess(false);}
            string cls=w.RunConfiguration.AdvancedClassId;
            Check("requested starting loadout",cls=="operative"?w.Abilities.Find("shiv")?.Unlocked==true && w.Abilities.Find("dart")?.Unlocked==true && w.Abilities.Find("basicrifle")==null:w.Abilities.Find("snipershot")?.Unlocked==true && w.Abilities.Find("basicrifle")?.Unlocked==true && w.Abilities.Find("dart")?.Unlocked==false);
            w.Abilities.ResetUnlocks();
            Check("starting loadout survives restart",cls=="operative"?w.Abilities.Find("shiv")!.Unlocked && w.Abilities.Find("dart")!.Unlocked:w.Abilities.Find("snipershot")!.Unlocked && w.Abilities.Find("basicrifle")!.Unlocked);
            var cat=TalentCatalog.Current;var ts=cat.ForClass(cls);
            Check("three trees, nine freely allocated talents in 3x3 each",ts.Length==27 && ts.GroupBy(t=>t.Tree).Count()==3 && ts.GroupBy(t=>t.Tree).All(g=>g.Count()==9 && g.Select(t=>(t.Tier,t.Column)).Distinct().Count()==9 && g.All(t=>t.Tier>=0 && t.Tier<3 && t.Column>=0 && t.Column<3)) && ts.All(t=>t.MaxRank==1 && t.RequiredPoints==0));
            Check("ten point hybrid budget",cat.Validate(cls,ts.ToDictionary(t=>t.Id,t=>1)).Count==10);
            Check("six offensive and three utility slots",w.Abilities.OffensiveLimit==6 && w.Abilities.UtilityLimit==3);
            Check("primary starter is free and replaceable",w.Combat.Definition.Slotless && w.Combat.Definition.ReplaceableStarter);
            Check("starting offensive count excludes free starter",w.Abilities.Equipped(AbilityCategory.Offensive)==(cls=="operative"?1:0));
            foreach(var category in new[]{AbilityCategory.Offensive,AbilityCategory.Utility}){
                foreach(var a in w.Abilities.Abilities.Where(a=>a.Definition.Category==category && !a.Definition.IsBasicAttack && !a.Definition.Slotless))w.Abilities.Unlock(a.Definition.Id);
                Check(category+" cap admits full class loadout",w.Abilities.Equipped(category)==w.Abilities.Limit(category));
                Check(category+" cap still enforces replacements",w.Abilities.Abilities.Any(a=>!a.Unlocked && a.Definition.Category==category && !w.Abilities.CanUnlock(a)));
            }
            Check("full Agent loadout fits quickbar",w.Abilities.QuickbarAbilities.Length==w.Abilities.Abilities.Count(a=>a.Unlocked));
            w.Abilities.ResetUnlocks();
            foreach(var a in w.Abilities.Abilities)Check(a.Definition.Id+" icon asset resolves",ResourceLoader.Exists("res://Assets/UI/icons/"+a.Definition.Icon+".png"));
            Check("all talent icons resolve",ts.All(t=>ResourceLoader.Exists(t.Icon)));
            Check("medical and shield icons differ",cls!="operative" || w.Abilities.Find("koltoinjection")!.Definition.Icon!=w.Abilities.Find("shieldprobe")!.Definition.Icon);
            Check("Rifle Shot and Snipe icons differ",cls!="sniper" || w.Abilities.Find("basicrifle")!.Definition.Icon!=w.Combat.Definition.Icon);
            Check("all original gesture bindings resolve",w.Abilities.Abilities.All(a=>a.Definition.StartClip=="" || w.Player.Presentation.Animator.HasAnimation(a.Definition.StartClip)));
            var target=new Combatant {Name="AgentFixture",Team=CombatTeam.Hostile};target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Forward*.16f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var rt=w.Build.Pregame;rt.Reset();
            if(cls=="operative") {
                rt.Activated(w.Abilities.Find("shiv")!);rt.Activated(w.Abilities.Find("shiv")!);rt.Activated(w.Abilities.Find("shiv")!);
                Check("Tactical Advantage caps at two",rt.TacticalAdvantage==2);
                rt.Activated(w.Abilities.Find("laceration")!);Check("Laceration spends Advantage",rt.TacticalAdvantage==1);
                rt.Advance(13);Check("Advantage expires",rt.TacticalAdvantage==0);
                rt.Activated(w.Abilities.Find("agentstealth")!);Check("stealth opens Hidden Strike",rt.CanBeginAgent(w.Abilities.Find("hiddenstrike")!));
                rt.Activated(w.Abilities.Find("hiddenstrike")!);Check("opening is consumed",!rt.CanBeginAgent(w.Abilities.Find("hiddenstrike")!));
                Check("baseline knives cleave",new[]{"shiv","backstab","laceration"}.All(id=>w.Abilities.Find(id)!.Definition.Area==AbilityArea.FrontalCone));
            } else {
                Check("baseline precision shots hit groups",new[]{"snipershot","ambush","seriesofshots"}.All(id=>w.Abilities.Find(id)!.Definition.Area==AbilityArea.Chain));
                foreach(string id in new[]{"snipershot","ambush","seriesofshots","suppressivefire","plasmaprobe","orbitalstrike","explosiveprobe","agentcull"}){
                    var a=w.Abilities.Find(id)!;a.SetUnlocked(true);a.ResetCombat();
                    Check(id+" cannot start before cover without spending cooldown",!a.TryBegin() && a.CooldownRemaining==0 && a.Casts==0);
                }
                rt.Advance(.61);
                rt.Activated(w.Abilities.Find("snipershot")!);Check("precision enables followthrough",rt.CanBeginAgent(w.Abilities.Find("followthrough")!));
                rt.Activated(w.Abilities.Find("followthrough")!);Check("followthrough consumes window",!rt.CanBeginAgent(w.Abilities.Find("followthrough")!));
            }
            var cull=w.Abilities.Find("agentcull")!;float before=rt.DirectBonus(cull,target);
            w.Periodic.Apply(w.Abilities.Find("dart")!,target);w.Periodic.Apply(w.Abilities.Find("corrosivegrenade")!,target);
            Check("Cull scales with two owned poisons",rt.DirectBonus(cull,target)>=before+.69f);
            var second=new Combatant {Name="AgentGroupFixture",Team=CombatTeam.Hostile};second.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(second);second.GlobalPosition=target.GlobalPosition+Vector3.Right*.035f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(string id in cls=="operative"?new[]{"shiv","backstab","laceration","corrosivegrenade","severtendon","carbineburst"}:new[]{"snipershot","ambush","seriesofshots","suppressivefire","orbitalstrike","plasmaprobe","explosiveprobe","agentcull"}){
                var a=w.Abilities.Find(id)!;w.Player.Presentation.ResetPresentation();a.SetUnlocked(true);a.ResetCombat();
                w.Targeting.Update(0,true);float h=target.Health.Current,h2=second.Health.Current;
                Check(id+" begins combat",a.TryBegin());
                if(id=="laceration"){
                    var visual=(D3AbilityPresentation)a.Presentation;int contacts=visual.AgentKnifeImpactsPresented;int advantage=rt.TacticalAdvantage;
                    Check("Laceration joins distinct recovered first and follow-up motions",w.Player.Presentation.Animator.HasAnimation("Agent/LacerationFollowup") && Math.Abs(w.Player.Presentation.Animator.GetAnimation("Agent/LacerationDouble").Length-2*w.Player.Presentation.Animator.GetAnimation("Agent/Laceration").Length)<.00001);
                    a.Advance(.19);Check("Laceration waits for first contact",target.Health.Current==h);
                    a.Advance(.02);float first=h-target.Health.Current;Check("Laceration first swipe hits the pack",first>0 && second.Health.Current<h2 && visual.AgentKnifeImpactsPresented==contacts+1);
                    a.Advance(.58);Check("Laceration separates its two contacts",target.Health.Current==h-first);
                    a.Advance(.02);Check("Laceration second swipe hits and sounds once",Math.Abs((h-target.Health.Current)-2*first)<.001 && visual.AgentKnifeImpactsPresented==contacts+2);
                    Check("second swipe does not spend more Tactical Advantage",rt.TacticalAdvantage==advantage);
                }
                if(cls=="sniper" && id is "suppressivefire" or "plasmaprobe"){
                    var visual=(D3AbilityPresentation)a.Presentation;
                    Check(id+" displays recovered Imperial ground marker",visual.AgentAreaIndicator is {EffectiveRadius:>0});
                    rt.Reset();Check(id+" cover can be left during active cast",!rt.AgentPositioned);
                }
                for(double t=0;t<8;t+=.05)a.Advance(.05);
                Check(id+" damages a group without talents",target.Health.Current<h && second.Health.Current<h2);
                if(cls=="sniper")rt.Advance(.61);
            }
            if(cls=="operative"){
                var lac=w.Abilities.Find("laceration")!;lac.ResetCombat();w.Player.Presentation.ResetPresentation();w.Targeting.Update(0,true);float ticks=lac.TickSeconds;lac.TickSeconds=.1f;
                var visual=(D3AbilityPresentation)lac.Presentation;int contacts=visual.AgentKnifeImpactsPresented;
                Check("Laceration haste fixture begins",lac.TryBegin());for(int i=0;i<32;i++)lac.Advance(.05);
                Check("tick acceleration cannot add Laceration swipes or sounds",lac.DamageEvents==4 && visual.AgentKnifeImpactsPresented==contacts+2);lac.TickSeconds=ticks;
                lac.ResetCombat();w.Player.Presentation.ResetPresentation();w.Targeting.Update(0,true);Check("Laceration interruption fixture begins",lac.TryBegin());lac.Advance(.21);float afterFirst=target.Health.Current;lac.Interrupt();lac.Advance(2);
                Check("interruption cancels Laceration second damage and presentation",target.Health.Current==afterFirst && ((D3AbilityPresentation)lac.Presentation).EffectCount==0);
            }
            foreach(var a in w.Abilities.Abilities)Check(a.Definition.Id+" tuning finite",a.Range>0 && float.IsFinite(a.DamagePerTick));
            TalentRuntime Load(params string[] ids){Check("talent profile saves",TalentProfiles.Save(cls,ids.ToDictionary(id=>id,id=>1))==null);var r=new TalentRuntime(w);r.Reset();return r;}
            var hp=w.PlayerActor.Health;hp.ClearShield();hp.SpendHealth(40);
            if(cls=="operative"){
                var medicine=Load("medicine.enduring_kolto","medicine.medical_screen","medicine.emergency_response");
                float h=hp.Current;medicine.PeriodicTick(w.Abilities.Find("dart")!);medicine.PeriodicTick(w.Abilities.Find("dart")!);
                Check("Enduring Kolto heals with shared per-second cap",hp.Current==h+2);
                medicine.Activated(w.Abilities.Find("koltoinjection")!);Check("medical screen grants bounded shield",hp.ShieldRemaining==10);
                hp.ClearShield();hp.SpendHealth(hp.Current-hp.Maximum*.2f);h=hp.Current;medicine.Damaged(1);medicine.Damaged(1);
                Check("emergency healing has shared cooldown",hp.Current==h+15);
                var concealment=Load("concealment.tactical_opportunity","concealment.revitalizers","concealment.meticulously_kept_blades");
                concealment.Advance(7);for(int i=0;i<4;i++)concealment.Activated(w.Abilities.Find("backstab")!);
                Check("knife combo renews offensive opening",concealment.AgentOpening);
                h=hp.Current;concealment.Activated(w.Abilities.Find("laceration")!);
                Check("Venomous Blades replaces the old sustain effect",hp.Current==h && concealment.AgentVenomousBlades);
                concealment.Advance(5);Check("replacement adds no unrelated mitigation",concealment.IncomingMultiplier==1);
                var control=Load("concealment.advanced_cloaking","concealment.flanking","concealment.culling","concealment.collateral_strike");
                var mass=w.Abilities.Find("debilitate")!;mass.SetUnlocked(true);mass.ResetCombat();control.ApplyStats(mass);
                var pack=new List<EnemyAgent>();
                for(int i=0;i<7;i++){
                    var packBody=new CharacterBody3D();var member=new EnemyAgent {Name="ControlledPack"+i,Team=CombatTeam.Hostile,Target=w.PlayerActor};member.AddChild(new Health {Name="Health",Maximum=1000});packBody.AddChild(member);w.AddChild(packBody);member.SetPhysicsProcess(false);
                    packBody.GlobalPosition=w.PlayerActor.GlobalPosition+new Vector3(Mathf.Sin(i)*.2f,0,Mathf.Cos(i)*.2f);pack.Add(member);
                }
                Action<Combatant,float> mark=(t,_)=>control.DirectHit(mass,t);mass.DamageDealt+=mark;
                w.Player.Presentation.ResetPresentation();w.Targeting.Update(0,true);Check("Mass Debilitation starts against surrounding pack",mass.TryBegin());
                for(int i=0;i<30;i++)mass.Advance(.05);
                Check("uncapped area stuns and exposes all seven enemies",pack.All(e=>e.StunRemaining>0 && control.DirectBonus(w.Abilities.Find("shiv")!,e)>=.39f));
                mass.DamageDealt-=mark;mass.ResetCombat();foreach(var member in pack){member.CombatEnabled=false;member.GetParent().QueueFree();}
                control.Reset();
                var stun=new AutomaticAbility();stun.Configure(w.Abilities.Find("debilitate")!.Definition);control.ApplyStats(stun);
                Check("Mass Debilitation is offensive uncapped caster area",stun.Definition.Category==AbilityCategory.Offensive && stun.Definition.Area==AbilityArea.CasterRadial && stun.Radius==.55f && stun.Definition.MaximumTargets==0);
                float baseBonus=control.DirectBonus(w.Abilities.Find("laceration")!,target);
                control.DirectHit(stun,target);
                Check("stun-immune target receives vulnerability and controlled burst",control.DirectBonus(w.Abilities.Find("laceration")!,target)>=baseBonus+.89f);
                Check("damage mark benefits other attacks",control.DirectBonus(w.Abilities.Find("shiv")!,target)>=.39f);
                control.Advance(6.1);Check("damage mark expires",control.DirectBonus(w.Abilities.Find("laceration")!,target)==baseBonus);
                var cb=new AutomaticAbility();cb.Configure(w.Abilities.Find("laceration")!.Definition);float cbDamage=cb.DamagePerTick,cbRadius=cb.Radius;control.ApplyStats(cb);
                Check("Laceration talents improve damage and coverage",cb.DamagePerTick>=cbDamage*1.69f && cb.Radius==cbRadius*1.5f);
                Check("Blow for Blow is talent gated",!rt.CanBeginAgent(w.Abilities.Find("blowforblow")!) && control.CanBeginAgent(w.Abilities.Find("blowforblow")!));
                control.Activated(w.Abilities.Find("blowforblow")!);Check("retaliation window begins",control.AgentCounterstrikeActive);
                var body=new CharacterBody3D();var attacker=new EnemyAgent {Name="RetaliationFixture",Team=CombatTeam.Hostile,Target=w.PlayerActor};attacker.AddChild(new Health {Name="Health",Maximum=1000});body.AddChild(attacker);w.AddChild(body);attacker.SetPhysicsProcess(false);body.GlobalPosition=target.GlobalPosition;
                float enemyHp=attacker.Health.Current;hp.Restore();hp.ClearShield();float taken=hp.ApplyDamage(10);control.AgentRetaliate(attacker,taken);
                Check("returns 150 percent of actual health damage without preventing it",attacker.Health.Current==enemyHp-15 && hp.Current==hp.Maximum-10);
                hp.ApplyShield(20,5);enemyHp=attacker.Health.Current;control.AgentRetaliate(attacker,hp.ApplyDamage(10));
                Check("fully absorbed damage does not reflect",attacker.Health.Current==enemyHp);
                taken=hp.ApplyDamage(15);control.AgentRetaliate(attacker,taken);Check("partial absorption reflects only health loss",taken==5 && attacker.Health.Current==enemyHp-7.5f);
                attacker.RewardTier=2;control.DirectHit(stun,attacker);Check("boss resists stun but retains offensive mark",attacker.StunRemaining==0 && control.DirectBonus(w.Abilities.Find("shiv")!,attacker)>=.39f);
                body.QueueFree();
                control.Advance(4.1);Check("retaliation expires",!control.AgentCounterstrikeActive);
                control.Activated(w.Abilities.Find("blowforblow")!);control.Reset();Check("restart clears retaliation",!control.AgentCounterstrikeActive);
                var sample=new AutomaticAbility();sample.Configure(w.Abilities.Find("backstab")!.Definition);float damage=sample.DamagePerTick,radius=sample.Radius;concealment.ApplyStats(sample);
                Check("knife training scales damage and clearing",sample.DamagePerTick>damage*1.19f && sample.Radius>radius*1.29f);
            }else{
                var marksman=Load("marksmanship.sector_ranger","marksmanship.deployed_cover","marksmanship.reactive_shots");
                var rifle=new AutomaticAbility();rifle.Configure(w.Abilities.Find("seriesofshots")!.Definition);marksman.ApplyStats(rifle);
                Check("Focused Barrage improves Series of Shots ricochets",rifle.Definition.Area==AbilityArea.Chain && rifle.ExtraBounces==2);
                marksman.DirectHit(w.Abilities.Find("ambush")!,target);Check("core attack shield is bounded",hp.ShieldRemaining==8);
                hp.ClearShield();hp.SpendHealth(20);float h=hp.Current;
                var engineering=Load("engineering.interrogation_tactics","engineering.explosive_engineering","engineering.energy_overrides");engineering.Activated(w.Abilities.Find("empdischarge")!);
                Check("engineering EMP provides sustain",hp.Current==h+12);
                var orbital=new AutomaticAbility();orbital.Configure(w.Abilities.Find("orbitalstrike")!.Definition);float damage=orbital.DamagePerTick,radius=orbital.Radius;engineering.ApplyStats(orbital);
                Check("engineering strengthens artillery without gating baseline AoE",orbital.DamagePerTick>damage*1.19f && orbital.Radius==radius*1.25f);
            }
            var lethal=Load("agent_lethality.targeted_demolition","agent_lethality.toxic_resilience","agent_lethality.lethal_training");
            var dart=w.Abilities.Find("dart")!;var grenade=w.Abilities.Find("corrosivegrenade")!;
            w.Periodic.RemoveSource(dart);w.Periodic.RemoveSource(grenade);w.Periodic.Apply(dart,target);w.Periodic.Apply(grenade,target);
            if(cls=="sniper") {
                float poisonedHealth=target.Health.Current;lethal.DirectHit(cull,target);
                Check("Sniper Cull echoes poison without inheriting Operative spread",target.Health.Current<poisonedHealth && !w.Periodic.Has(dart,second));
                var weak=new AutomaticAbility();weak.Configure(w.Abilities.Find("weakeningblast")!.Definition);lethal.ApplyStats(weak);
                Check("Sniper talent puts contagion on Weakening Blast",weak.Definition.SpreadsPeriodicEffects && weak.Definition.Area==AbilityArea.TargetSplash && weak.Radius>=.3f);weak.Free();
            }else {
                lethal.DirectHit(cull,target);
                Check("Cull spreads both owned poisons",w.Periodic.Has(dart,second) && w.Periodic.Has(grenade,second));
            }
            var hybrid=ts.GroupBy(t=>t.Tree).SelectMany(g=>g.Take(3)).Append(ts.Last()).DistinctBy(t=>t.Id).ToDictionary(t=>t.Id,t=>1);
            Check("ten-point three-tree hybrid remains valid",hybrid.Count==10 && cat.Validate(cls,hybrid).Count==10);
            CheckProgressionOffers(w,Check);
            CheckCoreRefinements(w,target,second,Check);
            TalentProfiles.Save(cls,new Dictionary<string,int>());
        }catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../manifests/agent-"+w.RunConfiguration.AdvancedClassId+"-smoke.json"),JsonSerializer.Serialize(new{passed=failure==null,failure,checks},new JsonSerializerOptions{WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
