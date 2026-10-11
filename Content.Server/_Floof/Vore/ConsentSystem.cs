using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Content.Shared._Common.Consent;
using Content.Shared._Floof.Vore;
namespace Content.Server._Floof.Vore;

public sealed class ConsentSystem : EntitySystem
{
    [Dependency] private readonly SharedConsentSystem _consentSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public static readonly ProtoId<ConsentTogglePrototype> isPred = "PredVore";
    public static readonly ProtoId<ConsentTogglePrototype> isPrey = "PreyVore";
    public static readonly ProtoId<ConsentTogglePrototype> isDigest = "Digestable";

    private readonly HashSet<EntityUid> _pendingConsentUpdates = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ConsentComponent, ComponentStartup>(OnConsentStartup);
        SubscribeLocalEvent<ConsentComponent, EntityConsentToggleUpdatedEvent>(OnConsentUpdated);
    }

    /// <summary>
    /// To get the most recent values for consent
    /// </summary>
    public override void Update(float frameTime){
        base.Update(frameTime);

        // processing of consent updates
        foreach (var uid in _pendingConsentUpdates){
            if (!HasComp<ConsentComponent>(uid))
                continue;
            ApplyVoreConsent(uid);
        }
        _pendingConsentUpdates.Clear();
    }

    /// <summary>
    /// gives the mob vore component when they updated their consent to be pred or prey
    /// in order to avoid giving every mob it one by one, timer needed to get the recent change
    /// </summary>
    private void OnConsentUpdated(EntityUid uid, ConsentComponent comp, EntityConsentToggleUpdatedEvent args){
        // only if the updated toggle is prey or pred
        if (args.ConsentToggleProtoId != isPred && 
        args.ConsentToggleProtoId != isPrey &&
        args.ConsentToggleProtoId != isDigest)
            return;
        _pendingConsentUpdates.Add(uid);
    }

    /// <summary>
    /// same principle as OnConsentUpdated but without the need for checking consent change
    /// </summary>
    private void OnConsentStartup(EntityUid uid, ConsentComponent comp, ComponentStartup args){
        _pendingConsentUpdates.Add(uid);
    }

    /// <summary>
    /// gives a mob the prey and/or pred component if they have selected either consent and removal for deselection
    /// also handles container if consent is off by removing prey from it and shutting container down  
    /// </summary>
    private void ApplyVoreConsent(EntityUid uid){
        var hasPred = _consentSystem.HasConsent(uid, isPred);
        var hasPrey = _consentSystem.HasConsent(uid, isPrey);

        if (hasPrey){
            EnsureComp<PreyComponent>(uid);
        }
        else if (HasComp<PreyComponent>(uid)){
            /* in case prey is inside a container immediately release them when they turn off prey consent
            works as an emergency leave for the prey*/    
            var safety = 0;
            while (_containerSystem.TryGetContainingContainer(uid, out var container))
            {
                if (++safety > 10)
                    break;
                if (!TryComp<PredComponent>(container.Owner, out var predComp))
                    break;
                if (container.ID != predComp.ContainerId)
                    break;
                if (!_containerSystem.Remove(uid, container))
                    break;
            }
            RemComp<PreyComponent>(uid);
        }

        if (hasPred){
            var pred = EnsureComp<PredComponent>(uid);
            _containerSystem.EnsureContainer<Container>(uid, pred.ContainerId);
        }
        else if (TryComp<PredComponent>(uid, out var comp)){
            // same for pred release all current prey after turning off consent
            if (_containerSystem.TryGetContainer(uid, comp.ContainerId, out var container)){
                _containerSystem.EmptyContainer(container);
                _containerSystem.ShutdownContainer(container);
            }
            RemComp<PredComponent>(uid);
        }
    }
}