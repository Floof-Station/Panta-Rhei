using Content.Shared._Euphoria.Item.AttunableItem;
using Content.Shared._Euphoria.Tools.Components;
using Content.Shared._Euphoria.Tools.Systems;
using Content.Shared.Mind;
using Content.Shared.Roles;

namespace Content.Client._Euphoria.Tools;

public sealed partial class CorruptibleSystem : SharedCorruptibleSystem
{
    protected override void OnAttunedTo(Entity<CorruptibleComponent> ent, ref AttunedToEvent args)
    {
        return;
    }

}
