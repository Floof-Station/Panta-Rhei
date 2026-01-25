using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Language.Components;

// <summary>
//     Signifies that this entity can speak and understand any language.
//     Applies to such entities as ghosts.
// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UniversalLanguageSpeakerComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
