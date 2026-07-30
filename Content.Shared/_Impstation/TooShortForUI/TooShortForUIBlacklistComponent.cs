using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.TooShortForUI;

/// <summary>
/// Tag component that prevents actors with TooShortForUIComponent from interacting with this entity. Can be overruled by <see cref="TooShortForUIWhitelistComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TooShortForUIBlacklistComponent : Component
{
    //weh!
};
