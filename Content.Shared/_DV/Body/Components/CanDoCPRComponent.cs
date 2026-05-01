using Robust.Shared.GameStates;

namespace Content.Shared._DV.Body.Components;

/// <summary>
/// Anyone with this component can do CPR to people in critical state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CanDoCPRComponent : Component
{
    /// <summary>
    /// The length of the DoAfter.
    /// This decides when the CPR starts to work (After the first Do-After), as well as the frequency of popups.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TimeLength = 3f;

    /// <summary>
    /// The noise of CPR.
    /// </summary>
    [DataField]
    public SoundSpecifier CPRSound = new SoundPathSpecifier("/Audio/Effects/CPR.ogg");

    /// <summary>
    /// The damage CPR heals.
    /// </summary>
    [DataField] public DamageSpecifier CPRHealing = new()
    {
        DamageDict =
        {
            ["Asphyxiation"] = -6
        }
    };
    /// <summary>
    /// The revival chance of CPR.
    /// </summary>
    [DataField] public float ResuscitationChance = 0.1f;

    [DataField] public float RotReductionMultiplier;

    public EntityUid? CPRPlayingStream;
}
