using System.Linq;
using System.Transactions;
using Content.Server.Administration;
using Content.Shared._Floof.Ropes.Components;
using Content.Shared._Floof.Ropes.Prototypes;
using Content.Shared._Floof.Ropes.Systems;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server._Floof.Ropes.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class RopeCommand : ToolshedCommand
{
    private RopeSystem? _rope;

    [CommandImplementation("connect")]
    public EntityUid Connect(EntityUid leftAnchor, EntityUid rightAnchor, ProtoId<RopeConfigurationPrototype> prototype, float length = 0)
    {
        _rope ??= EntityManager.System<RopeSystem>();
        if (!Transform(leftAnchor).Coordinates.TryDistance(EntityManager, Transform(rightAnchor).Coordinates, out var dst))
            throw new Exception("Entities are on separate maps.");

        if (length <= 0)
            length = dst * 1.1f;
        if (length < dst)
            throw new Exception("Refusing to create a rope shorter than the distance between its anchors.");

        if (!_rope.TryCreateRope(leftAnchor, rightAnchor, prototype, length, out var rope))
            throw new Exception("Couldn't create rope. See the server console.");

        return rope.Value.Owner;
    }

    [CommandImplementation("create")]
    public EntityUid Create(EntityUid leftAnchor, ProtoId<RopeConfigurationPrototype> prototype, float length = 0)
    {
        _rope ??= EntityManager.System<RopeSystem>();
        if (!_rope.TryCreateRope(leftAnchor, null, prototype, length, out var rope))
            throw new Exception("Couldn't create rope. See the server console.");

        return rope.Value.Owner;
    }

    [CommandImplementation("enumerate_links")]
    public IEnumerable<EntityUid> EnumerateLinks([PipedArgument] EntityUid rope)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        return comp.Links.Select(it => it.LinkEntity);
    }

    [CommandImplementation("set_length")]
    public EntityUid SetLength([PipedArgument] EntityUid rope, float length)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        _rope ??= EntityManager.System<RopeSystem>();
        _rope.SetRopeLength(rope, length);

        return rope;
    }

    [CommandImplementation("set_links")]
    public EntityUid SetLinks([PipedArgument] EntityUid rope, int links)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        _rope ??= EntityManager.System<RopeSystem>();
        _rope.SetRopeLinks(rope, links);

        if (!comp.IsDisabled)
            _rope.DistributeLinksBetweenAnchors(rope);

        return rope;
    }

    [CommandImplementation("connect_start")]
    public EntityUid ConnectStart([PipedArgument] EntityUid rope, EntityUid anchor)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        _rope ??= EntityManager.System<RopeSystem>();
        if (!_rope.TryConnectRopeStart(rope, anchor))
            throw new Exception("System call failed");

        return rope;
    }

    [CommandImplementation("connect_end")]
    public EntityUid ConnectEnd([PipedArgument] EntityUid rope, EntityUid anchor)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        _rope ??= EntityManager.System<RopeSystem>();
        if (!_rope.TryConnectRopeEnd(rope, anchor))
            throw new Exception("System call failed");

        return rope;
    }

    [CommandImplementation("detach_start")]
    public EntityUid DetachStart([PipedArgument] EntityUid rope)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        _rope ??= EntityManager.System<RopeSystem>();
        if (!_rope.TryDetachStart(rope))
            throw new Exception("System call failed");

        return rope;
    }

    [CommandImplementation("detach_end")]
    public EntityUid DetachEnd([PipedArgument] EntityUid rope)
    {
        if (!TryComp<RopeComponent>(rope, out var comp))
            throw new Exception("Not a rope");

        _rope ??= EntityManager.System<RopeSystem>();
        if (!_rope.TryDetachEnd(rope))
            throw new Exception("System call failed");

        return rope;
    }
}
