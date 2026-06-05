<<<<<<< HEAD
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Vore;

[RegisterComponent, NetworkedComponent]
=======
[RegisterComponent]
>>>>>>> master
public sealed partial class DevouredComponent : Component
{
    public bool AddedPressure;
    public bool AddedBreathing;
    public bool AddedTemperature;
    public bool AddedRadiation;
}