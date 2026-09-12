using Content.Shared._Floof.Leash.Components;
using Content.Shared._Floof.Ropes.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    [Dependency] private readonly RopeSystem _ropes = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private void InitializeRopes()
    {
        SubscribeLocalEvent<LeashRopeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<LeashRopeComponent> ent, ref ComponentShutdown args)
    {
        // The rope this leash was using was deleted. Remove the leash.
        var ropeNet = GetNetEntity(ent);
        if (TerminatingOrDeleted(ent.Comp.Leash)
            || !TryComp<LeashComponent>(ent.Comp.Leash, out var leashComp)
            || leashComp.Leashed.Find(it => it.Rope == ropeNet) is not {} leashData)
            return;

        RemoveLeash(GetEntity(leashData.Pulled), ent.Comp.Leash);
    }

    /// <summary>
    ///     Removes all ropes on the leash and re-creates them.
    ///     If <paramref name="force"/> is false, only creates missing joints.
    /// </summary>
    public void RefreshRopes(Entity<LeashComponent> leash, bool force)
    {
        if (_net.IsClient)
            return;

        var config = leash.Comp.CurrentConfig;
        if (!_protoMan.Resolve(config.RopeConfig, out var ropeConfig))
            return;

        var destroyed = new List<LeashComponent.LeashData>();
        foreach (var data in leash.Comp.Leashed)
        {
            if (!TryGetEntity(data.Pulled, out var pulled)
                || !TryComp<LeashedComponent>(pulled, out var leashedComp)
                || !TryGetEntity(leashedComp.Anchor, out var anchor)
                || !TryComp<LeashAnchorComponent>(anchor, out var anchorComp))
                continue;

            if (force && TryGetEntity(data.Rope, out var rope))
                QueueDel(rope);

            if (!Transform(pulled.Value).Coordinates.TryDistance(EntityManager, _xform, Transform(leash).Coordinates, out var dst))
            {
                destroyed.Add(data);
                continue;
            }

            var length = Math.Max(dst, config.Length);
            if (!_ropes.TryCreateRope(leash,
                    pulled,
                    ropeConfig,
                    length,
                    out var newRope,
                    offsetRight: anchorComp.Offset))
            {
                destroyed.Add(data);
                continue;
            }

            data.Rope = GetNetEntity(newRope)!.Value;
            EnsureComp<LeashRopeComponent>(newRope.Value).Leash = leash;
        }

        // Clean up the ones that couldn't be re-created
        foreach (var leashData in destroyed)
        {
            var uid = GetEntity(leashData.Pulled);

            _popups.PopupEntity(Loc.GetString("rope-destroyed-popup", ("rope", uid)), uid, PopupType.Medium);
            RemoveLeash(uid, leash!);
        }
    }
}
