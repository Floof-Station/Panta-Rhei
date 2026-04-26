using Content.Shared._Coyote.SniffAndSmell;
using Content.Shared._Floof.Scent;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;

using ScentData = Content.Shared._Coyote.SniffAndSmell.Scent;

namespace Content.Client._Floof.Scent;

public sealed class ScentsUIController : UIController
{
    [Dependency] private readonly IDependencyCollection _depds = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IClientEntityManager _clientEntityManager = default!;

    private ScentsEditorDialog? _dialog;
    private EntityUid _editingEntity = EntityUid.Invalid;

    /// <summary>
    ///     Shows a scent editor dialog for the given entity or the local player (by default).
    ///     The dialog is re-created every time an open is requested.
    /// </summary>
    public ScentsEditorDialog ShowScentEditor(EntityUid targetEntity)
    {
        if (targetEntity == EntityUid.Invalid)
        {
            if (_playerMan.LocalEntity is not { } localEnt)
                throw new ArgumentException("Cannot open the scent editor right now.");
            targetEntity = localEnt;
        }

        if (_dialog == null)
        {
            _dialog = new(_depds);
            _dialog.OnSave += RequestSave;
            _dialog.OnClose += CloseScentEditor; // Let it be garbage collected
        }
        else
            _dialog.Close();

        // The mob may not have the scent component at first, but it's gonna be added later on.
        _editingEntity = targetEntity;
        var scentComp = EntityManager.EnsureComponent<ScentComponent>(targetEntity);
        _dialog.LoadFrom(scentComp);
        _dialog.OpenCentered();

        return _dialog;
    }

    public void CloseScentEditor()
    {
        // There's no reason to keep it loaded 24/7
        _dialog?.Orphan();
        _dialog = null;
    }

    private void RequestSave(List<ScentData> data)
    {
        if (EntityManager.GetNetEntity(_editingEntity) is not { Valid: true } editedNetEntity)
            return;

        _clientEntityManager.SendSystemNetworkMessage(new EditScentsRequestEvent(
            editedNetEntity,
            data));
    }
}
