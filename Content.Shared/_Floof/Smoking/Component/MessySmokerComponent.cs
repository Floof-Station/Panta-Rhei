using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Smoking.Component;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MessySmokerComponent : Robust.Shared.GameObjects.Component
{
    /// <summary>
    /// Low chance to spill chemicals while smoking by default
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpitChance = 0.03f;

    /// <summary>
    /// We can spill a very tiny amount of what they are smoking
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SpitAmount = 0.1;

    /// <summary>
    /// Popup message we show when a spill happens
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? SpitMessagePopup;
}
