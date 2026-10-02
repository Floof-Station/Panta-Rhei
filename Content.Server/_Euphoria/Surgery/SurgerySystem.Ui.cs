using Content.Shared._Euphoria.Surgery;
using Content.Shared._Euphoria.Surgery.Components;

namespace Content.Server._Euphoria.Surgery;

public sealed partial class SurgerySystem
{

    private void InitializeUI()
    {

        SubscribeLocalEvent<SurgeryUserInterfaceComponent, BoundUIOpenedEvent>(OnUIOpened);

        //This is where a bleeder event should be subscribed to

    }


    private void OnUIOpened(Entity<SurgeryUserInterfaceComponent> ent, ref BoundUIOpenedEvent args)
    {

    }
}
