using Content.Server.Floofstation.Traits.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Robust.Shared.Containers;


namespace Content.Server.Floofstation.Traits;

public sealed class VampirismSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly BodySystem _body = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<VampirismComponent, MapInitEvent>(OnInitVampire);
    }

    private void OnInitVampire(Entity<VampirismComponent> ent, ref MapInitEvent args)
    {
        // Make sure both the body component exists and its organ container exists.
        if (!TryComp<BodyComponent>(ent, out var bodyComp)
            || bodyComp.Organs is not { })
            return;

        // Get their stomach(s) into a nice little collection.......
        if (!_body.TryGetOrgansWithComponent<StomachComponent>((ent, bodyComp), out var stomachs))
            return;

        // THEN STEAL THEIR FUCKING STOMACHS EHEHEHEEE!!
        foreach (var stomachOrgan in stomachs)
        {
            if (stomachOrgan is { } organNotNullable)
            {
                _container.Remove(organNotNullable.Owner, bodyComp.Organs);
                _ent.DeleteEntity(stomachOrgan);
            }
        }

        // Give them a new one that digests blood :3

        var bloodStomach = _ent.Spawn("OrganBloodStomach");

        _container.Insert(bloodStomach, bodyComp.Organs);
    }
}
