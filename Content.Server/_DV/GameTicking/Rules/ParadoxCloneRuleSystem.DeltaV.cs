using Content.Server.Psionics;
using Content.Shared._Common.Consent;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Handles paradox anomaly related things when spawning paradox clones.
/// </summary>
public sealed partial class ParadoxCloneRuleSystem
{
    [Dependency] private readonly PsionicsSystem _psionics = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedConsentSystem _consent = default!; // Floofstation

    public static readonly ProtoId<ConsentTogglePrototype> ParadoxOptOutConsent = "NoClone"; // Floofstation

    private void FilterTargets(HashSet<Entity<MindComponent>> minds)
    {
        // TODO: use generic IMindFilter
        // no picking other antags or non-crew
        minds.RemoveWhere(mind => _role.MindIsAntagonist(mind) ||
            !_role.MindHasRole<JobRoleComponent>((mind, mind), out var role) ||
            role?.Comp1.JobPrototype == null || _consent.HasConsent(mind.Comp.CurrentEntity!.Value, ParadoxOptOutConsent)); // Floofstation - paradox opt out 
    }

    private void PostClone(EntityUid mob)
    {
        // guaranteed psionic power
        var psi = EnsureComp<PotentialPsionicComponent>(mob);
        _psionics.RollPsionics(mob, psi, false, 100);
    }
}
