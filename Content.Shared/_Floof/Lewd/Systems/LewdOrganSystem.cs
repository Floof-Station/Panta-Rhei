using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Floof.Lewd.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Forensics.Components;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Lewd.Systems;

/// <summary>
///     Handles lewd organs and their caching on the mob.
///     Solution processing is done on the server side, see LewdMobSystem.
/// </summary>
public sealed class LewdOrganSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solContainer = default!;
    [Dependency] private readonly SharedPuddleSystem _puddles = default!;
    [Dependency] private readonly ExamineSystemShared _examines = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LewdOrganComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LewdOrganComponent, OrganAddedToBodyEvent>(OnLewdAdded);
        SubscribeLocalEvent<LewdOrganComponent, OrganRemovedFromBodyEvent>(OnLewdRemoved);
        SubscribeLocalEvent<LewdMobDataComponent, GetVerbsEvent<ExamineVerb>>(OnLewdExamine);
    }

    private void OnMapInit(Entity<LewdOrganComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<OrganComponent>(ent, out var organ))
        {
            Log.Warning($"LewdOrganComponent added to an entity without Organ: {ent.Owner}.");
            return;
        }

        organ.SlotId = ent.Comp.Data.OrganKind.ToString().ToLowerInvariant();
    }

    private void OnLewdAdded(Entity<LewdOrganComponent> ent, ref OrganAddedToBodyEvent args)
    {
        if (_net.IsClient) // Client-side BodySystem spams mechanism attachments/removals whenever entities move in and out of PVS
            return;

        // TODO: we're not checking if it's in a valid slot? I'm not sure if it's an issue, but if it is, idk how to check
        AttachOrgan(ent, args.Body);
    }

    private void OnLewdRemoved(Entity<LewdOrganComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        if (_net.IsClient) // Client-side BodySystem spams mechanism attachments/removals whenever entities move in and out of PVS
            return;

        DetachOrgan(ent, args.OldBody);
    }

    private void OnLewdExamine(Entity<LewdMobDataComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        // When trying to examine yourself, add a verb showing all the lewd organs you have.
        var user = args.User;
        var target = args.Target;
        if (user != target)
            return;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("lewd-examine-organs-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/_Floof/Interface/VerbIcons/cancer.png")),
            Act = () =>
            {
                // A simple list of all organs and their fill fractions
                var organDescriptions = GetLewdOrgans(target)
                    .Select(it =>
                    {
                        var name = Name(it.Owner);
                        var fillFraction = TryGetOrganSolution((it.Owner, it.Comp2), ent.Owner, out var sol, out _)
                            ? sol.FillFraction
                            : 0;
                        var fillFractionStr = fillFraction.ToString("P1");

                        return Loc.GetString("lewd-examine-organs-status", ("name", name), ("fillFraction", fillFractionStr));
                    });

                var message = FormattedMessage.FromMarkupPermissive(Loc.GetString("lewd-examine-organs-self-header"));
                foreach (var organDescription in organDescriptions)
                    message.AddMarkupPermissive('\n' + organDescription);

                _examines.SendExamineTooltip(user, ent, message, getVerbs: false, centerAtCursor: false);
            },
            Category = VerbCategory.Examine,
            CloseMenu = true,
        });
    }

    /// <summary>
    ///     Returns true if the organ is marked with ANY flag specified in kind.
    /// </summary>
    public bool IsOfType(Entity<LewdOrganComponent?> ent, LewdOrganKind kind)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return (ent.Comp.Data.OrganKind & kind) != 0;
    }

    /// <summary>
    ///     Updates certain cached data inside the LewdOrganData class. Call this whenever it changes.
    /// </summary>
    public void UpdateData(LewdOrganData data)
    {
        data.ProducedReagentPrototypes = data.ProducedReagents?.Select(it => (ProtoId<ReagentPrototype>) it.Reagent.Prototype).ToArray();
    }

    /// <summary>
    ///     Updates the organ on a mob.
    /// </summary>
    public void UpdateOrgan(Entity<LewdOrganComponent> organ, EntityUid mob)
    {
        DebugTools.Assert(CompOrNull<OrganComponent>(organ)?.Body == mob);
        DetachOrgan(organ, mob);
        AttachOrgan(organ, mob);
    }

    /// <summary>
    ///     Creates a relevant slot for the lewd organ and attaches it to that slot.
    /// </summary>
    public bool TryAddOrganToBody(Entity<LewdOrganComponent> organ, EntityUid mob)
    {
        if (_body.GetRootPartOrNull(mob) is not { } rootPart)
            return false;

        var slotName = organ.Comp.Data.OrganKind.ToString().ToLowerInvariant();
        _body.TryCreateOrganSlot(rootPart.Entity, slotName, out var slot, rootPart.BodyPart);

        // The above method is shitcode, doesn't even specify [NotNullWhen, so we're ignoring the slot out var here.
        return _body.InsertOrgan(rootPart.Entity, organ, slotName, rootPart.BodyPart);
    }

    public IEnumerable<Entity<OrganComponent, LewdOrganComponent>> GetLewdOrgans(EntityUid mob)
    {
        if (!TryComp<BodyComponent>(mob, out var body))
            return [];

        var lewdQuery = GetEntityQuery<LewdOrganComponent>();
        return _body.GetBodyOrgans(mob, body)
            .Where(it => lewdQuery.HasComp(it.Id))
            .Select(it => new Entity<OrganComponent, LewdOrganComponent>(it.Id, it.Component, lewdQuery.Comp(it.Id)));
    }

    public bool TryGetOrganSolution(
        Entity<LewdOrganComponent> organ,
        Entity<SolutionContainerManagerComponent?> body,
        [NotNullWhen(true)] out Solution? solution,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEnt)
    {
        return _solContainer.TryGetSolution(body, organ.Comp.Data.SolutionName, out solutionEnt, out solution);
    }

    public bool TryGetOrganSolution(
        LewdOrganKind organ,
        Entity<SolutionContainerManagerComponent?> body,
        [NotNullWhen(true)] out Solution? solution,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solutionEnt)
    {
        var lewdQuery = GetEntityQuery<LewdOrganComponent>();
        foreach (var (organId, organComp) in _body.GetBodyOrgans(body))
        {
            if (!lewdQuery.TryComp(organId, out var lewd) || lewd.Data.OrganKind != organ)
                continue;

            if (TryGetOrganSolution((organId, lewd), body, out solution, out solutionEnt))
                return true;
        }

        solution = default;
        solutionEnt = default;
        return false;
    }

    private void AttachOrgan(Entity<LewdOrganComponent> organ, EntityUid body)
    {
        var bodyData = EnsureComp<LewdMobDataComponent>(body);
        var organData = organ.Comp.Data;

        // If this organ produces anything, change its produced reagents to contain the body's DNA
        // This is primarily so that if e.g. somehow chemicals from person A get into person B's organs, they will get drained
        if (organData.ProducedReagents is { Length: > 0 } && TryComp<DnaComponent>(body, out var donorComp) && donorComp.DNA != null)
        {
            var dna = new List<ReagentData> { new DnaData { DNA = donorComp.DNA } };
            for (var i = 0; i < organData.ProducedReagents.Length; i++)
            {
                var reagent = organData.ProducedReagents[i];
                organData.ProducedReagents[i] = new(reagent.Reagent.Prototype, reagent.Quantity, dna);
            }
        }

        UpdateData(organData);
        bodyData.OrganKinds |= organData.OrganKind;
        bodyData.CachedData[organData.OrganKind] = organData;

        // Add the solution to the mob
        _solContainer.EnsureSolution(body, organData.SolutionName, out var solution, organData.SolutionVolume);
    }

    private void DetachOrgan(Entity<LewdOrganComponent> ent, EntityUid body)
    {
        // There should NEVER be more than one organ corresponding to a single lewd type.
        DebugTools.Assert(_body.GetBodyOrgans(body)
                .Any(it => it.Id != ent.Owner && IsOfType(it.Id, ent.Comp.Data.OrganKind)),
            "Body contains multiple lewd organs of the same type? This will cause issues.");

        var bodyData = EnsureComp<LewdMobDataComponent>(body);
        var organData = ent.Comp.Data;
        bodyData.OrganKinds &= ~organData.OrganKind;
        bodyData.CachedData.Remove(organData.OrganKind);

        // Spill the solution onto the ground, and then remove it. Pro tip: don't add organs that direct into the bloodstream
        if (_solContainer.TryGetSolution(body, organData.SolutionName, out var solution))
        {
            _puddles.TrySpillAt(body, solution.Value.Comp.Solution, out _);
            QueueDel(solution);
        }
    }
}
