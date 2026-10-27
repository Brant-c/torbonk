using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace SwtorSurvivors;

public partial class BasicAttacksSmoke : Node
{
    private readonly List<string> _checks=new();
    private void Check(string name,bool pass){if(!pass)throw new Exception(name);_checks.Add(name);GD.Print("BASIC: "+name);}
    public async void Run(GameWorld w)
    {
        string? failure=null;
        try
        {
            w.Waves!.SetPhysicsProcess(false);w.Spawner!.Running=false;w.Player.SetPhysicsProcess(false);w.Abilities.SetPhysicsProcess(false);w.Periodic.SetPhysicsProcess(false);
            foreach(var enemy in w.Spawner.Enemies){enemy.SetPhysicsProcess(false);enemy.CombatEnabled=false;}
            var target=new Combatant {Name="BasicFixture",Team=CombatTeam.Hostile};target.AddChild(new Health {Name="Health",Maximum=10000});w.AddChild(target);target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(w.RunConfiguration.AdvancedClassId=="sorcerer")
            {
                Check("Sorcerer starts with Force Lightning alone",w.Combat.Definition.Id=="lightning" && w.Combat.Unlocked && w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
                Check("Sorcerer has no automatic saber attack",!w.Abilities.Abilities.Any(a=>a.Definition.IsBasicAttack));
                Check("original Force Lightning presentation retained",w.Combat.Presentation is LightningPresentation);
                w.Player.Presentation.ResetPresentation();w.Abilities._PhysicsProcess(.01);for(int i=0;i<40;i++)w.Abilities._PhysicsProcess(.05);
                Check("Force Lightning deals starting damage",w.Combat.DamageEvents>0);
                w.Progression!.ResetRun();Check("Sorcerer restart retains one starting attack",w.Abilities.Abilities.Count(a=>a.Unlocked)==1);
            }
            else
            {
            var basic=w.Abilities.Abilities.Single(a=>a.Definition.IsBasicAttack);var starter=w.Combat;
            Check("starter plus correct basic are unlocked",basic.Unlocked && starter.Unlocked && w.Abilities.Abilities.Count(a=>a.Unlocked)==2 && basic.Definition.Id==w.RunConfiguration.Profile.BasicAttack);
            Check("basic uses no offensive slot",w.Abilities.Equipped(AbilityCategory.Offensive)==1);
            Check("original weapon animation available",w.Player.Presentation.Animator.HasAnimation(basic.Definition.StartClip));
            Check("basic avoids shared cooldown",!basic.Definition.UsesGlobalCooldown && !basic.Definition.TriggersGlobalCooldown);
            // Force starter readiness with a single target for radial starter fixtures.
            starter.Definition.NearbyTriggerCount=1;
            w.Player.Presentation.ResetPresentation();w.Abilities.ResetCombat();
            w.Abilities._PhysicsProcess(.01);
            Check("class starter wins priority",starter.Casts==1 && basic.Casts==0);
            starter.Abort();starter.CombatEnabled=false;w.Player.Presentation.ResetPresentation();
            w.Abilities._PhysicsProcess(.2);
            Check("basic fills an active global cooldown",basic.Casts==1 && w.Abilities.GlobalCooldownRemaining>0);
            float hp=target.Health.Current;w.Abilities._PhysicsProcess(.13);
            Check("basic deals weapon damage",target.Health.Current<hp && basic.DamageEvents==1);
            Check("basic leaves global cooldown running",w.Abilities.GlobalCooldownRemaining>1);
            if(OS.GetCmdlineUserArgs().Contains("--basic-attacks-review"))
            {
                for(int i=0;i<6;i++)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../manifests/basic-attacks-{w.RunConfiguration.AdvancedClassId}.png"));
            }
            // Finish shared cooldown while leaving the low-priority weapon gesture active.
            starter.CombatEnabled=true;starter.ResetCombat();
            for(int i=0;i<16;i++){w.Abilities._PhysicsProcess(.1);w.Player.Presentation._Process(.1);}
            Check("starter is not starved by weapon attacks",starter.Casts>0);
            w.Abilities.ResetCombat();w.Player.Presentation.ResetPresentation();
            Check("weapon windup can begin between abilities",basic.TryBegin());
            hp=target.Health.Current;w.Abilities._PhysicsProcess(.01);
            Check("ready starter preempts weapon gesture without an extra hit",starter.Casts==1 && !basic.Casting && target.Health.Current==hp && w.Player.Presentation.GestureOwner==starter);
            Check("preemption preserves weapon recovery",basic.CooldownRemaining>1);
            // No target in range means no phantom weapon damage/cast.
            w.Abilities.Abort();starter.CombatEnabled=false;basic.ResetCombat();w.Player.Presentation.ResetPresentation();target.GlobalPosition+=Vector3.Right*4;
            Check("basic respects weapon reach",!basic.TryBegin());
            if(w.RunConfiguration.AdvancedClassId=="assassin")
            {
                target.GlobalPosition=w.PlayerActor.GlobalPosition+Vector3.Right*.15f;
                Check("Assassin learns Affliction",w.Progression!.Grant("unlock_affliction"));
                var dot=w.Abilities.Find("affliction")!;w.Player.Presentation.ResetPresentation();Check("Assassin casts Affliction",dot.TryBegin());
                for(int i=0;i<16;i++)dot.Advance(.05);
                Check("Assassin applies periodic Affliction",w.Periodic.Has(dot,target));
                hp=target.Health.Current;w.Periodic.Advance(3);Check("Assassin Affliction ticks",target.Health.Current<hp);
                Check("Creeping Terror prerequisite learnable with Affliction",w.Progression.Grant("unlock_creepingterror"));
                Check("Static Blades available alongside Affliction",w.Progression.Grant("assassin_static_blades"));
                w.Periodic.Advance(11);basic.ResetCombat();w.Player.Presentation.ResetPresentation();Check("Assassin basic hits afflicted target",basic.TryBegin());basic.Advance(.13);w.Periodic.Advance(1.1);
                Check("basic cannot refresh Affliction through Static Blades",!w.Periodic.Has(dot,target));
            }
            w.Abilities.Abort();w.Periodic.Clear();w.Progression!.ResetRun();
            Check("restart retains two starting attacks",w.Abilities.Abilities.Count(a=>a.Unlocked)==2 && w.Abilities.Equipped(AbilityCategory.Offensive)==1);
            Check("basic remains outside unlock offers",!w.Progression.Upgrades.Any(u=>u.Effect==UpgradeEffect.UnlockAbility && u.AbilityId==basic.Definition.Id));
            }
        }
        catch(Exception ex){failure=ex.ToString();GD.PushError(failure);}
        var report=new {passed=failure==null,checks=_checks,failure};
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath($"res://../manifests/basic-attacks-{w.RunConfiguration.AdvancedClassId}-smoke.json"),System.Text.Json.JsonSerializer.Serialize(report,new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
        GetTree().Quit(failure==null?0:1);
    }
}
