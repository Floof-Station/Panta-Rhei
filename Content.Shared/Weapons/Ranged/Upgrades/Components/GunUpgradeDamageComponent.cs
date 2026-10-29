using Content.Shared._Lavaland.Weapons.Ranged.Upgrades;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// A <see cref="GunUpgradeComponent"/> for increasing the damage of a gun's projectile.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunUpgradeSystem))] // Goobstation - Swapped from GunUpgradeSystem to SharedGunUpgradeSystem
public sealed partial class GunUpgradeDamageComponent : Component
{
    [DataField]
    public DamageSpecifier? BonusDamage;

    /// <summary>
    /// Goobstation
    /// How much should we multiply the total projectile's damage.
    /// </summary>
    [DataField]
    public float Modifier = 1f;
}
