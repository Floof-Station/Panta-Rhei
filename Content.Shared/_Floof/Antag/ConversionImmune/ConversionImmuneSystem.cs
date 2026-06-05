using Content.Shared._DV.CosmicCult.Components;
using Content.Shared.Antag;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Antag.ConversionImmune;

public sealed class ConversionImmuneSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ConversionImmuneComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ConversionImmuneComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnExamined(Entity<ConversionImmuneComponent> ent, ref ExaminedEvent args)
    {
        if (!ShouldShowIcon(args.Examiner, ent))
            return;

        args.PushMarkup(Loc.GetString("antag-immune-examine", ("target", ent.Owner)), -10);
    }

    private void OnGetStatusIcons(Entity<ConversionImmuneComponent> ent, ref GetStatusIconsEvent args)
    {
        // This is called clientside-only
        if (_playerMan.LocalEntity is null || !_protoMan.Resolve(ent.Comp.ShownIcon, out var iconProto))
            return;

        if (ShouldShowIcon(_playerMan.LocalEntity.Value, ent))
            args.StatusIcons.Add(iconProto);
    }

    /// <summary>
    ///     Checks whether the viewer should see a hud icon and an examine desc on the target
    /// </summary>
    /// <param name="viewer"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool ShouldShowIcon(EntityUid viewer, Entity<ConversionImmuneComponent> target)
    {
        if (HasComp<ShowAntagIconsComponent>(viewer))
            return true;

        var flags = target.Comp.ProtectionType;
        var result = false;
        if ((flags & ConversionProtectionType.CosmicCult) != 0)
            result |= HasComp<CosmicCultComponent>(viewer);

        return result;
    }

    public bool IsImmune(Entity<ConversionImmuneComponent?> ent, ConversionProtectionType type)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        return ent.Comp.ProtectionType.HasFlag(type);
    }

    /// <summary>
    ///     Variant of IsImmune that shows a popup if the target is immune.
    /// </summary>
    public bool CheckImmuneWithPopup(Entity<ConversionImmuneComponent?> ent,
        ConversionProtectionType type,
        string popupLoc = "antag-immune-failure-generic")
    {
        if (!IsImmune(ent, type))
            return false;

        _popup.PopupEntity(Loc.GetString(popupLoc, ("target", ent)), ent, PopupType.MediumCaution);
        return true;
    }
}
