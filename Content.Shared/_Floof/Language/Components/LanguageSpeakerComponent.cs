using Content.Shared._Floof.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Language.Components;

/// <summary>
///     Stores the current state of the languages the entity can speak and understand.
/// </summary>
/// <remarks>
///     All fields of this component are populated during a DetermineEntityLanguagesEvent.
///     They are not to be modified externally.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageSpeakerComponent : Component
{
    public override bool SendOnlyToOwner => true;

    /// <summary>
    ///     The current language the entity uses when speaking.
    ///     Other listeners will hear the entity speak in this language.
    /// </summary>
    [DataField]
    public string CurrentLanguage = ""; // The language system will override it on mapinit

    /// <summary>
    ///     List of languages this entity can speak at the current moment.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand at the current moment.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();

    /// <summary>
    ///     The order of languages set by the player in the language menu. Can be empty.
    ///     This list CAN contain languages the entity neither speaks nor understands. This list can also be missing languages the entity does speak.
    ///     <see cref="SpokenLanguages"/> and <see cref="UnderstoodLanguages"/> are ordered according to this list. Unlisted languages appear last.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> PreferredOrder = new();

    [Serializable, NetSerializable]
    public sealed class State : ComponentState
    {
        public string CurrentLanguage = default!;
        public List<ProtoId<LanguagePrototype>> SpokenLanguages = default!;
        public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = default!;
    }
}
