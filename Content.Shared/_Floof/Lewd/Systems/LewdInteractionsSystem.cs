using System.Collections;
using Content.Shared._Floof.InteractionVerbs.Events;
using Content.Shared._Floof.Lewd.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Lewd.Systems;

/// <summary>
///     Handles interaction verbs fascilated by LewdMobData.
/// </summary>
public sealed class LewdInteractionsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly LewdOrganSystem _lewdOrgan = default!;

    // I wasn't sure where to put this, so I put it here.
    // Okay so
    // Yesterday i ended by modding the verb system to allow cstom cats to be defined in yaml
    // is pretty sick
    // But now i need to map interactor organ x interactee organ -> verb
    // so basically all pairs of organs correspond to verbs
    // I also need to make a prototype to describe these relations

    // Currently there's only one interaction verb map. Replace this if we ever need per-mob mapping.
    public static readonly ProtoId<LewdInteractionMapPrototype> InteractionMapProtoId = "Default";
    public LewdInteractionMapPrototype? InteractionMap { get; private set; }


    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnProtoReload);
        SubscribeLocalEvent<GetInteractionVerbsEvent>(OnGetInteractionVerbs);

        ReloadInteractionMap();
    }

    private void OnProtoReload(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<LewdInteractionMapPrototype>())
            ReloadInteractionMap();
    }

    private void OnGetInteractionVerbs(ref GetInteractionVerbsEvent ev)
    {
        if (InteractionMap is null)
            return;

        var organsFlagsA = GetOrgans(ev.User);
        var organFlagsB = GetOrgans(ev.Target);
        foreach (var pair in EnumeratePairs(organsFlagsA, organFlagsB))
        {
            // Log.Info($"Emitted pair: {pair}");
            if (!InteractionMap.Map.TryGetValue(pair, out var interactionVerbIds))
                continue;

            foreach (var interactionVerbId in interactionVerbIds)
            {
                if (!_protoMan.Resolve(interactionVerbId, out var interactionVerb))
                    continue;

                ev.Add(interactionVerb);
            }
        }
    }

    private LewdOrganKind GetOrgans(EntityUid ent) =>
        TryComp<LewdMobDataComponent>(ent, out var lewd) ? lewd.OrganKinds : LewdOrganKind.None;

    private IEnumerable<LewdOrganMapping> EnumeratePairs(LewdOrganKind flagsA, LewdOrganKind flagsB)
    {
        var count = (int) LewdOrganKind.TotalCount;
        for (int i = 0; i < count; i++)
        {
            var fi = (LewdOrganKind)(1 << i);
            if ((flagsA & fi) == 0)
                continue;

            for (int j = 0; j < count; j++)
            {
                var fj = (LewdOrganKind)(1 << j);
                if ((flagsB & fj) == 0)
                    continue;

                yield return new(fi, fj);
            }
        }

        // Special case: emit pairs (None, X) and (X, None) for milking and the like.
        for (int k = 0; k < count; k++)
        {
            var fk = (LewdOrganKind)(1 << k);
            if ((flagsA & fk) != 0)
                yield return new(fk, LewdOrganKind.None);

            if ((flagsB & fk) != 0)
                yield return new(LewdOrganKind.None, fk);
        }
    }

    public void ReloadInteractionMap()
    {
        InteractionMap = _protoMan.Index(InteractionMapProtoId);
    }
}
