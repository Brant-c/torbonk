using Godot;
namespace SwtorSurvivors;
public partial class CharacterPresentation
{
    private readonly AnimationNodeAnimation _coverPose=new(){Animation="Idle"};
    private bool _sniperCoverEnabled,_coverRequested;
    private float _coverWeight;
    public bool SniperCoverActive=>_sniperCoverEnabled && _coverRequested;
    public float SniperCoverBlend=>_coverWeight;
    public void EnableSniperCover()
    {
        _sniperCoverEnabled=true;_coverPose.Animation="Agent/CoverIdle";
        Animator.GetAnimation("Agent/CoverIdle").LoopMode=Animation.LoopModeEnum.Linear;
        Animator.GetAnimation("Agent/CoverFire").LoopMode=Animation.LoopModeEnum.Linear;
    }
    public void SetSniperCover(bool active)
    {
        bool requested=_sniperCoverEnabled && active;
        if(_coverRequested==requested)return;
        _coverRequested=requested;
        if(!requested && _gestureClip.StartsWith("Agent/Cover") && GestureOwner is AutomaticAbility ability){
            SetGestureClip(GestureOwner,ability.State==LightningState.Starting?ability.Definition.StartClip:ability.Definition.LoopClip);
        }
    }
    private string CoverGesture(string clip)=>SniperCoverActive?clip switch{
        "D3/Sniper" or "D3/SniperSeries" or "D3/RifleFire3" or "Basic/Rifle" or "Agent/SniperSeries" or "Agent/RifleAutoStart" or "Agent/RifleAutoLoop"=>"Agent/CoverFire",
        "D3/RifleCharge"=>"Agent/CoverIdle",
        "Agent/Detonator"=>"Agent/CoverDetonator",
        _=>clip}:clip;
    private void UpdateSniperCover(double delta)
    {
        _coverWeight=Mathf.MoveToward(_coverWeight,SniperCoverActive?1:0,(float)delta/.18f);
        Tree.Set("parameters/CoverBlend/blend_amount",_coverWeight);
    }
}
