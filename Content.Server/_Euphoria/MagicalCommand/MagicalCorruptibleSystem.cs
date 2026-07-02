using Content.Server.Antag;
using Content.Shared._Euphoria.Item.AttunableItem;
using Content.Shared._Euphoria.MagicalCommand.Components;
using Content.Shared._Euphoria.MagicalCommand.Systems;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server._Euphoria.MagicalCommand;

public sealed partial class MagicalCorruptibleSystem : SharedMagicalCorruptibleSystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    protected override void OnAttunedTo(Entity<CorruptibleComponent> ent, ref AttunedToEvent args)
    {
        if (!_mind.TryGetMind(args.Attuner, out var mindId, out var mindComp))
            return;
        if (ent.Comp.Corrupted)
        {
            _role.MindAddRole(mindId, "MindRoleCorruptedMagicalGirl", mind: mindComp);
            _antag.SendBriefing(args.Attuner, Loc.GetString(RoleBriefing), Color.PaleVioletRed, RoleSound);
        }

    }
}
