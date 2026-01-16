using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Paint;

/// <summary>
/// Entity that, when used on another entity, will paint it
/// </summary>
/// <remarks>Floofstation note: this used to be called Paint, but was renamed to avoid future conflicts</remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ColorPaintComponent : Component
{
    /// Noise made when paint gets applied
    [DataField]
    public SoundSpecifier Spray = new SoundPathSpecifier("/Audio/Effects/spray2.ogg");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Blacklist;

    /// How long the doafter will take
    [DataField]
    public int Delay = 2;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#c62121");

    /// Solution on the entity that contains the paint
    [DataField]
    public string Solution = "drink";

    /// Reagent that will be used as paint
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> Reagent = "SpaceGlue";

    /// Reagent consumption per use
    [DataField]
    public FixedPoint2 ConsumptionUnit = FixedPoint2.New(5);

    [DataField]
    public TimeSpan DurationPerUnit = TimeSpan.FromSeconds(6);
}
