namespace Content.Server._Floof.HeightAdjust;

/// <summary>
///     Raised on an entity when its height changes.
/// </summary>
public sealed class HeightChangedEvent : EntityEventArgs
{
    public float OldScale, NewScale;

    /// <summary>
    ///     Ratio between the old scale and the new scale. This is typically the method you'd want to use in your calculations.
    /// </summary>
    public float Ratio;

    public HeightChangedEvent(float oldScale, float newScale, float ratio)
    {
        OldScale = oldScale;
        NewScale = newScale;
        Ratio = ratio;
    }
}
