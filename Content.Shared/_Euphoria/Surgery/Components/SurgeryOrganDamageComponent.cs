using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Euphoria.Surgery.Components;

/// <summary>
///     Used to pull damage out of a body, and to put damage back in if the organ has any.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryOrganDamageComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> damageType { get; private set; }

    [DataField]
    public FixedPoint2 Damage { get; set; }


}
