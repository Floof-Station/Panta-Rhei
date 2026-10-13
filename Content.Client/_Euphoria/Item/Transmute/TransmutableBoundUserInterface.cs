using Content.Client.UserInterface.Controls;
using Content.Shared._Euphoria.Item.Transmute;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using static Content.Shared._Euphoria.Item.Transmute.TransmutableSystem;

namespace Content.Client._Euphoria.Item.Transmute;

public sealed class TransmutableBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly EntityManager _entity = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private SimpleRadialMenu? _menu;

    public TransmutableBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<TransmutableComponent>(Owner, out var transmutable))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(MakeButtons(transmutable.AvailablePrototypes));
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> MakeButtons(HashSet<EntProtoId> prototypes)
    {
        Dictionary<string, List<RadialMenuActionOptionBase>> buttonsByCategory = new();
        ValueList<RadialMenuActionOptionBase> actionOptions = new(prototypes.Count);
        EntProtoId? ownerPrototype = EntMan.GetComponentOrNull<MetaDataComponent>(Owner)?.EntityPrototype?.ID;

        foreach (var protoId in prototypes)
        {
            if (protoId == ownerPrototype)
                continue;
            var prototype = _prototype.Index(protoId);
            var actionOption = new RadialMenuActionOption<EntProtoId>(HandleButtonPress, prototype)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(prototype),
                ToolTip = prototype.Name
            };
            actionOptions.Add(actionOption);
        }

        return actionOptions;
    }

    private void HandleButtonPress(EntProtoId prototype)
    {
        if (!PlayerManager.LocalEntity.HasValue)
            return;
        SendMessage(new TransmutableBoundUserInterfaceMessage(prototype, _entity.GetNetEntity(PlayerManager.LocalEntity.Value)));
    }
}
