using Godot;
using System;
namespace SwtorSurvivors;

// Bounty Hunter kit presentation. Textures are the April FXSpec dependencies recorded in
// Assets/BountyHunter/source-parameters.json; Godot sprites adapt them, never native particle simulation.
public partial class D3AbilityPresentation
{
    private const string BountyAssets="res://Assets/BountyHunter/";
    private bool BountyVisual=>Definition.PresentationKind.StartsWith("bh",StringComparison.Ordinal);
    public int BountyImpactsPresented {get;private set;}
    public int BountyDetonations {get;private set;}
    public string BountyEndAudio {get;private set;}="";
    private Combatant? _bountyTarget;
    private Vector3 _bountyAim;
    private bool _bountyImpacted;
    private StormAreaIndicator? _bountyArea;
    public bool BountyAreaVisible=>GodotObject.IsInstanceValid(_bountyArea) && _bountyArea!.Visible;
    private void BeginBountyHunter(Combatant target)
    {
        if(!BountyVisual)return;
        _bountyTarget=target;_bountyAim=target.AimPosition;_bountyImpacted=false;
        // Death From Above marks its barrage with the original Imperial persistent-AoE projector, like Force Storm.
        if(Definition.PresentationKind=="bhdfa") {
            if(!GodotObject.IsInstanceValid(_bountyArea)){_bountyArea=new StormAreaIndicator {Name="DeathFromAboveArea"};AddChild(_bountyArea);}
            _bountyArea!.UpdateArea(target.GlobalPosition,Math.Max(.05f,_radius));
        }
        if(Definition.PresentationKind=="bhmissileblast")Missile(target,Math.Max(.1,Definition.WindupSeconds),0,1);
        // Bracer-launched missiles and darts fly from the left wrist over the authored wind-up.
        if(Definition.PresentationKind is "bhexplosive" or "bhelectro" or "bhkolto" or "bhconcussion" or "bhthermal")Missile(target,Math.Max(.1,Definition.WindupSeconds),0,1);
        if(Definition.PresentationKind=="bhfusion")for(int i=0;i<3;i++)Missile(target,Math.Max(.1,Definition.WindupSeconds),i,3);
        if(Definition.PresentationKind=="bhjet")BountyFlash(BountyAssets+"fx_2x2_white_smoke_alpha.png",Actor.GlobalPosition+Vector3.Up*.05f,.14f,.5,new(1,1,1,.6f),2,alpha:true);
        if(Definition.PresentationKind=="bhoverride")BountyRing(BountyAssets+"tri_ring_blurred.png",Actor.GlobalPosition+Vector3.Up*.02f,.25f,.5,new(1,.45f,.1f,.8f));
        if(Definition.PresentationKind=="bhshell")BountyFlash(BountyAssets+"proj_hexgrid_tile.png",Actor.GlobalPosition+Vector3.Up*.09f,.2f,.6,new(.35f,1,.55f,.55f));
    }
    private bool ImpactBountyHunter(Vector3 at)
    {
        if(!BountyVisual)return false;
        BountyImpactsPresented++;_bountyImpacted=true;
        string kind=Definition.PresentationKind;
        // Channels publish per-volley impacts; avoid restarting one long explosion every tick.
        if(kind!="bhsweep")Play(Definition.ImpactAudio,at,3);
        switch(kind)
        {
            case "bhblade":
                BountyFlash(BountyAssets+"saber_scorch_01.png",at,.09f,.3,new(1,.5f,.2f,.9f));
                BountyFlash(BountyAssets+"bw_fx_2x2_sparknglow_01.png",at,.08f,.25,new(1,.6f,.3f,1),2);break;
            case "bhrail":
                MuzzleFlash();Bolt(MuzzleWorld(),at,.014f,.18);
                BountyFlash(BountyAssets+"bw_starflare.png",at,.12f,.25,Definition.EffectTint);break;
            case "bhpower":
                MuzzleFlash();Bolt(MuzzleWorld(),at,.01f,.14);
                BountyFlash(BountyAssets+"bw_starflare.png",at,.09f,.2,Definition.EffectTint);break;
            case "bhexplosive":
                // The dart sticks here; BountyDetonate presents the delayed blast.
                BountyFlash(BountyAssets+"bw_flare_gray.png",at,.05f,.3,new(1,.6f,.25f,.9f));break;
            case "bhthermal":
                // The detonator clamps on; its delayed blast is presented by BountyDetonate.
                BountyFlash(BountyAssets+"bw_flare_gray.png",at,.06f,.3,new(1,.5f,.2f,.9f));break;
            case "bhmissileblast":
                BountyFlash(BountyAssets+"bw_explosion_sphere_single_frame.png",at,Math.Max(.12f,_radius*1.8f),.45,new(1,.6f,.25f,.9f));
                BountyFlash(BountyAssets+"fx_2x2_flames_emmissive_bw_01_d.png",at,Math.Max(.1f,_radius*1.3f),.5,new(1,.55f,.2f,1),2);break;
            case "bhelectro":
                BountyFlash(BountyAssets+"electric_rings_bw_02.png",at,.16f,.4,Definition.EffectTint);
                BountyFlash(BountyAssets+"512_singlearc_bw_02.png",at,.12f,.3,Definition.EffectTint);break;
            case "bhimmolate":
                BountyFlash(BountyAssets+"4x4_flame_lick_02_bw.png",at,.14f,.55,new(1,.55f,.2f,1),4);break;
            case "bhjet":
                BountyRing(BountyAssets+"fx_force_shockwave.png",at,Math.Max(.18f,_radius*2.2f),.4,new(1,.7f,.4f,.7f));
                BountyFlash(BountyAssets+"fx_2x2_white_smoke_alpha.png",at,.16f,.5,new(1,1,1,.55f),2,alpha:true);break;
            case "bhdfa":
                for(int i=0;i<3;i++) {
                    var point=at+new Vector3(_random.RandfRange(-1,1),0,_random.RandfRange(-1,1))*_radius*.8f;
                    Bolt(point+Vector3.Up*.5f,point,.01f,.12);
                    BountyFlash(BountyAssets+"bw_explosion_sphere_single_frame.png",point,.14f,.4,new(1,.6f,.25f,.9f));
                }
                break;
            case "bhfusion":
                BountyRing(BountyAssets+"blast_mark_rim.png",at,Math.Max(.2f,_radius*2.2f),.6,new(1,.55f,.2f,.85f));
                BountyFlash(BountyAssets+"sphere_displaced_01.png",at,Math.Max(.16f,_radius*1.6f),.5,new(1,.5f,.2f,.8f));
                BountyFlash(BountyAssets+"fx_2x2_flames_emmissive_bw_01_d.png",at,Math.Max(.12f,_radius*1.3f),.6,new(1,.55f,.2f,1),2);break;
            case "bhkolto":
                BountyRing(BountyAssets+"fx_force_shockwave.png",at,Math.Max(.18f,_radius*2.2f),.5,Definition.EffectTint);break;
            case "bhconcussion":
                BountyFlash(BountyAssets+"2x2_electrical_blast_alpha.png",at,Math.Max(.14f,_radius*1.6f),.4,Definition.EffectTint,2,alpha:true);
                BountyRing(BountyAssets+"glow_ring_thin_innergrad_01.png",at,Math.Max(.18f,_radius*2.2f),.45,Definition.EffectTint);break;
            case "bhjetboost":
                BountyRing(BountyAssets+"fx_force_shockwave.png",Actor.GlobalPosition+Vector3.Up*.02f,Math.Max(.2f,_radius*2.2f),.45,new(1,.7f,.4f,.7f));
                BountyFlash(BountyAssets+"fx_smoke_10_alpha.png",Actor.GlobalPosition+Vector3.Up*.04f,.18f,.6,new(1,1,1,.5f),alpha:true);break;
        }
        return true;
    }
    private bool BountyTargetImpact(Combatant target)
    {
        if(!BountyVisual || !GodotObject.IsInstanceValid(target))return false;
        var at=target.AimPosition;
        if(Definition.PresentationKind=="bhsweep") {
            int slot=_voices.FindIndex(5,v=>!v.Playing);if(slot<0)slot=5;
            Play(Definition.ShotAudio,Actor.GlobalPosition,slot);
            MuzzleFlash();Bolt(MuzzleWorld(),at,.006f,.1);
        }
        else if(Definition.PresentationKind is "bhexplosive" or "bhfusion" or "bhdfa")
            BountyFlash(BountyAssets+"bw_fx_2x2_sparknglow_01.png",at,.07f,.25,new(1,.6f,.3f,.9f),2);
        else if(Definition.PresentationKind=="bhelectro")
            BountyFlash(BountyAssets+"512_singlearc_bw_02.png",at,.1f,.3,Definition.EffectTint);
        else if(Definition.PresentationKind=="bhkolto")
            BountyFlash(BountyAssets+"8x8_billowing_steam.png",at,.12f,.6,new(.45f,1,.6f,.6f),8);
        return true;
    }
    /// <summary>Explosive Dart's delayed blast, presented where the dart was stuck.</summary>
    public void BountyDetonate(Vector3 at)
    {
        BountyDetonations++;
        Play(Definition.FollowupImpactAudio,at,4);
        BountyFlash(BountyAssets+"bw_explosion_sphere_single_frame.png",at+Vector3.Up*.06f,Math.Max(.14f,_radius*1.8f),.45,new(1,.6f,.25f,.9f));
        BountyFlash(BountyAssets+"4x4_flame_lick_tintable.png",at+Vector3.Up*.06f,Math.Max(.12f,_radius*1.4f),.5,new(1,.55f,.2f,.9f),4);
    }
    private void AdvanceBountyHunter()
    {
        if(GodotObject.IsInstanceValid(_bountyTarget) && _bountyTarget!.Alive)_bountyAim=_bountyTarget.AimPosition;
    }
    // A shot whose target dies during the wind-up still fires: sound and bolt at the last aim point.
    private void EndBountyHunter(bool interrupted)
    {
        if(GodotObject.IsInstanceValid(_bountyArea))_bountyArea!.Visible=false;
        if(!interrupted || !BountyVisual || _bountyImpacted || Definition.PresentationKind is not ("bhrail" or "bhpower"))return;
        _bountyImpacted=true;BountyEndAudio=Definition.ImpactAudio;
        Play(Definition.ImpactAudio,_bountyAim,3);
        MuzzleFlash();Bolt(MuzzleWorld(),_bountyAim,Definition.PresentationKind=="bhrail"?.014f:.01f,.16);
    }
    private void BountyFlash(string texture,Vector3 at,float size,double life,Color tint,int frames=1,bool alpha=false)
    {
        if(!ResourceLoader.Exists(texture))return;
        var s=alpha?AlphaSprite(texture,size,frames):Sprite(texture,size,frames);s.Modulate=tint;
        Add(s,life,fx=>{
            var sp=(Sprite3D)fx.Node;float f=(float)(fx.Age/fx.Life);sp.Modulate=new Color(tint,tint.A*(1-f));
            if(frames>1)sp.Frame=Math.Min(frames*frames-1,(int)(f*frames*frames));
        });
        s.GlobalPosition=at;
    }
    private void BountyRing(string texture,Vector3 at,float size,double life,Color tint)
    {
        if(!ResourceLoader.Exists(texture))return;
        var s=Sprite(texture,size,1,false);s.RotationDegrees=new Vector3(-90,0,0);s.Modulate=tint;
        Add(s,life,fx=>{float f=(float)(fx.Age/fx.Life);((Sprite3D)fx.Node).Scale=Vector3.One*(.4f+f);((Sprite3D)fx.Node).Modulate=new Color(tint,tint.A*(1-f));});
        s.GlobalPosition=at;
    }
}
