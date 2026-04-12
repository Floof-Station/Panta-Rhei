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
            if (!InteractionMap.Map.TryGetValue(pair, out var interactionVerbId))
                continue;

            if (!_protoMan.Resolve(interactionVerbId, out var interactionVerb))
                continue;

            ev.Add(interactionVerb);
        }
    }

    private LewdOrganKind GetOrgans(EntityUid ent) =>
        TryComp<LewdMobDataComponent>(ent, out var lewd) ? lewd.OrganKinds : LewdOrganKind.None;

    private IEnumerable<LewdOrganMapping> EnumeratePairs(LewdOrganKind flagsA, LewdOrganKind flagsB)
    {
        var count = (int) LewdOrganKind.TotalCount;
        for (int i = 0; i < count; i++)
        {
            if ((flagsA & (LewdOrganKind)(1 << i)) == 0)
                continue;

            for (int j = 0; j < count; j++)
            {
                if ((flagsB & (LewdOrganKind)(1 << i)) == 0)
                    continue;

                yield return new((LewdOrganKind) i, (LewdOrganKind) j);
            }
        }

        // Special case: emit pairs (None, X) and (X, None) for milking and the like.
        for (int k = 0; k < count; k++)
        {
            if ((flagsA & (LewdOrganKind)(1 << k)) != 0)
                yield return new((LewdOrganKind) k, LewdOrganKind.None);

            if ((flagsB & (LewdOrganKind)(1 << k)) != 0)
                yield return new(LewdOrganKind.None, (LewdOrganKind) k);
        }
    }

    public void ReloadInteractionMap()
    {
        InteractionMap = _protoMan.Index(InteractionMapProtoId);
    }
}
