using Content.Shared.Examine;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Server._FarHorizons.Silicons.HumanoidEMP;

public sealed partial class HumanoidEMPSystem
{
    public void InitializeInspect()
    {
        SubscribeLocalEvent<EmpOnTriggerComponent, ExaminedEvent>(OnEmpGrenadeExamine);
    }

    private void OnEmpGrenadeExamine(Entity<EmpOnTriggerComponent> ent, ref ExaminedEvent args)
    {
        // Euph - no emp strength
        if (args.IsInDetailsRange)
            args.PushText(Loc.GetString("emp-grenade-strength-description", ("empStrength", 1/*ent.Comp.Strength*/)), 10);
    }
}
