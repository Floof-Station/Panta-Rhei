using Content.Shared._Euphoria.Item.AttunableItem;
using Content.Shared._Euphoria.MagicalCommand.Components;
using Content.Shared._Euphoria.MagicalCommand.Systems;

namespace Content.Client._Euphoria.MagicalCommand;

public sealed partial class MagicalCorruptibleSystem : SharedMagicalCorruptibleSystem
{
    protected override void OnAttunedTo(Entity<CorruptibleComponent> ent, ref AttunedToEvent args)
    {
        return;
    }

}
