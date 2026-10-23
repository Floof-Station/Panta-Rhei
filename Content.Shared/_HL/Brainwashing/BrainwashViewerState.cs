using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._HL.Brainwashing;

[Serializable, NetSerializable]
public sealed class BrainwashViewerState(List<string> compulsions) : BoundUserInterfaceState
{
    public List<string> Compulsions { get; } = compulsions;
}

[NetSerializable, Serializable]
public enum CompulsionsUiKey : byte
{
    Key
}
