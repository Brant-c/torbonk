using Godot;
using System;
namespace SwtorSurvivors;

// Bounded adapters for sp_explosiveprobe_probe / sp_explosiveprobe_explode_02.
// Original geometry/textures; native Hero trajectories and P curves are adapted.
public partial class D3AbilityPresentation
{
    private Node3D? _explosiveProbe;
    private bool _explosiveDetonated;
    public int ExplosiveProbeDeployments {get;private set;}
    public int ExplosiveProbeDetonations {get;private set;}
    public bool ExplosiveProbeVisible=>GodotObject.IsInstanceValid(_explosiveProbe) && !_explosiveProbe!.IsQueuedForDeletion() && _explosiveProbe.Visible;
    private void BeginExplosiveProbe(Combatant target)
    {
        _explosiveDetonated=false;ExplosiveProbeDeployments++;
        if(GodotObject.IsInstanceValid(_explosiveProbe)){_explosiveProbe!.Visible=false;_explosiveProbe.QueueFree();}
        var probe=GD.Load<PackedScene>("res://Assets/PresentationRepairs/vfx_probe_mini01.glb").Instantiate<Node3D>();
        probe.Name="ExplosiveProbeDeployment";AgentStatusVisual.Fit(probe,.055f);_explosiveProbe=probe;
        Vector3 from=MuzzleWorld(),last=target.AimPosition+Vector3.Up*.025f;
        double flight=Math.Max(.1,Definition.WindupSeconds);
        Add(probe,flight+.1,fx=>{
            if(GodotObject.IsInstanceValid(target) && target.Alive)last=target.AimPosition+Vector3.Up*.025f;
            float t=Math.Clamp((float)(fx.Age/flight),0,1);
            probe.GlobalPosition=from.Lerp(last,t)+Vector3.Up*Mathf.Sin(t*Mathf.Pi)*.07f;
        });probe.GlobalPosition=from;
        var pulse=Sprite(AgentAssets+"glow_ring_thin_innergrad_01.png",.075f);pulse.Modulate=new Color(1,.06f,.015f,.8f);
        Add(pulse,flight+.1,fx=>{
            pulse.Visible=GodotObject.IsInstanceValid(probe) && !probe.IsQueuedForDeletion() && probe.Visible;
            if(pulse.Visible){pulse.GlobalPosition=probe.GlobalPosition;pulse.Scale=Vector3.One*(.8f+.2f*Mathf.Sin((float)fx.Age*22));}
        });pulse.GlobalPosition=from;
    }
    private void ExplodeProbe(Vector3 at)
    {
        if(_explosiveDetonated)return;
        _explosiveDetonated=true;ExplosiveProbeDetonations++;
        Play(Definition.ImpactAudio,at,3);
        if(GodotObject.IsInstanceValid(_explosiveProbe)){_explosiveProbe!.Visible=false;_explosiveProbe.QueueFree();}_explosiveProbe=null;
        // The recovered FX has a flash core, red ring, 22 sparks, seven pieces
        // of shrapnel and dirt/smoke. One burst per activation, not per victim.
        var core=Sprite(AgentAssets+"trail_misc_03.png",.23f);core.Modulate=new Color(1,.55f,.15f);
        Add(core,.125,fx=>{float t=(float)(fx.Age/fx.Life);core.Scale=new Vector3(1+t,.4f,1);core.Modulate=new Color(1,.55f,.15f,1-t);});core.GlobalPosition=at;
        var glow=Sprite(AgentAssets+"2x2_flares.png",.24f,2);glow.Frame=3;glow.Modulate=new Color(1,.6f,.2f);
        Add(glow,.15,fx=>glow.Modulate=new Color(1,.6f,.2f,1-(float)(fx.Age/fx.Life)));glow.GlobalPosition=at;
        var ring=Sprite(AgentAssets+"glow_ring_thin_innergrad_01.png",Math.Max(.16f,_radius*2),1,false);ring.RotationDegrees=new Vector3(-90,0,0);
        Add(ring,.3,fx=>{float t=(float)(fx.Age/fx.Life);ring.Scale=Vector3.One*(.2f+.8f*t);ring.Modulate=new Color(1,.04f,.01f,1-t);});ring.GlobalPosition=at-Vector3.Up*.08f;
        for(int i=0;i<22;i++){
            var spark=Sprite(AgentAssets+"bw_fx_2x2_sparknglow_01.png",.018f,2);spark.Frame=3;
            double life=_random.RandfRange(.25f,.55f);Vector3 velocity=new(_random.RandfRange(-.2f,.2f),_random.RandfRange(.15f,.4f),_random.RandfRange(-.2f,.2f));
            Add(spark,life,fx=>{float t=(float)fx.Age;spark.GlobalPosition=at+velocity*t-Vector3.Up*.3f*t*t;spark.Modulate=new Color(1,.7f,.3f,1-(float)(fx.Age/fx.Life));});spark.GlobalPosition=at;
        }
        for(int i=0;i<7;i++){
            var piece=AlphaSprite(AgentAssets+"shrapnel_01.png",.015f);Vector3 velocity=new(_random.RandfRange(-.12f,.12f),_random.RandfRange(.08f,.2f),_random.RandfRange(-.12f,.12f));
            Add(piece,.7,fx=>{float t=(float)fx.Age;piece.GlobalPosition=at+velocity*t-Vector3.Up*.3f*t*t;piece.Modulate=new Color(.38f,.38f,.38f,1-(float)(fx.Age/fx.Life));});piece.GlobalPosition=at;
            var smoke=AlphaSprite(AgentAssets+"2x2volumesmokegradient_blur2.png",.1f,2);smoke.Frame=i%4;
            var drift=new Vector3(_random.RandfRange(-.08f,.08f),.035f,_random.RandfRange(-.08f,.08f));
            Add(smoke,_random.RandfRange(1,2),fx=>{float t=(float)(fx.Age/fx.Life);smoke.GlobalPosition=at+drift*(float)fx.Age;smoke.Scale=Vector3.One*(.6f+t);smoke.Modulate=new Color(.55f,.48f,.4f,(1-t)*.65f);});smoke.GlobalPosition=at;
        }
    }
}
