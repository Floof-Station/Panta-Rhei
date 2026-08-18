using Content.Shared._Common.Consent;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._HL.Brainwashing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BrainwasherComponent : Component
{
    [DataField(serverOnly: true)]
    public DoAfterId? DoAfter;

    [DataField]
    public bool BypassMindshield;

    [DataField]
    public SoundSpecifier ChargingSound = new SoundPathSpecifier("/Audio/Effects/PowerSink/charge_fire.ogg");

    [DataField]
    public SoundSpecifier EngageSound = new SoundPathSpecifier("/Audio/Weapons/flash.ogg");

    [DataField]
    public TimeSpan ChardingDuration = TimeSpan.FromSeconds(3);

    [DataField, ViewVariables, AutoNetworkedField]
    public List<string> Compulsions = [];

    [DataField, AutoNetworkedField]
    public string ConfigureText = "Configure Neuralyzer";
}

public abstract class SharedBrainwasherSystem : EntitySystem // HL: Move verbs to shared so the client can draw them without waiting for the server
{
    [Dependency] private readonly SharedConsentSystem _consentSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainwasherComponent, GetVerbsEvent<Verb>>(ConfigureVerb);
        SubscribeLocalEvent<BrainwasherComponent, GetVerbsEvent<InnateVerb>>(BrainwashingVerb);
    }

    private void ConfigureVerb(EntityUid uid, BrainwasherComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract
            || HasComp<MobStateComponent>(uid) && args.User != uid)
            return;

        args.Verbs.Add(new Verb
        {
            Act = () => DoConfigureVerb(uid, args.User, component),
            Text = component.ConfigureText,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
            Priority = 1
        });
    }

    private void BrainwashingVerb(EntityUid uid, BrainwasherComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.User != uid
            || HasComp<BorgChassisComponent>(args.Target))
            return;

        if (uid != args.Target && _consentSystem.HasConsent(args.Target, "MindControl"))
        {
            args.Verbs.Add(new InnateVerb
            {
                Act = () => DoBrainwashingVerb(args.User, args.Target, component),
                Text = "Brainwash",
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
                Priority = 1
            });
        }
    }

    protected virtual void DoConfigureVerb(EntityUid uid, EntityUid user, BrainwasherComponent component) { }
    protected virtual void DoBrainwashingVerb(EntityUid uid, EntityUid target, BrainwasherComponent component) { }
}

[Serializable, NetSerializable]
public sealed partial class EngagedEvent : SimpleDoAfterEvent
{
    public EngagedEvent(NetEntity wearer)
    {
        Wearer = wearer;
    }
    [DataField]
    public NetEntity Wearer;
}

[Serializable, NetSerializable]
public sealed class BrainwashedEvent : EntityEventArgs;
