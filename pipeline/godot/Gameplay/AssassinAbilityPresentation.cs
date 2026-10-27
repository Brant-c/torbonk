using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>April EPP-linked particles and audio, adapted to finite survivor effects. Combat stays in AutomaticAbility.</summary>
public partial class AssassinAbilityPresentation : Node3D,IAbilityPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    public GameWorld World {get;set;}=null!;
    private sealed class Particle {public Sprite3D Node=null!;public double Age,Life;public float Size;public int Frames;public bool Follow;public Color Tint;}
    private readonly List<Particle> _particles=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private JsonDocument _effects=null!,_audio=null!;
    private float _radius;private double _loop,_emit;
    public int EffectCount=>_particles.Count;
    public int AudioVoicesStarted {get;private set;}
    public bool Vanishing {get;private set;}
    public override void _Ready()
    {
        _effects=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Assassin/render-effects.json"));
        _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Assassin/audio.json"));
        for(int i=0;i<8;i++){var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-20,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};AddChild(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);_voices.Add(voice);}
    }
    private int _voice;
    private void Play(string name,Vector3 at)
    {
        if(!_audio.RootElement.GetProperty("events").TryGetProperty(name,out var id) || !_audio.RootElement.GetProperty("graphs").TryGetProperty(id.GetString()!,out var graph))return;
        var media=graph.GetProperty("media");if(media.GetArrayLength()==0)return;
        var voice=_voices[_voice++%_voices.Count];voice.GlobalPosition=at;voice.Stream=GD.Load<AudioStream>($"res://Assets/Assassin/Audio/{media[0].GetInt32()}.wav");voice.Play();AudioVoicesStarted++;
    }
    private void Emit(string effect,Vector3 at,float size,double life,bool follow=false)
    {
        if(!_effects.RootElement.TryGetProperty(effect,out var recipe))return;
        int index=0;
        foreach(var r in recipe.EnumerateArray().Take(5))
        {
            if(_particles.Count>=72)break;
            var tex=GD.Load<Texture2D>($"res://Assets/Assassin/{r.GetProperty("texture").GetString()}.png");int frames=r.GetProperty("atlas").GetInt32();
            var c=r.GetProperty("color");var tint=new Color(c[0].GetSingle(),c[1].GetSingle(),c[2].GetSingle(),Math.Max(.35f,c[3].GetSingle()));
            var material=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode=r.GetProperty("alpha").GetBoolean()?BaseMaterial3D.BlendModeEnum.Mix:BaseMaterial3D.BlendModeEnum.Add,
                AlbedoTexture=tex,AlbedoColor=tint,BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=false,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
            var sprite=new Sprite3D {Texture=tex,MaterialOverride=material,Hframes=frames,Vframes=frames,PixelSize=size/(tex.GetWidth()/frames),Shaded=false,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
            AddChild(sprite);sprite.GlobalPosition=at+Vector3.Up*(index++*.012f);
            _particles.Add(new Particle {Node=sprite,Life=life,Size=size,Frames=frames,Tint=tint,Follow=follow});
        }
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        Release();_radius=radius;
        if(Definition.Id=="spike"){
            Vanishing=true;Emit("si_mindtrick_cast_01",Actor.GlobalPosition+Vector3.Up*.1f,.16f,.6);
            Play("Play_ability_sithinquisitor_shadowcloak_activate",Actor.GlobalPosition);return true;
        }
        if(Definition.Id=="forcecloak"){
            _loop=2;Emit("si_mindtrick_cast_01",Actor.GlobalPosition+Vector3.Up*.1f,.18f,.6);
            Play("Play_ability_sithinquisitor_shadowcloak_activate",Actor.GlobalPosition);
        }
        if(Definition.Id=="deflection"){
            _loop=4;Emit("si_reflection_buff_loop",Actor.GlobalPosition+Vector3.Up*.1f,.24f,.8,true);
            Play("Play_ability_sithinquisitor_deflection_cast",Actor.GlobalPosition);
        }
        if(Definition.Id=="wither")Emit("si_hand_cast_particle_trail",Actor.GlobalPosition+Vector3.Up*.14f,.12f,.5);
        if(Definition.Id is "maul" or "voltaicslash" or "lowslash"){
            var voice=_voices[_voice++%_voices.Count];voice.Stream=GD.Load<AudioStream>("res://Assets/D3/Audio/173808209.wav");voice.GlobalPosition=Actor.GlobalPosition;voice.Play();AudioVoicesStarted++;
        }
        return Definition.StartClip=="" || Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel()
    {
        if(Definition.Id!="spike")return;
        Vanishing=false;Emit("si_spike_source_saber",Actor.GlobalPosition+Vector3.Up*.12f,.10f,.65);
        Play("Play_ability_sithinquisitor_spike_saber_shock",Actor.GlobalPosition);
    }
    public void Impact(Vector3 center)
    {
        switch(Definition.Id){
            case "spike":Emit("si_spike_target_02",Actor.GlobalPosition+Vector3.Up*.05f,_radius*1.5f,.7);break;
            case "wither":var wave=new AssassinWitherVisual {Center=Actor.GlobalPosition,Radius=_radius};AddChild(wave);Play("Play_ability_sithinquisitor_wither_aoe",center);break;
            case "maul":Play("Play_ability_sithinquisitor_maul_impact",center);break;
            case "lowslash":Play("Play_ability_jediconsular_lowslash_hugehit",center);break;
            case "voltaicslash":Play("Play_ability_jediconsular_clairvoyantstrike_flesh_normal",center);break;
        }
    }
    public void TargetImpact(Combatant target)
    {
        if(Definition.Id=="wither") {if(target.Alive){var debuff=new AssassinWitherVisual {Target=target};AddChild(debuff);}return;}
        string effect=Definition.Id switch {"spike"=>"si_spike_target_02","wither"=>"si_wither_debuff_target","maul"=>"si_maul_01","lowslash"=>"jc_lowslash_impact_01","voltaicslash"=>"jc_clairvoyantstrike_impact_01",_=>""};
        if(effect!="")Emit(effect,target.AimPosition,Definition.Id=="wither"?.18f:.14f,Definition.Id=="wither"?1.4:.55);
    }
    public override void _Process(double delta)
    {
        if(_loop>0){
            _loop-=delta;_emit-=delta;
            if(Definition.Id=="forcecloak" && !World.Build.Pregame.AssassinCloaked)_loop=0;
            if(_loop>0 && _emit<=0){_emit=.4;Emit(Definition.Id=="forcecloak"?"si_mindtrick_loop_01":"si_reflection_buff_loop",Actor.GlobalPosition+Vector3.Up*.1f,.19f,.6,true);}
        }
        foreach(var p in _particles.ToArray()){
            p.Age+=delta;if(p.Age>=p.Life){p.Node.QueueFree();_particles.Remove(p);continue;}
            if(p.Follow)p.Node.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.1f;
            float progress=(float)(p.Age/p.Life);p.Node.Scale=Vector3.One*(.65f+progress*.5f);
            p.Node.Frame=Math.Min(p.Frames*p.Frames-1,(int)(progress*p.Frames*p.Frames));
            ((StandardMaterial3D)p.Node.MaterialOverride).AlbedoColor=new Color(p.Tint.R,p.Tint.G,p.Tint.B,p.Tint.A*(1-progress));
        }
    }
    public void End(bool interrupted){if(interrupted){World.Build.Pregame.RevealSpike();Release();}}
    public void Complete(){}
    public void Release(){foreach(var child in GetChildren().OfType<AssassinWitherVisual>())child.QueueFree();Vanishing=false;_loop=0;foreach(var p in _particles)p.Node.QueueFree();_particles.Clear();foreach(var v in _voices)v.Stop();}
    public override void _ExitTree(){Release();_effects?.Dispose();_audio?.Dispose();}
}
