using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Content.Shared.Medical.SuitSensor;

namespace Content.Shared._Floof.Vore;

[RegisterComponent, NetworkedComponent]
public sealed partial class DevouredComponent : Component
{
    public bool AddedPressure;
    public bool AddedBreathing;
    public bool AddedTemperature;
    public bool AddedRadiation;
    public bool AddedFlash;

    [DataField("originalSensorModes")]
    public Dictionary<EntityUid, SuitSensorMode> OriginalSensorModes = new(); 
}