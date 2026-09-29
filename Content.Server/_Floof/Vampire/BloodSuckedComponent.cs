using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Vampire;

/// <summary>
/// For entities who have been succed.
/// </summary>
[RegisterComponent]
public sealed partial class BloodSuckedComponent : Component
{
    /// <summary>
    ///     This component will be removed when there's no longer any damage in any of these types.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> RemoveWhenNoDamage;
}
