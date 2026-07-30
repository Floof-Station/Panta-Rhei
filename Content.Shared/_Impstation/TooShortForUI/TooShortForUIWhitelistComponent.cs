using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.TooShortForUI;

/// <summary>
/// Tag component that allows all actors with TooShortForUIComponent to interact with this entity, even if it has <see cref="TooShortForUIBlacklistComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TooShortForUIWhitelistComponent : Component
{
    //weh!
};
