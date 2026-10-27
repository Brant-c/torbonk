using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SwtorSurvivors.Frontend;
using SwtorSurvivors.Integration;

namespace SwtorSurvivors.Talents;

/// <summary>Small isolated T1 acceptance fixture. Never reads or overwrites the normal player's save.</summary>
public partial class TalentsSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool passed){if(!passed)throw new Exception(name);_checks.Add(name);GD.Print("T1: "+name);}
    public async void Run(FrontendFlow menu)
    {
        string? failure=null;
        try
        {
            var catalog=TalentCatalog.Current;
            var build=new Dictionary<string,int>{
                ["lightning.convection"]=2,["lightning.lightning_spire"]=2,["lightning.conduction"]=1,
                ["madness.calcify"]=2,["madness.torment"]=1,
                ["corruption.dark_mending"]=2,["corruption.empty_body"]=2};
            if(OS.GetCmdlineUserArgs().Contains("--talents-persistence-check"))
            {
                Check("allocation survives another process launch",TalentProfiles.Load("sorcerer").OrderBy(p=>p.Key).SequenceEqual(build.OrderBy(p=>p.Key)));
            }
            else
            {
                Check("isolated profile initialized",TalentProfiles.Save("sorcerer",new Dictionary<string,int>())==null);
                Check("24 data talents across three trees",catalog.Talents.Length==24 && catalog.Trees.All(t=>catalog.Talents.Count(a=>a.Tree==t.Id)==8));
                menu.ShowTalents();
                var screen=menu.FindChild("Talents",true,false) as TalentsScreen;
                Check("main menu opens Talents",screen!=null);
                Check("capstone requires investment",!screen!.Invest("lightning.thundering_blast"));
                Check("first rank allocates",screen.Invest("lightning.convection"));
                Check("second rank allocates",screen.Invest("lightning.convection"));
                Check("rank limit enforced",!screen.Invest("lightning.convection"));
                screen.Respec();Check("free respec clears saved profile",TalentProfiles.Load("sorcerer").Count==0);
                screen.SelectClass("juggernaut");Check("other classes unavailable",!screen.Invest("lightning.convection"));
                screen.SelectClass("sorcerer");
                foreach(var pair in build)for(int i=0;i<pair.Value;i++)Check("allocate "+pair.Key,screen.Invest(pair.Key));
                Check("12-point hybrid budget",catalog.Spent(screen.Ranks)==12 && !screen.Invest("madness.torment"));
                var invalid=new Dictionary<string,int>(build){["missing.old.talent"]=8,["madness.creeping_death"]=1};
                Check("stale IDs and unmet prerequisites discarded",catalog.Validate("sorcerer",invalid).Count==build.Count);
                Check("other class cannot inherit talents",catalog.Validate("assassin",build).Count==0);
                var dependent=new Dictionary<string,int>{
                    ["lightning.convection"]=2,["lightning.lightning_spire"]=2,
                    ["lightning.conduction"]=2,["lightning.lightning_barrage"]=2};
                Check("valid higher-tier dependency survives",catalog.Validate("sorcerer",dependent).GetValueOrDefault("lightning.lightning_barrage")==2);
                dependent.Remove("lightning.conduction");
                Check("invalid dependency removed on migration",!catalog.Validate("sorcerer",dependent).ContainsKey("lightning.lightning_barrage"));
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(DisplayServer.GetName()!="headless")
                    GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../manifests/t1-talents.png"));
                var selection=new SelectionConfiguration {FactionId="empire",BaseClassId="sith_inquisitor",AdvancedClassId="sorcerer",Appearance=AppearanceConfiguration.ForClass("sorcerer")};
                Check("existing frontend run handoff accepts talents",GameNavigation.TryStart(menu,selection,out _));
                GameWorld? world=null;
                for(int frame=0;frame<600;frame++)
                {
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    if(GetTree().CurrentScene is GameWorld {Progression:not null} ready){world=ready;break;}
                }
                Check("run starts through existing selection flow",world!=null);
                var w=world!;
                w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
                foreach(var enemy in w.Spawner.Enemies){enemy.CombatEnabled=false;enemy.SetPhysicsProcess(false);}
                var p=w.Progression!;
                Check("starting ability preserved",w.Abilities.Abilities.Count(a=>a.Unlocked)==1 && w.Combat.Definition.Id=="lightning");
                Check("run uses validated saved snapshot",w.Build.Pregame.Ranks.Count==build.Count);
                float damage=w.Combat.DamagePerTick;
                Check("talent damage applied",Math.Abs(damage-w.Combat.Definition.Damage*1.12f)<.001f);
                p.Recalculate();p.Recalculate();Check("recalculation applies once",Math.Abs(w.Combat.DamagePerTick-damage)<.001f);
                Check("Storm and Death Field hybrid unlocks",p.Grant("unlock_storm") && p.Grant("unlock_deathfield") && p.Grant("unlock_affliction"));
                Check("offensive limit retained",w.Abilities.Equipped(AbilityCategory.Offensive)==4 && !p.Grant("unlock_shock"));
                Check("full-slot off-tree unlock remains replacement eligible",p.IsEligible(p.Upgrades.First(u=>u.Id=="unlock_shock")));
                Check("no Sorcerer spec milestones remain",p.Upgrades.All(u=>u.MilestoneLevel==0));
                for(int level=2;level<=5;level++)
                {
                    p.AddExperience(p.RequiredExperience);p._Process(10);
                    Check("normal mixed offer at level "+level,p.Level==level && !p.SpecChoice && p.Offers.Count==3);
                    int index=p.Offers.ToList().FindIndex(u=>u.Effect!=UpgradeEffect.UnlockAbility);
                    Check("ordinary offer can be selected",p.Choose(Math.Max(0,index)));
                    if(p.Replacing)Check("full-slot selection replaces an ability",p.Replace(0));
                    p._Process(10);
                }
                var target=new Combatant {Name="T1Fixture",Team=CombatTeam.Hostile};
                target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);
                target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.2f;
                var dot=w.Abilities.Find("affliction")!;
                w.Periodic.Apply(dot,target);
                Check("existing DoT system accepts talented source",w.Periodic.Has(dot,target));
                Check("Death Field still uses existing area propagation",w.Abilities.Find("deathfield")!.Definition.SpreadsPeriodicEffects);
                w.ResetCombatWorld();
                Check("restart clears temporary run power",p.Level==1 && p.Ranks.Count==0);
                Check("restart preserves talent snapshot without stacking",Math.Abs(w.Combat.DamagePerTick-damage)<.001f && w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
                Check("respec cannot change active run snapshot",TalentProfiles.Save("sorcerer",new Dictionary<string,int>())==null && w.Build.Pregame.Ranks.Count==build.Count);
                Check("restore fixture for launch-persistence check",TalentProfiles.Save("sorcerer",build)==null);
            }
        }
        catch(Exception e){failure=e.ToString();GD.PushError(failure);}
        string suffix=OS.GetCmdlineUserArgs().Contains("--talents-persistence-check")?"persistence":"smoke";
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/t1-talents-{suffix}.json"),
            JsonSerializer.Serialize(new {passed=failure==null,failure,checks=_checks},new JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
