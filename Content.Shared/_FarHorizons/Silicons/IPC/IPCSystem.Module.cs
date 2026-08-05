using Content.Shared._FarHorizons.Silicons.IPC.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared._FarHorizons.Silicons.IPC;

public abstract partial class SharedIPCSystem
{
    // Euph - i don't want to bother porting this and this will lead to people powergaming (borgs have access to much more powerful but specialized tools than regular people.)
    protected virtual void SetupModule()
    {
        // SubscribeLocalEvent<IPCModulesComponent, EntInsertedIntoContainerMessage>(InstallModule);
        // SubscribeLocalEvent<IPCModulesComponent, EntRemovedFromContainerMessage>(UninstallModule);
        // SubscribeLocalEvent<IPCModulesComponent, GotEmaggedEvent>(OnEmag);
    }

    private void InstallModule(Entity<IPCModulesComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        // if (!TryComp<BorgModuleComponent>(args.Entity, out var module) || args.Container.ID != ent.Comp.ModuleContainerId)
        //     return;
        //
        // if (module.Installed)
        //     return;
        //
        // module.InstalledEntity = ent.Owner;
        // Dirty(args.Entity, module);
        // var ev = new BorgModuleInstalledEvent(ent.Owner);
        // RaiseLocalEvent(args.Entity, ref ev);
    }

    private void UninstallModule(Entity<IPCModulesComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // if (!TryComp<BorgModuleComponent>(args.Entity, out var module))
        //     return;
        //
        // if (!module.Installed || args.Container.ID != ent.Comp.ModuleContainerId)
        //     return;
        //
        // module.InstalledEntity = null;
        // Dirty(args.Entity, module);
        // var ev = new BorgModuleUninstalledEvent(ent.Owner);
        // RaiseLocalEvent(args.Entity, ref ev);
    }

    private void OnEmag(Entity<IPCModulesComponent> ent, ref GotEmaggedEvent args)
    {
        // if(!_items.TryGetSlot(ent.Owner, ent.Comp.ModuleContainerId, out var slot) || slot.Whitelist == null || slot.Whitelist.Tags == null)
        //     return;
        // slot.Whitelist.Tags.Add("BorgModuleSyndicate");
        // args.Handled = true;
    }

    /// <summary>
    /// Selects a module, enabling the borg to use its provided abilities.
    /// </summary>
    public void SelectModule(Entity<IPCModulesComponent?> chassis,
        Entity<BorgModuleComponent?> module)
    {
        // if (LifeStage(chassis) >= EntityLifeStage.Terminating)
        //     return;
        //
        // if (!Resolve(chassis, ref chassis.Comp))
        //     return;
        //
        // if (!Resolve(module, ref module.Comp) || !module.Comp.Installed || module.Comp.InstalledEntity != chassis.Owner)
        // {
        //     Log.Error($"{ToPrettyString(chassis)} attempted to select uninstalled module {ToPrettyString(module)}");
        //     return;
        // }
        //
        // if (!HasComp<SelectableBorgModuleComponent>(module))
        // {
        //     Log.Error($"{ToPrettyString(chassis)} attempted to select invalid module {ToPrettyString(module)}");
        //     return;
        // }
        // if (chassis.Comp.SelectedModule == module.Owner)
        //     return;
        //
        // UnselectModule(chassis);
        //
        // var ev = new BorgModuleSelectedEvent(chassis);
        // RaiseLocalEvent(module, ref ev);
        // chassis.Comp.SelectedModule = module.Owner;
        // Dirty(chassis);
    }

    /// <summary>
    /// Unselects a module, removing its provided abilities.
    /// </summary>
    public void UnselectModule(Entity<IPCModulesComponent?> chassis)
    {
        // if (LifeStage(chassis) >= EntityLifeStage.Terminating)
        //     return;
        //
        // if (!Resolve(chassis, ref chassis.Comp))
        //     return;
        //
        // if (chassis.Comp.SelectedModule == null)
        //     return;
        //
        // var ev = new BorgModuleUnselectedEvent(chassis);
        // RaiseLocalEvent(chassis.Comp.SelectedModule.Value, ref ev);
        // chassis.Comp.SelectedModule = null;
        // Dirty(chassis);
    }
}
