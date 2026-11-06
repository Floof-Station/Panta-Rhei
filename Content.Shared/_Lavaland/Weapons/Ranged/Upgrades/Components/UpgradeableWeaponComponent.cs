using Robust.Shared.GameStates;

namespace Content.Shared._Lavaland.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunUpgradeSystem))]
public sealed partial class UpgradeableWeaponComponent : Component
{
    /// <summary>
    /// If specified, upgrades that support capacity will block any new upgrades from being inserted
    /// </summary>
    [DataField]
    public int? MaxUpgradeCapacity;
}
