// SPDX-FileCopyrightText: Copyright (c) 2024-2025 Space Wizards Federation
// SPDX-License-Identifier: MIT

using Content.Shared._Common.Consent;
using Robust.Shared.Prototypes;

namespace Content.Client._Common.Consent;

public interface IClientConsentManager
{
    event Action OnServerDataLoaded;
    bool HasLoaded { get; }

    void Initialize();
    void UpdateConsent(PlayerConsentSettings consentSettings);
    PlayerConsentSettings GetConsentSettings();
    bool HasConsent(ProtoId<ConsentTogglePrototype> consentId); // Floofstation
}
