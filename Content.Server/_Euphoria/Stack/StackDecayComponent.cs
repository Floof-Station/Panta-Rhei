namespace Content.Server._Euphoria.Stack;

/// <summary>
///     When applied to a stack (typically of radioactive materials), makes it decay over time.
/// </summary>
[RegisterComponent]
public sealed partial class StackDecayComponent : Component
{
    /// <summary>
    ///     Time it will take for the stack to decay by half.
    /// </summary>
    [DataField]
    public TimeSpan HalfLifeTime = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     How many items this stack contained during the last decay tick. If -1, decay has not yet been initialized.
    /// </summary>
    [DataField]
    public float LastTickCount = -1;

    /// <summary>
    ///     The time of the last decay tick. If TimeSpan.Zero, decay has not yet been initialized.
    /// </summary>
    [DataField]
    public TimeSpan LastTickTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero, UpdateInterval = TimeSpan.FromSeconds(2);
}
