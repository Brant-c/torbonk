using Godot;
namespace SwtorSurvivors;
/// <summary>No health, cooldown or damage API is exposed to a presentation.</summary>
public interface IAbilityPresentation
{
    CharacterPresentation Actor { get; }
    bool Begin(Combatant target, Vector3 center, float radius);
    void Channel();
    void Impact(Vector3 center);
    void End(bool interrupted);
    void Release();
    // Normal completion may retain one-shot audio tails. Abort/death still use Release.
    void Complete() => Release();
}
