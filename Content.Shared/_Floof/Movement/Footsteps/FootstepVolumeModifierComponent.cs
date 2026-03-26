using Robust.Shared.GameStates;

namespace Content.Shared._Floof.Movement.Footsteps;

/// <summary>
///     Changes the volume of the mover's footsteps.<br/>
///
///     This component is primarily used in traits, but cannot be made into a simple TraitEffect because SharedMoverController
///     HARDCODES the footstep volume modifiers. I don't want to change the upstream code and make those constants into properties,
///     so here we are.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FootstepVolumeModifierComponent : Component
{
    /// <summary>
    ///     Volume change, in decibels.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Volume;
}
