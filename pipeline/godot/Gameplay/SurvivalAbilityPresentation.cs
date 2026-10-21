using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Original B8 art/geometry/PCM in a bounded Godot adapter. Observes resolved health only.</summary>
public partial class SurvivalAbilityPresentation : Node3D, IAbilityPresentation
{
    public CharacterPresentation Actor { get; set; } = null!;
    public AbilityDefinition Definition { get; set; } = null!;
    public Health Health { get; set; } = null!;
    [Export] public string BarrierCenterSocket { get; set; } = "Chest";
    private readonly List<(Node3D node,double life,double total,string bone,Vector3 scale)> _effects=new();
    private AudioStreamPlayer3D[] _voices=Array.Empty<AudioStreamPlayer3D>();
    private Node3D? _shield;
    private Vector3 _center;
    private float _radius;
    private double _pulse;
    public bool ShieldVisible => GodotObject.IsInstanceValid(_shield);
    public int EffectCount => _effects.Count+(ShieldVisible?1:0);
    public int AudioVoicesStarted { get; private set; }
    public int PlayingAudioVoices => _voices.Count(v=>v.Playing);
    public override void _Ready()
    {
        ProcessPriority=100;
        _voices=Enumerable.Range(0,3).Select(_=>new AudioStreamPlayer3D {
            AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled,Bus="B7Combat",VolumeDb=-15 }).ToArray();
        foreach(var voice in _voices) {AddChild(voice);AudioPresentation.Find(this)?.RegisterCombatVoice(voice);}
        if(Definition.Effect==AbilityEffect.Shield) {Health.Resolved+=ObserveShieldHit;Health.ShieldEnded+=ObserveShieldEnd;}
    }
    private Vector3 Socket(string bone)
    {
        int index=Actor.Skeleton.FindBone(bone);
        // April FXSpec uses "bip01" while the unchanged exported rig names it "Bip01".
        if(index<0 && bone=="bip01")index=Actor.Skeleton.FindBone("Bip01");
        return index<0?Actor.GlobalPosition+Vector3.Up*0.08f:
            (Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(index)).Origin;
    }
    public bool Begin(Combatant target,Vector3 center,float radius)
    {
        ClearTransient();_center=center;_radius=radius;
        if(Definition.Id=="deathfield")
            Flash("electric_rings_bw_02",Socket("vfx_hand_left"),0.075f,0.6,new Color(0.65f,0.22f,0.95f),"vfx_hand_left");
        if(Definition.Id=="darkheal")
            Flash("2x2_lightning_core",Socket("LeftWrist"),0.09f,0.65,new Color(0.48f,0.13f,0.55f),"LeftWrist",true);
        if(Definition.Id=="barrier") Play(168062615,Definition.CastAudioEvent,-17);
        return true;
    }
    public void Channel() { }
    public void Impact(Vector3 center)
    {
        if(Definition.Id=="deathfield")
        {
            Play(26930158,Definition.ImpactAudioEvent,-15);
            var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://Gameplay/DeathFieldGround.gdshader")};
            material.SetShaderParameter("field_texture",GD.Load<Texture2D>("res://Assets/B8/si_death_field_d.png"));
            material.SetShaderParameter("ring_texture",GD.Load<Texture2D>("res://Assets/B8/jk_proj_wave_force_ring.png"));
            var ground=new StormAreaIndicator {Name="DeathFieldGround",ProjectionMaterial=material};
            AddChild(ground);ground.UpdateArea(_center,_radius);
            _effects.Add((ground,0.75,0.75,"",Vector3.One));
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.Tau/6;
                Flash("2x2_smoke_whisps_02",_center+new Vector3(Mathf.Cos(a)*_radius*0.5f,0.035f,Mathf.Sin(a)*_radius*0.5f),
                    _radius*0.8f,0.9,new Color(0.52f,0.25f,0.7f,0.5f),atlas:true);
            }
        }
        else if(Definition.Id=="barrier" && Health.Shielded)
        {
            ClearShieldVisual();_shield=new Node3D {Name="OriginalStaticBarrier"};AddChild(_shield);
            var shell=Sprite("sphere_displaced_01",0.22f,new Color(0.424f,0.365f,0.875f,0.4f));_shield.AddChild(shell);
            var highlights=Sprite("2x2_shield_orbs",0.225f,new Color(0.895f,0.783f,0.957f,0.48f),true);_shield.AddChild(highlights);
            // Original shield-accents GR2 is a small cap; the source particle scales it 6–12x.
            for(int i=0;i<6;i++)
            {
                float angle=i*Mathf.Tau/6;
                var plate=Geometry("fx_si_backlash_partialshieldcentered","fx_si_backlash_partialshieldcentered_01_d",new Color(0.7f,0.52f,1,0.38f));
                RemoveChild(plate);_shield.AddChild(plate);
                plate.Position=new Vector3(Mathf.Sin(angle)*0.072f,0,Mathf.Cos(angle)*0.072f);
                plate.Basis=new Basis(Vector3.Up,angle)*new Basis(Vector3.Right,Mathf.Pi/2);plate.Scale=Vector3.One*8;
            }
            _shield.GlobalPosition=Socket(BarrierCenterSocket);
        }
        else if(Definition.Id=="darkheal")
        {
            Play(142371144,Definition.ImpactAudioEvent,-14);
            Flash("2x2_electrical_blast_alpha",Socket("Chest"),0.18f,0.65,new Color(0.30f,0.36f,0.98f),"Chest",true);
            Flash("bw_flare_gray",Socket("LeftWrist"),0.055f,0.3,new Color(0.45f,0.30f,1),"LeftWrist");
        }
    }
    public void TargetImpact(Combatant target)
    {
        Flash("2x2_electrical_blast_alpha",target.AimPosition,0.11f,0.55,new Color(0.8f,0.59f,0.93f),atlas:true);
        Flash("dark_streak_purp_horizontal_alpha",target.AimPosition,0.13f,0.4,Colors.White);
    }
    public void SustainHealing(float amount)
    {
        if(amount>0) Flash("2x2_smoke_whisps_02",Socket("bip01"),0.12f,0.65,new Color(0.6f,0.49f,0.99f,0.6f),"bip01",true);
    }
    private void ShieldHit(HealthChange change)
    {
        if(change.Kind!=CombatEventKind.Absorbed)return;
        _pulse=0.18;
        Flash("sphere_displaced_01",Socket(BarrierCenterSocket),0.23f,0.18,new Color(0.63f,0.48f,1,0.65f),BarrierCenterSocket);
    }
    private void ObserveShieldHit(HealthChange change)
    {try {ShieldHit(change);}catch(Exception ex){GD.PushWarning("Barrier feedback: "+ex.Message);}}
    private void ObserveShieldEnd(bool depleted)
    {try {ShieldEnded(depleted);}catch(Exception ex){GD.PushWarning("Barrier feedback: "+ex.Message);}}
    private void ShieldEnded(bool depleted)
    {
        ClearShieldVisual();
        if(!depleted)return;
        // No separate April break binding recovered: short original shield accent is a documented reuse.
        Play(388541304,"b8/barrier_break",-20);
        Flash("2x2_shield_orbs",Socket(BarrierCenterSocket),0.25f,0.3,new Color(0.895f,0.783f,0.957f,0.6f),BarrierCenterSocket,true);
    }
    private static Sprite3D Sprite(string texture,float size,Color color,bool atlas=false)
    {
        var tex=GD.Load<Texture2D>($"res://Assets/B8/{texture}.png");
        var sprite=new Sprite3D {Texture=tex,PixelSize=size/(tex.GetWidth()/(atlas?2:1)),
            Hframes=atlas?2:1,Vframes=atlas?2:1,Frame=atlas?3:0,Modulate=color,
            Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,Shaded=false,
            AlphaCut=SpriteBase3D.AlphaCutMode.Disabled,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        // These source glow/ring textures use SRCALPHA + ONE, including fully opaque black backgrounds.
        // Smoke retains its actual source alpha; all light layers use additive blending.
        if(texture!="2x2_smoke_whisps_02")
            sprite.MaterialOverride=new StandardMaterial3D {
                ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoTexture=tex,
                VertexColorUseAsAlbedo=true,Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode=BaseMaterial3D.BlendModeEnum.Add,BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled,
                BillboardKeepScale=true,DepthDrawMode=BaseMaterial3D.DepthDrawModeEnum.Disabled,
                CullMode=BaseMaterial3D.CullModeEnum.Disabled };
        return sprite;
    }
    private void Flash(string texture,Vector3 point,float size,double life,Color color,string bone="",bool atlas=false)
    {
        var sprite=Sprite(texture,size,color,atlas);
        AddChild(sprite);sprite.GlobalPosition=point;_effects.Add((sprite,life,life,bone,Vector3.One));
    }
    private Node3D Geometry(string model,string texture,Color color)
    {
        var node=GD.Load<PackedScene>($"res://Assets/B8/{model}.glb").Instantiate<Node3D>();AddChild(node);
        var mat=new StandardMaterial3D {ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoTexture=GD.Load<Texture2D>($"res://Assets/B8/{texture}.png"),AlbedoColor=color,
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BlendMode=BaseMaterial3D.BlendModeEnum.Add,
            DepthDrawMode=BaseMaterial3D.DepthDrawModeEnum.Disabled,CullMode=BaseMaterial3D.CullModeEnum.Disabled};
        foreach(var mesh in node.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
        {mesh.MaterialOverride=mat;mesh.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;}
        return node;
    }
    private void Play(int media,string name,float gain)
    {
        var mixer=AudioPresentation.Find(this);if(mixer!=null && !mixer.PermitAbilityCombat(name,true))return;
        var voice=_voices.FirstOrDefault(v=>!v.Playing);if(voice==null)return;
        voice.Stream=GD.Load<AudioStreamWav>($"res://Assets/B8/Audio/{media}.wav");
        voice.GlobalPosition=Actor.GlobalPosition;voice.VolumeDb=gain;voice.Play();AudioVoicesStarted++;
    }
    public override void _Process(double delta)
    {
        if(ShieldVisible)
        {
            if(!Health.Shielded)ClearShieldVisual();
            else {_shield!.GlobalPosition=Socket(BarrierCenterSocket);_pulse=Math.Max(0,_pulse-delta);_shield.Scale=Vector3.One*(1+(float)_pulse*0.25f);}
        }
        for(int i=_effects.Count-1;i>=0;i--)
        {
            var e=_effects[i];double life=e.life-delta;
            if(life<=0) {e.node.Free();_effects.RemoveAt(i);continue;}
            if(e.bone!="")e.node.GlobalPosition=Socket(e.bone);
            float t=(float)(life/e.total);
            if(e.node is Sprite3D sprite) sprite.Transparency=1-Mathf.Min(1,t*3);
            else if(e.node is StormAreaIndicator ground)
                ((ShaderMaterial)ground.MaterialOverride).SetShaderParameter("opacity",Mathf.Min(1,t*2));
            else {e.node.Scale=e.scale*(1-t*0.4f);foreach(var mesh in e.node.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())mesh.Transparency=1-t;}
            _effects[i]=(e.node,life,e.total,e.bone,e.scale);
        }
    }
    public void End(bool interrupted) { }
    public void Complete() { } // The shield and finite impact tails do not own the gesture controller.
    private void ClearShieldVisual() {if(ShieldVisible)_shield!.Free();_shield=null;}
    private void ClearTransient() {foreach(var e in _effects)e.node.Free();_effects.Clear();}
    public void Release() {ClearTransient();ClearShieldVisual();foreach(var voice in _voices)voice.Stop();}
    public override void _ExitTree()
    {
        if(GodotObject.IsInstanceValid(Health)) {Health.Resolved-=ObserveShieldHit;Health.ShieldEnded-=ObserveShieldEnd;}
        Release();
    }
}
