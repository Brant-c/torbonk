using Godot;
namespace SwtorSurvivors;
// Chain gameplay can use either recovered lightning renderer without depending on its concrete type.
public interface IArcPresentation
{
    void Arc(Vector3 from,Vector3 to);
}
