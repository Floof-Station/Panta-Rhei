using System.Linq;
using Content.Shared._EE.FootPrint;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Fluids;
using Content.Shared.Whitelist;

namespace Content.Shared._Floof.Fluids.Absorbent;

public sealed class AreaAbsorbentSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    [Dependency] private readonly SharedAbsorbentSystem _absorbent = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private int _recursionCounter = 0;

    public override void Initialize()
    {
        SubscribeLocalEvent<AreaAbsorbentComponent, PuddleMoppedEvent>(OnMopped);
    }

    private void OnMopped(Entity<AreaAbsorbentComponent> ent, ref PuddleMoppedEvent args)
    {
        // This shouldn't ever happen, but just in case.
        if (_recursionCounter != 0)
            return;

        try
        {
            _recursionCounter++;
            TryCleanNearbyFootprints(args.Used, args.User, args.Target);
        }
        finally
        {
            _recursionCounter = 0;
        }
    }

    /// <summary>
    ///     Tries to clean a number of footprints in a range around the target entity determined by the component. Returns the number of cleaned footprints.
    /// </summary>
    public int TryCleanNearbyFootprints(Entity<AbsorbentComponent?, AreaAbsorbentComponent?> used, EntityUid user, EntityUid target)
    {
        // Don't log an error if AOE is missing, but log it if normal absorbent is missing
        if (!Resolve(used, ref used.Comp1) || !Resolve(used, ref used.Comp2, logMissing: false))
            return 0;

        if (!_solutionContainer.TryGetSolution(used.Owner, used.Comp1.SolutionName, out var absorberSolution, true))
            return 0;

        var targetCoords = Transform(target).Coordinates;
        var entities = _lookup.GetEntitiesInRange(targetCoords, used.Comp2.CleaningRange, LookupFlags.Uncontained);

        // Take up to [MaxCleanedFootprints] footprints closest to the target
        var cleaned = entities.AsEnumerable()
            .Where(uid => _whitelist.IsWhitelistPass(used.Comp2.CleaningWhitelist, uid))
            .Select(uid => (uid, dst: Transform(uid).Coordinates.TryDistance(EntityManager, _xform, targetCoords, out var dst) ? dst : 0f))
            .Where(ent => ent.dst > 0f && ent.dst <= used.Comp2.CleaningRange)
            .OrderBy(ent => ent.dst)
            .Select(ent => ent.uid);

        // And try to interact with each one of them, ignoring useDelay
        var processed = 0;
        foreach (var targetFootprint in cleaned)
        {
            if (_absorbent.TryPuddleInteract((used.Owner, used.Comp1, null), absorberSolution.Value, user, targetFootprint))
                processed++;

            if (processed >= used.Comp2.MaxCleanedEntities)
                break;
        }

        return processed;
    }
}
