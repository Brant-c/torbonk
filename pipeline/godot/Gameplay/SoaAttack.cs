using Godot;
using System;
using System.Linq;

namespace SwtorSurvivors;

/// <summary>Authored Survivors adaptation: locked lightning, eruptions and a breakable shield.</summary>
public partial class SoaAttack : Node
{
    [Export] public bool Image {get;set;}
    private EnemyAgent _actor=null!;
    private TombAttack _ground=null!;
    private int _cycle;
    private bool _locked;
    private double _shieldIn=12;
    public int ShieldsCast {get;private set;}
    public int Strikes=>_ground.Strikes;
    public BossPhase Phase=>_ground.Phase;
    public string Instruction=>_ground.Instruction;
    public override void _Ready()
    {
        _actor=GetParent<EnemyAgent>();
        _ground=new TombAttack {Name="SoaGroundAttack",Damage=Image?12:18,Windup=Image?1.5f:1.3f,
            Cooldown=Image?5.5f:4.5f,TriggerRange=2.5f,Pattern="trial"};
        _ground.Windup*=_actor.Crowd?.OperationMode.WarningTime??1;
        _ground.Cooldown*=_actor.Crowd?.OperationMode.AttackInterval??1;
        _actor.CallDeferred(Node.MethodName.AddChild,_ground);
        if(Image)
        {
            foreach(var mesh in _actor.GetNode<Node3D>("Visual/Original").FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>())
                for(int i=0;i<mesh.Mesh.GetSurfaceCount();i++)
                    if(mesh.Mesh.SurfaceGetMaterial(i) is StandardMaterial3D original)
                    {
                        var material=(StandardMaterial3D)original.Duplicate();
                        material.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
                        material.AlbedoColor=new Color(.35f,.7f,1,.48f);
                        material.EmissionEnabled=true;material.Emission=new Color(.08f,.25f,.5f);
                        mesh.SetSurfaceOverrideMaterial(i,material);
                    }
        }
    }
    public bool Tick(double delta)
    {
        if(!_actor.Alive || !_ground.IsNodeReady())return false;
        if(!Image && (_shieldIn-=delta)<=0)
        {
            _shieldIn=22;
            if(_actor.Health.ApplyShield(_actor.Health.Maximum*.08f,6))ShieldsCast++;
        }
        if(_ground.Phase==BossPhase.Pursuit && !_locked)
        {
            _cycle++;
            _ground.Pattern=_cycle%3==1?"bolt":_cycle%3==2?"trial":"crossfire";
            _ground.InstructionOverride=_cycle%3==1?"SOA'S LIGHTNING · SIDESTEP THE LOCKED LINE":
                _cycle%3==2?"INFERNAL ERUPTIONS · LEAVE THE MARKED GROUND":"MIND FRACTURE · MOVE INTO A GAP";
            _locked=true;
        }
        bool hold=_ground.Tick(delta);
        if(_ground.Phase==BossPhase.Recovery)_locked=false;
        return hold;
    }
    public void End()=>_ground?.End();
}
