using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
namespace SwtorSurvivors;

/// <summary>Bounded adapter for the recovered April blast textures, cast/release gestures and Wwise media.</summary>
public partial class SorcererAbilityPresentation : Node3D,IAbilityPresentation
{
    public CharacterPresentation Actor {get;set;}=null!;
    public AbilityDefinition Definition {get;set;}=null!;
    private readonly List<(Sprite3D node,double life,double total)> _effects=new();
    private readonly List<AudioStreamPlayer3D> _voices=new();
    private JsonDocument _audio=null!;
    private Node3D? _charge;
    private readonly List<(Node3D node,string bone,Sprite3D ring)> _chargeHands=new();
    private double _chargeAge;
    private Node3D? _projectile;
    private Vector3 _projectileStart,_projectileEnd;
    private double _projectileAge;
    public int AudioVoicesStarted {get;private set;}
    public bool ChargeVisible=>_charge!=null;
    public int ChargeHandCount=>_chargeHands.Count;
    public int EffectCount=>_effects.Count+(_charge!=null?1:0)+(_projectile!=null?1:0);
    public override void _Ready()
    {
        ProcessPriority=100; // Follow the updated animation/skeleton pose.
        _audio=JsonDocument.Parse(FileAccess.GetFileAsString("res://Assets/Sorcerer/audio.json"));
        for(int i=0;i<4;i++)
        {
            var voice=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-18,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
            AddChild(voice);_voices.Add(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);
        }
    }
    private Vector3 Socket(string name)
    {
        int i=Actor.Skeleton.FindBone(name);
        return i<0?Actor.GlobalPosition+Vector3.Up*.1f:(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(i)).Origin;
    }
    private Vector3 Hand=>Socket("vfx_hand_right");
    private Sprite3D Sprite(string stem,float size,int atlas=1,float brightness=2.5f,Node3D? parent=null)
    {
        var texture=GD.Load<Texture2D>($"res://Assets/Sorcerer/{stem}.png");
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/SorcererParticle.gdshader")};
        material.SetShaderParameter("source_texture",texture);material.SetShaderParameter("brightness",brightness);
        var sprite=new Sprite3D {Texture=texture,Hframes=atlas,Vframes=atlas,PixelSize=size/(texture.GetWidth()/atlas),
            Modulate=new Color(.8f,.55f,1),MaterialOverride=material};
        (parent??this).AddChild(sprite);return sprite;
    }
    private void Play(string name,int slot,Vector3 at)
    {
        if(name=="" || !_audio.RootElement.GetProperty("events").TryGetProperty(name,out var eventId))return;
        if(!_audio.RootElement.GetProperty("graphs").TryGetProperty(eventId.GetString()!,out var graph))return;
        var media=graph.GetProperty("media");if(media.GetArrayLength()==0)return;
        var voice=_voices[slot];voice.GlobalPosition=at;
        voice.Stream=GD.Load<AudioStream>($"res://Assets/Sorcerer/Audio/{media[0].GetInt32()}.wav");
        voice.Play();AudioVoicesStarted++;
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        Release();
        if(Definition.Id=="thunderingblast")
        {
            _charge=new Node3D {Name="ThunderingBlastCharge"};AddChild(_charge);_chargeAge=0;
            // Original cast graph has wrist glows and electrical layers on both hands.
            foreach(string bone in new[]{"vfx_hand_right","vfx_hand_left"})
            {
                var hand=new Node3D {Name=bone};_charge.AddChild(hand);hand.GlobalPosition=Socket(bone);
                Sprite("bw_flare_gray",.095f,brightness:2.5f,parent:hand);
                var core=Sprite("bw_flare_gray",.04f,brightness:3,parent:hand);core.Modulate=new Color(.95f,.85f,1);
                var ring=Sprite("electric_rings_bw_02",.10f,atlas:2,brightness:2.5f,parent:hand);
                _chargeHands.Add((hand,bone,ring));
            }
            Play(Definition.CastAudioEvent,0,Hand);Play(Definition.LoopAudioEvent,1,Hand);
        }
        return Actor.Animator.HasAnimation(Definition.StartClip);
    }
    public void Channel(){StopCharge();}
    public void Impact(Vector3 center)
    {
        if(Definition.Id!="thunderingblast")return;
        Play("Play_ability_sithinquisitor_thunderingblast_projectile",2,Hand);
        Play(Definition.ImpactAudioEvent,3,center);
        _projectile=GD.Load<PackedScene>("res://Assets/Sorcerer/si_thundering_blast_force_energy.glb").Instantiate<Node3D>();
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/SorcererBlast.gdshader")};
        material.SetShaderParameter("energy_texture",GD.Load<Texture2D>("res://Assets/Sorcerer/force_energy_draw_in.png"));
        material.SetShaderParameter("caustics_texture",GD.Load<Texture2D>("res://Assets/Sorcerer/caustics_tile_01.png"));
        material.SetShaderParameter("blast_texture",GD.Load<Texture2D>("res://Assets/Sorcerer/fx_thundering_blast_k.png"));
        foreach(var mesh in _projectile.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {mesh.MaterialOverride=material;mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;}
        AddChild(_projectile);_projectileStart=Hand;_projectileEnd=center;_projectileAge=0;_projectile.GlobalPosition=Hand;
        var direction=center-Hand;
        if(!direction.IsZeroApprox())_projectile.Basis=new Basis(new Quaternion(Vector3.Up,direction.Normalized())).Scaled(Vector3.One*.4f);
        var wave=Sprite("fx_force_shockwave",.23f,brightness:2.3f);wave.GlobalPosition=center;_effects.Add((wave,.55,.55));
        var ring=Sprite("vfx_force_impact_ring",.18f,brightness:2.3f);ring.GlobalPosition=center;_effects.Add((ring,.45,.45));
        var core=Sprite("bw_flare_gray",.075f,brightness:3);core.GlobalPosition=center;_effects.Add((core,.22,.22));
    }
    private void StopCharge(){if(_charge!=null){_charge.Free();_charge=null;}_chargeHands.Clear();if(_voices.Count>1){_voices[0].Stop();_voices[1].Stop();}}
    public override void _Process(double delta)
    {
        if(_charge!=null)
        {
            _chargeAge+=delta;
            foreach(var hand in _chargeHands)
            {
                hand.node.GlobalPosition=Socket(hand.bone);
                hand.node.Scale=Vector3.One*(.8f+.2f*Math.Min(1,(float)_chargeAge/1.5f)+.035f*Mathf.Sin((float)_chargeAge*18));
                hand.ring.Frame=(int)(_chargeAge*16)%4;
            }
        }
        if(_projectile!=null)
        {
            _projectileAge+=delta;
            if(_projectileAge>=.25){_projectile.Free();_projectile=null;}
            else _projectile.GlobalPosition=_projectileStart.Lerp(_projectileEnd,(float)(_projectileAge/.25));
        }
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var e=_effects[i];e.life-=delta;
            if(e.life<=0){e.node.Free();_effects.RemoveAt(i);continue;}
            e.node.Modulate=new Color(1,1,1,(float)(e.life/e.total));_effects[i]=e;
        }
    }
    public void End(bool interrupted){StopCharge();if(interrupted)Release();}
    public void Complete(){}
    public void Release(){StopCharge();if(_projectile!=null){_projectile.Free();_projectile=null;}foreach(var e in _effects)e.node.Free();_effects.Clear();foreach(var v in _voices)v.Stop();}
    public override void _ExitTree(){Release();_audio?.Dispose();}
}
