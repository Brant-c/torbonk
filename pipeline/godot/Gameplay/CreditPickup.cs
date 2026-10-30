using Godot;
using System;
namespace SwtorSurvivors;

/// <summary>Credits commit on collection; a failed save leaves the pickup available to retry.</summary>
public partial class CreditPickup : Node3D
{
    public GameWorld World {get;set;}=null!;
    public int Value {get;set;}
    public Func<int,bool> SaveCredits {get;set;}=null!;
    public bool Collecting {get;private set;}
    private Node3D _visual=null!;
    private double _age,_retry,_collectAge;
    private Vector3 _start;
    public override void _Ready()
    {
        _visual=new Node3D();AddChild(_visual);
        _visual.AddChild(DropSymbol.Icon(DropSymbolKind.Credits,.065f));
        _age=GetInstanceId()%100*.17;
    }
    public bool TryCollect()
    {
        if(Collecting || _retry>0 || !World.PlayerActor.Alive || World.Progression!.Selecting)return false;
        if(!SaveCredits(Value)){_retry=2;return false;}
        Collecting=true;_start=GlobalPosition;World.Audio.PlayCue("pickup");
        var amount=new Label3D {Text="+"+Value+" Credits",FontSize=32,PixelSize=.0012f,
            Modulate=new Color("65f781"),Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true};
        _visual.AddChild(amount);amount.Position=Vector3.Up*.07f;
        return true;
    }
    public override void _Process(double delta)
    {
        _age+=delta;_retry=Math.Max(0,_retry-delta);
        if(Collecting){
            _collectAge+=delta;
            float t=Mathf.Clamp((float)_collectAge/.45f,0,1);
            GlobalPosition=_start.Lerp(World.PlayerActor.GlobalPosition+Vector3.Up*.1f,1-Mathf.Pow(1-t,3));
            _visual.Scale=Vector3.One*(1-t*.6f);
            if(t>=1)QueueFree();return;
        }
        _visual.Position=Vector3.Up*(Mathf.Sin((float)_age*2.8f)*.006f);
    }
    public override void _PhysicsProcess(double delta)
    {
        if(Collecting || !World.PlayerActor.Alive || World.Progression!.Selecting)return;
        Vector3 target=World.PlayerActor.GlobalPosition+Vector3.Up*.035f;
        float distance=GlobalPosition.DistanceTo(target);
        if(distance<=World.Progression.PickupRadius){TryCollect();return;}
        if(distance<=World.Progression.MagnetRadius)GlobalPosition=GlobalPosition.MoveToward(target,(float)delta*.8f);
    }
}
