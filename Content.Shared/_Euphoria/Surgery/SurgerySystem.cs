using Content.Shared.GameTicking;
using Content.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Shared._Euphoria.Surgery;

public sealed class SurgerySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!TryComp<BodyComponent>(args.Mob, out var body))
            return;

        if (body.Organs == null)
            return;

        AddComp(args.Mob, new SurgeryComponent());

        if (!TryComp<SurgeryComponent>(args.Mob, out var surgery))
            return;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!TryComp<OrganComponent>(organ, out var comp))
                continue;

            if(comp.Category == null)
                continue;

            if (!surgery.DictOrgans.ContainsKey(comp.Category.Value))
                surgery.DictOrgans[comp.Category.Value] = new List<EntityUid>();

            surgery.DictOrgans[comp.Category.Value].Add(organ);
        }

    }
}
