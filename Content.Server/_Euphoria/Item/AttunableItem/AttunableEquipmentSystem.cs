using Content.Shared._Euphoria.Item.AttunableItem;
using Content.Shared._Euphoria.MagicalCommand;
using Content.Shared._Euphoria.Tools.Systems;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;

namespace Content.Server._Euphoria.Item.AttunableItem;

/// <summary>
/// Makes it so that an item cannot be equipped until it is 'attuned' to, effectively meaning it can only have one user.
/// </summary>
public sealed partial class AttunableEquipmentSystem : SharedAttunableEquipmentSystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AttunedEntityComponent, MindRemovedMessage>(OnMindRemoved);
    }

    private void OnMindRemoved(Entity<AttunedEntityComponent> ent, ref MindRemovedMessage message)
    {
        Log.Debug("honking");
        if (!_mind.TryGetMind(message.Mind, out var mindId, out var mindComp))
            return;
        _role.MindRemoveRole(message.Mind.Owner, "MindRoleCorruptedMagicalGirl");

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
