using Content.Shared._Floof.InteractionVerbs;
using Content.Shared._Floof.Scent;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using ScentData = Content.Shared._Coyote.SniffAndSmell.Scent;

namespace Content.Client._Floof.Scent;

public sealed class ScentEditingSystem : SharedScentEditingSystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (_playerManager.LocalSession is not {} localSession || !CanEditScent(localSession, args.Target))
            return;

        // No tryX method for controllers
        ScentsUIController scentsUi;
        try { scentsUi = _ui.GetUIController<ScentsUIController>(); }
        catch (Exception e) { return; }

        var verb = new Verb
        {
            Text = Loc.GetString("scent-editor-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/pencil.png")), // I love putting this everywhere
            Priority = -21, // Below "ignore scent"
            Category = SharedInteractionVerbsSystem.InteractionCategory, // This also doesn't belong here, but I'm keeping it here for consistency. TODO: own category?
            Act = () => { scentsUi.ShowScentEditor(args.Target); },
            ClientExclusive = true, // Prediction bad
        };
        args.Verbs.Add(verb);
    }
}
