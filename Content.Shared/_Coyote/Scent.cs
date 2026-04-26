using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._Coyote.SniffAndSmell;

/// <summary>
/// This defines a discrete scent that can be detected.
/// </summary>
[Serializable, NetSerializable] // Floofstation - needs to be net-serializable
public sealed class Scent(
    ProtoId<ScentPrototype> scentProto,
    string scentGuid)
{
    /// <summary>
    /// The proto for this scent
    /// </summary>
    [DataField]
    public ProtoId<ScentPrototype> ScentProto = scentProto;

    /// <summary>
    /// The unique-ish ID for this scent instance
    /// </summary>
    [DataField]
    public string ScentInstanceId = scentGuid;
}
