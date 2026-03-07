using Content.Shared._Floof.Util;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Standing;

/// <summary>
/// Allows the entity to toggle between crawling under and over furniture with a keybind.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrawlingExtensionsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanCrawlUnderTables = true, CanChangeDirections = true;

    #region Under-table crawling

    [DataField, AutoNetworkedField]
    public float CrawlingUnderSpeedModifier = 0.5f;

    /// <summary>
    ///     If true, the entity is choosing to crawl under furniture. This is purely visual and has no effect on physics.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsCrawlingUnder = false;

    [DataField, AutoNetworkedField]
    public int NormalDrawDepth = (int) DrawDepth.DrawDepth.Mobs,
        CrawlingUnderDrawDepth = (int) DrawDepth.DrawDepth.SmallMobs;

    #endregion

    #region Togglable crawling direction

    [DataField, AutoNetworkedField]
    public bool InvertedCrawlingDirection = false;

    /// <summary>
    ///     An interval between direction changes to prevent people from spamming rotation animations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Ticker CrawlingDirectionChangeCooldown = new(TimeSpan.FromSeconds(1f));

    #endregion
}
