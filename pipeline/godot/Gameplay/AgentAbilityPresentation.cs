using Godot;
using System;
namespace SwtorSurvivors;
// Finite Godot adapters for recovered April geometry and texture dependencies.
public partial class D3AbilityPresentation
{
    private const string AgentAssets="res://Assets/Agent/";
    public int AgentKnivesPresented {get;private set;}
    public bool AgentKnifeAbility=>Definition.Id is "shiv" or "backstab" or "hiddenstrike" or "laceration" or "severtendon";
    public StormAreaIndicator? AgentAreaIndicator {get;private set;}
    public int AgentKnifeImpactsPresented {get;private set;}
    public AgentKnifeVisual? ActiveKnife {get;private set;}
    private bool _knifeImpactPlayed;
    private int _agentKnifeStrike;
    private bool _acidBladeSoundPlayed;
    public int AcidBladeDetonationsPresented {get;private set;}
    public void PresentAcidBladeDetonation(Combatant target)
    {
        AcidBladeDetonationsPresented++;
        AgentAtlas(target.AimPosition,"fx_explosion_core_02_bw",4,.16f,new Color(.35f,.9f,.08f));
        if(!_acidBladeSoundPlayed){_acidBladeSoundPlayed=true;Play("res://Assets/D3/Audio/224018998.wav",target.AimPosition,6);}
    }
    public void PrepareAgentKnifeStrike(int strike)
    {
        if(strike==_agentKnifeStrike)return;
        _agentKnifeStrike=strike;_knifeImpactPlayed=false;
    }
    private Vector3 _agentOrbitalAt;
    private double _agentOrbitalLock;
    private Transform3D AgentSocket(string bone)
    {
        int i=Actor.Skeleton.FindBone(bone);
        return i<0?Actor.GlobalTransform:Actor.Skeleton.GlobalTransform*Actor.Skeleton.GetBoneGlobalPose(i);
    }
    private void AgentAtlas(Vector3 at,string texture,int side,float size,Color tint)
    {
        var sprite=Sprite(AgentAssets+texture+".png",size,side);sprite.Modulate=tint;
        Add(sprite,.6,f=>{sprite.Frame=Math.Min(side*side-1,(int)(f.Age/f.Life*side*side));sprite.Modulate=new Color(tint,tint.A*(1-(float)(f.Age/f.Life)));});sprite.GlobalPosition=at;
    }
    private bool BeginAgent(Combatant target)
    {
        string id=Definition.Id;
        if(id=="explosiveprobe"){BeginExplosiveProbe(target);return true;}
        if(id is "suppressivefire" or "plasmaprobe"){
            // Reuse the recovered SWTOR green ground projector and Imperial emblem.
            var center=target.GlobalPosition;
            var marker=new StormAreaIndicator {Name="AgentImperialArea"};AgentAreaIndicator=marker;
            double life=Definition.WindupSeconds+Definition.ChannelSeconds;
            Add(marker,life);
            marker.UpdateArea(center,_radius);
            if(id=="plasmaprobe"){
                var probe=new Node3D {Name="GroundPlasmaProbe"};
                foreach(string part in new[]{"sp_plasma_probe_top","sp_plasma_probe_bottom"}){
                    var model=GD.Load<PackedScene>(AgentAssets+part+".glb").Instantiate<Node3D>();probe.AddChild(model);AgentStatusVisual.Fit(model,.07f);
                }
                Add(probe,life,f=>probe.Rotation=new Vector3(0,(float)f.Age,0));
                probe.GlobalPosition=center+Vector3.Up*.08f;
                var flame=Sprite(AgentAssets+"5x5_flame_lick_04_bw.png",_radius*1.7f,5);flame.Modulate=new Color(1,.55f,.12f,.7f);
                Add(flame,life,f=>{flame.Frame=(int)(f.Age*20)%25;});flame.GlobalPosition=center+Vector3.Up*.04f;
            }
            return true;
        }
        if(id=="blowforblow"){
            var counter=Sprite(AgentAssets+"glow_ring_thin_innergrad_01.png",.24f);counter.Modulate=new Color(.3f,.65f,1,.6f);
            Add(counter,4,f=>counter.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.1f);counter.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.1f;return true;
        }
        if(id=="orbitalstrike"){
            _agentOrbitalAt=target.GlobalPosition;_agentOrbitalLock=0;
            var ring=Sprite(AgentAssets+"glow_ring_thin_innergrad_01.png",Math.Max(.2f,_radius*2),1,false);ring.RotationDegrees=new Vector3(-90,0,0);ring.Modulate=new Color(1,.4f,.12f,.5f);
            Add(ring,Math.Max(1,Definition.ChannelSeconds),f=>ring.GlobalPosition=_agentOrbitalAt+Vector3.Up*.005f);ring.GlobalPosition=_agentOrbitalAt+Vector3.Up*.005f;return true;
        }
        if(AgentKnifeAbility){
            if(Actor.GestureOwner is {} owner)Actor.FitGesture(owner,Definition.GestureSeconds);
            ActiveKnife=AgentKnifeVisual.Create(Actor,Math.Max(.45,Definition.GestureSeconds),id=="laceration");
            _agentKnifeStrike=0;_knifeImpactPlayed=false;_acidBladeSoundPlayed=false;AgentKnivesPresented++;
            _delayed.Add((Definition.GestureSeconds,()=>{if(_voices.Count>1)_voices[1].Stop();}));
            return true;
        }
        if(id is "koltoprobe" or "koltoinjection" or "surgicalprobe" or "adrenalineprobe" or "stimboost"){
            var glow=Sprite(AgentAssets+"proj_broken_ring_motion_lines.png",.22f);glow.Modulate=new Color(.2f,.9f,.45f,.65f);
            Add(glow,id=="koltoprobe"?8:1.5,f=>{glow.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.12f;glow.Rotation=new Vector3(0,0,(float)f.Age);});glow.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.12f;
            if(id!="koltoinjection"){
                var probe=GD.Load<PackedScene>("res://Assets/PresentationRepairs/vfx_probe_mini01.glb").Instantiate<Node3D>();AgentStatusVisual.Fit(probe,.04f);
                Add(probe,id=="koltoprobe"?8:1.5,f=>probe.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.2f+Actor.GlobalBasis.X*.07f);probe.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.2f;
            }
            return true;
        }
        if(id is "agentstealth" or "cloakingscreen"){
            var camo=Sprite(AgentAssets+"proj_ia_stealthcamo_02.png",.24f);camo.Modulate=new Color(.35f,.65f,1,.5f);
            Add(camo,.8,f=>{camo.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.12f;camo.Modulate=new Color(.35f,.65f,1,.5f*(1-(float)(f.Age/f.Life)));});camo.GlobalPosition=Actor.GlobalPosition+Vector3.Up*.12f;return true;
        }
        return false;
    }
    private bool ImpactAgent(Vector3 at)
    {
        string id=Definition.Id;
        if(id is "fragmentationgrenade" or "flashbang"){
            AgentAtlas(at,id=="flashbang"?"bw_flare_gray":"fx_explosion_core_02_bw",id=="flashbang"?1:4,_radius*2,id=="flashbang"?Colors.White:new Color(1,.6f,.22f));return true;
        }
        if(AgentKnifeAbility){
            bool followup=id=="laceration" && _agentKnifeStrike>1;
            AgentAtlas(at,id=="laceration" && !followup?"2x2_electrical_blast_alpha":"bw_fx_2x2_sparknglow_01",2,id=="laceration" && !followup?.13f:.065f,id=="laceration" && !followup?new Color(.349f,.993f,.327f):new Color(1,.75f,.3f));
            if(!_knifeImpactPlayed){_knifeImpactPlayed=true;AgentKnifeImpactsPresented++;string audio=id=="laceration" && _agentKnifeStrike>1 && Definition.FollowupImpactAudio!=""?Definition.FollowupImpactAudio:Definition.ImpactAudio;Play(audio,at,id=="laceration"?3+(_agentKnifeStrike%2):3);}return true;
        }
        if(id=="orbitalstrike"){
            if(_agentOrbitalLock>0)return true;_agentOrbitalLock=.5;at=_agentOrbitalAt;
            var beam=Sprite(AgentAssets+"cleanbeam_thick_bw.png",1.3f);beam.Modulate=new Color(1,.4f,.12f);
            beam.Scale=new Vector3(.10f,1,1);Add(beam,.45,f=>f.Node.GlobalPosition=at+Vector3.Up*.65f);beam.GlobalPosition=at+Vector3.Up*.65f;
            AgentAtlas(at+Vector3.Up*.06f,"bw_flare_gray",1,_radius*.8f,new Color(1,.5f,.2f));return true;
        }
        if(id is "plasmaprobe" or "interrogationprobe")return true;
        if(id=="explosiveprobe"){ExplodeProbe(at);return true;}
        if(id is "empdischarge" or "coverpulse" or "debilitate"){AgentAtlas(at,"2x2_electrical_blast_alpha",2,Math.Max(.1f,_radius*2),new Color(.55f,.8f,1));return true;}
        return false;
    }
}
