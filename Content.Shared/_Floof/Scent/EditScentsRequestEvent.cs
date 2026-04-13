using Robust.Shared.Serialization;
using ScentData = Content.Shared._Coyote.SniffAndSmell.Scent;

namespace Content.Shared._Floof.Scent;

/// <summary>
///     Raised client->server to request to edit scents on an entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class EditScentsRequestEvent : EntityEventArgs
{
    [DataField]
    public NetEntity TargetEntity;

    [DataField]
    public List<ScentData> Scents;

    public EditScentsRequestEvent(NetEntity targetEntity, List<ScentData> scents)
    {
        TargetEntity = targetEntity;
        Scents = scents;
    }
}
