using Content.Shared.Medical.SuitSensor;
namespace Content.Server._Floof.Vore;

[RegisterComponent]
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