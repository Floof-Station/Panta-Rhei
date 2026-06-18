using Content.Shared._Euphoria.Item.AttunableItem;
using Content.Shared._Euphoria.Tools.Components;
using Content.Shared._Euphoria.Tools.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;

namespace Content.Server._Euphoria.Tools;

public sealed partial class CorruptibleSystem : SharedCorruptibleSystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    protected override void OnAttunedTo(Entity<CorruptibleComponent> ent, ref AttunedToEvent args)
    {
        if (!_mind.TryGetMind(args.Attuner, out var mindId, out var mindComp))
            return;
        if (ent.Comp.Corrupted)
            _role.MindAddRole(mindId, "MindRoleCorruptedMagicalGirl", mind: mindComp);
    }
}
