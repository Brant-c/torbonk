using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using SwtorSurvivors.Equipment;
namespace SwtorSurvivors;

// Per-run state. Secondary damage deliberately bypasses proc hooks; death callbacks are guarded.
public sealed class AssassinRelics(RunBuildEffects build)
{
    private GameWorld W=>build.World;
    private double _time,_cloakUntil,_hasteUntil,_nextHeal;
    private bool _armed,_voltaic,_secondary;
    private AutomaticAbility? _openingAttack;
    private readonly Dictionary<Combatant,double> _marks=new();
    private sealed class Zone { public AutomaticAbility Source=null!;public Vector3 Center;public float Radius;public double End,Next;public bool Aura,Shroud;public Node3D? Visual; }
    private readonly List<Zone> _zones=new();
    public int Rank(string id) {
        if(W.RunConfiguration.AdvancedClassId!="assassin")return -1;
        var inv=UI.EquipmentWindows.Instance?.Inventory;
        return inv!=null && PersistentInventory.Slots.Where(s=>s.StartsWith("relic")).Any(s=>inv.Equipped("assassin",s)==id)?inv.State.Owned.GetValueOrDefault(id,-1):-1;
    }
    private bool Saber(AutomaticAbility a)=>a.Definition.IsBasicAttack || a.Definition.Id is "maul" or "voltaicslash" or "lacerate" or "lowslash" or "spike";
    public float Haste=>Rank("hidden_predator")>=0 && _hasteUntil>_time?1.5f:1;
    public float Radius(AutomaticAbility a)=>a.Radius*(a.Definition.Id=="wither" && Rank("withering_force") is int r && r>=0?new[]{1.25f,1.4f,1.6f,1.8f}[r]:1);
    public float Bonus(AutomaticAbility a,Combatant t) {
        float b=a.Definition.Id=="wither" && Rank("withering_force") is int r && r>=0?new[]{.75f,1.25f,1.75f,2.5f}[r]:0;
        if(Saber(a) && Rank("lingering_death_field")==3 && _zones.Any(z=>!z.Aura && z.End>_time && t.GlobalPosition.DistanceTo(z.Center)<=z.Radius))b+=.25f;
        return b;
    }
    public void Activated(AutomaticAbility a) {
        if(a.Caster!=W.PlayerActor)return;
        if(a.Definition.Id=="forcecloak" && Rank("hidden_predator")>=0){_armed=true;_cloakUntil=_time+2;}
        else if(_armed && Saber(a) && ( _time>=_cloakUntil || a.Definition.Effect==AbilityEffect.Damage)) {
            _armed=false;build.Pregame.RevealSpike();_openingAttack=a;_hasteUntil=_time+2+Rank("hidden_predator");
        }
        int r=Rank("shrouded_assassin");
        if(r>=0 && a.Definition.Id is "forceshroud" or "deflection") {
            bool shroud=a.Definition.Id=="forceshroud";
            _zones.RemoveAll(z=>{if(z.Aura && z.Shroud==shroud){z.Visual?.QueueFree();return true;}return false;});
            _zones.Add(new Zone{Source=a,Aura=true,Shroud=shroud,Radius=.25f+.05f*r,End=_time+(shroud?a.ShieldDuration:build.Pregame.AssassinDeflectionDuration),Next=_time+.8-.15*r});
        }
    }
    private Combatant[] Near(AutomaticAbility a,Vector3 center,float radius)=>a.Targeting.InArea(center,radius,a.Range,true).OrderBy(t=>t.GlobalPosition.DistanceSquaredTo(center)).ThenBy(t=>t.SelectionKey,StringComparer.Ordinal).ToArray();
    private void Wave(Vector3 center,float radius){W.AddChild(new AssassinWitherVisual{Center=center,Radius=radius});}
    private void Hit(AutomaticAbility a,Combatant t,float damage,bool suppressDeath=false) {
        if(!t.Alive || !a.Caster.Alive)return;
        bool old=_secondary;_secondary|=suppressDeath;
        try {if(t.Health.ApplyDamage(damage,DamageType.Internal,false,"assassin_relic")>0)Heal(a,t);} finally {_secondary=old;}
    }
    private void Heal(AutomaticAbility a,Combatant t) {
        int r=Rank("dark_parasite");
        if(r>=0 && _time>=_nextHeal && W.Periodic.On(t).Any(x=>x.Caster==a.Caster && x.Definition.Id=="creepingterror")) {
            _nextHeal=_time+1;a.Caster.Health.ApplyHealing(2+r*2);
        }
    }
    public void Damaged(AutomaticAbility a,Combatant t) {
        if(a.Caster!=W.PlayerActor)return;
        Heal(a,t);
        int r;
        if(a.Definition.Id=="wither" && Rank("crushing_darkness")>=0)_marks[t]=_time+6;
        r=Rank("crushing_darkness");
        if(r>=0 && Saber(a) && _marks.GetValueOrDefault(t)>_time) {
            foreach(var other in Near(a,t.GlobalPosition,.6f).Where(x=>x!=t && _marks.GetValueOrDefault(x)>_time).Take(2+r))Hit(a,other,a.DamagePerTick*(.3f+.15f*r));
            Wave(t.GlobalPosition,.2f);
        }
        if(a.Definition.Id=="voltaicslash" && Rank("voltaic_fury")>=0)_voltaic=true;
    }
    public void Resolved(AutomaticAbility a,Combatant[] targets) {
        if(a.Caster!=W.PlayerActor || targets.Length==0)return;
        var primary=a.CastTarget??targets[0];int r;
        if(a.Definition.Id=="shock" && _voltaic && (r=Rank("voltaic_fury"))>=0) {
            _voltaic=false;var from=primary.AimPosition;
            foreach(var t in Near(a,primary.GlobalPosition,.5f).Where(t=>!targets.Contains(t)).Take(2+r)) {
                Hit(a,t,a.DamagePerTick*.7f);if(a.Presentation is IArcPresentation v)v.Arc(from,t.AimPosition);from=t.AimPosition;
            }
        }
        if(a.Definition.Id=="maul" && (r=Rank("phantom_blade"))>=0) {
            foreach(var t in Near(a,primary.GlobalPosition,.18f+.06f*r).Append(primary).Distinct()) {
                Hit(a,t,a.DamagePerTick*(.4f+.2f*r));if(a.Presentation is AssassinAbilityPresentation v)v.TargetImpact(t);
            }
        }
        if(_openingAttack==a) {
            _openingAttack=null;r=Rank("hidden_predator");if(r>=0)foreach(var t in Near(a,primary.GlobalPosition,.35f).Where(t=>!targets.Contains(t)).Take(2+r))Hit(a,t,a.DamagePerTick);
        }
        if(a.Definition.Id=="deathfield" && (r=Rank("lingering_death_field"))>=0) {
            var material=new ShaderMaterial{Shader=GD.Load<Shader>("res://Gameplay/DeathFieldGround.gdshader")};
            material.SetShaderParameter("field_texture",GD.Load<Texture2D>("res://Assets/B8/si_death_field_d.png"));
            material.SetShaderParameter("ring_texture",GD.Load<Texture2D>("res://Assets/B8/jk_proj_wave_force_ring.png"));
            var visual=new StormAreaIndicator{ProjectionMaterial=material};W.AddChild(visual);visual.UpdateArea(a.GroundCenter,a.Radius);
            _zones.Add(new Zone{Source=a,Center=a.GroundCenter,Radius=a.Radius,End=_time+4+2*r,Next=_time+1,Visual=visual});
        }
    }
    public void Spread(AutomaticAbility a,IEnumerable<Combatant> fresh) {
        int r=Rank("devouring_plague");if(a.Definition.Id!="lacerate" || r<0)return;
        foreach(var t in fresh.OrderBy(t=>t.SelectionKey,StringComparer.Ordinal).Take(2+r))Hit(a,t,a.DamagePerTick*(.5f+.25f*r));
    }
    public void Killed(Combatant dead) {
        if(_secondary)return;
        int r=Rank("dark_parasite");
        var terror=W.Periodic.On(dead).FirstOrDefault(a=>a.Caster==W.PlayerActor && a.Definition.Id=="creepingterror");
        if(r>=0 && terror!=null){foreach(var t in Near(terror,dead.GlobalPosition,.2f+.05f*r))Hit(terror,t,terror.DamagePerTick*.75f,true);Wave(dead.GlobalPosition,.2f+.05f*r);}
    }
    // Called directly from Wither's successful lethal damage so unrelated kills cannot trigger it.
    public void WitherKilled(AutomaticAbility a,Combatant dead) {
        if(a.Definition.Id!="wither" || Rank("withering_force")!=3)return;
        Wave(dead.GlobalPosition,Radius(a)*.5f);foreach(var t in Near(a,dead.GlobalPosition,Radius(a)*.5f))Hit(a,t,a.DamagePerTick*1.75f);
    }
    public void Advance(double delta) {
        _time+=delta;
        foreach(var t in _marks.Keys.Where(t=>!GodotObject.IsInstanceValid(t) || !t.Alive || _marks[t]<=_time).ToArray())_marks.Remove(t);
        foreach(var z in _zones.ToArray()) {
            int r=Rank(z.Aura?"shrouded_assassin":"lingering_death_field");
            double activeEnd=Math.Min(_time,z.End);
            if(!z.Source.Caster.Alive || !z.Source.CombatEnabled || r<0 || z.Aura && z.Shroud && !z.Source.Caster.Health.Shielded)activeEnd=-1;
            while(z.Next<=activeEnd) {
                z.Next+=z.Aura?.8-.15*r:1;
                var center=z.Aura?z.Source.Caster.GlobalPosition:z.Center;
                foreach(var t in Near(z.Source,center,z.Radius))Hit(z.Source,t,z.Aura?10:z.Source.DamagePerTick*.3f);
                if(z.Aura)Wave(center,z.Radius);
            }
            if(activeEnd<0 || _time>=z.End){z.Visual?.QueueFree();_zones.Remove(z);}
        }
    }
    public void Reset(){foreach(var z in _zones)z.Visual?.QueueFree();_zones.Clear();_marks.Clear();_time=_cloakUntil=_hasteUntil=_nextHeal=0;_armed=_voltaic=_secondary=false;_openingAttack=null;}
}

