namespace SwtorSurvivors;
/// <summary>B2 compatibility entry point; scheduling and damage live in AutomaticAbility.</summary>
public partial class AutomaticLightning : AutomaticAbility
{
    public LightningPresentation Visual
    {
        get => (LightningPresentation)Presentation;
        set => Presentation=value;
    }
}
