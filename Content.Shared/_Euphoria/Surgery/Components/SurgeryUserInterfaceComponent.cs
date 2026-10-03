namespace Content.Shared._Euphoria.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryUserInterfaceComponent : Component
{
    public TimeSpan NextUpdate = TimeSpan.Zero;

    public TimeSpan RefreshRate = TimeSpan.FromSeconds(5);
}
