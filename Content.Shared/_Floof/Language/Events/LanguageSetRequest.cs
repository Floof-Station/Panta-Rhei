using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Language.Events;

/// <summary>
///     Sent from the client to the server when changing its current language via the language menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class LanguageSetRequest(ProtoId<LanguagePrototype> currentLanguage) : EntityEventArgs
{
    public ProtoId<LanguagePrototype> CurrentLanguage => currentLanguage;
}
