using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._HL.Brainwashing;

[Serializable, NetSerializable]
public sealed class BrainwashEuiState(List<string> compulsions, NetEntity target) : EuiStateBase
{
    public List<string> Compulsions { get; } = compulsions;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class BrainwashSaveMessage : EuiMessageBase
{
    public List<string> Compulsions { get; }
    public NetEntity Target { get; }

    public BrainwashSaveMessage(List<string> compulsions, NetEntity target)
    {
        IoCManager.InjectDependencies(this);
        Compulsions = compulsions;
        Target = target;
    }
}
