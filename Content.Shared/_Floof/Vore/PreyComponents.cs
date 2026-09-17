using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Content.Shared.Medical.SuitSensor;

namespace Content.Shared._Floof.Vore;


/// <summary>
/// Passive component for prey to keep track of health and digestion state including outside
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PreyComponent : Component
{
    // the max health of the prey used for digestion and slow regeneration
    [DataField("health"), AutoNetworkedField]
    public float Health = 100f;
    [DataField("maxHealth"), AutoNetworkedField]
    public float MaxHealth = 100f;
    // trackers for digestion and regeneration
    public bool ActiveDigesting;
    public float Timer;
    public int DigestPopupStage;
}


/// <summary>
/// Active Component for prey that is devoured for immunites, overlays and sensors
/// </summary>
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