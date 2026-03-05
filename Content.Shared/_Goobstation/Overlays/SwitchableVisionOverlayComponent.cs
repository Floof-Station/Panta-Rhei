// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Spatison <137375981+Spatison@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Overlays;

public abstract partial class SwitchableVisionOverlayComponent : BaseVisionOverlayComponent
{
    [DataField]
    public virtual bool IsActive { get; set; }

    [DataField]
    public virtual bool DrawOverlay { get; set; } = true;

    /// <summary>
    /// Whether it should grant equipment enhanced vision or is it mob vision
    /// </summary>
    [DataField]
    public virtual bool IsEquipment { get; set; }

    /// <summary>
    /// If it is greater than 0, overlay isn't toggled but pulsed instead
    /// </summary>
    [DataField]
    public virtual float PulseTime { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public float PulseAccumulator;

    [DataField]
    public virtual float FlashDurationMultiplier { get; set; } = 1f; // ! goober

    [DataField]
    public SoundSpecifier? ActivateSound = new SoundPathSpecifier("/Audio/_White/Items/Goggles/activate.ogg");

    [DataField]
    public SoundSpecifier? DeactivateSound = new SoundPathSpecifier("/Audio/_White/Items/Goggles/deactivate.ogg");

    [DataField]
    public virtual EntProtoId? ToggleAction { get; set; }

    [ViewVariables]
    public EntityUid? ToggleActionEntity;
}

[Serializable, NetSerializable]
public sealed class SwitchableVisionOverlayComponentState : IComponentState
{
    public Color Color;
    public bool IsActive;
    public float FlashDurationMultiplier;
    public SoundSpecifier? ActivateSound;
    public SoundSpecifier? DeactivateSound;
    public EntProtoId? ToggleAction;
    public float LightRadius;
}
