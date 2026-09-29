using Content.Shared._Vulp.Weather;
using Content.Shared.Weather;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;


namespace Content.Server._Vulp.Weather;


[RegisterComponent]
public sealed partial class WeatherCycleComponent : Component
{
    [DataField(required: true)]
    public ProtoId<WeatherCyclePrototype> Prototype = default!;

    [DataField]
    public TimeSpan
        UpdateInterval = TimeSpan.FromSeconds(2),
        NextUpdate = TimeSpan.Zero,
        NextWeather = TimeSpan.Zero;

    /// <summary>
    ///     The state of the cycle. This field is only ever null before the weather cycle is initialized.
    /// </summary>
    [DataField]
    public WeatherCycleData? CurrentState = null;

    /// <summary>
    ///     A status effect entity added to the map that represents a weather state associated with <see cref="CurrentState"/>
    /// </summary>
    [DataField]
    public EntityUid? CurrentWeatherEntity;

    /// <summary>
    ///     For debug use only, makes the weather cycle go on faster or slower.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeScale = 1f;

    // Accessibility
    [ViewVariables(VVAccess.ReadWrite), UsedImplicitly]
    private string PrototypeVv { get => Prototype; set => Prototype = value; }
}
