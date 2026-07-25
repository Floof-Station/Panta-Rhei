using Content.Shared._Common.Consent;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CS.Traits.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class AphrodesiacBiteComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionAphrodesiacBite";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField("reagent")]
    public string Reagent = "AphrodesiacVenom";

    [DataField("amount")]
    public int Amount = 5;

    [DataField("sound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/bite.ogg");

    [DataField("consentRequired")]
    public bool RequiresConsent = true;

    [DataField("consentPrototype")]
    public ProtoId<ConsentTogglePrototype> ConsentToggleId = "Aphrodisiacs";
}

public sealed partial class AphrodesiacBiteEvent : EntityTargetActionEvent
{

}
