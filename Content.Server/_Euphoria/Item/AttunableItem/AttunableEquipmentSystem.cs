using Content.Server.Antag;
using Content.Shared._Euphoria.Item.AttunableItem;
using Content.Shared._Euphoria.MagicalCommand;
using Content.Shared._Euphoria.MagicalCommand.Components;
using Content.Shared._Euphoria.MagicalCommand.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server._Euphoria.Item.AttunableItem;

/// <summary>
/// Makes it so that an item cannot be equipped until it is 'attuned' to, effectively meaning it can only have one user.
/// </summary>
public sealed partial class AttunableEquipmentSystem : SharedAttunableEquipmentSystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AttunedEntityComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<AttunedEntityComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindRemoved(Entity<AttunedEntityComponent> ent, ref MindRemovedMessage message)
    {
        _role.MindRemoveRole(message.Mind.Owner, "MindRoleCorruptedMagicalGirl");
    }

    private void OnMindAdded(Entity<AttunedEntityComponent> ent, ref MindAddedMessage message)
    {
        // I need to get the actual item attuned to, to test if it is corrupted or not. Annoying.
        if (!TryComp<CorruptibleComponent>(ent.Comp.AttunedTo, out var corruptible) || !corruptible.Corrupted)
            return;
        _role.MindAddRole(message.Mind.Owner, "MindRoleCorruptedMagicalGirl");
        _antag.SendBriefing(ent,
            Loc.GetString(SharedMagicalCorruptibleSystem.RoleBriefing),
            Color.PaleVioletRed,
            SharedMagicalCorruptibleSystem.RoleSound);
    }

    protected override void OnPostCorruption(Entity<AttunableEquipmentComponent> ent, ref PostCorruptingEvent args)
    {
        // It took me almost half a week to realize I should add this one line.
        base.OnPostCorruption(ent, ref args);
        var attuned = ent.Comp.AttunedEnt;
        if (attuned == null || !_mind.TryGetMind(attuned.Value, out var mindId, out var mindComp))
            return;

        if (args.Evil)
            _role.MindAddRole(mindId, "MindRoleCorruptedMagicalGirl");
        else
        // There shouldn't be any way for it to be possible to be attuned to a corrupted item without being corrupt yourself, so...
            _role.MindRemoveRole(mindId, "MindRoleCorruptedMagicalGirl");
    }
}
