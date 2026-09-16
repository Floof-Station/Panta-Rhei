using Content.Shared.Humanoid.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{

    private void SetCustomSpecieName(string customname)
    {
        Profile = Profile?.WithCustomSpeciesName(customname);
        IsDirty = true;
    }

    private void UpdateCustomSpecieNameEdit()
    {
        if (Profile == null)
            return;

        CCustomSpecieNameEdit.Text = Profile.Customspeciename ?? "";

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(Profile.Species, out var speciesProto))
            return;

        CCustomSpecieName.Visible = speciesProto.CustomName;
    }
}
