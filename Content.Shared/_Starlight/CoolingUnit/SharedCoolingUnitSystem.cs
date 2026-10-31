using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;

namespace Content.Shared._Starlight.CoolingUnit;

public abstract partial class SharedCoolingUnitSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CoolingUnitComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<CoolingUnitComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<CoolingUnitComponent, ToggleActionEvent>(OnActionToggle);
    }

    private void OnGetActions(Entity<CoolingUnitComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
        Dirty(ent);
    }

    private void OnExamined(Entity<CoolingUnitComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(_itemToggle.IsActivated(ent.Owner)
            ? "coolingunit-on-examine"
            : "coolingunit-off-examine"));
    }

    private void OnActionToggle(Entity<CoolingUnitComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        _itemToggle.Toggle(ent.Owner, args.Performer);
        args.Handled = true;
    }
}
