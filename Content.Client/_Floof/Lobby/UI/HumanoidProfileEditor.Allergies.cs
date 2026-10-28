using System.Linq;
using Content.Client._CD.Humanoid;
using Content.Shared.Chemistry.Reagent;
using Content.Shared._CD.Body; //Allergy Update

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    // Port from CD
    private readonly AllergyPicker _allergiesTab;

    private void UpdateAllergies(Dictionary<ReagentPrototype, AllergyData> allergies)
    {
        Profile = Profile?.WithCDAllergies(allergies.Select(allergy => (allergy.Key.ID, allergy.Value))
            .ToDictionary());
        SetDirty();
    }

    private void UpdateCDAllergies()
    {
        if (Profile == null)
        {
            return;
        }

        var allergies = new Dictionary<ReagentPrototype, AllergyData>();
        foreach (var entry in (Dictionary<string, AllergyData>) Profile.CDAllergies)
        {
            if (!_prototypeManager.TryIndex(entry.Key, out ReagentPrototype? reagent))
                continue;
            allergies.Add(reagent, entry.Value);
        }
        _allergiesTab.SetData(allergies);
    }
}
