using Content.Client._Floof.Lobby.UI;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Player;
using Content.Shared.Roles;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

// Floofstation-specific extensions of the profile editor
public sealed partial class HumanoidProfileEditor
{
    private void OpenLoadoutFloof(JobPrototype? jobProto,
        RoleLoadout roleLoadout,
        RoleLoadoutPrototype roleLoadoutProto,
        ICommonSession session,
        IDependencyCollection collection)
    {
        // Loadout metadata editor
        _loadoutWindow!.OnRequestLoadoutMetadataEdit += (groupProto, loadoutProto) =>
        {
            if (!roleLoadout.SelectedLoadouts.TryGetValue(groupProto, out var group)
                || group.Find(it => it.Prototype == loadoutProto) is not { } loadout)
                return;

            var dlg = new LoadoutMetadataEditorDialog(loadout, loadoutProto, groupProto);
            dlg.OnSave += (args) =>
            {
                var (newLoadout, copyMetadataToAll, copyLoadoutToAll) = args;
                // The role loadouts could have changed, we cant trust the old value
                if (!roleLoadout.SelectedLoadouts.TryGetValue(groupProto, out var newGroup))
                    return;

                newGroup.RemoveAll(it => it.Prototype == loadoutProto);
                newGroup.Add(newLoadout);
                Profile = Profile?.WithLoadout(roleLoadout);

                // If "copy to all" is checked, we go through all other role loadouts and try to edit all matching loadouts
                if (copyMetadataToAll && Profile is not null)
                {
                    foreach (var (otherJob, otherRoleLoadout) in Profile.Loadouts)
                    {
                        if (!_prototypeManager.TryIndex(otherRoleLoadout.Role, out var roleLoadoutPrototype))
                            continue;

                        foreach (var otherGroupId in roleLoadoutPrototype.Groups)
                        {
                            var otherLoadouts = otherRoleLoadout.SelectedLoadouts.GetOrNew(otherGroupId);

                            // I assume no one is going to create more than 1 loadout proto per entity prototype, so no entProtoId checks here
                            // We only add the loadout if it was chosen before OR if "copy loadout to all" is checked
                            if (otherLoadouts.RemoveAll(it => it.Prototype == loadoutProto) > 0 || copyLoadoutToAll)
                                otherLoadouts.Add(newLoadout);
                        }

                        // When copying to other role loadouts, some of them may become invalid
                        otherRoleLoadout.EnsureValid(Profile, session, collection);
                        Profile = Profile.WithLoadout(otherRoleLoadout);
                    }
                }

                _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
                SetDirty();
                ReloadPreview();
            };
            dlg.OpenCentered();
        };
    }
}
