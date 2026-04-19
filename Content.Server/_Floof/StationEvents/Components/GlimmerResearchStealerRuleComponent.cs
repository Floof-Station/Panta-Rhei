using Content.Server._Floof.StationEvents.Events;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.StationEvents.Components;

[RegisterComponent, Access(typeof(GlimmerResearchStealerRule))]
public sealed partial class GlimmerResearchStealerRuleComponent : Component
{
    [DataField]
    public int MinToSteal = 1;
    [DataField]
    public int MaxToSteal = 8;
    [DataField]
    public SoundSpecifier GlimmerStealSound = new SoundPathSpecifier("/Audio/_DV/CosmicCult/ability_siphon.ogg");
    [DataField]
    public ProtoId<RadioChannelPrototype> AnnouncementChannel = "Science";
}
