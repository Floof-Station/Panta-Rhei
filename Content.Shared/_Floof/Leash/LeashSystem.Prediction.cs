using Content.Shared._Floof.CCVar;
using Robust.Shared.Configuration;

namespace Content.Shared._Floof.Leash;

public sealed partial class LeashSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    public bool ClientLeashJointPrediction;

    private void InitializePrediction()
    {
        if (!_net.IsClient)
            return;

        _config.OnValueChanged(FloofCCVars.PredictLeashes, value => ClientLeashJointPrediction = value, true);
    }

    private bool ShouldPredictLeashes()
    {
        return !_net.IsClient || ClientLeashJointPrediction;
    }
}
