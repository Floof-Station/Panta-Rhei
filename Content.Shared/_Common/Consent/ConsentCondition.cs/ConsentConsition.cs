// SPDX-FileCopyrightText: Copyright (c) 2025 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Common.Consent.EffectConditions;

// This is dumb. Why, wizden?
public sealed partial class ConsentEntityConditionSystem : EntityConditionSystem<ConsentComponent, ConsentCondition>
{
    [Dependency] private readonly SharedConsentSystem _consent = default!;

    protected override void Condition(Entity<ConsentComponent> entity, ref EntityConditionEvent<ConsentCondition> args) =>
        args.Result = _consent.HasConsent(entity.Owner, args.Condition.Consent);
}

/// <summary>
/// Checks if the target entity has consented to a specific toggle.
/// </summary>
public sealed partial class ConsentCondition : EntityConditionBase<ConsentCondition>
{
    [DataField(required: true)]
    public ProtoId<ConsentTogglePrototype> Consent;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-consent-condition", ("consent", Consent));
    }
}
