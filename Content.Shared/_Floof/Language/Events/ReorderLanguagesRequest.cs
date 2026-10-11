using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Language.Events;

/// <summary>
///     Sent from the client to the server when changing the order of languageOrder via the language menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ReorderLanguagesRequest(List<ProtoId<LanguagePrototype>> languageOrder) : EntityEventArgs
{
    public List<ProtoId<LanguagePrototype>> LanguageOrder => languageOrder;
}
