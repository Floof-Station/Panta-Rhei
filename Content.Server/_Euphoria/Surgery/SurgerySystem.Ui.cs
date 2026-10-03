using Content.Shared._Euphoria.Surgery;
using Content.Shared._Euphoria.Surgery.Components;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server._Euphoria.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    private void InitializeUI()
    {

        SubscribeLocalEvent<SurgeryUserInterfaceComponent, BoundUIOpenedEvent>(OnUIOpened);

        //This is where a bleeder event should be subscribed to

    }


    private void OnUIOpened(Entity<SurgeryUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {

    }

    public void UpdateUserInterfaceState(EntityUid uid, SurgeryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new SurgeryUpdateState(GetAvailableSurgeries(uid, component));
        _ui.SetUiState(uid, SurgeryUiKey.Key, state);
    }

    private void AddVerbs(GetVerbsEvent<InteractionVerb> args)
    {

        if (!TryComp<SurgeryToolComponent>(args.Using, out var usingSurgeryComp))
            return;

        if (!TryComp<SurgeryComponent>(args.Target, out var surgery))
            return;

        var disabled = false;
        string? message = null;

        EntityUid surgeryObject = default;
        if (usingSurgeryComp != null)
            surgeryObject = args.Using!.Value;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                if (!TryComp<ActivatableUIComponent>(args.Target, out var uiKey))
                    return;
                if (uiKey.Key == null)
                    return;

                //if (!disabled)
                //    TryStartSurgeryDoafter(surgeryObject, args.Target, args.User);
                UpdateUserInterfaceState(args.Target, surgery);
                _ui.TryOpenUi(args.Target, uiKey.Key,args.User);

            },
            Message = message,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("surgery-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}
