using Godot;
using System;
using System.Linq;
namespace SwtorSurvivors;

// Godot adaptations of recovered April dependencies. See PRESENTATION-REPAIRS.md for fidelity limits.
public partial class D3AbilityPresentation
{
    private const string RepairAssets="res://Assets/PresentationRepairs/";
    private AudioStreamPlayer3D? _channelVoice;
    private AudioStreamPlayer3D? _persistentVoice;
    private Combatant? _repairTarget;
    private Sprite3D? _reticle;
    private double _buffVisual, _buffSpawn;
    private Health? _probeHealth;
    public int ShotsPresented {get;private set;}
    public int MissilesPresented {get;private set;}
    public bool ChannelSoundPlaying=>_channelVoice?.Playing??false;
    public bool ReticleVisible=>GodotObject.IsInstanceValid(_reticle);
    public string ShieldStyle=>Definition.Id=="shieldprobe"?"probe":Definition.Id=="forceshroud"?"shroud":"energy";
    private bool MissileAbility=>Definition.Id is "tracer" or "heatseeker" or "incendiarymissile";
    private Vector3 BoneWorld(string bone)
    {
        int i=Actor.Skeleton.FindBone(bone);
        return i<0?Actor.GlobalPosition+Vector3.Up*.12f:(Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(i)).Origin;
    }
    private Vector3 RepairAim()=>GodotObject.IsInstanceValid(_repairTarget)?_repairTarget!.AimPosition:Actor.GlobalPosition-_forward*.2f;
    private void BeginRepair(Combatant target)
    {
        _repairTarget=target;
        if(MissileAbility)
        {
            int count=Definition.Id=="heatseeker"?3:1;
            for(int i=0;i<count;i++)Missile(target,Math.Max(.08,Definition.WindupSeconds),i,count);
        }
        if(Definition.Id=="ambush")
        {
            ClearReticle();
            _reticle=Sprite(RepairAssets+"spy_reticle_v01.png",.12f);
            _reticle.Modulate=new Color(1,.2f,.12f,.7f);AddChild(_reticle);_reticle.GlobalPosition=target.AimPosition;
            Play(RepairAssets+"Audio/12809334.wav",Actor.GlobalPosition,4);
        }
        if(Definition.Id=="reactivearmor") {_buffVisual=Definition.BuffSeconds;_buffSpawn=0;PersistentSound("562857809");}
        if(Definition.Id=="creepingterror")Burst(BoneWorld("LeftWrist"),RepairAssets+"2x2_electricity_bolts.png",.08f);
    }
    private void Missile(Combatant target,double life,int index,int count)
    {
        MissilesPresented++;
        var from=BoneWorld("LeftWrist");var last=target.AimPosition;
        var head=Sprite(RepairAssets+"bw_flare_gray.png",.035f);head.Modulate=new Color(1,.7f,.3f);
        double smokeClock=0;
        Add(head,life,fx=>{
            float t=(float)(fx.Age/fx.Life);
            if(GodotObject.IsInstanceValid(target))last=target.AimPosition;
            var arc=Vector3.Up*Mathf.Sin(t*Mathf.Pi)*(.07f+index*.035f);
            var spread=Vector3.Right*Mathf.Sin(t*Mathf.Pi)*(index-(count-1)*.5f)*.07f;
            head.GlobalPosition=from.Lerp(last,t)+arc+spread;
            if(fx.Age>=smokeClock){smokeClock=fx.Age+.04;MissileSmoke(head.GlobalPosition);}
        });head.GlobalPosition=from;
    }
    private void MissileSmoke(Vector3 at)
    {
        var s=AlphaSprite(RepairAssets+"2x2volumesmokegradient_blur2.png",.035f,2);s.Frame=1;
        Add(s,.3,fx=>{float t=(float)(fx.Age/fx.Life);s.Scale=Vector3.One*(1+t*1.5f);s.Modulate=new Color(.6f,.6f,.6f,(1-t)*.4f);});s.GlobalPosition=at;
    }
    private void ChannelRepair()
    {
        if(Definition.ChannelAudio=="")return;
        _channelVoice??=CreateChannelVoice();
        var stream=(AudioStreamWav)GD.Load<AudioStreamWav>(Definition.ChannelAudio).Duplicate();
        stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);
        _channelVoice.Stream=stream;_channelVoice.Play();
    }
    private AudioStreamPlayer3D CreateChannelVoice()
    {
        var v=new AudioStreamPlayer3D {Bus="B7Combat",VolumeDb=-21,AttenuationModel=AudioStreamPlayer3D.AttenuationModelEnum.Disabled};
        AddChild(v);AudioPresentation.Find(this)?.RegisterCombatVoice(v);return v;
    }
    private void PersistentSound(string media)
    {
        _persistentVoice??=CreateChannelVoice();
        var stream=(AudioStreamWav)GD.Load<AudioStreamWav>(RepairAssets+"Audio/"+media+".wav").Duplicate();
        stream.LoopMode=AudioStreamWav.LoopModeEnum.Forward;stream.LoopBegin=0;stream.LoopEnd=(int)(stream.GetLength()*stream.MixRate);
        _persistentVoice.Stream=stream;_persistentVoice.Play();
    }
    private bool ImpactRepair(Vector3 at)
    {
        if(MissileAbility)
        {
            Play(Definition.ImpactAudio,at,3);
            Burst(at,Definition.ImpactTexture,.15f);return true;
        }
        if(Definition.Id is "unload" or "seriesofshots" or "agentcull" or "suppressivefire" or "carbineburst" or "ambush" or "weakeningblast" or "basicrapiddual")
        {
            ClearReticle();ShotsPresented++;
            int shotSlot=_voices.FindIndex(5,v=>!v.Playing);if(shotSlot<0)shotSlot=5;
            Play(Definition.ShotAudio,Actor.GlobalPosition,shotSlot);Play(Definition.ImpactAudio,at,3);
            MuzzleFlash();Bolt(MuzzleWorld(),at,.006f,.1);
            if(Definition.Id is "unload" or "basicrapiddual")
            {
                var left=BoneWorld("LeftWeapon");
                var flash=Sprite("res://Assets/D3/bw_orange_flare02.png",.045f);
                Add(flash,.08);flash.GlobalPosition=left;Bolt(left,at,.006f,.1);
            }
            Burst(at,"res://Assets/D3/bw_fx_2x2_sparknglow_01.png",.05f);return true;
        }
        if(Definition.Id=="forcescream")
        {
            var wave=Sprite(RepairAssets+"2x2_elec_blast_ring_alpha.png",.06f,2);
            var from=BoneWorld("Head");
            Add(wave,.22,fx=>{float t=(float)(fx.Age/fx.Life);wave.GlobalPosition=from.Lerp(at,t);wave.Scale=Vector3.One*(1+t*2);wave.Modulate=new Color(1,.75f,.65f,1-t);});wave.GlobalPosition=from;
            Burst(at,RepairAssets+"bw_flare_gray.png",.12f);return true;
        }
        return false;
    }
    private bool ShowDistinctShield()
    {
        if(WarriorVisual && Definition.Id=="endurepain"){
            // Recovered shoulder/hip smoke is emitted by AdvanceWarriorEffects.
            _bubble=new Node3D {Name="EndurePainAura"};AddChild(_bubble);
            _bubble.GlobalPosition=ShieldPosition();return true;
        }
        if(WarriorVisual && Definition.Id=="saberward"){
            _bubble=new Node3D {Name="SaberWardAura"};AddChild(_bubble);
            var guard=Sprite(WarriorTexture+"glow/vfx_force_impact_ring.png",.22f);guard.Modulate=new Color(1,.65f,.25f,.4f);_bubble.AddChild(guard);
            _bubble.GlobalPosition=ShieldPosition();return true;
        }
        if(Definition.Id=="shieldprobe")
        {
            _bubble=GD.Load<PackedScene>(RepairAssets+"vfx_probe_mini01.glb").Instantiate<Node3D>();
            // Fit the recovered probe geometry without depending on its original authoring units.
            var meshes=_bubble.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
            float size=meshes.Select(m=>m.GetAabb().Size.Length()).DefaultIfEmpty(1).Max();
            _bubble.Scale=Vector3.One*(.055f/Math.Max(.0001f,size));
            foreach(var m in meshes)m.MaterialOverride=new StandardMaterial3D {AlbedoColor=new Color(.35f,.4f,.44f),Metallic=.7f,Roughness=.4f};
            AddChild(_bubble);_bubble.GlobalPosition=ShieldPosition();
            _probeHealth=_shieldHealth;_probeHealth!.Resolved+=ProbeHit;
            Play(RepairAssets+"Audio/124571908.wav",Actor.GlobalPosition,4);PersistentSound("460682371");return true;
        }
        if(Definition.Id!="forceshroud")return false;
        // April GOM binds Shroud to jc_resilience_buff and force_ghost_projector.
        _bubble=new Node3D {Name="ResilienceAura"};AddChild(_bubble);
        var glow=Sprite(RepairAssets+"wavy_shock_rings_gray_alpha.png",.22f,2);glow.Modulate=new Color(.9f,.92f,.99f,.3f);_bubble.AddChild(glow);
        _bubble.GlobalPosition=ShieldPosition();return true;
    }
    private void ProbeHit(HealthChange change)
    {
        if(change.Kind!=CombatEventKind.Absorbed || !ShieldVisible)return;
        Burst(Actor.GlobalPosition+Vector3.Up*.1f,RepairAssets+"bw_fx_2x2_sparknglow_01.png",.08f);
        Play(RepairAssets+"Audio/38107453.wav",Actor.GlobalPosition,3);
    }
    private Vector3 ShieldPosition()=>Actor.GlobalPosition+Vector3.Up*(Definition.Id=="shieldprobe"?.2f:.09f)+(Definition.Id=="shieldprobe"?Actor.GlobalBasis.X*.07f:Vector3.Zero);
    private void AdvanceRepair(double delta)
    {
        if(ReticleVisible)_reticle!.GlobalPosition=RepairAim();
        if(_probeHealth!=null && !ShieldVisible)DetachProbe();
        if(_channelVoice!=null)_channelVoice.GlobalPosition=Actor.GlobalPosition;
        if(_persistentVoice!=null)_persistentVoice.GlobalPosition=Actor.GlobalPosition;
        if(_buffVisual>0)
        {
            _buffVisual-=delta;_buffSpawn-=delta;
            if(_buffVisual<=0)_persistentVoice?.Stop();
            if(_buffSpawn<=0){_buffSpawn=.15;WristFlame("LeftWrist");WristFlame("RightWrist");}
        }
    }
    private void WristFlame(string bone)
    {
        var s=Sprite(RepairAssets+"4x4_unlooping_flame_01.png",.055f,4);
        Add(s,.4,fx=>{float t=(float)(fx.Age/fx.Life);s.GlobalPosition=BoneWorld(bone)+Vector3.Up*t*.025f;s.Frame=Math.Min(15,(int)(t*16));s.Modulate=new Color(1,1,1,1-t);});s.GlobalPosition=BoneWorld(bone);
    }
    private void ClearReticle(){if(GodotObject.IsInstanceValid(_reticle))_reticle!.QueueFree();_reticle=null;}
    private void DetachProbe(){if(GodotObject.IsInstanceValid(_probeHealth))_probeHealth!.Resolved-=ProbeHit;_probeHealth=null;if(Definition.Id=="shieldprobe")_persistentVoice?.Stop();}
    private void EndRepair(){_channelVoice?.Stop();ClearReticle();}
    private void ReleaseRepair(){EndRepair();DetachProbe();_persistentVoice?.Stop();_buffVisual=0;_repairTarget=null;}
}
