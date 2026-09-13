using System.Linq;
using Content.Shared._Euphoria.Selector;
using Content.Shared._Floof.Leash.Components;
using Content.Shared._Floof.Ropes.Systems;
using Content.Shared.Popups;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    [Dependency] private readonly RopeSystem _ropes = default!;
    [Dependency] private readonly EntityConfigurationSystem _entCfg = default!;

    private void InitializeRopes()
    {
        SubscribeLocalEvent<LeashRopeComponent, ComponentShutdown>(OnRopeShutdown);
        SubscribeLocalEvent<LeashComponent, ComponentShutdown>(OnLeashShutdown);
    }

    private void OnRopeShutdown(Entity<LeashRopeComponent> ent, ref ComponentShutdown args)
    {
        // The rope this leash was using was deleted. Remove the leash.
        var ropeNet = GetNetEntity(ent);
        if (TerminatingOrDeleted(ent.Comp.Leash)
            || !TryComp<LeashComponent>(ent.Comp.Leash, out var leashComp)
            || leashComp.Leashed.Find(it => it.Rope == ropeNet) is not {} leashData)
            return;

        if (GetEntity(leashData.Pulled) is {} pulled)
            RemoveLeash(pulled, ent.Comp.Leash);
    }

    private void OnLeashShutdown(Entity<LeashComponent> ent, ref ComponentShutdown args)
    {
        foreach (var leashData in ent.Comp.Leashed.ToList())
        {
            // Clean up all attachments
            if (GetEntity(leashData.Pulled) is {} pulled)
                RemoveLeash(pulled, ent!);
        }
    }

    private IEnumerable<EntityUid> EnumerateRopes(Entity<LeashComponent> leash)
    {
        foreach (var data in leash.Comp.Leashed)
        {
            if (GetEntity(data.Rope) is not {} rope)
                continue;

            yield return rope;
        }
    }

    /// <summary>
    ///     Removes all ropes on the leash and re-creates them.
    ///     If <paramref name="force"/> is false, only creates missing joints.
    /// </summary>
    public void RefreshRopes(Entity<LeashComponent> leash, bool force)
    {
        if (_net.IsClient)
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

            var ropeLength = Math.Max(dst, leash.Comp.CurrentLength);
            if (!_ropes.TryCreateRope(leash,
                    pulled,
                    leash.Comp.RopeConfig,
                    ropeLength,
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

        Dirty(leash);
    }
}
